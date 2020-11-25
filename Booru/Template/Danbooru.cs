using System.Net.Http;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Danbooru, A taggable image board.
	/// </summary>
	public abstract class Danbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Danbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Danbooru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new SplitMix64())
		{

		}

		/// <summary>
		/// <see cref="Danbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Danbooru(string domain, HttpClient httpClient, IRNG rng) : base(domain, httpClient, rng)
		{
			this._PostLimit = 200;
			this._TagsLimit = 2;
			this._PageLimit = 10;
			this.IsSafe = false;
			this._ApiVersion = "";
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Danbooru()
		{

		}

		#endregion Constructor & Destructor

		#region Protected Overrride Method

		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			if (json)
			{
				return $"{ this._BaseUrl.AbsoluteUri }{ query }.json?";
			}
			return $"{ this._BaseUrl.AbsoluteUri }{ query }.xml?";
		}

		#endregion Protected Overrride Method
	}
}
