namespace BooruDex.Models
{
	/// <summary>
	///		Represent Wiki entry. 
	/// </summary>
	public struct Wiki
	{
		/// <summary>
		///		Initialize <see cref="Wiki"/> instance.
		/// </summary>
		/// <param name="id">
		///		The ID of the <see cref="Wiki"/> entry.
		///	</param>
		/// <param name="title">
		///		The name or title of the <see cref="Wiki"/>.
		///	</param>
		/// <param name="body">
		///		The <see cref="Wiki"/> description.
		///	</param>
		public Wiki(uint id = 0, string title = "", string body = "")
		{
			this.ID = id;
			this.Title = title;
			this.Body = body;
		}

		/// <summary>
		///		Gets the ID of the <see cref="Wiki"/> entry.
		/// </summary>
		public uint ID { internal set; get; }

		/// <summary>
		///		Gets the name ot title of the <see cref="Wiki"/>.
		/// </summary>
		public string Title { internal set; get; }

		/// <summary>
		///		Gets the <see cref="Wiki"/> description.
		/// </summary>
		public string Body { internal set; get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Title;
		}
	}
}
