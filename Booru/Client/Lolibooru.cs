using System.Net.Http;

using BooruDex.Booru.Template;
using BooruDex.Models;

using Newtonsoft.Json.Linq;

namespace BooruDex.Booru.Client
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
		protected override Tag ReadTag(JToken json)
		{
			var item = json;
			return new Tag(
				item["id"].Value<uint>(),
				item["name"].Value<string>(),
				(TagType)item["tag_type"].Value<int>(),
				item["post_count"].Value<uint>()
				);
		}

		#endregion Protected Override Method
	}
}
