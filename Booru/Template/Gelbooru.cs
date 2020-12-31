﻿using System;
using System.Net.Http;
using System.Text;
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
		/// <param name="httpClient">
		///		Client for sending and receive http response.
		///	</param>
		/// <param name="rng">
		///		Random generator for random <see cref="Post"/>.
		///	</param>
		public Gelbooru(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng)
		{
			this.IsSafe = false;
			this.HasTagApi = true;
			this._DefaultPostLimit = 100; // may increased up to 1000
			this._TagsLimit = 0; // no tag limit
			this._PageLimit = 20000;
			this._ApiVersion = "";
			this._PasswordSalt = "";
		}

		#endregion Constructor & Destructor

		#region Protected Overrride Method

		/// <inheritdoc/>
		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			var sb = new StringBuilder($"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index");

			if (json)
			{
				sb.Append("&json=1");
			}

			return sb.ToString();
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post
			{
				ID = json.GetProperty("id").GetUInt32(),
				PostUrl = this._BaseUrl + "index.php?page=post&s=view&id=",
				FileUrl = json.GetProperty("file_url").GetString(),
				PreviewUrl = this._BaseUrl + "thumbnails/" + json.GetProperty("directory").GetString() + "/thumbnail_" + json.GetProperty("hash").GetString() + ".jpg",
				Rating = this.ConvertRating(json.GetProperty("rating").GetString()),
				Tags = json.GetProperty("tags").GetString(),
				Size = 0,
				Height = json.GetProperty("height").GetInt32(),
				Width = json.GetProperty("width").GetInt32(),
				PreviewHeight = 0,
				PreviewWidth = 0,
				Source = json.GetProperty("source").GetString()
			};
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Name = json.GetProperty("tag").GetString(),
				Type = this.ToTagType(json.GetProperty("type").GetString()),
				Count = uint.Parse(json.GetProperty("count").GetString())
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

		#endregion Protected Overrride Method
	}
}
