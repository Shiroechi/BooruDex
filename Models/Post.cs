namespace BooruDex.Models
{
	/// <summary>
	///		Represents a Post object.
	/// </summary>
	readonly public struct Post
	{
		/// <summary>
		///		Initialize <see cref="Post"/> instance.
		/// </summary>
		/// <param name="id">
		///		The ID of the <see cref="Post"/>.
		/// </param>
		/// <param name="postUrl">
		///		The URL of the <see cref="Post"/>.
		/// </param>
		/// <param name="fileUrl">
		///		The URL of the file.
		/// </param>
		/// <param name="previewUrl">
		///		The URL of the preview image.
		/// </param>
		/// <param name="rating">
		///		The <see cref="Post"/>'s <see cref="Rating"/>.
		/// </param>
		/// <param name="tags">
		///		The array containing all the <see cref="Tag"/> associated with the file.
		/// </param>
		/// <param name="size">
		///		The size of the file, in bytes.
		/// </param>
		/// <param name="height">
		///		The height of the image, in pixels.
		/// </param>
		/// <param name="width">
		///		The width of the image, in pixels.
		/// </param>
		/// <param name="previewHeight">
		///		The height of the preview image, in pixels.
		/// </param>
		/// <param name="previewWidth">
		///		The width of the preview image, in pixels.
		/// </param>
		/// <param name="source">
		///		The URL of the original file.
		/// </param>
		public Post(
			uint id,
			string postUrl,
			string fileUrl,
			string previewUrl,
			Rating rating,
			string tags,
			uint size,
			int height,
			int width,
			int? previewHeight,
			int? previewWidth,
			string source)
		{
			this.ID = id;
			this.FileUrl = fileUrl;
			this.PreviewUrl = previewUrl;
			this.PostUrl = postUrl + this.ID;
			this.Rating = rating;
			this.Tags = tags;
			this.Size = size;
			this.Height = height;
			this.Width = width;
			this.PreviewHeight = previewHeight;
			this.PreviewWidth = previewWidth;
			this.Source = source;
		}

		/// <summary>
		/// Gets the ID of the post.
		/// </summary>
		public uint ID { get; }

		/// <summary>
		///		Gets the URI of the <see cref="Post"/>.
		/// </summary>
		public string PostUrl { get; }

		/// <summary>
		///		Gets the URL of the file.
		/// </summary>
		public string FileUrl { get; }

		/// <summary>
		///		Gets the URL of the preview image.
		/// </summary>
		public string PreviewUrl { get; }

		/// <summary>
		///		Gets the <see cref="Post"/>'s <see cref="Rating"/>.
		/// </summary>
		public Rating Rating { get; }

		/// <summary>
		///		Gets the collection containing all the <see cref="Tag"/> associated with the file.
		/// </summary>
		public string Tags { get; }

		/// <summary>
		///		Gets the size of the file, in bytes, or
		///		<see langword="null"/> if file size is unknown.
		/// </summary>
		public uint Size { get; }

		/// <summary>
		///		Gets the height of the image, in pixels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		///		Gets the width of the image, in pixels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		///		Gets the height of the preview image, in pixels,
		///		or <see langword="null"/> if the height is unknown.
		/// </summary>
		public int? PreviewHeight { get; }

		/// <summary>
		///		Gets the width of the preview image, in pixels,
		///		or <see langword="null"/> if the width is unknown.
		/// </summary>
		public int? PreviewWidth { get; }

		/// <summary>
		///		Gets URL of original file.
		/// </summary>
		public string Source { get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.ID.ToString();
		}
	}
}
