namespace BooruDex.Models
{
	/// <summary>
	///		Represents a Artist object.
	/// </summary>
	readonly public struct Pool
	{
		/// <summary>
		///		Initialize <see cref="Pool"/> instance.
		/// </summary>
		/// <param name="id">
		///		The <see cref="Pool"/> id.
		/// </param>
		/// <param name="name">
		///		The <see cref="Pool"/> name or title.
		/// </param>
		/// <param name="postCount">
		///		Number of <see cref="Post"/> in the <see cref="Pool"/>.
		/// </param>
		/// <param name="description">
		///		Description of the <see cref="Pool"/>.
		/// </param>
		public Pool(uint id, string name, uint postCount, string description)
		{
			this.ID = id;
			this.Name = name;
			this.PostCount = postCount;
			this.Description = description;
		}

		/// <summary>
		///		Gets the ID of the <see cref="Pool"/>.
		/// </summary>
		public uint ID { get; }

		/// <summary>
		///		Gets the name of the <see cref="Pool"/>.
		/// </summary>
		public string Name { get; }

		/// <summary>
		///		Gets the number of <see cref="Post"/> in the <see cref="Pool"/>.
		/// </summary>
		public uint PostCount { get; }

		/// <summary>
		///		Gets the description of the <see cref="Pool"/>.
		/// </summary>
		public string Description { get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
