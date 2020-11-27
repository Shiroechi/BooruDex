using System;

namespace BooruDex2.Exceptions
{
	/// <summary>
	/// Error that occurs if no search results are found.
	/// </summary>
	public class SearchNotFoundException : Exception
	{
		public SearchNotFoundException(string msg) : base(msg)
		{

		}

		public SearchNotFoundException(string msg, Exception e) : base(msg, e)
		{

		}
	}
}
