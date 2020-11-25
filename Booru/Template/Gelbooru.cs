﻿using System.Net.Http;
using System.Text;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Gelbooru.
	/// </summary>
	public abstract class Gelbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Gelbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Gelbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Gelbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
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
		~Gelbooru()
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

		#endregion Protected Overrride Method

	}
}
