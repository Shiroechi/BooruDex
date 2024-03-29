﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

using BooruDex.Booru.Template;
using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

namespace BooruDex.Booru
{
	/// <summary>
	///		Base class for all booru client.
	/// </summary>
	public abstract class Booru
	{
		#region Member

		/// <summary>
		///		Http client to send request and receive response.
		/// </summary>
		private HttpClient _HttpClient;

		/// <summary>
		///		<see langword="true"/> if <see cref="HttpClient"/> is supplied by user; <see langword="false"/> otherwise.
		/// </summary>
		private bool _HttpClientSupplied;

		/// <summary>
		///		Base API request URL.
		/// </summary>
		protected Uri _BaseUrl;

		/// <summary>
		///		Default retrieved post for each request.
		/// </summary>
		protected byte _DefaultPostLimit;

		/// <summary>
		///		Max allowed <see cref="Tag"/> to use for search a <see cref="Post"/>. 
		/// </summary>
		protected byte _TagsLimit;

		/// <summary>
		///		Max page number.
		/// </summary>
		protected uint _PageLimit;

		/// <summary>
		///		Random generator.
		/// </summary>
		protected IRNG _RNG;

		/// <summary>
		///		Version of Booru API.
		/// </summary>
		protected string _ApiVersion;

		/// <summary>
		///		Default user agent value.
		/// </summary>
		protected string _DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36";

		#endregion Member

		#region Constructor & Destructor

		/// <summary>
		///		Create base object for booru client.
		/// </summary>
		/// <param name="domain">
		///		URL of booru based sites.
		///	</param>
		///	<param name="useHttps">
		///		Using HTTPS protocol or not.
		/// </param>
		/// <param name="httpClient">
		///		Client for sending and receive http response.
		///	</param>
		/// <param name="rng">
		///		Random generator for determine random <see cref="Post"/>.
		///	</param>
		public Booru(string domain, bool useHttps = true, HttpClient httpClient = null, IRNG rng = null)
		{
			this._BaseUrl = new Uri("http" + (useHttps ? "s" : "") + "://" + domain, UriKind.Absolute);
			this.HttpClient = httpClient;
			this._RNG = rng ?? new JSF64();
			this._RNG.Reseed();
			this.DefaultApiSettings();
		}

		/// <summary>
		///		Release all resource that this object hold.
		/// </summary>
		~Booru()
		{
			if (this._HttpClientSupplied == false)
			{
				this.HttpClient.Dispose();
			}

			this._ApiVersion =
			this._DefaultUserAgent = string.Empty;
			this._DefaultPostLimit =
			this._TagsLimit = 0;
			this._PageLimit = 0;

			//this._Password =
			//this._PasswordSalt =
			//this._Username = null;
		}

		#endregion Constructor & Destructor

		#region Booru API Settings

		/// <summary>
		///		Gets or sets whether this booru contains explicit content or not.
		/// </summary>
		public bool IsSafe { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="Artist"/> API or not.
		/// </summary>
		public bool HasArtistApi { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="Pool"/> API or not.
		/// </summary>
		public bool HasPoolApi { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="Post"/> API or not.
		/// </summary>
		public bool HasPostApi { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="Tag"/> API or not.
		/// </summary>
		public bool HasTagApi { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="TagRelated"/> API or not.
		/// </summary>
		public bool HasTagRelatedApi { protected set; get; }

		/// <summary>
		///		Detemine whether this booru has <see cref="Wiki"/> API or not.
		/// </summary>
		public bool HasWikiApi { protected set; get; }

		#endregion Booru API Settings

		#region Public Properties

		/// <summary>
		///		Client for sending HTTP requests and receiving HTTP responses.
		/// </summary>
		public HttpClient HttpClient
		{
			set
			{
				if (value == null)
				{
					return;
				}
				else
				{
					this._HttpClient = value;
					this._HttpClientSupplied = true;
				}
			}
			get
			{
				if (this._HttpClient == null)
				{
					this._HttpClient = _LazyHttpClient.Value;
					this._HttpClientSupplied = false;
					this.AddHttpUserAgent();
				}
				return this._HttpClient;
			}
		}

		/// <summary>
		///		Gets or sets Booru API version.
		/// </summary>
		public string ApiVersion
		{
			protected set
			{
				if (value == null)
				{
					this._ApiVersion = "";
				}
				else
				{
					this._ApiVersion = value;
				}
			}
			get
			{
				return this._ApiVersion;
			}
		}

		/// <summary>
		///		Gets maximum <see cref="Tag"/> that this booru can process for each request.
		/// </summary>
		public byte TagsLimit
		{
			protected set
			{
				this._TagsLimit = value;
			}
			get
			{
				return this._TagsLimit;
			}
		}

		#endregion Public Properties

		#region Private Method

		private static readonly Lazy<HttpClient> _LazyHttpClient = new Lazy<HttpClient>(() =>
		{
			return new HttpClient();
		});

		private void DefaultApiSettings()
		{
			this.HasPostApi = true;
			this.HasArtistApi =
			this.HasPoolApi =
			this.HasTagApi =
			this.HasTagRelatedApi =
			this.HasWikiApi = false;
		}

		#endregion Private Method

		#region Protected Method - HTTP request and response

		/// <summary>
		///		Get JSON response from url.
		/// </summary>
		/// <typeparam name="T">
		///		The type of the object to deserialize.
		///	</typeparam>
		/// <param name="url">
		///		URL of the request. 
		/// </param>
		/// <returns>
		///		The instance of <typeparamref name="T"/> being deserialized.
		///	</returns>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="TaskCanceledException">
		///		The request failed due timeout.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		protected async Task<T> GetJsonResponseAsync<T>(string url)
		{
			try
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, url))
				using (var response = await this.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				using (var stream = await response.Content.ReadAsStreamAsync())
				{
					if (response.IsSuccessStatusCode)
					{
						try
						{
							return await JsonSerializer.DeserializeAsync<T>(stream);
						}
						catch (JsonException)
						{
							throw;
						}
					}

					throw new HttpResponseException(
						$"Unexpected error occured.\nStatus code = { (int)response.StatusCode }\nReason = { response.ReasonPhrase }.");
				}
			}
			catch (HttpRequestException)
			{
				throw;
			}
			catch (TaskCanceledException)
			{
				throw;
			}
		}

		/// <summary>
		///		Get <see cref="string"/> response from url.
		/// </summary>
		/// <param name="url">
		///		URL of request.
		/// </param>
		/// <returns>
		///		<see cref="string"/> response.
		///	</returns>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="TaskCanceledException">
		///		The request failed due timeout.
		/// </exception>
		protected async Task<string> GetStringResponseAsync(string url)
		{
			try
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, url))
				using (var response = await this.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				using (var stream = await response.Content.ReadAsStreamAsync())
				{
					if (response.IsSuccessStatusCode)
					{
						return await this.DeserializeStringFromStreamAsync(stream);
					}

					throw new HttpResponseException(
						$"Unexpected error occured.\nStatus code = { (int)response.StatusCode }\nReason = { response.ReasonPhrase }.");
				}
			}
			catch (HttpRequestException)
			{
				throw;
			}
			catch (TaskCanceledException)
			{
				throw;
			}
		}

		/// <summary>
		///		Deserializes response into string.
		/// </summary>
		/// <param name="stream">
		///		
		/// </param>
		/// <returns>
		///		<see cref="string"/> content.
		///	</returns>
		protected async Task<string> DeserializeStringFromStreamAsync(Stream stream)
		{
			if (stream != null)
			{
				using (var sr = new StreamReader(stream, Encoding.UTF8))
				{
					return await sr.ReadToEndAsync();
				}
			}

			return null;
		}

		#endregion Protected Method

		#region Create API url

		/// <summary>
		///		Create base API call url. 
		/// </summary>
		/// <param name="query">
		///		Categories.
		///	</param>
		/// <param name="json">
		///		Create JSON API or not. <see langword="true"/> for JSON.
		///	</param>
		/// <returns>
		///		URL of API request.
		/// </returns>
		protected abstract string CreateBaseApiUrl(string query, bool json = true);

		/// <summary>
		///		Create API url for search <see cref="Artist"/>.
		/// </summary>
		/// <param name="name">
		///		The name (or a fragment of the name) of the artist.
		///	</param>
		/// <param name="page">
		///		The page number.
		///	</param>
		/// <param name="sort">
		///		Sort the search result by <see cref="Artist"/> name. Default <see langword="false"/>.
		/// </param>
		/// <returns>
		///		string that contain API url.
		/// </returns>
		protected virtual string CreateArtistListUrl(string name, ushort page = 0, bool sort = false)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreateArtistListUrl method.");
		}

		/// <summary>
		///		Create API url for search <see cref="Pool"/> by title.
		/// </summary>
		/// <param name="title">
		///		The title of <see cref="Pool"/>.
		///	</param>
		/// <param name="page">
		///		The page number.
		///	</param>
		/// <returns>
		///		String that contain API url.
		///	</returns>
		protected virtual string CreatePoolListUrl(string title, uint page)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreatePoolListUrl method.");
		}

		/// <summary>
		///		Create API url to get all <see cref="Post"/> inside the <see cref="Pool"/>.
		/// </summary>
		/// <param name="poolId">
		///		The <see cref="Pool"/> id.
		///	</param>
		/// <returns>
		///		String that contain API url.
		///	</returns>
		protected virtual string CreatePoolPostListUrl(uint poolId)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreatePoolPostListUrl method.");
		}

		/// <summary>
		///		Create API url to get a list of the latest <see cref="Post"/>.
		/// </summary>
		/// <param name="limit">
		///		How many <see cref="Post"/> to retrieve.
		/// </param>
		/// <param name="page">
		///		The page number.
		/// </param>
		/// <param name="tags">
		///		The tags to search for.
		/// </param>
		/// <returns>
		///		String taht contain API url.
		/// </returns>
		protected virtual string CreatePostListUrl(byte limit, uint page = 0)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreatePostListUrl method.");
		}

		/// <summary>
		///		Create API url to search for <see cref="Tag"/> with the name is similiar or alike.
		/// </summary>
		/// <param name="name">
		///		The <see cref="Tag"/> name.
		///	</param>
		/// <returns>
		///		String that contain API url.
		/// </returns>
		protected virtual string CreateTagListUrl(string name)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreateTagListUrl method.");
		}

		/// <summary>
		///		Create API url to search for <see cref="Tag"/> that related with other <see cref="Tag"/>.	
		/// </summary>
		/// <param name="name">
		///		The <see cref="Tag"/> name.
		///	</param>
		/// <param name="type">
		///		Restrict results to search by <see cref="TagType"/> (can be general, artist, copyright, or character).
		///	</param>
		/// <returns>
		///		String that contain API url.
		/// </returns>
		protected virtual string CreateTagRelatedUrl(string name, TagType type = TagType.General)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreateTagListUrl method.");
		}

		/// <summary>
		///		Search for <see cref="Wiki"/> by title.
		/// </summary>
		/// <param name="title">
		///		<see cref="Wiki"/> title.
		///	</param>
		/// <returns>
		///		String that contain API url.
		/// </returns>
		protected virtual string CreateWikiListUrl(string title)
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreateWikiListUrl method.");
		}

		/// <summary>
		///		Create API url to get number of post with the specific tags.
		/// </summary>
		/// <returns>
		///		String that contain API url.
		/// </returns>
		protected virtual string CreatePostCountUrl()
		{
			throw new NotImplementedException($"{ this.GetDomain() } do not support CreatePostCountUrl method.");
		}

		#endregion Create API url

		#region Read JSON to convert it into object

		/// <summary>
		///		Read <see cref="Artist"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Artist"/> object.
		///	</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Artist ReadArtist(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadArtist) } is not implemented yet.");
		}

		/// <summary>
		///		Read <see cref="Pool"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Pool"/> object.
		///	</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Pool ReadPool(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadPool) } is not implemented yet.");
		}

		/// <summary>
		///		Read <see cref="Post"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Post"/> object.
		/// </returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Post ReadPost(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadPost) } is not implemented yet.");
		}

		/// <summary>
		///		Read <see cref="Tag"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Tag"/> object.
		///	</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Tag ReadTag(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadTag) } is not implemented yet.");
		}

		/// <summary>
		///		Read <see cref="TagRelated"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="TagRelated"/> object.
		///	</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual TagRelated ReadTagRelated(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadTagRelated) } is not implemented yet.");
		}

		/// <summary>
		///		Read <see cref="Wiki"/> JSON search result.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <returns>
		///		<see cref="Wiki"/> object.
		/// </returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Wiki ReadWiki(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadWiki) } is not implemented yet.");
		}

		#endregion Virtual Method

		#region Helper Method

		/// <summary>
		///		Check the property of JSON object exist or not.
		/// </summary>
		/// <param name="json">
		///		JSON object.
		///	</param>
		/// <param name="propertyName">
		///		The name of the property to find.
		///	</param>
		/// <returns>
		///		<see langword="true"/> if the property was found; otherwise, <see langword="false"/>.
		///	</returns>
		protected bool PropertyExist(JsonElement json, string propertyName)
		{
			return json.TryGetProperty(propertyName, out _);
		}

		/// <summary>
		///		Convert string rating to <see cref="Rating"/>.
		/// </summary>
		/// <param name="rating">
		///		String rating
		/// </param>
		/// <returns>
		///		<see cref="Rating"/> based on <paramref name="rating"/>.
		/// </returns>
		protected Rating ConvertRating(string rating)
		{
			switch (rating[0])
			{
				case 'E':
				case 'e':
					return Rating.Explicit;
				case 'Q':
				case 'q':
					return Rating.Questionable;
				case 'S':
				case 's':
					return Rating.Safe;
				default:
					return Rating.Questionable;
			}
		}

		/// <summary>
		///		Check pre-condition for the tags.
		/// </summary>
		/// <param name="tags">
		///		Tags to check.
		///	</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The provided <see cref="Tag"/> is more than the limit.
		/// </exception>
		protected void CheckTagsLimit(string[] tags)
		{
			if ((this._TagsLimit != 0) &&
				(tags != null) &&
				(tags.Length > this._TagsLimit))
			{
				throw new ArgumentOutOfRangeException($"Tag can't more than { this._TagsLimit } tag.");
			}
		}

		/// <summary>
		///		Check pre-condition for page number.
		/// </summary>
		/// <param name="pageNumber">
		///		Page number to check.
		///	</param>
		/// <returns>
		///		A valid page number that not lower or greater than required.
		///	</returns>
		protected uint CheckPageLimit(uint pageNumber)
		{
			if (this._PageLimit == 0)
			{
				return pageNumber;
			}

			if (pageNumber <= 0)
			{
				return 1;
			}
			else if (pageNumber > this._PageLimit)
			{
				return this._PageLimit;
			}
			else
			{
				return pageNumber;
			}
		}

		/// <summary>
		///		Check pre-condition for number of requested <see cref="Post"/>.
		/// </summary>
		/// <param name="postLimit">
		///		Number of post to check.
		///	</param>
		/// <returns>
		///		A valid number of retrieved <see cref="Post"/> that not lower or greater than required.
		///	</returns>
		protected byte CheckPostLimit(byte postLimit)
		{
			if (postLimit <= 0)
			{
				return 1;
			}
			else if (postLimit > this._DefaultPostLimit)
			{
				return this._DefaultPostLimit;
			}
			else
			{
				return postLimit;
			}
		}

		/// <summary>
		///		Get max number of <see cref="Post"/> with 
		///		the given <see cref="Tag"/> the site have.
		/// </summary>
		/// <param name="tags">
		///		<see cref="Tag"/> of the requested <see cref="Post"/>.
		///	</param>
		/// <returns>
		///		Number of <see cref="Post"/>.
		/// </returns>
		/// <exception cref="XmlException">
		///		There is a load or parse error in the XML.
		/// </exception>
		/// <exception cref="FormatException">
		///		Can't convert to <see cref="uint"/>.
		/// </exception>
		protected async Task<uint> GetPostCountAsync(string[] tags)
		{
			var url = this.CreatePostCountUrl();

			if (tags != null)
			{
				url += $"&tags={ string.Join(" ", tags) }";
			}

			try
			{
				var xml = new XmlDocument();
				xml.LoadXml(await this.GetStringResponseAsync(url));
				return uint.Parse(xml.ChildNodes.Item(1).Attributes[0].InnerXml);
			}
			catch (XmlException)
			{
				throw;
			}
			catch (FormatException)
			{
				throw;
			}
		}

		#endregion Helper Method

		#region Basic Public Method

		/// <summary>
		///		Get the current booru domain.
		/// </summary>
		/// <returns>
		///		Booru domain.
		/// </returns>
		public string GetDomain()
		{
			if (this._BaseUrl == null)
			{
				return "";
			}
			return this._BaseUrl.Host;
		}

		/// <summary>
		///		Add http user agent if not exist.
		/// </summary>
		/// <remarks>
		///		by default using google chrome browser user agent.
		/// </remarks>
		/// <param name="userAgent">
		///		User Agent value.
		///	</param>
		public void AddHttpUserAgent(string userAgent = "")
		{
			if (this._HttpClient == null)
			{
				return;
			}

			if (userAgent == null || userAgent.Trim() == "")
			{
				userAgent = this._DefaultUserAgent;
			}

			if (this._HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				this._HttpClient.DefaultRequestHeaders.Add(
					"User-Agent",
					userAgent);
			}
			else
			{
				this._HttpClient.DefaultRequestHeaders.UserAgent.Clear();
				this._HttpClient.DefaultRequestHeaders.Add(
					"User-Agent",
					userAgent);
			}
		}

		/// <summary>
		///		Check the availability of booru website.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if the website is available or reachable; <see langword="false"/> otherwise.
		/// </returns>
		public virtual bool IsOnline()
		{
			try
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, this._BaseUrl.AbsoluteUri))
				using (var response = this.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				{
					return response.GetAwaiter().GetResult().IsSuccessStatusCode;
				}
			}
			catch
			{
				return false;
			}
		}

		#endregion Basic Public Method

		#region Artist

		/// <summary>
		///		Search <see cref="Artist"/> by name.
		/// </summary>
		/// <param name="name">
		///		The name (or a fragment of the name) of the artist.
		///	</param>
		/// <param name="page">
		///		The page number.
		///	</param>
		/// <param name="sort">
		///		Sort the search result by <see cref="Artist"/> name. Default <see langword="false"/>.
		/// </param>
		/// <returns>
		///		Array of <see cref="Artist"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		One or more parameter is null or empty.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Artist"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Artist[]> ArtistListAsync(string name, ushort page = 0, bool sort = false)
		{
			if (this.HasArtistApi == false)
			{
				throw new NotImplementedException($"Method { nameof(ArtistListAsync) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty.");
			}

			var url = this.CreateArtistListUrl(name, page, sort);

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Artist with name \"{ name }\" at page { page }.");
			}

			var artists = new List<Artist>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				artists.Add(this.ReadArtist(item));
			}

			return artists.ToArray();
		}

		#endregion Artist

		#region Pool

		/// <summary>
		///		Search <see cref="Pool"/> by title.
		/// </summary>
		/// <param name="title">
		///		The title of <see cref="Pool"/>.
		///	</param>
		/// <param name="page">
		///		The page number.
		///	</param>
		/// <returns>
		///		Array of <see cref="Pool"/>.
		///	</returns>
		/// <exception cref="ArgumentNullException">
		///		The <see cref="Pool"/> title or name can't null or empty.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Pool"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Pool[]> PoolList(string title, uint page = 0)
		{
			if (this.HasPoolApi == false)
			{
				throw new NotImplementedException($"Method { nameof(PoolList) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(title))
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			page = this.CheckPageLimit(page);

			var url = this.CreatePoolListUrl(title, page);

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"Can't find Pool with title \"{ title }\".");
			}

			var pools = new List<Pool>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				pools.Add(this.ReadPool(item));
			}

			return pools.ToArray();
		}

		/// <summary>
		///		Get all <see cref="Post"/> inside the <see cref="Pool"/>.
		/// </summary>
		/// <param name="poolId">
		///		The <see cref="Pool"/> id.
		///	</param>
		/// <returns>
		///		Array of <see cref="Post"/> from <see cref="Pool"/>.
		///	</returns>
		/// <exception cref="ArgumentNullException">
		///		One or more parameter is null or empty.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Post"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Post[]> PoolPostList(uint poolId)
		{
			if (this.HasPoolApi == false)
			{
				throw new NotImplementedException($"Method { nameof(PoolPostList) } is not implemented yet.");
			}

			var url = this.CreatePoolPostListUrl(poolId);

			var posts = new List<Post>();

			try
			{
				using (var doc = await this.GetJsonResponseAsync<JsonDocument>(url))
				{
					if (this is Danbooru)
					{
						// if Pool not found, it return JSON response
						// containing a reason why it not found

						if (doc.RootElement.TryGetProperty("success", out _))
						{
							throw new SearchNotFoundException($"Can't find Pool with id { poolId }.");
						}

						// the JSON response only give the Post id
						// so we need get the Post data from another API call.

						var postIds = doc.RootElement.GetProperty("post_ids");

						if (postIds.GetArrayLength() == 0)
						{
							throw new SearchNotFoundException($"No Post inside Pool with id { poolId }.");
						}

						foreach (var id in postIds.EnumerateArray())
						{
							// TODO: Use method PostShowAsync for more detailed
							posts.Add(
								new Post(
									id.GetUInt32(),
									this._BaseUrl + "posts/", "", "", Rating.None, "", 0, 0, 0, 0, 0, ""));
						}
					}
					else if (this is Moebooru)
					{
						foreach (var item in doc.RootElement.GetProperty("posts").EnumerateArray())
						{
							posts.Add(this.ReadPost(item));
						}
					}
				}
			}
			catch (Exception e)
			{
				// if pool not found, it will return to pool page 
				// like yande.re/pool, not a empty JSON.
				throw new SearchNotFoundException($"Can't find Pool with id { poolId }.", e);
			}

			return posts.ToArray();
		}

		#endregion Pool

		#region Post

		/// <summary>
		///		Get a list of the latest <see cref="Post"/>.
		/// </summary>
		/// <param name="limit">
		///		How many <see cref="Post"/> to retrieve.
		/// </param>
		/// <param name="page">
		///		The page number.
		/// </param>
		/// <param name="tags">
		///		The tags to search for.
		/// </param>
		/// <returns>
		///		Array of <see cref="Post"/>.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The provided <see cref="Tag"/> is more than the limit.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Post"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Post[]> PostListAsync(byte limit, string[] tags, uint page = 0)
		{
			if (this.HasPostApi == false)
			{
				throw new NotImplementedException($"Method { nameof(PostListAsync) } is not implemented yet.");
			}

			this.CheckTagsLimit(tags);

			page = this.CheckPageLimit(page);

			limit = this.CheckPostLimit(limit);

			var url = this.CreatePostListUrl(limit, page);

			if (tags != null)
			{
				url += $"&tags={ string.Join(" ", tags) }";
			}

			// get Post count in XML response.
			// gelbooru and gelbooru beta 0.2 return empty page 
			// if the request is json format.

			if (this is Gelbooru || this is Gelbooru02)
			{
				var postCount = await this.GetPostCountAsync(tags);

				if (postCount == 0)
				{
					if (tags == null || tags.Length <= 0)
					{
						throw new SearchNotFoundException($"No Post found with empty tags at page { page }.");
					}
					else
					{
						throw new SearchNotFoundException($"No Post found with tags { string.Join(", ", tags) } at page { page }.");
					}
				}
			}

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				if (tags == null || tags.Length <= 0)
				{
					throw new SearchNotFoundException($"No Post found with empty tags at page { page }.");
				}
				else
				{
					throw new SearchNotFoundException($"No Post found with tags { string.Join(", ", tags) } at page { page }.");
				}
			}

			var posts = new List<Post>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				posts.Add(this.ReadPost(item));
			}

			return posts.ToArray();
		}

		/// <summary>
		///		Get a single random <see cref="Post"/> with the given tags.
		/// </summary>
		/// <param name="tags">
		///		<see cref="Tag"/> to search.
		///	</param>
		/// <returns>
		///		A random <see cref="Post"/>.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The provided <see cref="Tag"/> is more than the limit.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Post"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			if (this.HasPostApi == false)
			{
				throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
			}

			// this algorithm for gelbooru, gelbooru beta 0.2 and moebooru
			// because they do not have random post API.
			// danbooru have their separated API/

			this.CheckTagsLimit(tags);

			// get Post count in XML response.

			var postCount = await this.GetPostCountAsync(tags);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}

			// get random post with random the page number, each page 
			// limited only with 1 post.

			// there's a limit for page number,
			// more than that will return error 

			var pageNumber = this.CheckPageLimit(this._RNG.NextInt(1, postCount));

			var post = await this.PostListAsync(1, tags, pageNumber);

			return post[0];
		}

		/// <summary>
		///		Get multiple random <see cref="Post"/> with the given tags.
		/// </summary>
		/// <param name="tags">
		///		<see cref="Tag"/> to search.
		///	</param>
		/// <param name="limit">
		///		How many post to retrieve.
		///	</param>
		/// <returns>
		///		Array of <see cref="Post"/>.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The provided <see cref="Tag"/> is more than the limit.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Post"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Post[]> GetRandomPostAsync(byte limit, string[] tags = null)
		{
			if (this.HasPostApi == false)
			{
				throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
			}

			// this algorithm for gelbooru, gelbooru beta 0.2 and moebooru
			// because they do not have random post API.
			// danbooru have their separated API/

			this.CheckTagsLimit(tags);

			limit = this.CheckPostLimit(limit);

			var postCount = await this.GetPostCountAsync(tags);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(", ", tags) }.");
			}
			else if (postCount < limit)
			{
				throw new SearchNotFoundException($"The site only have { postCount } post with tags { string.Join(", ", tags) }.");
			}

			// there's a limit for page number,
			// more than that will return error 

			var maxPageNumber = this.CheckPageLimit((uint)Math.Floor(postCount / limit * 1.0));

			if (maxPageNumber == 1)
			{
				// get all post
				return await this.PostListAsync(limit, tags);
			}
			else
			{
				// maxPageNumber - 1, to ensure the leftovers post
				// in last page not included.

				maxPageNumber -= 1;

				return await this.PostListAsync(limit, tags, this._RNG.NextInt(1, maxPageNumber));
			}
		}

		#endregion Post

		#region Tag

		/// <summary>
		///		Search for <see cref="Tag"/> with the name is similiar or alike.
		/// </summary>
		/// <param name="name">
		///		The <see cref="Tag"/> name.
		///	</param>
		/// <returns>
		///		Array of <see cref="Tag"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		The provided <see cref="Tag"/> name is null or empty string.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Tag"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Tag[]> TagListAsync(string name)
		{
			if (this.HasTagApi == false)
			{
				throw new NotImplementedException($"Method { nameof(TagListAsync) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateTagListUrl(name);

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

		/// <summary>
		///		Search for <see cref="Tag"/> that related with other <see cref="Tag"/>.	
		/// </summary>
		/// <param name="name">
		///		The <see cref="Tag"/> name.
		///	</param>
		/// <param name="type">
		///		Restrict results to search by <see cref="TagType"/> (can be general, artist, copyright, or character).
		///	</param>
		/// <returns>
		///		Array of <see cref="TagRelated"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		The provided <see cref="Tag"/> name is null or empty string.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="TagRelated"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<TagRelated[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			if (this.HasTagRelatedApi == false)
			{
				throw new NotImplementedException($"Method { nameof(TagRelatedAsync) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name), "Tag name can't null or empty.");
			}

			var url = this.CreateTagRelatedUrl(name, type);

			using (var doc = await this.GetJsonResponseAsync<JsonDocument>(url))
			{
				JsonElement jsonArray;

				if (this is Danbooru)
				{
					jsonArray = doc.RootElement.GetProperty("tags");
				}
				else
				{
					// moebooru

					if (this.PropertyExist(doc.RootElement, name))
					{
						jsonArray = doc.RootElement.GetProperty(name);
					}
					else
					{
						jsonArray = doc.RootElement.GetProperty("useless_tags");
					}
				}

				if (jsonArray.GetArrayLength() == 0)
				{
					throw new SearchNotFoundException($"Can't find related Tags with Tag name \"{ name }\".");
				}

				var tags = new List<TagRelated>();

				foreach (var item in jsonArray.EnumerateArray())
				{
					tags.Add(this.ReadTagRelated(item));
				}

				return tags.ToArray();
			}
		}

		/// <summary>
		///		Check if the <see cref="Tag"/> is exist (available) or not in the booru.
		/// </summary>
		/// <param name="tag">
		///		<see cref="Tag"/> name to check.
		/// </param>
		/// <returns>
		///		<see langword="true"></see> if the <see cref="Tag"/> name is exist or availabe in the booru.
		/// </returns>
		public async Task<bool> IsTagExistAsync(string tag)
		{
			if (this.HasTagApi == false)
			{
				throw new NotImplementedException($"Method { nameof(TagListAsync) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentNullException(nameof(tag), "Tag name can't null or empty.");
			}

			try
			{
				var tags = await this.TagListAsync(tag);
				foreach (var item in tags)
				{
					if (item.Name.Equals(tag, StringComparison.CurrentCultureIgnoreCase))
					{
						return true;
					}
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		#endregion Tag

		#region Wiki

		/// <summary>
		///		Search for <see cref="Wiki"/> by title.
		/// </summary>
		/// <param name="title">
		///		<see cref="Wiki"/> title.
		///	</param>
		/// <returns>
		///		Array of <see cref="Wiki"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		The provided <see cref="Wiki"/> title is null or empty string.
		/// </exception>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		/// <exception cref="HttpResponseException">
		///		Unexpected error occured.
		/// </exception>
		/// <exception cref="HttpRequestException">
		///		The request failed due to an underlying issue such as network connectivity, DNS
		///     failure, server certificate validation or timeout.
		/// </exception>
		/// <exception cref="SearchNotFoundException">
		///		The search result is empty. No <see cref="Wiki"/> is found.
		/// </exception>
		/// <exception cref="JsonException">
		///		The JSON is invalid.
		/// </exception>
		public virtual async Task<Wiki[]> WikiListAsync(string title)
		{
			if (this.HasWikiApi == false)
			{
				throw new NotImplementedException($"Method { nameof(WikiListAsync) } is not implemented yet.");
			}

			if (string.IsNullOrWhiteSpace(title))
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			var url = this.CreateWikiListUrl(title);

			var jsonArray = await this.GetJsonResponseAsync<JsonElement>(url);

			if (jsonArray.GetArrayLength() == 0)
			{
				throw new SearchNotFoundException($"No Wiki found with title \"{ title }\"");
			}

			var wikis = new List<Wiki>();

			foreach (var item in jsonArray.EnumerateArray())
			{
				wikis.Add(this.ReadWiki(item));
			}

			return wikis.ToArray();
		}

		#endregion Wiki
	}
}
