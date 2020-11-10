using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Lolibooru : Moebooru
	{
		/// <summary>
		/// Create <see cref="Lolibooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Lolibooru(HttpClient httpClient = null) : base("http://lolibooru.moe/", httpClient)
		{
			// lolibooru tag limit can more than 6
		}
	}
}
