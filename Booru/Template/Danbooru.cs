using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
	/// Danbooru, A taggable image board.
	/// </summary>
	public abstract class Danbooru : Booru
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
		protected override Artist ReadArtist(JToken json)
		{
			return new Artist(
				json["id"].Value<uint>(),
				json["name"].Value<string>(),
				null); // no artist urls in API response.
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JToken json)
		{
			return new Pool(
				json["id"].Value<uint>(),
				json["name"].Value<string>(),
				json["post_count"].Value<uint>(),
				json["description"].Value<string>());
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JToken json)
		{
			return new Post(
				json["id"].Value<uint>(),
				this._BaseUrl + "posts/",
				json["file_url"].Value<string>(),
				json["preview_file_url"].Value<string>(),
				this.ConvertRating(json["rating"].Value<string>()),
				json["tag_string"].Value<string>(),
				json["file_size"].Value<uint>(),
				json["image_height"].Value<int>(),
				json["image_width"].Value<int>(),
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
				json["name"].Value<string>(),
				(TagType)json["category"].Value<int>(),
				json["post_count"].Value<uint>()
				);
		}

		/// <inheritdoc/>
		protected override TagRelated ReadTagRelated(JToken json)
		{
			var item = (JArray)json;
			return new TagRelated(
				item[0].Value<string>(),
				item[1].Value<uint>());
		}

		/// <inheritdoc/>
		protected override Wiki ReadWiki(JToken json)
		{
			return new Wiki(
				json["id"].Value<uint>(),
				json["title"].Value<string>(),
				json["body"].Value<string>()
				);
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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Artist with name \"{ name }\" at page { page }.");
			}

			return jsonArray.Select(ReadArtist).ToArray();
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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Pool with title \"{ title }\" at page { page }.");
			}

			return jsonArray.Select(this.ReadPool).ToArray();
		}

		/// <inheritdoc/>
		public override async Task<Post[]> PoolPostList(uint poolId)
		{
			var url = this.CreateBaseApiCall($"pools/{ poolId }");
	
			var obj = await this.GetJsonResponseAsync<JObject>(url);

			// if Pool not found, it return JSON response
			// containing a reason why it not found

			if (obj.ContainsKey("success"))
			{
				throw new SearchNotFoundException($"Can't find Pool with id { poolId }.");
			}

			// the JSON response only give the Post id
			// so we need get the Post data from another API call.

			var postIds = JsonConvert.DeserializeObject<int[]>(
				obj["post_ids"].ToString());

			if (postIds.Length == 0)
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

			var obj = await this.GetJsonResponseAsync<JObject>(url);

			// if Post is not found, it return JSON response
			// containing error message

			if (obj.ContainsKey("success"))
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

			string url = "";

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

			string url = "";

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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
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
				return jsonArray.Select(this.ReadPost).ToArray();
			}
			catch (Exception e)
			{
				throw new SearchNotFoundException("Something happen when deserialize Post data.", e);
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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Tags with name \"{ name }\".");
			}

			return jsonArray.Select(ReadTag).ToArray();
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

			var obj = await this.GetJsonResponseAsync<JObject>(url);

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				obj["tags"].ToString());

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find related Tags with Tag name \"{ name }\".");
			}

			return jsonArray.Select(this.ReadTagRelated).ToArray();
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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"No Wiki found with title \"{ title }\"");
			}

			return jsonArray.Select(this.ReadWiki).ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
