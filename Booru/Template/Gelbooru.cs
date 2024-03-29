﻿using System;
using System.Net.Http;
using System.Text.Json;

using BooruDex.Models;

using Litdex.Security.RNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	///		Gelbooru.
	/// </summary>
	public abstract class Gelbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		///		<see cref="Gelbooru"/> template for booru client.
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
		public Gelbooru(string domain, bool useHttps = true, HttpClient httpClient = null, IRNG rng = null) : base(domain, useHttps: useHttps, httpClient: httpClient, rng: rng)
		{
			this.IsSafe = false;
			this.HasTagApi = true;
			this._DefaultPostLimit = 100; // may increased up to 1000
			this._TagsLimit = 0; // no tag limit
			this._PageLimit = 20000;
			this._ApiVersion = "";
		}

		#endregion Constructor & Destructor

		#region Create API url

		/// <inheritdoc/>
		protected override string CreateBaseApiUrl(string query, bool json = true)
		{
			var url = $"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index";

			if (json)
			{
				url += "&json=1";
			}

			return url;
		}

		/// <inheritdoc/>
		protected override string CreatePostListUrl(byte limit, uint page = 0)
		{
			return this.CreateBaseApiUrl("post") +
				$"&limit={ limit }&pid={ page }";
		}

		/// <inheritdoc/>
		protected override string CreateTagListUrl(string name)
		{
			return this.CreateBaseApiUrl("tag") +
				$"&limit={ this._DefaultPostLimit }&orderby=name&name_pattern={ name }";
		}

		/// <inheritdoc/>
		protected override string CreatePostCountUrl()
		{
			return this.CreateBaseApiUrl("post", false) + "&limit=0";
		}

		#endregion Create API url

		#region Read JSON to convert it into object

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post(
				id: json.GetProperty("id").GetUInt32(),
				postUrl: this._BaseUrl + "index.php?page=post&s=view&id=",
				fileUrl: json.GetProperty("file_url").GetString(),
				previewUrl: this._BaseUrl + "thumbnails/" + json.GetProperty("directory").GetString() + "/thumbnail_" + json.GetProperty("hash").GetString() + ".jpg",
				rating: this.ConvertRating(json.GetProperty("rating").GetString()),
				tags: json.GetProperty("tags").GetString(),
				size: 0,
				height: json.GetProperty("height").GetInt32(),
				width: json.GetProperty("width").GetInt32(),
				previewHeight: 0,
				previewWidth: 0,
				source: json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Name = json.GetProperty("tag").GetString(),
				Type = this.ToTagType(json.GetProperty("type").GetString()),
				Count = int.Parse(json.GetProperty("count").GetString())
			};
		}

		/// <summary>
		///		Convert string "type" from tag JSON to <see cref="TagType"/>.
		/// </summary>
		/// <param name="tagTypeName">
		///		Tag type name.
		///	</param>
		/// <returns>
		///		<see cref="TagType"/> based on the name or 
		///		<see cref="TagType.Undefined"/> if "type" from tag JSON is not recognizable.
		/// </returns>
		protected virtual TagType ToTagType(string tagTypeName)
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;

			if (comparer.Equals(tagTypeName, "tag"))
			{
				return TagType.General;
			}

			foreach (TagType type in Enum.GetValues(typeof(TagType)))
			{
				if (comparer.Equals(tagTypeName, type.ToString()))
				{
					return type;
				}
			}

			return TagType.Undefined;
		}

		#endregion Read JSON to convert it into object
	}
}
