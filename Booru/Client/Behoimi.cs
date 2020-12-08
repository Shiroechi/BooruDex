using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	/// <summary>
	/// 3dbooru client.
	/// </summary>
	public class Behoimi : Moebooru
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Behoimi"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Behoimi(HttpClient httpClient = null) : base("http://behoimi.org/", httpClient)
		{
			this._PasswordSalt = "meganekko-heaven--{}--";
			this._PageLimit = 1000;
		}

		#endregion Constructor & Destructor
	}
}
