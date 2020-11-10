using System;
using System.Collections.Generic;
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
		public Danbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new JSF32())
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
			this._PageLimit = 100;
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

		#region Overrride Method

		protected override string CreateBaseApiCall(string query)
		{
			return $"{this._BaseUrl.AbsoluteUri}{query}.json?";
		}

		#endregion Overrride Method

		#region Protected Method

		/// <summary>
		/// Read <see cref="Artist"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected Artist ReadArtist(object json)
		{
			var item = (JObject)json;
			return new Artist(
				item["id"].Value<uint>(),
				item["name"].Value<string>());
		}

		/// <summary>
		/// Read <see cref="Pool"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected Pool ReadPool(object json)
		{
			var item = (JObject)json;
			return new Pool(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				item["post_count"].Value<uint>(),
				item["description"].Value<string>());
		}

		/// <summary>
		/// Read <see cref="Post"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected Post ReadPost(object json)
		{
			var item = (JObject)json;
			return new Post(
				item["id"].Value<uint>(),
				new Uri(this._BaseUrl + "posts/" + item["id"].Value<int>()),
				new Uri(item["file_url"].Value<string>()),
				new Uri(item["preview_file_url"].Value<string>()),
				this.ConvertRating(item["rating"].Value<string>()),
				item["tag_string"].Value<string>(),
				item["file_size"].Value<uint>(),
				item["image_height"].Value<int>(),
				item["image_width"].Value<int>(),
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
		protected Tag ReadTag(object json)
		{
			var item = (JObject)json;
			return new Tag(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				(TagType)item["category"].Value<int>(),
				item["post_count"].Value<uint>()
				);
		}

		/// <summary>
		/// Read related tag json search result.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		protected string ReadRelatedTag(object json)
		{
			var item = (JArray)json;
			return item[0].Value<string>();
		}

		/// <summary>
		/// Read <see cref="Wiki"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected Wiki ReadWiki(object json)
		{
			var item = (JObject)json;
			return new Wiki(
				item["id"].Value<uint>(),
				item["title"].Value<string>(),
				item["created_at"].Value<DateTime>(),
				item["updated_at"].Value<DateTime>(),
				item["body"].Value<string>()
				);
		}

		#endregion Protected Method

		#region Public Method

		#region Artist

		/// <summary>
		/// Get a list of artists.
		/// </summary>
		/// <param name="name">The name (or a fragment of the name) of the artist.</param>
		/// <param name="page">The page number.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public async Task<Artist[]> ArtistListAsync(string name, uint page = 0)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty");
			}

			var url = this.CreateBaseApiCall("artists") +
				$"limit={ this._PostLimit }&page={ page }&search[any_name_matches]={ name }";

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return jsonArray.Select(ReadArtist).ToArray();
		}

		#endregion Artist

		#region Pool

		/// <summary>
		/// Search a pool.
		/// </summary>
		/// <param name="title">The title of pool.</param>
		/// <param name="page">Tha page number.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public async Task<Pool[]> PoolList(string title, uint page = 0)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");;
			}

			var url = this.CreateBaseApiCall("pools") +
				$"limit={ this._PostLimit }&page={ page }&search[name_matches]={ title }";

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await this.GetJsonAsync(url));

			return jsonArray.Select(this.ReadPool).ToArray();
		}

		/// <summary>
		/// Get list of post inside the pool.
		/// </summary>
		/// <param name="poolId">The <see cref="Pool"/> id.</param>
		/// <param name="page">The page number.</param>
		/// <returns></returns>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public async Task<Post[]> PoolPostList(uint poolId, uint page = 0)
		{
			var url = this.CreateBaseApiCall($"pools/{ poolId }");

			var obj = JsonConvert.DeserializeObject<JObject>(
				await this.GetJsonAsync(url));
		
			var postIds = JsonConvert.DeserializeObject<int[]>(
				obj["post_ids"].ToString());

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
		/// Show a detail of specific post.
		/// </summary>
		/// <param name="postId"><see cref="Post"/> id to show.</param>
		/// <returns></returns>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		/// <exception cref="SearchNotFoundException"></exception>
		public async Task<Post> PostShowAsync(uint postId)
		{
			var url = this.CreateBaseApiCall($"posts/{ postId }");

			var obj = JsonConvert.DeserializeObject<JObject>(
				await this.GetJsonAsync(url));

			try
			{
				return this.ReadPost(obj);
			}
			catch
			{
				throw new SearchNotFoundException("Post not found.");
			}
		}

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
		public async Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			if (tags != null && tags.Length > this._TagsLimit)
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
				string tagString = string.Join(" ", tags);
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&page={ page }&tags={ tagString }";
			}

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await this.GetJsonAsync(url));
			
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
		public async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			var post = await this.GetRandomPostAsync(
					1,
					tags);
		
			return post[0];
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
		public async Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			if (tags != null && tags.Length > this._TagsLimit)
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
				string tagString = string.Join(" ", tags);
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&tags={ tagString }&random=true";
			}

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await this.GetJsonAsync(url));

			try
			{
				return jsonArray.Select(this.ReadPost).ToArray();
			}
			catch
			{
				throw new SearchNotFoundException("No post found.");
			}
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
		public async Task<Tag[]> TagListAsync(string name)
		{
			if (name == null || name.Length <= 0)
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tags") +
				$"limit={ this._PostLimit }&order=name&search[name_matches]={ name }";

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return jsonArray.Select(ReadTag).ToArray();
		}

		/// <summary>
		/// Get a list of related tags.
		/// </summary>
		/// <param name="name">The tag names to query.</param>
		/// <param name="type">Restrict results to tag type (can be general, artist, copyright, or character).</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public async Task<string[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			if (name == null || name.Length <= 0)
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			if (type != TagType.Artist ||
				type != TagType.General ||
				type != TagType.Copyright ||
				type != TagType.Character)
			{
				throw new ArgumentException("Tag type is invalid.");
			}

			var url = this.CreateBaseApiCall("related_tag") +
				$"query={ name }&category={ type }";

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return jsonArray.Select(this.ReadRelatedTag).ToArray();
		}

		#endregion Tag

		#region Wiki

		/// <summary>
		/// Search a wiki content.
		/// </summary>
		/// <param name="title">Wiki title.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public async Task<Wiki[]> WikiListAsync(string title)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			var url = this.CreateBaseApiCall("wiki_pages") +
				$"search[order]=title&search[title]={ title }";

			var array = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return array.Select(this.ReadWiki).ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
