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
		/// <param name="id"></param>
		/// <param name="name"></param>
		public Artist(uint id, string name)
		{
			this.ID = id;
			this.Name = name;
		}

		/// <summary>
		/// Gets the ID of the artist.
		/// </summary>
		public uint ID { private set; get; }

		/// <summary>
		/// Gets the name of the artist.
		/// </summary>
		public string Name { private set; get; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}
