using System;

namespace BooruDex.Models
{
	/// <summary>
	/// Represents a Post object.
	/// </summary>
	public struct Post
	{
		/// <summary>
		/// Create <see cref="Post"/> object.
		/// </summary>
		/// <param name="fileUrl">The URI of the file.</param>
		/// <param name="previewUrl">The URI of the preview image.</param>
		/// <param name="postUrl">The URI of the post.</param>
		/// <param name="rating">The post's rating.</param>
		/// <param name="tags">The array containing all the tags associated with the file.</param>
		/// <param name="id">The ID of the post.</param>
		/// <param name="size">The size of the file, in bytes.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="previewHeight">The height of the preview image, in pixels.</param>
		/// <param name="previewWidth">The width of the preview image, in pixels.</param>
		/// <param name="creation">The creation date of the post.</param>
		/// <param name="source">The original source of the file.</param>
		/// <param name="score">The score of the post.</param>
		/// <param name="md5">The MD5 hash of the file.</param>
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
			this.FileUrl = new Uri(fileUrl);
			this.PreviewUrl = new Uri(previewUrl);
			this.PostUrl = new Uri(postUrl + this.ID);
			this.Rating = rating;
			this.Tags = tags;
			this.Size = size;
			this.Height = height;
			this.Width = width;
			this.PreviewHeight = previewHeight;
			this.PreviewWidth = previewWidth;
			this.Source = source == null? null : new Uri(source);
		}

		/// <summary>
		/// Gets the ID of the post.
		/// </summary>
		public uint ID { private set;  get; }

		/// <summary>
		/// Gets the URI of the post.
		/// </summary>
		public Uri PostUrl { private set; get; }

		/// <summary>
		/// Gets the URI of the file.
		/// </summary>
		public Uri FileUrl { private set;  get; }

		/// <summary>
		/// Gets the URI of the preview image.
		/// </summary>
		public Uri PreviewUrl { private set; get; }

		/// <summary>
		/// Gets the post's rating.
		/// </summary>
		public Rating Rating { private set; get; }

		/// <summary>
		/// Gets the collection containing all the tags associated with the file.
		/// </summary>
		public string Tags { private set; get; }

		/// <summary>
		/// Gets the size of the file, in bytes, or
		/// <see langword="null"/> if file size is unknown.
		/// </summary>
		public uint Size { private set; get; }

		/// <summary>
		/// Gets the height of the image, in pixels.
		/// </summary>
		public int Height { private set; get; }

		/// <summary>
		/// Gets the width of the image, in pixels.
		/// </summary>
		public int Width { private set; get; }

		/// <summary>
		/// Gets the height of the preview image, in pixels,
		/// or <see langword="null"/> if the height is unknown.
		/// </summary>
		public int? PreviewHeight { private set; get; }

		/// <summary>
		/// Gets the width of the preview image, in pixels,
		/// or <see langword="null"/> if the width is unknown.
		/// </summary>
		public int? PreviewWidth { private set; get; }

		/// <summary>
		/// Gets the original source of the file.
		/// </summary>
		public Uri Source { private set; get; }
	}
}
