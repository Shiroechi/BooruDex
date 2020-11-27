using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using BooruDex2.Exceptions;
using BooruDex2.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

using Newtonsoft.Json.Linq;

namespace BooruDex2.Booru.Template
{
	/// <summary>
	/// Gelbooru.
	/// </summary>
	public abstract class Gelbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Gelbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Gelbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Gelbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
		{
			this._PostLimit = 100;
			this._TagsLimit = 0; // no tag limit
			this._PageLimit = 10;
			this.IsSafe = false;
			this._ApiVersion = "";
			this._PasswordSalt = "";
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Gelbooru()
		{

		}

		#endregion Constructor & Destructor

		#region Protected Method

		/// <summary>
		/// Get max number of <see cref="Post"/> with 
		/// the given <see cref="Tag"/> the site have.
		/// </summary>
		/// <param name="url">Url of the requested <see cref="Post"/>.</param>
		/// <returns>Number of <see cref="Post"/>.</returns>
		protected async Task<uint> GetPostCount(string url)
		{
			var xml = new XmlDocument();
			xml.LoadXml(await this.GetStringResponseAsync(url));
			return uint.Parse(xml.ChildNodes.Item(1).Attributes[0].InnerXml);
		}

		#endregion Protected Method

		#region Protected Overrride Method

		/// <inheritdoc/>
		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			var sb = new StringBuilder($"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index");
			
			if (json)
			{
				sb.Append("&json=1");
			}

			return sb.ToString();
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JToken json)
		{
			return new Post(
				json["id"].Value<uint>(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				json["file_url"].Value<string>(),
				this._BaseUrl + "thumbnails/" + json["directory"].Value<string>() + "/thumbnail_" + json["hash"].Value<string>() + ".jpg",
				this.ConvertRating(json["rating"].Value<string>()),
				json["tags"].Value<string>(),
				0,
				json["height"].Value<int>(),
				json["width"].Value<int>(),
				0,
				0,
				json["source"].Value<string>()
				);
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JToken json)
		{
			return new Tag(
				json["id"].Value<uint>(),
				json["tag"].Value<string>(),
				this.ToTagType(json["type"].Value<string>()),
				json["count"].Value<uint>()
				);
		}

		/// <summary>
		/// Convert string "type" from tag JSON to <see cref="TagType"/>.
		/// </summary>
		/// <param name="tagTypeName">Tag type name.</param>
		/// <returns>
		/// <see cref="TagType"/> based on the name or 
		/// <see cref="TagType.Undefined"/> if "type" from tag JSON is not recognizable.
		/// </returns>
		protected virtual TagType ToTagType(string tagTypeName)
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;

			if (comparer.Equals(tagTypeName, "tag"))
			{
				return TagType.General;
			}

			foreach (TagType type in Enum.GetValues(typeof(TagType)))
			{
				if (comparer.Equals(tagTypeName, type.ToString()))
				{
					return type;
				}
			}

			return TagType.Undefined;
		}

		#endregion Protected Overrride Method

		#region Public Method

		#region Post

		/// <inheritdoc/>
		public override async Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			if ((this._TagsLimit != 0) &&
				(tags != null) &&
				(tags.Length > this._TagsLimit))
			{
				throw new ArgumentOutOfRangeException($"Tag can't more than { this._TagsLimit } tag.");
			}

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._PostLimit)
			{
				limit = this._PostLimit;
			}

			string url = "";

			if (tags == null)
			{
				url = this.CreateBaseApiCall("post") +
					$"&limit={ limit }&pid={ page }";
			}
			else
			{
				url = this.CreateBaseApiCall("post") +
					$"&limit={ limit }&pid={ page }&tags={ string.Join(" ", tags) }";
			}

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				if (tags == null || tags.Length <= 0)
				{
					throw new SearchNotFoundException($"No Post found with empty tags at page { page }.");
				}
				else
				{
					throw new SearchNotFoundException($"No Post found with tags { string.Join(", ", tags) } at page { page }.");
				}
			}

			try
			{
				return jsonArray.Select(this.ReadPost).ToArray();
			}
			catch (Exception e)
			{
				throw new SearchNotFoundException("Something happen when deserialize Post data.", e);
			}
		}

		/// <inheritdoc/>
		public override async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			if ((this._TagsLimit != 0) &&
				(tags != null) &&
				(tags.Length > this._TagsLimit))
			{
				throw new ArgumentOutOfRangeException($"Tag can't more than { this._TagsLimit } tag.");
			}

			string url;

			if (tags == null)
			{
				url = this.CreateBaseApiCall("post", false) +
					$"&limit={ 1 }&pid={ 0 }";
			}
			else
			{
				url = this.CreateBaseApiCall("post", false) +
					$"&limit={ 1 }&pid={ 0 }&tags={ string.Join(" ", tags) }";
			}

			// get Post count in XML response.

			var postCount = await this.GetPostCount(url);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}

			// get post with random the page number, each page 
			// limited only with 1 post.

			var pageNumber = this._RNG.NextInt(1, postCount);

			var post = await this.PostListAsync(1, tags, pageNumber);

			return post[0];
		}

		/// <inheritdoc/>
		public async override Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			if ((this._TagsLimit != 0) &&
				(tags != null) &&
				(tags.Length > this._TagsLimit))
			{
				throw new ArgumentOutOfRangeException($"Tag can't more than { this._TagsLimit } tag.");
			}

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._PostLimit)
			{
				limit = this._PostLimit;
			}

			string url;

			if (tags == null)
			{
				url = this.CreateBaseApiCall("post", false) +
					$"&limit={ 1 }&pid={ 0 }";
			}
			else
			{
				url = this.CreateBaseApiCall("post", false) +
					$"&limit={ 1 }&pid={ 0 }&tags={ string.Join(" ", tags) }";
			}

			// get Post count in XML response.

			var postCount = await this.GetPostCount(url);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}
			else if (postCount < limit)
			{
				throw new SearchNotFoundException($"The site only have { postCount } post with tags { string.Join(", ", tags) }.");
			}

			var maxPageNumber = (uint)Math.Floor(postCount / limit * 1.0);

			if (maxPageNumber == 1)
			{
				// get all post
				return await this.PostListAsync(limit, tags);
			}
			else
			{
				// maxPageNumber - 1, to ensure the leftovers post
				// in last page not included.

				maxPageNumber -= 1;

				return await this.PostListAsync(limit, tags, this._RNG.NextInt(1, maxPageNumber));
			}
		}

		#endregion Post

		#region Tag

		/// <inheritdoc/>
		public override async Task<Tag[]> TagListAsync(string name)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tag") +
				$"&limit={ this._PostLimit }&orderby=name&name_pattern={ name }";

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Tags with name \"{ name }\".");
			}

			return jsonArray.Select(ReadTag).ToArray();
		}

		#endregion Tag

		#endregion Public Method
	}
}
