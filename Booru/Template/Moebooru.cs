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
		/// <param name="httpClient">
		///		Client for sending and receive http response.
		///	</param>
		/// <param name="rng">
		///		Random generator for random <see cref="Post"/>.
		/// </param>
		public Moebooru(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng)
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
			return new Post
			{
				ID = json.GetProperty("id").GetUInt32(),
				PostUrl = this._BaseUrl + "post/show/",
				FileUrl = json.GetProperty("file_url").GetString(),
				PreviewUrl = json.GetProperty("preview_url").GetString(),
				Rating = this.ConvertRating(json.GetProperty("rating").GetString()),
				Tags = json.GetProperty("tags").GetString(),
				Size = json.GetProperty("file_size").GetUInt32(),
				Height = json.GetProperty("height").GetInt32(),
				Width = json.GetProperty("width").GetInt32(),
				PreviewHeight = json.GetProperty("preview_height").GetInt32(),
				PreviewWidth = json.GetProperty("preview_width").GetInt32(),
				Source = json.GetProperty("source").GetString()
			};
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = json.GetProperty("id").GetUInt32(),
				Name = json.GetProperty("name").GetString(),
				Type = (TagType)json.GetProperty("type").GetInt32(),
				Count = json.GetProperty("count").GetUInt32()
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

		#endregion Protected Overrride Method
	}
}
