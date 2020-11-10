using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Konachan : Moebooru
	{
		/// <summary>
		/// Create <see cref="Konachan"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Konachan(HttpClient httpClient = null) : base("http://konachan.com/", httpClient)
		{

		}
	}
}
