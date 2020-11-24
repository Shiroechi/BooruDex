using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

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
					$"limit={ limit }&page={ page }";
			}
			else
			{
				url = this.CreateBaseApiCall("post") +
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
				Console.WriteLine(e);
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

			var postCount = await this.GetPostCount(url); 

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}

			// get post with random the page number, each page 
			// limited only with 1 post.

			var pageNumber = this._RNG.NextInt(1, (uint)postCount);

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

			// get Post count int XML response.

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
				if (tags == null)
				{
					url = this.CreateBaseApiCall("post") +
						$"limit={ limit }&page={ this._RNG.NextInt(1, maxPageNumber - 1) }";
				}
				else
				{
					url = this.CreateBaseApiCall("post") +
						$"limit={ limit }&page={ this._RNG.NextInt(1, maxPageNumber - 1) }&tags={ string.Join(" ", tags) }";
				}

				var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

				return jsonArray.Select(this.ReadPost).ToArray();
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

			var jsonArray = await this.GetJsonResponseAsync<JArray>(url);

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find Tags with name { name }.");
			}

			return jsonArray.Select(ReadTag).ToArray();
		}

		/// <inheritdoc/>
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

			if (jsonArray.Count == 0)
			{
				throw new SearchNotFoundException($"Can't find related Tags with Tag name { name }.");
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

			var url = this.CreateBaseApiCall("wiki") +
				$"order=title&query={ title }";

			var array = await this.GetJsonResponseAsync<JArray>(url);

			return array.Select(this.ReadWiki).ToArray();
		}

		#endregion Wiki

		#endregion Public Method
	}
}
