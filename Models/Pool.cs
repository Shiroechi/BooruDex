namespace BooruDex.Models
{
	/// <summary>
	///		Represents a Pool object.
	/// </summary>
	public class Pool
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
		public Pool(uint id = 0, string name = "", uint postCount = 0, string description = "")
		{
			this.ID = id;
			this.Name = name;
			this.PostCount = postCount;
			this.Description = description;
		}

		/// <summary>
		///		Gets the ID of the <see cref="Pool"/>.
		/// </summary>
		public uint ID { internal set; get; }

		/// <summary>
		///		Gets the name of the <see cref="Pool"/>.
		/// </summary>
		public string Name { internal set; get; }

		/// <summary>
		///		Gets the number of <see cref="Post"/> in the <see cref="Pool"/>.
		/// </summary>
		public uint PostCount { internal set; get; }

		/// <summary>
		///		Gets the description of the <see cref="Pool"/>.
		/// </summary>
		public string Description { internal set; get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
