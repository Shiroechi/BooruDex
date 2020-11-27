using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using BooruDex2.Exceptions;
using BooruDex2.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex2.Booru.Template
{
	/// <summary>
	/// Danbooru, A taggable image board.
	/// </summary>
	public abstract class Danbooru : Boorus
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Danbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Danbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Danbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Danbooru(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
		{
			this._PostLimit = 200;
			this._TagsLimit = 2;
			this._PageLimit = 10;
			this.IsSafe = false;
			this._ApiVersion = "";
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Danbooru()
		{

		}

		#endregion Constructor & Destructor

		#region Protected Overrride Method

		/// <inheritdoc/>
		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			if (json)
			{
				return $"{ this._BaseUrl.AbsoluteUri }{ query }.json?";
			}
			return $"{ this._BaseUrl.AbsoluteUri }{ query }.xml?";
		}

		/// <inheritdoc/>
		protected override Artist ReadArtist(JsonElement json)
		{
			return new Artist(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				new string[] { "" }); // no artist urls in JSON API response.
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
				this.PropertyExist(json, "id") ? json.GetProperty("id").GetUInt32() : 0,
				this._BaseUrl + "posts/",
				this.PropertyExist(json, "file_url") ? json.GetProperty("file_url").GetString() : null,
				this.PropertyExist(json, "preview_file_url") ? json.GetProperty("preview_file_url").GetString() : null,
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tag_string").GetString(),
				json.GetProperty("file_size").GetUInt32(),
				json.GetProperty("image_height").GetInt32(),
				json.GetProperty("image_width").GetInt32(),
				0,
				0,
				json.GetProperty("source").GetString()
				);
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				(TagType)json.GetProperty("category").GetInt32(),
				json.GetProperty("post_count").GetUInt32()
				);
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

			var url = this.CreateBaseApiCall("artists") +
				$"limit={ this._PostLimit }&page={ page }&search[any_name_matches]={ name }";

			if (sort)
			{
				url += "&search[order]=name";
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

			var url = this.CreateBaseApiCall("pools") +
				$"limit={ this._PostLimit }&page={ page }&search[name_matches]={ title }";

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
			var url = this.CreateBaseApiCall($"pools/{ poolId }");

			JsonElement obj;

			using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				obj = temp.RootElement.Clone();
			}

			// if Pool not found, it return JSON response
			// containing a reason why it not found

			if (obj.TryGetProperty("success", out _))
			{
				throw new SearchNotFoundException($"Can't find Pool with id { poolId }.");
			}

			// the JSON response only give the Post id
			// so we need get the Post data from another API call.

			var postIds = new List<int>();

			foreach (var id in obj.GetProperty("post_ids").EnumerateArray())
			{
				postIds.Add(id.GetInt32());
			}

			if (postIds.Count == 0)
			{
				throw new SearchNotFoundException($"No Post inside Pool with id { poolId }.");
			}

			var posts = new List<Post>();

			foreach (uint id in postIds)
			{
				posts.Add(await this.PostShowAsync(id));
			}

			return posts.ToArray();
		}

		#endregion Pool

		#region Post

		/// <summary>
		/// Show a detailed information of the <see cref="Post"/>.
		/// </summary>
		/// <param name="postId">Id of the <see cref="Post"/>.</param>
		/// <returns><see cref="Post"/>.</returns>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Post"/> is found.
		/// </exception>
		public virtual async Task<Post> PostShowAsync(uint postId)
		{
			var url = this.CreateBaseApiCall($"posts/{ postId }");

			JsonElement obj;

			using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				obj = temp.RootElement.Clone();
			}

			// if Post is not found, it return JSON response
			// containing error message

			if (obj.TryGetProperty("success", out _))
			{
				throw new SearchNotFoundException($"Post with id { postId } is not found.");
			}

			return this.ReadPost(obj);
		}

		/// <inheritdoc/>
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

			string url;

			if (tags == null)
			{
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&page={ page }";
			}
			else
			{
				url = this.CreateBaseApiCall("posts") +
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
				throw new SearchNotFoundException("Post is found but something happen when deserialize Post data.", e);
			}
		}

		/// <inheritdoc/>
		public override async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			return (await this.GetRandomPostAsync(
					1,
					tags))[0];
		}

		/// <inheritdoc/>
		public override async Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
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

			string url;

			if (tags == null)
			{
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&random=true";
			}
			else
			{
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&tags={ string.Join(" ", tags) }&random=true";
			}

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				if (tags == null || tags.Length <= 0)
				{
					throw new SearchNotFoundException($"No Post found with empty tags.");
				}
				else
				{
					throw new SearchNotFoundException($"No Post found with tags { string.Join(", ", tags) }.");
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
				throw new SearchNotFoundException("Post is found but something happen when deserialize Post data.", e);
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

			var url = this.CreateBaseApiCall("tags") +
				$"limit={ this._PostLimit }&order=name&search[name_matches]={ name }";

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

			var url = this.CreateBaseApiCall("related_tag") +
				$"query={ name }&category={ type }";

			JsonElement obj;

			using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				obj = temp.RootElement.Clone();
			}

			var jsonArray = obj.GetProperty("tags");

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

			var url = this.CreateBaseApiCall("wiki_pages") +
				$"search[order]=title&search[title]={ title }";

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
