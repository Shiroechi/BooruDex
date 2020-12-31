namespace BooruDex.Models
{
	/// <summary>
	///		Represent a Tag object.
	/// </summary>
	public class Tag
	{
		/// <summary>
		///		Initialize <see cref="Tag"/> instance
		/// </summary>
		/// <param name="id">
		///		The ID of the <see cref="Tag"/>.
		///	</param>
		/// <param name="name">
		///		The name of the <see cref="Tag"/>.
		/// </param>
		/// <param name="type">
		///		The type of the <see cref="Tag"/>.
		/// </param>
		/// <param name="count">
		///		The number of occurences of the <see cref="Tag"/>.
		/// </param>
		public Tag(uint id = 0, string name = "", TagType type = TagType.Undefined, uint count = 0)
		{
			this.ID = id;
			this.Name = name;
			this.Type = type;
			this.Count = count;
		}

		/// <summary>
		///		Gets the ID of the <see cref="Tag"/>.
		/// </summary>
		public uint ID { internal set; get; }

		/// <summary>
		///		Gets the name of the <see cref="Tag"/>.
		/// </summary>
		public string Name { internal set; get; }

		/// <summary>
		///		Gets the type of the <see cref="Tag"/>.
		/// </summary>
		public TagType Type { internal set; get; }

		/// <summary>
		///		Gets the number of occurences of the <see cref="Tag"/>.
		/// </summary>
		public uint Count { internal set; get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
