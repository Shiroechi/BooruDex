namespace BooruDex.Models
{
	/// <summary>
	/// Represents a Artist object.
	/// </summary>
	public struct Pool
	{
		public Pool(uint id, string name, uint postCount, string description)
		{
			this.ID = id;
			this.Name = name;
			this.PostCount = postCount;
			this.Description = description;
		}

		/// <summary>
		/// Gets the ID of the pool.
		/// </summary>
		public uint ID { private set; get; }

		/// <summary>
		/// Gets the name of the pool.
		/// </summary>
		public string Name { private set; get; }

		/// <summary>
		/// Gets the number of post in the pool.
		/// </summary>
		public uint PostCount { private set; get; }

		/// <summary>
		/// Gets the description of the pool.
		/// </summary>
		public string Description { private set; get; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}
