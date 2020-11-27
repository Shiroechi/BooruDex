namespace BooruDex2.Models
{
    /// <summary>
    /// Represents a level of explicit content of the post.
    /// </summary>
    public enum Rating
	{
        /// <summary>
        /// The post dont have a rating.
        /// </summary>
        None,
        /// <summary>
        /// Safe posts are images that you would not feel guilty looking at openly in public. 
        /// Pictures of nudes, exposed nipples or pubic hair, cameltoe, or any sort of sexually suggestive pose are NOT safe and belong in questionable. 
        /// Swimsuits and lingerie are borderline cases; some are safe, some are questionable.
        /// </summary>
        Safe,
        /// <summary>
        /// Basically anything that isn't safe or explicit. 
        /// This is the great middle area, and since it includes unrated posts, you shouldn't really expect anything one way or the other when browsing questionable posts.
        /// </summary>
        Questionable,
        /// <summary>
        /// Any image where the vagina or penis are exposed and easily visible. 
        /// This includes depictions of sex, masturbation, or any sort of penetration.
        /// Literally NSFW.
        /// </summary>
        Explicit
    }
}
