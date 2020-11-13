using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Xbooru : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Xbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Xbooru(HttpClient httpClient = null) : base("https://xbooru.com/", httpClient)
		{

		}

		#endregion Constructor & Destructor
	}
}
