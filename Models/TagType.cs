namespace BooruDex.Models
{
	/// <summary>
	/// Tag is keywords you can use to describe posts.
	/// </summary>
	public enum TagType
	{
		/// <summary>
		/// Artist tags identify the tag as the artist. 
		/// This doesn't mean the artist of the original copyrighted artwork.
		/// </summary>
		Artist = 1,
		/// <summary>
		/// Character tags identify the tag as a character.
		/// </summary>
		Character = 4,
		/// <summary>
		/// The copyright type indicates the tag represents an anime, 
		/// a game, a novel, or some sort of copyrighted setting. 
		/// Otherwise they work identically to character and artist tags.
		/// </summary>
		Copyright = 3,
		/// <summary>
		/// General tags are used for everything else. 
		/// General tags should objectively describe the contents of the post.
		/// </summary>
		General = 0,
		/// <summary>
		/// Meta tags generally describe things beyond the content of the image itself. 
		/// Examples include translated, copyright request, duplicate, image sample, and bad id.
		/// </summary>
		Metadata,
		/// <summary>
		/// There's something wrong with the post.
		/// </summary>
		Faults = 6,
		/// <summary>
		/// Circle tags are meta-copyrights. 
		/// Most are artist circles (artist collectives) or studios 
		/// like key which are in a way also artist circles. 
		/// Also included are publications like Megami and NyanType. 
		/// These are used ONLY for official artwork (produced or published by...)
		/// unless otherwise stated in the wiki (eg. Nintendo is an exception).
		/// </summary>
		Circle = 5
	}
}
