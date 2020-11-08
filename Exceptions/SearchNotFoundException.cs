using System;

namespace BooruDex.Exceptions
{
	/// <summary>
	/// Error that occurs if no search results are found.
	/// </summary>
	public class SearchNotFoundException : Exception
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="msg"></param>
		public SearchNotFoundException(string msg) : base(msg)
		{

		}
	}
}
