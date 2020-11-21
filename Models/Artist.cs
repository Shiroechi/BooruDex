using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BooruDex.Models
{
	/// <summary>
	/// Represents a Artist object.
	/// </summary>
	public struct Artist
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"><see cref="Artist"/> id.</param>
		/// <param name="name"><see cref="Artist"/> name.</param>
		/// <param name="urls">List of <see cref="Artist"/> urls.</param>
		public Artist(uint id, string name, IList<string> urls)
		{
			this.ID = id;
			this.Name = name;
			this.Urls = new ReadOnlyCollection<string>(urls);
		}

		/// <summary>
		/// Gets the ID of the artist.
		/// </summary>
		public uint ID { private set; get; }

		/// <summary>
		/// Gets the name of the artist.
		/// </summary>
		public string Name { private set; get; }

		public ReadOnlyCollection<string> Urls { private set; get; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}
