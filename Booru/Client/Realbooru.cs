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
			
			return new Post
			{
				ID = json.GetProperty("id").GetUInt32(),
				PostUrl = this._BaseUrl + "index.php?page=post&s=view&id=",
				FileUrl = this._BaseUrl + "images/" + directory + "/" + imageName,
				PreviewUrl = this._BaseUrl + "thumbnails/" + directory + "/thumbnail_" + imageName.Substring(0, imageName.IndexOf(".")) + ".jpg",
				Rating = this.ConvertRating(json.GetProperty("rating").GetString()),
				Tags = json.GetProperty("tags").GetString(),
				Size = 0,
				Height = json.GetProperty("height").GetInt32(),
				Width = json.GetProperty("width").GetInt32(),
				PreviewHeight = 0,
				PreviewWidth = 0,
				Source = string.Empty
			};
		}

		#endregion Protected Override Method
	}
}
