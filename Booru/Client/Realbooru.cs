using System.Net.Http;
using System.Text.Json;

using BooruDex.Booru.Template;
using BooruDex.Models;

namespace BooruDex.Booru.Client
{
	public class Realbooru : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		///		Create <see cref="Realbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">
		///		Http client for sending request and recieving response.
		///	</param>
		public Realbooru(HttpClient httpClient = null) : base("https://realbooru.com/", httpClient)
		{
			this._PageLimit = 200000;
		}

		#endregion Constructor & Destructor

		#region Protected Override Method

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			var imageName = json.GetProperty("image").GetString();

			var directory = json.GetProperty("directory").GetInt32();

			return new Post(
				json.GetProperty("id").GetUInt32(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				this._BaseUrl + "images/" + directory + "/" + imageName,
				this._BaseUrl + "thumbnails/" + directory + "/thumbnail_" + imageName.Substring(0, imageName.IndexOf(".")) + ".jpg",
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				0,
				json.GetProperty("height").GetInt32(),
				json.GetProperty("width").GetInt32(),
				0,
				0,
				string.Empty);
		}

		#endregion Protected Override Method
	}
}
