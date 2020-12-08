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
		/// <param name="rng">Random generator for random post.</param>
		public Danbooru(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng == null ? new SplitMix64() : rng)
		{
			this.IsSafe = false;
			this.HasArtistApi =
				this.HasPoolApi =
				this.HasTagApi =
				this.HasTagRelatedApi =
				this.HasWikiApi = true;
			this._PostLimit = 200;
			this._TagsLimit = 2;
			this._PageLimit = 1000;
			this._ApiVersion = "";
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
				json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				(TagType)json.GetProperty("category").GetInt32(),
				json.GetProperty("post_count").GetUInt32());
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
		/// <exception cref="JsonException">
		///		The JSON is invalid.
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
		public override async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			return (await this.GetRandomPostAsync(1, tags))[0];
		}

		/// <inheritdoc/>
		public override async Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			this.CheckTagsLimit(tags);

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._PostLimit)
			{
				limit = this._PostLimit;
			}

			var url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&random=true";

			if (tags != null)
			{
				url += $"&tags={ string.Join(" ", tags) }";
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

			var posts = new List<Post>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				posts.Add(this.ReadPost(item));
			}

			return posts.ToArray();
		}

		#endregion Post

		#endregion Public Method
	}
}
