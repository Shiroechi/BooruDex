using System.Net.Http;
using System.Text;

using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

using Newtonsoft.Json.Linq;

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
		public Gelbooru02(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Gelbooru02"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru02(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
		{
			this._PostLimit = 100;
			this._TagsLimit = 0; // no tag limit
			this._PageLimit = 10;
			this.IsSafe = false;
			this._ApiVersion = "";
			this._PasswordSalt = "";
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Gelbooru02()
		{

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
		protected override Post ReadPost(JToken json)
		{
			return new Post(
				json["id"].Value<uint>(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				json["file_url"].Value<string>(),
				this._BaseUrl + "thumbnails/" + json["directory"].Value<string>() + "/thumbnail_" + json["hash"].Value<string>() + ".jpg",
				this.ConvertRating(json["rating"].Value<string>()),
				json["tags"].Value<string>(),
				0,
				json["height"].Value<int>(),
				json["width"].Value<int>(),
				0,
				0,
				json["source"].Value<string>()
				);
		}

		#endregion Protected Overrride Method

	}
}
