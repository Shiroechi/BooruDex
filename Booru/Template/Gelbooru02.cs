using System.Net.Http;
using System.Text;
using System.Text.Json;

using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Gelbooru beta version 0.2.0.
	/// </summary>
	public abstract class Gelbooru02 : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Gelbooru02"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru02(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng)
		{
			this._PostLimit = 100;
			this._TagsLimit = 0; // no tag limit
			this.IsSafe = false;
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
			var imageName = json.GetProperty("image").GetString();

			var directory = json.GetProperty("directory").GetString();

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

		#endregion Protected Overrride Method
	}
}
