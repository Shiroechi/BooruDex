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


		#endregion Public Method
	}
}
