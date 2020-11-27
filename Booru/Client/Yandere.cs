using System.Net.Http;

using BooruDex2.Booru.Template;

namespace BooruDex2.Booru.Client
{
	public class Yandere : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Yandere"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Yandere(HttpClient httpClient = null) : base("https://yande.re/", httpClient)
		{
			this._PasswordSalt = "choujin-steiner--{}--";
		}

		#endregion Constructor & Destructor
	}
}
