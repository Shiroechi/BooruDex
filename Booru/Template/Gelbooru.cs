using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru.Template
{
	/// <summary>
	/// Gelbooru.
	/// </summary>
	public abstract class Gelbooru : Booru
	{
		#region Constructor & Destructor

		/// <summary>
		/// <see cref="Gelbooru"/> template for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Gelbooru(string domain, HttpClient httpClient = null, IRNG rng = null) : base(domain, httpClient, rng == null ? new SplitMix64() : rng)
		{
			this.IsSafe = false;
			this.HasTagApi = true;
			this._PostLimit = 100;
			this._TagsLimit = 0; // no tag limit
			this._PageLimit = 10;
			this._ApiVersion = "";
			this._PasswordSalt = "";
		}

		#endregion Constructor & Destructor

		#region Protected Overrride Method

		/// <inheritdoc/>
		protected override string CreateBaseApiCall(string query, bool json = true)
		{
			var sb = new StringBuilder($"{ this._BaseUrl.AbsoluteUri }index.php?page=dapi&s={ query }&q=index");
			
			if (json)
			{
				sb.Append("&json=1");
			}

			return sb.ToString();
		}

		/// <inheritdoc/>
		protected override Post ReadPost(JsonElement json)
		{
			return new Post(
				json.GetProperty("id").GetUInt32(),
				this._BaseUrl + "index.php?page=post&s=view&id=",
				json.GetProperty("file_url").GetString(),
				this._BaseUrl + "thumbnails/" + json.GetProperty("directory").GetString() + "/thumbnail_" + json.GetProperty("hash").GetString() + ".jpg",
				this.ConvertRating(json.GetProperty("rating").GetString()),
				json.GetProperty("tags").GetString(),
				0,
				json.GetProperty("height").GetInt32(),
				json.GetProperty("width").GetInt32(),
				0,
				0,
				json.GetProperty("source").GetString());
		}

		/// <inheritdoc/>
		protected override Tag ReadTag(JsonElement json)
		{
			return new Tag(
				uint.Parse(json.GetProperty("id").GetString()),
				json.GetProperty("tag").GetString(),
				this.ToTagType(json.GetProperty("type").GetString()),
				uint.Parse(json.GetProperty("count").GetString()));
		}

		/// <summary>
		/// Convert string "type" from tag JSON to <see cref="TagType"/>.
		/// </summary>
		/// <param name="tagTypeName">Tag type name.</param>
		/// <returns>
		/// <see cref="TagType"/> based on the name or 
		/// <see cref="TagType.Undefined"/> if "type" from tag JSON is not recognizable.
		/// </returns>
		protected virtual TagType ToTagType(string tagTypeName)
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;

			if (comparer.Equals(tagTypeName, "tag"))
			{
				return TagType.General;
			}

			foreach (TagType type in Enum.GetValues(typeof(TagType)))
			{
				if (comparer.Equals(tagTypeName, type.ToString()))
				{
					return type;
				}
			}

			return TagType.Undefined;
		}

		#endregion Protected Overrride Method

		#region Public Method

		#region Tag

		/// <inheritdoc/>
		public override async Task<Tag[]> TagListAsync(string name)
		{
			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateBaseApiCall("tag") +
				$"&limit={ this._PostLimit }&orderby=name&name_pattern={ name }";

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Tags with name \"{ name }\".");
			}

			var tags = new List<Tag>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				tags.Add(this.ReadTag(item));
			}

			return tags.ToArray();
		}

		#endregion Tag

		#endregion Public Method
	}
}
