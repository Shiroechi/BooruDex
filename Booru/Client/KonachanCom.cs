using System.Net.Http;

using BooruDex2.Booru.Template;

namespace BooruDex2.Booru.Client
{
	public class KonachanCom : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="KonachanCom"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public KonachanCom(HttpClient httpClient = null) : base("https://konachan.com/", httpClient)
		{
			this._PasswordSalt = "So-I-Heard-You-Like-Mupkids-?--{}--";
		}

		#endregion Constructor & Destructor
	}
}
