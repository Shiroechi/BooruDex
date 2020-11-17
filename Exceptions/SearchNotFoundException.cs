using System;

namespace BooruDex.Exceptions
{
	/// <summary>
	/// Error that occurs if no search results are found.
	/// </summary>
	public class SearchNotFoundException : Exception
	{
		public SearchNotFoundException(string msg) : base(msg)
		{

		}
	}
}
