namespace BooruDex.Models
{
    /// <summary>
    /// Represent a Tag object.
    /// </summary>
    readonly public struct Tag
	{
        /// <summary>
        /// Create a instance of <see cref="Tag"/>
        /// </summary>
        /// <param name="id">The ID of the tag.</param>
        /// <param name="name">The name of the tag.</param>
        /// <param name="type">The type of the tag.</param>
        /// <param name="count">The number of occurences of the tag.</param>
        public Tag(uint id, string name, TagType type, uint count)
        {
            this.ID = id;
            this.Name = name;
            this.Type = type;
            this.Count = count;
        }

        /// <summary>
        /// Gets the ID of the tag.
        /// </summary>
        public uint ID { get; }

        /// <summary>
        /// Gets the name of the tag.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the tag.
        /// </summary>
        public TagType Type { get; }

        /// <summary>
        /// Gets the number of occurences of the tag.
        /// </summary>
        public uint Count { get; }

        /// <inheritdoc/>
        public override string ToString()
		{
            return this.Name;
		}
	}
}
