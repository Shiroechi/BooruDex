using System;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BooruDex.Booru.Template
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
		public Gelbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new JSF32())
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

		#region Protected Overrride Method

		protected override string CreateBaseApiCall(string query)
		{
			return $"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index&json=1";
		}

		#endregion Protected Overrride Method

		#region Protected Virtual Method

		/// <summary>
		/// Read <see cref="Post"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Post ReadPost(object json)
		{
			var item = (JObject)json;
			return new Post(
				item["id"].Value<uint>(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				item["file_url"].Value<string>(),
				this._BaseUrl + "thumbnails/" + item["directory"].Value<string>() + "/thumbnail_" + item["hash"].Value<string>() + ".jpg",
				this.ConvertRating(item["rating"].Value<string>()),
				item["tags"].Value<string>(),
				0,
				item["height"].Value<int>(),
				item["width"].Value<int>(),
				0,
				0,
				item["source"].Value<string>()
				);
		}

		/// <summary>
		/// Read <see cref="Tag"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Tag ReadTag(object json)
		{
			var item = (JObject)json;
			return new Tag(
				item["id"].Value<uint>(),
				item["tag"].Value<string>(),
				this.ToTagType(item["type"].Value<string>()),
				item["count"].Value<uint>()
				);
		}

		/// <summary>
		/// Convert sting tag type to <see cref="TagType"/>.
		/// </summary>
		/// <param name="tagTypeName">Tag type name.</param>
		/// <returns>
		/// <see cref="TagType"/> based on the name or 
		/// <see cref="TagType.Undefined"/> if tag type is not recognizable.
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

		#endregion Protected Virtual Method

		#region Public Method

		#region Post

		/// <summary>
		/// Get a list of <see cref="Post"/>.
		/// </summary>
		/// <param name="limit">How many <see cref="Post"/> to retrieve.</param>
		/// <param name="page">The page number.</param>
		/// <param name="tags">The tags to search for.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		/// <exception cref="SearchNotFoundException"></exception>
		public override async Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			if ((this._TagsLimit != 0) &&
				(tags != null) &&
				(tags.Length > this._TagsLimit))
			{
				throw new ArgumentException($"Tag can't more than { this._TagsLimit } tag.");
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
				string tagString = string.Join(" ", tags);
				url = this.CreateBaseApiCall("post") +
					$"&limit={ limit }&pid={ page }&tags={ tagString }";
			}

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await this.GetJsonAsync(url));
			Console.WriteLine(url);
			try
			{
				return jsonArray.Select(this.ReadPost).ToArray();
			}
			catch
			{
				throw new SearchNotFoundException("No post found.");
			}
		}

		/// <summary>
		/// Search a single random post from booru with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		/// <exception cref="SearchNotFoundException"></exception>
		public override async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			var post = await this.GetRandomPostAsync(
					this._PostLimit,
					tags);

			if (post.Length == 0)
			{
				throw new SearchNotFoundException("No post found.");
			}

			return post[this._RNG.NextInt(0, (uint)(post.Length - 1))];
		}

		/// <summary>
		/// Search some post from booru with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
		/// <param name="limit">How many post to retrieve.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public override async Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			return await this.PostListAsync(
				limit,
				tags,
				this._RNG.NextInt(0, this._PageLimit));
		}

		#endregion Post

		#region Tag

		/// <summary>
		/// Get a list of tag that contains 
		/// </summary>
		/// <param name="name">The tag names to query.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public override async Task<Tag[]> TagListAsync(string name)
		{
			if (name == null || name.Length <= 0)
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tag") +
				$"&limit={ this._PostLimit }&orderby=name&name_pattern ={ name }";

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return jsonArray.Select(ReadTag).ToArray();
		}

		#endregion Tag

		#endregion Public Method
	}
}
