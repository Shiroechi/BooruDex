using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Xbooru : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		///		Create <see cref="Xbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">
		///		Http client for sending request and recieving response.
		///	</param>
		public Xbooru(HttpClient httpClient = null) : base("xbooru.com", true, httpClient)
		{
			this._PageLimit = 0; // no page limit
		}

		#endregion Constructor & Destructor
	}
}
