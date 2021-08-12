using System.Net.Http;
using System.Text;
using System.Text.Json;

using BooruDex.Models;

using Litdex.Security.RNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	///		Gelbooru beta version 0.2.0.
	/// </summary>
	public abstract class Gelbooru02 : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		///		<see cref="Gelbooru02"/> template for booru client.
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
		/// </param>
		public Gelbooru02(string domain, bool useHttps = true, HttpClient httpClient = null, IRNG rng = null) : base(domain, useHttps: useHttps, httpClient: httpClient, rng: rng)
		{
			this._DefaultPostLimit = 100;
			this._TagsLimit = 0; // no tag limit
			this.IsSafe = false;
			this._ApiVersion = "";
		}

		#endregion Constructor & Destructor

		#region Create API url

		/// <inheritdoc/>
		protected override string CreateBaseApiUrl(string query, bool json = true)
		{
			var sb = new StringBuilder($"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index");

			if (json)
			{
				sb.Append("&json=1");
			}

			return sb.ToString();
		}

		/// <inheritdoc/>
		protected override string CreatePostListUrl(byte limit, uint page = 0)
		{
			return this.CreateBaseApiUrl("post") +
				$"&limit={ limit }&pid={ page }";
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
			var imageName = json.GetProperty("image").GetString();

			var directory = json.GetProperty("directory").GetString();

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
