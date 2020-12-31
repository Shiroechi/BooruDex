using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BooruDex.Models
{
	/// <summary>
	///		Represents a Artist object.
	/// </summary>
	public class Artist
	{
		/// <summary>
		///		Initialize <see cref="Artist"/> instance.
		/// </summary>
		/// <param name="id">
		///		The <see cref="Artist"/> id.
		///	</param>
		/// <param name="name">
		///		The <see cref="Artist"/> name.
		///	</param>
		/// <param name="urls">
		///		List of <see cref="Artist"/> urls.
		///	</param>
		public Artist(uint id, string name, IList<string> urls)
		{
			this.ID = id;
			this.Name = name;
			this.Urls = new ReadOnlyCollection<string>(urls);
		}

		/// <summary>
		///		Gets the ID of the artist.
		/// </summary>
		public uint ID { internal set; get; }

		/// <summary>
		///		Gets the name of the artist.
		/// </summary>
		public string Name { internal set; get; }

		/// <summary>
		///		List of <see cref="Artist"/> official url.
		/// </summary>
		public ReadOnlyCollection<string> Urls { internal set; get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
