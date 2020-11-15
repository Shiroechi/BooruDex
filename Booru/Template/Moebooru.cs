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
		public Moebooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new JSF32())
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

		protected override string CreateBaseApiCall(string query)
		{
			if (query.Contains("/"))
			{
				return $"{ this._BaseUrl.AbsoluteUri }{ query }.json?";
			}
			else
			{
				return $"{ this._BaseUrl.AbsoluteUri }{ query }/index.json?";
			}
		}

		#endregion Protected Overrride Method

		#region Protected Virtual Method

		/// <summary>
		/// Read <see cref="Artist"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Artist ReadArtist(JToken json)
		{
			var item = json;
			return new Artist(
				item["id"].Value<uint>(),
				item["name"].Value<string>());
		}

		/// <summary>
		/// Read <see cref="Pool"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Pool ReadPool(JToken json)
		{
			var item = json;
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
		protected virtual Post ReadPost(JToken json)
		{
			var item = json;
			return new Post(
				item["id"].Value<uint>(),
				this._BaseUrl + "post/show/",
				item["file_url"].Value<string>(),
				item["preview_url"].Value<string>(),
				this.ConvertRating(item["rating"].Value<string>()),
				item["tags"].Value<string>(),
				item["file_size"].Value<uint>(),
				item["height"].Value<int>(),
				item["width"].Value<int>(),
				item["preview_height"].Value<int>(),
				item["preview_width"].Value<int>(),
				item["source"].Value<string>()
				);
		}

		/// <summary>
		/// Read <see cref="Tag"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Tag ReadTag(JToken json)
		{
			var item = json;
			return new Tag(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				(TagType)item["type"].Value<int>(),
				item["count"].Value<uint>()
				);
		}

		/// <summary>
		/// Read related tag json search result.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		protected virtual TagRelated ReadRelatedTag(JToken json)
		{
			var item = (JArray)json;
			return new TagRelated(
				item[0].Value<string>(),
				item[1].Value<uint>());
		}

		/// <summary>
		/// Read <see cref="Wiki"/> json search result.
		/// </summary>
		/// <param name="json">Json object.</param>
		/// <returns></returns>
		protected virtual Wiki ReadWiki(JToken json)
		{
			var item = json;
			return new Wiki(
				item["id"].Value<uint>(),
				item["title"].Value<string>(),
				item["body"].Value<string>()
				);
		}

		#endregion Protected Virtual Method

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
		public override async Task<Artist[]> ArtistListAsync(string name, uint page = 0)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty");
			}

			var url = this.CreateBaseApiCall("artist") +
				$"page={ page }&name={ name }";

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
		public override async Task<Pool[]> PoolList(string title, uint page = 0)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");;
			}

			var url = this.CreateBaseApiCall("pool") +
				$"page={ page }&query={ title }";

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
		public override async Task<Post[]> PoolPostList(uint poolId, uint page = 0)
		{
			var url = this.CreateBaseApiCall("pool/show") +
				$"page={ page }&id={ poolId }";

			var obj = JsonConvert.DeserializeObject<JObject>(
				await this.GetJsonAsync(url));
		
			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				obj["posts"].ToString());

			return jsonArray.Select(this.ReadPost).ToArray();
		}

		#endregion Pool

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
					$"limit={ limit }&page={ page }";
			}
			else
			{
				string tagString = string.Join(" ", tags);
				url = this.CreateBaseApiCall("post") +
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
		public override async Task<Post[]> GetRandomPostAsync(uint limit , string[] tags = null)
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
				$"limit=0&order=name&name={ name }";

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
		public override async Task<TagRelated[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			if (name == null || name.Length <= 0)
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tag/related") +
				$"tags={ name }&type={ type }";

			var obj = JsonConvert.DeserializeObject<JObject>(
				await GetJsonAsync(url));

			JArray jsonArray;

			if (obj.ContainsKey(name))
			{
				jsonArray = JsonConvert.DeserializeObject<JArray>(
				obj[name].ToString());
			}
			else
			{
				jsonArray = JsonConvert.DeserializeObject<JArray>(
				obj["useless_tags"].ToString());
			}

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
		public override async Task<Wiki[]> WikiListAsync(string title)
		{
			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			var url = this.CreateBaseApiCall("wiki") +
				$"order=title&query={ title }";

			var array = JsonConvert.DeserializeObject<JArray>(
				await GetJsonAsync(url));

			return array.Select(this.ReadWiki).ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
