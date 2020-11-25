using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Realbooru : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Realbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Realbooru(HttpClient httpClient = null) : base("https://realbooru.com/", httpClient)
		{

		}

		#endregion Constructor & Destructor
	}
}
