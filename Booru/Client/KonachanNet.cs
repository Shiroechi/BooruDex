using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class KonachanNet : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		///		Create <see cref="KonachanNet"/> client object.
		/// </summary>
		/// <param name="httpClient">
		///		Http client for sending request and recieving response.
		///	</param>
		public KonachanNet(HttpClient httpClient = null) : base("https://konachan.net/", httpClient)
		{
			this.IsSafe = true;
			this._PasswordSalt = "So-I-Heard-You-Like-Mupkids-?--{}--";
		}

		#endregion Constructor & Destructor
	}
}
