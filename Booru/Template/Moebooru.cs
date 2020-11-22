using System;
using System.Collections;
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

		/// <inheritdoc/>
		protected override Artist ReadArtist(JToken json)
		{
			var item = (JObject)json;
			var array = JsonConvert.DeserializeObject<JArray>(
				item["urls"].ToString());

			List<string> urls = new List<string>();

			if (array.Count != 0)
			{
				for (var i = 0; i < array.Count; i++)
				{
					urls.Add(array[i].Value<string>());
				}
			}

			return new Artist(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				urls);
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JToken json)
		{
			var item = json;
			return new Pool(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				item["post_count"].Value<uint>(),
				item["description"].Value<string>());
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JToken json)
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

		/// <inheritdoc/>
		protected override Tag ReadTag(JToken json)
		{
			var item = json;
			return new Tag(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				(TagType)item["type"].Value<int>(),
				item["count"].Value<uint>()
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
			var item = json;
			return new Wiki(
				item["id"].Value<uint>(),
				item["title"].Value<string>(),
				item["body"].Value<string>()
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

			var url = this.CreateBaseApiCall("artist") +
				$"page={ page }&name={ name }";

			if (sort)
			{
				url += "&order=name";
			}

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Artist with name { name } at page { page }.");
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
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");;
			}

			var url = this.CreateBaseApiCall("pool") +
				$"page={ page }&query={ title }";

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Pool with title { title } at page { page }.");
			}

			return jsonArray.Select(this.ReadPool).ToArray();
		}

		/// <inheritdoc/>
		public override async Task<Post[]> PoolPostList(uint poolId)
		{
			var url = this.CreateBaseApiCall("pool/show") +
				$"id={ poolId }";

			try
			{
				var obj = await this.GetJsonResponseAsync<JObject>(url);

				var jsonArray = JsonConvert.DeserializeObject<JArray>(
						obj["posts"].ToString());

				return jsonArray.Select(this.ReadPost).ToArray();
			}
			catch (JsonReaderException e)
			{
				// if pool not found, it will return pool page 
				// like yande.re/pool, not empty JSON.
				throw new SearchNotFoundException($"Can't find Pool with id { poolId }.");
			}
		}

		/// <summary>
		/// Get partial <see cref="Post"/> inside the <see cref="Pool"/>.
		/// </summary>
		/// <param name="poolId">The <see cref="Pool"/> id.</param>
		/// <param name="page">The page number.</param>
		/// <returns>Array of <see cref="Post"/> from <see cref="Pool"/>.</returns>
		/// <exception cref="ArgumentNullException">
		///		One or more parameter is null or empty.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty.
		/// </exception>
		protected virtual async Task<Post[]> PoolPostList(uint poolId, uint page)
		{
			var url = this.CreateBaseApiCall("pool/show") +
				$"page={ page }&id={ poolId }";

			var obj = await this.GetJsonResponseAsync<JObject>(url);

			var jsonArray = JsonConvert.DeserializeObject<JArray>(
				obj["posts"].ToString());

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find post in pool id { poolId } at page { page }.");
			}

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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

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

			var obj = await this.GetJsonResponseAsync<JObject>(url);

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

			return jsonArray.Select(this.ReadTagRelated).ToArray();
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

			var array = await this.GetJsonResponseAsync<JArray>(url);

			return array.Select(this.ReadWiki).ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
