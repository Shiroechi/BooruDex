using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Moebooru, a fork of Danbooru1 that has been heavily modified.
	/// </summary>
	public abstract class Moebooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Moebooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Moebooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Moebooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Moebooru(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
		{
			this._PostLimit = 100;
			this._TagsLimit = 6;
			this._PageLimit = 10;
			this.IsSafe = false;
			this._ApiVersion = "1.13.0+update.3";
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Moebooru()
		{

		}

		#endregion Constructor & Destructor

		#region Protected Overrride Method

		/// <inheritdoc/>
		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			if (query.Contains("/"))
			{
				if (json)
				{
					return $"{ this._BaseUrl.AbsoluteUri }{ query }.json?";
				}
				return $"{ this._BaseUrl.AbsoluteUri }{ query }.xml?";
			}
			else
			{
				if (json)
				{
					return $"{ this._BaseUrl.AbsoluteUri }{ query }/index.json?";
				}
				return $"{ this._BaseUrl.AbsoluteUri }{ query }/index.xml?";
			}
		}

		/// <inheritdoc/>
		protected override Artist ReadArtist(JsonElement json)
		{
			var array = json.GetProperty("urls");

			var urls = new List<string>();

			if (array.GetArrayLength() != 0)
			{
				foreach (var item in array.EnumerateArray())
				{
					urls.Add(item.GetString());
				}
			}

			return new Artist(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				urls);
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JsonElement json)
		{
			return new Pool(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				json.GetProperty("post_count").GetUInt32(),
				json.GetProperty("description").GetString());
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post(
				json.GetProperty("id").GetUInt32(),
				this._BaseUrl + "post/show/",
				json.GetProperty("file_url").GetString(),
				json.GetProperty("preview_url").GetString(),
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				json.GetProperty("file_size").GetUInt32(),
				json.GetProperty("height").GetInt32(),
				json.GetProperty("width").GetInt32(),
				json.GetProperty("preview_height").GetInt32(),
				json.GetProperty("preview_width").GetInt32(),
				json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				(TagType)json.GetProperty("type").GetInt32(),
				json.GetProperty("count").GetUInt32());
		}

		/// <inheritdoc/>
		protected override TagRelated ReadTagRelated(JsonElement json)
		{
			return new TagRelated(
				json[0].GetString(),
				json[1].GetUInt32());
		}

		/// <inheritdoc/>
		protected override Wiki ReadWiki(JsonElement json)
		{
			return new Wiki(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("title").GetString(),
				json.GetProperty("body").GetString());
		}

		#endregion Protected Overrride Method

		#region Public Method

		#region Artist

		/// <inheritdoc/>
		public override async Task<Artist[]> ArtistListAsync(string name, uint page = 0, bool sort = false)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty");
			}

			var url = this.CreateBaseApiCall("artist") +
				$"page={ page }&name={ name }";

			if (sort)
			{
				url += "&order=name";
			}

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Artist with name \"{ name }\" at page { page }.");
			}

			var artists = new List<Artist>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				artists.Add(this.ReadArtist(item));
			}

			return artists.ToArray();
		}

		#endregion Artist

		#region Pool

		/// <inheritdoc/>
		public override async Task<Pool[]> PoolList(string title, uint page = 0)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			var url = this.CreateBaseApiCall("pool") +
				$"page={ page }&query={ title }";

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			var pools = new List<Pool>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				pools.Add(this.ReadPool(item));
			}

			return pools.ToArray();
		}

		/// <inheritdoc/>
		public override async Task<Post[]> PoolPostList(uint poolId)
		{
			var url = this.CreateBaseApiCall("pool/show") +
				$"id={ poolId }";

			try
			{
				JsonElement obj;

				using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
				{
					obj = temp.RootElement.Clone();
				}

				var posts = new List<Post>();

				foreach (var item in obj.GetProperty("posts").EnumerateArray())
				{
					posts.Add(this.ReadPost(item));
				}

				return posts.ToArray();
			}
			catch (Exception e)
			{
				// if pool not found, it will return to pool page 
				// like yande.re/pool, not a empty JSON.
				throw new SearchNotFoundException($"Can't find Pool with id { poolId }.", e);
			}
		}
		
		#endregion Pool

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

			string url;

			if (tags == null)
			{
				url = this.CreateBaseApiCall("post") +
					$"limit={ limit }&page={ page }";
			}
			else
			{
				url = this.CreateBaseApiCall("post") +
					$"limit={ limit }&page={ page }&tags={ string.Join(" ", tags) }";
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
					$"limit={ 1 }&page={ 0 }";
			}
			else
			{
				url = this.CreateBaseApiCall("post", false) +
					$"limit={ 1 }&page={ 0 }&tags={ string.Join(" ", tags) }";
			}

			// get Post count in XML response.

			var postCount = await this.GetPostCountAsync(url); 

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
		public async override Task<Post[]> GetRandomPostAsync(uint limit , string[] tags = null)
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
					$"limit={ 1 }&page={ 0 }";
			}
			else
			{
				url = this.CreateBaseApiCall("post", false) +
					$"limit={ 1 }&page={ 0 }&tags={ string.Join(" ", tags) }";
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
				$"limit=0&order=name&name={ name }";

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Tags with name \"{ name }\".");
			}

			var tags = new List<Tag>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				tags.Add(this.ReadTag(item));
			}

			return tags.ToArray();
		}

		/// <inheritdoc/>
		public override async Task<TagRelated[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tag/related") +
				$"tags={ name }&type={ type }";
			
			JsonElement obj;

			using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				obj = temp.RootElement.Clone();
			}

			JsonElement jsonArray;

			if (this.PropertyExist(obj, name))
			{
				jsonArray = obj.GetProperty(name);
			}
			else
			{
				jsonArray = obj.GetProperty("useless_tags");
			}

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find related Tags with Tag name \"{ name }\".");
			}

			var tags = new List<TagRelated>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				tags.Add(this.ReadTagRelated(item));
			}

			return tags.ToArray();
		}

		#endregion Tag

		#region Wiki

		/// <inheritdoc/>
		public override async Task<Wiki[]> WikiListAsync(string title)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			var url = this.CreateBaseApiCall("wiki") +
				$"order=title&query={ title }";

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"No Wiki found with title \"{ title }\"");
			}

			var wikis = new List<Wiki>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				wikis.Add(this.ReadWiki(item));
			}

			return wikis.ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
