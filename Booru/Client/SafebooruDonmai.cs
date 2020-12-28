using System.Net.Http;

namespace BooruDex.Booru.Client
{
	public class SafebooruDonmai : Template.Danbooru
	{
		#region Constructor & Destructor

		/// <summary>
		///		Create <see cref="SafebooruDonmai"/> client object.
		/// </summary>
		/// <param name="httpClient">
		///		Http client for sending request and recieving response.
		///	</param>
		public SafebooruDonmai(HttpClient httpClient = null) : base("https://safebooru.donmai.us/", httpClient)
		{
			this.IsSafe = true;
		}

		#endregion Constructor & Destructor
	}
}
