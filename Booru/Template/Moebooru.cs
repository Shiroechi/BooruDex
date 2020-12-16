using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

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
		/// <param name="rng">Random generator for random post.</param>
		public Moebooru(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng)
		{
			this.IsSafe = false;
			this.HasArtistApi =
				this.HasPoolApi =
				this.HasTagApi =
				this.HasTagRelatedApi =
				this.HasWikiApi = true;
			this._PostLimit = 100; // may increased up to 1000
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

			return new Artist(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				urls);
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
				json.GetProperty("id").GetUInt32(),
				this._BaseUrl + "post/show/",
				json.GetProperty("file_url").GetString(),
				json.GetProperty("preview_url").GetString(),
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				json.GetProperty("file_size").GetUInt32(),
				json.GetProperty("height").GetInt32(),
				json.GetProperty("width").GetInt32(),
				json.GetProperty("preview_height").GetInt32(),
				json.GetProperty("preview_width").GetInt32(),
				json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				(TagType)json.GetProperty("type").GetInt32(),
				json.GetProperty("count").GetUInt32());
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
	}
}
