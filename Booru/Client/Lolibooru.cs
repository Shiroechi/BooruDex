using System.Collections.Generic;
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
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				json.GetProperty("id").GetUInt32(),
				json.GetProperty("name").GetString(),
				(TagType)json.GetProperty("tag_type").GetInt32(),
				json.GetProperty("post_count").GetUInt32()
				);
		}

		#endregion Protected Override Method
	}
}
