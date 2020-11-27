using System;
using System.Net.Http;

namespace BooruDex2.Exceptions
{
	/// <summary>
	/// Error that occur whenever <see cref="HttpResponseMessage"/> is not OK.
	/// </summary>
	public class HttpResponseException : Exception
	{
		/// <summary>
		/// Create <see cref="HttpResponseException"/>.
		/// </summary>
		/// <param name="msg">Error message.</param>
		public HttpResponseException(string msg) : base(msg)
		{

		}

		/// <summary>
		/// Create <see cref="HttpResponseException"/>.
		/// </summary>
		/// <param name="msg">Error message.</param>
		/// <param name="e">Exception.</param>
		public HttpResponseException(string msg, Exception e) : base(msg, e.InnerException)
		{

		}
	}
}
