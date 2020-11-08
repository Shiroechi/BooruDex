using System;
using System.Collections.Generic;
using System.Text;

namespace BooruDex.Models
{
    /// <summary>
    /// Represent Wiki entry. 
    /// </summary>
	public struct Wiki
	{
        /// <summary>
        /// Create a instance of <see cref="Wiki"/>.
        /// </summary>
        /// <param name="id">The ID of the wiki entry.</param>
        /// <param name="title">The name of the wiki</param>
        /// <param name="creation">The date when the wiki entry was created.</param>
        /// <param name="lastUpdate">The date of the latest update to the wiki entry.</param>
        /// <param name="body">The wiki description.</param>
        public Wiki(uint id, string title, DateTime creation, DateTime lastUpdate, string body)
        {
            ID = id;
            Title = title;
            Creation = creation;
            LastUpdate = lastUpdate;
            Body = body;
        }

        /// <summary>
        /// Gets the ID of the wiki entry.
        /// </summary>
        public uint ID { private set; get; }

        /// <summary>
        /// Gets the name of the described tag.
        /// </summary>
        public string Title { private set; get; }

        /// <summary>
        /// Gets the date when the wiki entry was created.
        /// </summary>
        public DateTime Creation { private set; get; }

        /// <summary>
        /// Gets the date of the latest update to the wiki entry.
        /// </summary>
        public DateTime LastUpdate { private set; get; }

        /// <summary>
        /// Gets the tag description.
        /// </summary>
        public string Body { private set; get; }

    }
}
