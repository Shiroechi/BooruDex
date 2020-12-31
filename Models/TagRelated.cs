namespace BooruDex.Models
{
	/// <summary>
	///		Represent a Tag Related object.
	/// </summary>
	public class TagRelated
	{
		/// <summary>
		///		Initialize <see cref="TagRelated"/> instance.
		/// </summary>
		/// <param name="name">
		///		The name of the <see cref="Tag"/>.
		///	</param>
		/// <param name="count">
		///		The number of occurences of the <see cref="Tag"/>.
		///	</param>
		public TagRelated(string name = "", uint count = 0)
		{
			this.Name = name;
			this.Count = count;
		}

		/// <summary>
		///		Gets the name of the <see cref="Tag"/>.
		/// </summary>
		public string Name { internal set; get; }

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
