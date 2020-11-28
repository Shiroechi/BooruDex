using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Gelbooru beta version 0.2.0.
	/// </summary>
	public abstract class Gelbooru02 : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Gelbooru02"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Gelbooru02(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Gelbooru02"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru02(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
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
		~Gelbooru02()
		{

		}

		#endregion Constructor & Destructor

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
		protected override Post ReadPost(JsonElement json)
		{
			var imageName = json.GetProperty("image").GetString();

			var directory = json.GetProperty("directory").GetString();

			return new Post(
				json.GetProperty("id").GetUInt32(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				this._BaseUrl + "images/" + directory + "/" + imageName,
				this._BaseUrl + "thumbnails/" + directory + "/thumbnails_" + imageName.Substring(0, imageName.IndexOf(".")) + ".jpg",
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				0,
				json.GetProperty("height").GetInt32(),
				json.GetProperty("width").GetInt32(),
				0,
				0,
				string.Empty);
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

			if (page > 200000)
			{
				page = 200000;
			}

			string url;

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

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
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
				var posts = new List<Post>();

				foreach (var item in jsonArray.EnumerateArray())
				{
					posts.Add(this.ReadPost(item));
				}

				return posts.ToArray();
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

			var postCount = await this.GetPostCountAsync(url);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}

			// get post with random the page number, each page 
			// limited only with 1 post.

			// there's rate limit for page number, 200.000 page only.
			// more than that will return error 

			var pageNumber = this._RNG.NextInt(1, postCount) % 200000;

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

			var postCount = await this.GetPostCountAsync(url);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}
			else if (postCount < limit)
			{
				throw new SearchNotFoundException($"The site only have { postCount } post with tags { string.Join(", ", tags) }.");
			}

			// there's rate limit for page number, 200.000 page only.
			// more than that will return error 

			var maxPageNumber = (uint)Math.Floor(postCount / limit * 1.0) % 200000;

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

		#endregion Public Method
	}
}
