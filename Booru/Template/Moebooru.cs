using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

using BooruDex.Models;

using Litdex.Security.RNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	///		Moebooru, a fork of Danbooru1 that has been heavily modified.
	/// </summary>
	public abstract class Moebooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		///		<see cref="Moebooru"/> template for booru client.
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
		/// </param>
		public Moebooru(string domain, bool useHttps = true, HttpClient httpClient = null, IRNG rng = null) : base(domain, useHttps: useHttps, httpClient: httpClient, rng: rng)
		{
			this.IsSafe = false;
			this.HasArtistApi =
				this.HasPoolApi =
				this.HasTagApi =
				this.HasTagRelatedApi =
				this.HasWikiApi = true;
			this._DefaultPostLimit = 100; // may increased up to 1000
			this._TagsLimit = 6;
			this._PageLimit = 0;
			this._ApiVersion = "1.13.0+update.3";
		}

		#endregion Constructor & Destructor

		#region Create API url

		/// <inheritdoc/>
		protected override string CreateBaseApiUrl(string query, bool json = true)
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
		protected override string CreateArtistListUrl(string name, ushort page = 0, bool sort = false)
		{
			var url = this.CreateBaseApiUrl("artist") +
				$"page={ page }&name={ name }";

			if (sort)
			{
				url += "&order=name";
			}

			return url;
		}

		/// <inheritdoc/>
		protected override string CreatePostListUrl(byte limit, uint page = 0)
		{
			return this.CreateBaseApiUrl("post") +
				$"limit={ limit }&page={ page }";
		}

		/// <inheritdoc/>
		protected override string CreatePoolPostListUrl(uint poolId)
		{
			return this.CreateBaseApiUrl("pool/show") +
				$"id={ poolId }";
		}

		/// <inheritdoc/>
		protected override string CreatePoolListUrl(string title, uint page)
		{
			return this.CreateBaseApiUrl("pool") +
				$"page={ page }&query={ title }";
		}

		/// <inheritdoc/>
		protected override string CreateTagListUrl(string name)
		{
			return this.CreateBaseApiUrl("tag") +
				$"limit=0&order=name&name={ name }";
		}

		/// <inheritdoc/>
		protected override string CreateTagRelatedUrl(string name, TagType type = TagType.General)
		{
			return this.CreateBaseApiUrl("tag/related") +
				$"tags={ name }&type={ type }";
		}

		/// <inheritdoc/>
		protected override string CreateWikiListUrl(string title)
		{
			return this.CreateBaseApiUrl("wiki") +
				$"order=title&query={ title }";
		}

		/// <inheritdoc/>
		protected override string CreatePostCountUrl()
		{
			return this.CreateBaseApiUrl("post", false) + "limit=1";
		}

		#endregion Create API url

		#region Read JSON to convert it into object

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

			return new Artist
			{
				ID = json.GetProperty("id").GetUInt32(),
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
				id: json.GetProperty("id").GetUInt32(),
				postUrl: this._BaseUrl + "post/show/",
				fileUrl: json.GetProperty("file_url").GetString(),
				previewUrl: json.GetProperty("preview_url").GetString(),
				rating: this.ConvertRating(json.GetProperty("rating").GetString()),
				tags: json.GetProperty("tags").GetString(),
				size: json.GetProperty("file_size").GetUInt32(),
				height: json.GetProperty("height").GetInt32(),
				width: json.GetProperty("width").GetInt32(),
				previewHeight: json.GetProperty("preview_height").GetInt32(),
				previewWidth: json.GetProperty("preview_width").GetInt32(),
				source: json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = json.GetProperty("id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				Type = (TagType)json.GetProperty("type").GetByte(),
				Count = json.GetProperty("count").GetInt32()
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
	}
}
