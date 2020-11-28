using System.Net.Http;

namespace BooruDex.Booru.Client
{
	public class DanbooruDonmai : Template.Danbooru
	{
		#region Constructor & Destructor
		
		/// <summary>
		/// Create <see cref="DanbooruDonmai"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public DanbooruDonmai(HttpClient httpClient = null) : base("https://danbooru.donmai.us/", httpClient)
		{

		}
		
		#endregion Constructor & Destructor
	}
}
