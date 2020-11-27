using System.Net.Http;

using BooruDex2.Booru.Template;

namespace BooruDex2.Booru.Client
{
	public class Safebooru : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Safebooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Safebooru(HttpClient httpClient = null) : base("https://safebooru.org/", httpClient)
		{
			this.IsSafe = true;
		}

		#endregion Constructor & Destructor
	}
}
