using System.Net.Http;

namespace BooruDex.Booru.Client
{
	public class Danbooru : Template.Danbooru
	{
		/// <summary>
		/// Create <see cref="Danbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Danbooru(HttpClient httpClient = null) : base("https://danbooru.donmai.us/", httpClient)
		{

		}
	}
}
