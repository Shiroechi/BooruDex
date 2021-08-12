using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	///		Danbooru, A taggable image board.
	/// </summary>
	public abstract class Danbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		///		<see cref="Danbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">
		///		URL of booru based sites.
		///	</param>
		/// <param name="useHttps">
		///		Using HTTPS protocol or not.
		/// </param>
		/// <param name="httpClient">
		///		Client for sending and receive http response.
		///	</param>
		/// <param name="rng">
		///		Random generator for random <see cref="Post"/>.
		///	</param>
		public Danbooru(string domain, bool useHttps = true, HttpClient httpClient = null, IRNG rng = null) : base(domain, useHttps: useHttps, httpClient: httpClient, rng: rng)
		{
			this.IsSafe = false;
			this.HasArtistApi =
			this.HasPoolApi =
			this.HasTagApi =
			this.HasTagRelatedApi =
			this.HasWikiApi = true;
			this._DefaultPostLimit = 200;
			this._TagsLimit = 2;
			this._PageLimit = 1000;
			this._ApiVersion = "";
		}

		#endregion Constructor & Destructor

		#region Create API url

		/// <inheritdoc/>
		protected override string CreateBaseApiUrl(string query, bool json = true)
		{
			if (json)
			{
				return $"{ this._BaseUrl.AbsoluteUri }{ query }.json?";
			}
			return $"{ this._BaseUrl.AbsoluteUri }{ query }.xml?";
		}

		/// <inheritdoc/>
		protected override string CreateArtistListUrl(string name, ushort page = 0, bool sort = false)
		{
			var url = this.CreateBaseApiUrl("artists") +
				$"limit={ this._DefaultPostLimit }&page={ page }&search[any_name_matches]={ name }";

			if (sort)
			{
				url += "&search[order]=name";
			}

			return url;
		}

		/// <inheritdoc/>
		protected override string CreatePoolListUrl(string title, uint page)
		{
			return this.CreateBaseApiUrl("pools") +
				$"limit={ this._DefaultPostLimit }&page={ page }&search[name_matches]={ title }";
		}

		/// <inheritdoc/>
		protected override string CreatePoolPostListUrl(uint poolId)
		{
			return this.CreateBaseApiUrl($"pools/{ poolId }");
		}

		/// <inheritdoc/>
		protected override string CreateTagListUrl(string name)
		{
			return this.CreateBaseApiUrl("tags") +
				$"limit={ this._DefaultPostLimit }&order=name&search[name_matches]={ name }";
		}

		/// <inheritdoc/>
		protected override string CreateTagRelatedUrl(string name, TagType type = TagType.General)
		{
			return this.CreateBaseApiUrl("related_tag") +
				$"query={ name }&category={ type }";
		}

		/// <inheritdoc/>
		protected override string CreateWikiListUrl(string title)
		{
			return this.CreateBaseApiUrl("wiki_pages") +
				$"search[order]=title&search[title]={ title }";
		}

		/// <inheritdoc/>
		protected override string CreatePostListUrl(byte limit, uint page = 0)
		{
			return this.CreateBaseApiUrl("posts") +
				$"limit={ limit }&page={ page }";
		}

		#endregion Create API url

		#region Read JSON to convert it into object

		/// <inheritdoc/>
		protected override Artist ReadArtist(JsonElement json)
		{
			return new Artist
			{
				ID = json.GetProperty("id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				Urls = null // no artist urls in JSON API response.
			};
		}

		/// <summary>
		///		Read JSON <see cref="Artist"/>.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Artist"/> object.
		///	</returns>
		protected virtual Artist ReadArtistDetail(JsonElement json)
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

			return new Artist
			{
				ID = json.GetProperty("artist_id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				Urls = new ReadOnlyCollection<string>(urls)
			};
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JsonElement json)
		{
			return new Pool
			{
				ID = json.GetProperty("id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				PostCount = json.GetProperty("post_count").GetUInt32(),
				Description = json.GetProperty("description").GetString()
			};
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post(
				id: this.PropertyExist(json, "id") ? json.GetProperty("id").GetUInt32() : 0,
				postUrl: this._BaseUrl + "posts/",
				fileUrl: this.PropertyExist(json, "file_url") ? json.GetProperty("file_url").GetString() : null,
				previewUrl: this.PropertyExist(json, "preview_file_url") ? json.GetProperty("preview_file_url").GetString() : null,
				rating: this.ConvertRating(json.GetProperty("rating").GetString()),
				tags: json.GetProperty("tag_string").GetString(),
				size: json.GetProperty("file_size").GetUInt32(),
				height: json.GetProperty("image_height").GetInt32(),
				width: json.GetProperty("image_width").GetInt32(),
				previewHeight: 0,
				previewWidth: 0,
				source: json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = json.GetProperty("id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				Type = (TagType)json.GetProperty("category").GetInt32(),
				Count = json.GetProperty("post_count").GetInt32()
			};
		}

		/// <inheritdoc/>
		protected override TagRelated ReadTagRelated(JsonElement json)
		{
			return new TagRelated
			{
				Name = json[0].GetString(),
				Count = json[1].GetUInt32()
			};
		}

		/// <inheritdoc/>
		protected override Wiki ReadWiki(JsonElement json)
		{
			return new Wiki
			{
				ID = json.GetProperty("id").GetUInt32(),
				Title = json.GetProperty("title").GetString(),
				Body = json.GetProperty("body").GetString()
			};
		}

		#endregion Read JSON to convert it into object

		#region Artist

		/// <summary>
		///		Get <see cref="Artist"/> details.
		/// </summary>
		/// <param name="name">
		///		The exact name of the artist.
		///	</param>
		/// <returns>
		///		Array of <see cref="Artist"/>.
		///	</returns>
		/// <exception cref="ArgumentNullException">
		///		One or more parameter is null or empty.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Artist"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Artist> ArtistDetailAsync(string name)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty.");
			}

			var url = this.CreateBaseApiUrl("artist_versions") +
				$"limit=1&only=artist_id,name,urls&search[name]={ name }";

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Artist with name \"{ name }\".");
			}

			foreach (var item in jsonArray.EnumerateArray())
			{
				return this.ReadArtistDetail(item);
			}

			throw new SearchNotFoundException($"Can't find Artist with name \"{ name }\".");
		}

		#endregion Artist

		#region Post

		/// <summary>
		///		Show a detailed information of the <see cref="Post"/>.
		/// </summary>
		/// <param name="postId">
		///		Id of the <see cref="Post"/>.
		///	</param>
		/// <returns>
		///		<see cref="Post"/>.
		///	</returns>
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
			var url = this.CreateBaseApiUrl($"posts/{ postId }");

			using (var doc = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				// if Post is not found, it return JSON response
				// containing error message

				if (doc.RootElement.TryGetProperty("success", out _))
				{
					throw new SearchNotFoundException($"Post with id { postId } is not found.");
				}

				return this.ReadPost(doc.RootElement);
			}
		}

		/// <inheritdoc/>
		public override async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			return (await this.GetRandomPostAsync(1, tags))[0];
		}

		/// <inheritdoc/>
		public override async Task<Post[]> GetRandomPostAsync(byte limit, string[] tags = null)
		{
			this.CheckTagsLimit(tags);

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._DefaultPostLimit)
			{
				limit = this._DefaultPostLimit;
			}

			var url = this.CreateBaseApiUrl("posts") +
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
	}
}
