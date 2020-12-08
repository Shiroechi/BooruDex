using System.Net.Http;

using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Rule34 : Gelbooru02
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Rule34"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Rule34(HttpClient httpClient = null) : base("https://rule34.xxx/", httpClient)
		{
			this._PageLimit = 200000;
		}

		#endregion Constructor & Destructor
	}
}
