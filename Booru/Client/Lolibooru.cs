﻿using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

using BooruDex2.Booru.Template;
using BooruDex2.Models;

using Newtonsoft.Json.Linq;

namespace BooruDex2.Booru.Client
{
	public class Lolibooru : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Lolibooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Lolibooru(HttpClient httpClient = null) : base("https://lolibooru.moe/", httpClient)
		{
			this._TagsLimit = 0; // not tag limit
			this._PasswordSalt = "--{}--";
		}

		#endregion Constructor & Destructor

		#region Protected Override Method

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
				uint.Parse(json.GetProperty("id").GetString()),
				json.GetProperty("name").GetString(),
				urls);
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JsonElement json)
		{
			return new Pool(
				uint.Parse(json.GetProperty("id").GetString()),
				json.GetProperty("name").GetString(),
				uint.Parse(json.GetProperty("post_count").GetString()),
				json.GetProperty("description").GetString());
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post(
				uint.Parse(json.GetProperty("id").GetString()),
				this._BaseUrl + "post/show/",
				json.GetProperty("file_url").GetString(),
				json.GetProperty("preview_url").GetString(),
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				uint.Parse(json.GetProperty("file_size").GetString()),
				int.Parse(json.GetProperty("height").GetString()),
				int.Parse(json.GetProperty("width").GetString()),
				int.Parse(json.GetProperty("preview_height").GetString()),
				int.Parse(json.GetProperty("preview_width").GetString()),
				json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				uint.Parse(json.GetProperty("id").GetString()),
				json.GetProperty("name").GetString(),
				(TagType)int.Parse(json.GetProperty("tag_type").GetString()),
				uint.Parse(json.GetProperty("post_count").GetString()));
		}

		/// <inheritdoc/>
		protected override TagRelated ReadTagRelated(JsonElement json)
		{
			return new TagRelated(
				json[0].GetString(),
				uint.Parse(json[1].GetString()));
		}

		/// <inheritdoc/>
		protected override Wiki ReadWiki(JsonElement json)
		{
			return new Wiki(
				uint.Parse(json.GetProperty("id").GetString()),
				json.GetProperty("title").GetString(),
				json.GetProperty("body").GetString());
		}

		#endregion Protected Override Method
	}
}
