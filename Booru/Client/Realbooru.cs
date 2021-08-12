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
		public Realbooru(HttpClient httpClient = null) : base("realbooru.com", true, httpClient)
		{
			this._PageLimit = 200000;
		}

		#endregion Constructor & Destructor

		#region Read JSON to convert it into object

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			var imageName = json.GetProperty("image").GetString();

			var directory = json.GetProperty("directory").GetInt32();

			return new Post(
				id: json.GetProperty("id").GetUInt32(),
				postUrl: this._BaseUrl + "index.php?page=post&s=view&id=",
				fileUrl: this._BaseUrl + "images/" + directory + "/" + imageName,
				previewUrl: this._BaseUrl + "thumbnails/" + directory + "/thumbnail_" + imageName.Substring(0, imageName.IndexOf(".")) + ".jpg",
				rating: this.ConvertRating(json.GetProperty("rating").GetString()),
				tags: json.GetProperty("tags").GetString(),
				size: 0,
				height: json.GetProperty("height").GetInt32(),
				width: json.GetProperty("width").GetInt32(),
				previewHeight: 0,
				previewWidth: 0,
				source: string.Empty);
		}

		#endregion Read JSON to convert it into object
	}
}
