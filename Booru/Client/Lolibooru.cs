using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

using BooruDex.Booru.Template;
using BooruDex.Models;

namespace BooruDex.Booru.Client
{
	public class Lolibooru : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		///		Create <see cref="Lolibooru"/> client object.
		/// </summary>
		/// <param name="httpClient">
		///		Http client for sending request and recieving response.
		///	</param>
		public Lolibooru(HttpClient httpClient = null) : base("lolibooru.moe", true, httpClient)
		{
			this._TagsLimit = 0; // not tag limit
								 //this._PasswordSalt = "--{}--";
		}

		#endregion Constructor & Destructor

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
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Name = json.GetProperty("name").GetString(),
				Urls = new ReadOnlyCollection<string>(urls)
			};
		}

		/// <inheritdoc/>
		protected override Pool ReadPool(JsonElement json)
		{
			return new Pool
			{
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Name = json.GetProperty("name").GetString(),
				PostCount = uint.Parse(json.GetProperty("post_count").GetString()),
				Description = json.GetProperty("description").GetString()
			};
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag
			{
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Name = json.GetProperty("name").GetString(),
				Type = (TagType)json.GetProperty("tag_type").GetInt32(),
				Count = int.Parse(json.GetProperty("post_count").GetString())
			};
		}

		/// <inheritdoc/>
		protected override TagRelated ReadTagRelated(JsonElement json)
		{
			return new TagRelated
			{
				Name = json[0].GetString(),
				Count = uint.Parse(json[1].GetString())
			};
		}

		/// <inheritdoc/>
		protected override Wiki ReadWiki(JsonElement json)
		{
			return new Wiki
			{
				ID = uint.Parse(json.GetProperty("id").GetString()),
				Title = json.GetProperty("title").GetString(),
				Body = json.GetProperty("body").GetString()
			};
		}

		#endregion Read JSON to convert it into object
	}
}
