using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
	/// Base class for all booru client.
	/// </summary>
	public abstract class Booru
	{
		#region Member

		/// <summary>
		/// Http client to send reuquest and receive response.
		/// </summary>
		private HttpClient _HttpClient;

		/// <summary>
		/// Base API request URL.
		/// </summary>
		protected Uri _BaseUrl;

		/// <summary>
		/// Max retrieved post for each request.
		/// </summary>
		protected byte _PostLimit;

		/// <summary>
		/// Max allowed <see cref="Tag"/>s to use for search a <see cref="Post"/>. 
		/// </summary>
		protected byte _TagsLimit;

		/// <summary>
		/// Max page number.
		/// </summary>
		protected byte _PageLimit;

		/// <summary>
		/// Random generator.
		/// </summary>
		protected IRNG _RNG;

		/// <summary>
		/// Your username of the site (Required only for 
		/// functions that modify the content).
		/// </summary>
		protected string _Username;

		/// <summary>
		///  Your user password in plain text (Required only 
		///  for functions that modify the content).
		/// </summary>
		protected string _Password;

		/// <summary>
		/// String that is append to password (required to login). 
		/// (See the API documentation of the site for more information).
		/// </summary>
		protected string _PasswordSalt;

		/// <summary>
		/// Version of Booru API.
		/// </summary>
		protected string _ApiVersion;

		/// <summary>
		/// Authentication check.
		/// </summary>
		protected bool _Authentication;

		#endregion Member

		#region Constructor & Destructor

		/// <summary>
		/// Create base object for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Booru(string domain, HttpClient httpClient = null, IRNG rng = null)
		{
			this._BaseUrl = new Uri(domain, UriKind.Absolute);
			this.HttpClient = httpClient;
			this._RNG = rng is null ? new SplitMix64() : rng;
			this.DefaultApiSettings();
			this._Authentication = false;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Booru()
		{
			this._BaseUrl = null;
			this._ApiVersion = 
				this._Password = 
				this._PasswordSalt = 
				this._Username = null;
		}

		#endregion Constructor & Destructor

		#region Properties

		/// <summary>
		/// For sending HTTP requests and receiving HTTP responses.
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
					this.AddHttpUserAgent();
				}
			}
			get
			{
				if (this._HttpClient == null)
				{
					this._HttpClient = _LazyHttpClient.Value;
					this.AddHttpUserAgent();
				}
				return this._HttpClient;
			}
		}

		/// <summary>
		/// Gets or sets Booru API version.
		/// </summary>
		public string ApiVersion
		{
			private set
			{
				this._ApiVersion = value;
			}
			get
			{
				return this._ApiVersion;
			}
		}

		#region Booru API Settings

		/// <summary>
		/// Gets or sets whether this booru contains explicit content or not.
		/// </summary>
		public bool IsSafe { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="Artist"/> API or not.
		/// </summary>
		public bool HasArtistApi { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="Pool"/> API or not.
		/// </summary>
		public bool HasPoolApi { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="Post"/> API or not.
		/// </summary>
		public bool HasPostApi { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="Tag"/> API or not.
		/// </summary>
		public bool HasTagApi { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="TagRelated"/> API or not.
		/// </summary>
		public bool HasTagRelatedApi { protected set; get; }

		/// <summary>
		/// Detemine whether this booru has <see cref="Wiki"/> API or not.
		/// </summary>
		public bool HasWikiApi { protected set; get; }

		#endregion Booru API Settings

		#endregion Properties

		#region Private Method

		private static readonly Lazy<HttpClient> _LazyHttpClient = new Lazy<HttpClient>(() =>
		{
			var http = new HttpClient();
			return http;
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

		#region Protected Method

		/// <summary>
		/// Create base API call url. 
		/// </summary>
		/// <param name="query">Categories.</param>
		/// <param name="json">Create JSON API or not. <see langword="true"/> for JSON.</param>
		/// <returns></returns>
		protected abstract string CreateBaseApiCall(string query, bool json = true);

		/// <summary>
		/// Get JSON response from url.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="url"></param>
		/// <returns>The instance of <typeparamref name="T"/> being deserialized.</returns>
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
				using (var response = await this._HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				using (var stream = await response.Content.ReadAsStreamAsync())
				{
					if (response.IsSuccessStatusCode)
					{
						try
						{
							return await JsonSerializer.DeserializeAsync<T>(stream);
						}
						catch (JsonException e)
						{
							throw e;
						}
					}

					throw new HttpResponseException(
						$"Unexpected error occured.\n" +
						$"Status code = { (int)response.StatusCode }\n" +
						$"Reason = { response.ReasonPhrase }.");
				}
			}
			catch (HttpRequestException e)
			{
				throw e;
			}
			catch (TaskCanceledException e)
			{
				throw e;
			}
		}

		/// <summary>
		/// Get <see cref="string"/> response from url.
		/// </summary>
		/// <param name="url"></param>
		/// <returns><see cref="string"/> response.</returns>
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
				using (var response = await this._HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				using (var stream = await response.Content.ReadAsStreamAsync())
				{
					if (response.IsSuccessStatusCode)
					{
						return await this.DeserializeStringFromStreamAsync(stream);
					}

					throw new HttpResponseException(
						$"Unexpected error occured.\n" +
						$"Status code = { (int)response.StatusCode }\n" +
						$"Reason = { response.ReasonPhrase }.");
				}
			}
			catch (HttpRequestException e)
			{
				throw e;
			}
			catch (TaskCanceledException e)
			{
				throw e;
			}
		}

		/// <summary>
		/// Deserializes response into string.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns><see cref="string"/> content.</returns>
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

		/// <summary>
		/// Get max number of <see cref="Post"/> with 
		/// the given <see cref="Tag"/> the site have.
		/// </summary>
		/// <param name="url">Url of the requested <see cref="Post"/>.</param>
		/// <returns>Number of <see cref="Post"/>.</returns>
		/// <exception cref="XmlException">
		///		There is a load or parse error in the XML.
		/// </exception>
		/// <exception cref="FormatException">
		///		Can't convert to <see cref="uint"/>.
		/// </exception>
		protected async Task<uint> GetPostCountAsync(string[] tags)
		{
			string url = "";

			if (this is Gelbooru || this is Gelbooru02)
			{
				url = this.CreateBaseApiCall("post", false) +
					$"&limit=0";
			}
			else if (this is Moebooru)
			{
				url = this.CreateBaseApiCall("post", false) +
					$"limit=1";
			}

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
			catch (XmlException e)
			{
				throw e;
			}
			catch (FormatException e)
			{
				throw e;
			}
		}

		#region Virtual Method

		/// <summary>
		/// Read <see cref="Artist"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="Artist"/> object.</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Artist ReadArtist(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadArtist) } is not implemented yet.");
		}

		/// <summary>
		/// Read <see cref="Pool"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="Pool"/> object.</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Pool ReadPool(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadPool) } is not implemented yet.");
		}

		/// <summary>
		/// Read <see cref="Post"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="Post"/> object.</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Post ReadPost(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadPost) } is not implemented yet.");
		}

		/// <summary>
		/// Read <see cref="Tag"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="Tag"/> object.</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual Tag ReadTag(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadTag) } is not implemented yet.");
		}

		/// <summary>
		/// Read <see cref="TagRelated"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="TagRelated"/> object.</returns>
		/// <exception cref="NotImplementedException">
		///		Method is not implemented yet.
		/// </exception>
		protected virtual TagRelated ReadTagRelated(JsonElement json)
		{
			throw new NotImplementedException($"Method { nameof(ReadTagRelated) } is not implemented yet.");
		}

		/// <summary>
		/// Read <see cref="Wiki"/> JSON search result.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <returns><see cref="Wiki"/> object.</returns>
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
		/// Check the property of JSON object exist or not.
		/// </summary>
		/// <param name="json">JSON object.</param>
		/// <param name="propertyName">The name of the property to find.</param>
		/// <returns>
		///		<see langword="true"/> if the property was found; otherwise, <see langword="false"/>.
		///	</returns>
		protected bool PropertyExist(JsonElement json, string propertyName)
		{
			return json.TryGetProperty(propertyName, out _);
		}

		/// <summary>
		/// Convert string rating to <see cref="Rating"/>.
		/// </summary>
		/// <param name="rating">String rating</param>
		/// <returns></returns>
		protected Rating ConvertRating(string rating)
		{
			switch (char.ToLower(rating[0]))
			{
				case 'e':
					return Rating.Explicit;
				case 'q':
					return Rating.Questionable;
				case 's':
					return Rating.Safe;
				default:
					return Rating.Questionable;
			}
		}

		/// <summary>
		/// Check pre-condition for the tags.
		/// </summary>
		/// <param name="tags">Tags to check.</param>
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

		#endregion Helper Method

		#endregion Protected Method

		#region Public Method

		/// <summary>
		/// Add http user agent if not exist.
		/// </summary>
		/// <param name="userAgent">User Agrnt value.</param>
		public void AddHttpUserAgent(string userAgent = "")
		{
			if (this._HttpClient == null)
			{
				return;
			}

			if (this._HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				if (userAgent == null | userAgent.Trim() == "")
				{
					this.HttpClient.DefaultRequestHeaders.Add(
					"User-Agent",
					"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36");
				}
				else
				{
					this.HttpClient.DefaultRequestHeaders.Add(
					"User-Agent",
					userAgent);
				}
			}
		}

		/// <summary>
		/// Login with booru username and password.
		/// </summary>
		/// <param name="username">Your username.</param>
		/// <param name="password">Your password.</param>
		/// <returns></returns>
		protected bool Authenticate(string username, string password)
		{
			throw new NotImplementedException($"Method { nameof(Authenticate) } is not implemented yet.");
			this._Username = username;
			this._Password = this._PasswordSalt.Replace("{}", password);

			return false;
		}

		#region Artist

		/// <summary>
		/// Search <see cref="Artist"/> by name.
		/// </summary>
		/// <param name="name">The name (or a fragment of the name) of the artist.</param>
		/// <param name="page">The page number.</param>
		/// <param name="sort">Sort the search result by <see cref="Artist"/> name. Default <see langword="false"/>.</param>
		/// <returns>Array of <see cref="Artist"/>.</returns>
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
		public virtual async Task<Artist[]> ArtistListAsync(string name, uint page = 0, bool sort = false)
		{
			if (this.HasArtistApi == false)
			{
				throw new NotImplementedException($"Method { nameof(ArtistListAsync) } is not implemented yet.");
			}

			if (name == null || name.Trim() == "")
			{
				throw new ArgumentNullException(nameof(name), "Artist name can't null or empty.");
			}

			string url = "";
			
			if (this is Danbooru)
			{
				url = this.CreateBaseApiCall("artists") +
					$"limit={ this._PostLimit }&page={ page }&search[any_name_matches]={ name }";

				if (sort)
				{
					url += "&search[order]=name";
				}
			}
			else if (this is Moebooru)
			{
				url = this.CreateBaseApiCall("artist") + 
					$"page={ page }&name={ name }";

				if (sort)
				{
					url += "&order=name";
				}
			}

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
		/// Search <see cref="Pool"/> by title.
		/// </summary>
		/// <param name="title">The title of <see cref="Pool"/>.</param>
		/// <param name="page">The page number.</param>
		/// <returns>Array of <see cref="Pool"/>.</returns>
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

			if (title == null || title.Trim() == "")
			{
				throw new ArgumentNullException(nameof(title), "Title can't null or empty.");
			}

			string url = "";

			if (this is Danbooru)
			{
				url = this.CreateBaseApiCall("pools") +
					$"limit={ this._PostLimit }&page={ page }&search[name_matches]={ title }";
			}
			else if (this  is Moebooru)
			{
				url = this.CreateBaseApiCall("pool") + 
					$"page={ page }&query={ title }";
			}

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
		/// Get all <see cref="Post"/> inside the <see cref="Pool"/>.
		/// </summary>
		/// <param name="poolId">The <see cref="Pool"/> id.</param>
		/// <returns>Array of <see cref="Post"/> from <see cref="Pool"/>.</returns>
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

			string url = "";
			JsonElement obj;
			var posts = new List<Post>();

			if (this is Danbooru)
			{
				url = this.CreateBaseApiCall($"pools/{ poolId }");

				using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
				{
					obj = temp.RootElement.Clone();
				}

				// if Pool not found, it return JSON response
				// containing a reason why it not found

				if (obj.TryGetProperty("success", out _))
				{
					throw new SearchNotFoundException($"Can't find Pool with id { poolId }.");
				}

				// the JSON response only give the Post id
				// so we need get the Post data from another API call.

				var postIds = obj.GetProperty("post_ids");

				if (postIds.GetArrayLength() == 0)
				{
					throw new SearchNotFoundException($"No Post inside Pool with id { poolId }.");
				}

				foreach (var id in postIds.EnumerateArray())
				{
					posts.Add(
						new Post(
							id.GetUInt32(),
							this._BaseUrl + "posts/", "", "", Rating.None, "", 0, 0, 0, 0, 0, ""));
				}
			}
			else if (this is Moebooru)
			{
				url = this.CreateBaseApiCall("pool/show") + 
					$"id={ poolId }";

				try
				{
					using (var temp = await this.GetJsonResponseAsync<JsonDocument>(url))
					{
						obj = temp.RootElement.Clone();
					}

					foreach (var item in obj.GetProperty("posts").EnumerateArray())
					{
						posts.Add(this.ReadPost(item));
					}
				}
				catch (Exception e)
				{
					// if pool not found, it will return to pool page 
					// like yande.re/pool, not a empty JSON.
					throw new SearchNotFoundException($"Can't find Pool with id { poolId }.", e);
				}
			}
			
			return posts.ToArray();
		}

		#endregion Pool

		#region Post

		/// <summary>
		/// Get a list of the latest <see cref="Post"/>.
		/// </summary>
		/// <param name="limit">How many <see cref="Post"/> to retrieve.</param>
		/// <param name="page">The page number.</param>
		/// <param name="tags">The tags to search for.</param>
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
		public virtual async Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			if (this.HasPostApi == false)
			{
				throw new NotImplementedException($"Method { nameof(PostListAsync) } is not implemented yet.");
			}

			this.CheckTagsLimit(tags);

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._PostLimit)
			{
				limit = this._PostLimit;
			}

			string url = "";

			if (this is Danbooru)
			{
				url = this.CreateBaseApiCall("posts") +
					$"limit={ limit }&page={ page }";
			}
			else if (this is Gelbooru || this is Gelbooru02)
			{
				if (page > 200000)
				{
					page = 200000;
				}

				url = this.CreateBaseApiCall("post") +
					$"&limit={ limit }&pid={ page }";
			}
			else if (this is Moebooru)
			{
				url = this.CreateBaseApiCall("post") +
					$"limit={ limit }&page={ page }";
			}

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
		/// Get a single random <see cref="Post"/> with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
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

			this.CheckTagsLimit(tags);

			// get Post count in XML response.

			var postCount = await this.GetPostCountAsync(tags);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(" ", tags) }.");
			}

			// get post with random the page number, each page 
			// limited only with 1 post.

			var pageNumber = this._RNG.NextInt(1, postCount);

			// there's a limit for page number
			// more than that will return error 

			if (this is Gelbooru)
			{
				pageNumber %= 20000;
			}
			else if (this is Gelbooru02)
			{
				pageNumber %= 200000;
			}

			var post = await this.PostListAsync(1, tags, pageNumber);

			return post[0];
		}

		/// <summary>
		/// Get multiple random <see cref="Post"/> with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
		/// <param name="limit">How many post to retrieve.</param>
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
		public virtual async Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			if (this.HasPostApi == false)
			{
				throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
			}

			// this algorithm for gelbooru, gelbooru beta 0.2 and moebooru

			this.CheckTagsLimit(tags);

			if (limit <= 0)
			{
				limit = 1;
			}
			else if (limit > this._PostLimit)
			{
				limit = this._PostLimit;
			}

			var postCount = await this.GetPostCountAsync(tags);

			if (postCount == 0)
			{
				throw new SearchNotFoundException($"No post found with tags { string.Join(", ", tags) }.");
			}
			else if (postCount < limit)
			{
				throw new SearchNotFoundException($"The site only have { postCount } post with tags { string.Join(", ", tags) }.");
			}

			var maxPageNumber = (int)Math.Floor(postCount / limit * 1.0);

			// there's a limit for page number
			// more than that will return error 

			if (this is Gelbooru)
			{
				maxPageNumber %= 20000;
			}
			else if ( this is Gelbooru02)
			{
				maxPageNumber %= 200000;
			}

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

				return await this.PostListAsync(limit, tags, this._RNG.NextInt(1, (uint)maxPageNumber));
			}
		}

		#endregion Post

		#region Tag

		/// <summary>
		/// Search for <see cref="Tag"/> with the name is similiar or alike.
		/// </summary>
		/// <param name="name">The <see cref="Tag"/> name.</param>
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
		public virtual Task<Tag[]> TagListAsync(string name)
		{
			throw new NotImplementedException($"Method { nameof(TagListAsync) } is not implemented yet.");
		}

		/// <summary>
		/// Search for <see cref="Tag"/> that related with other <see cref="Tag"/>.	
		/// </summary>
		/// <param name="name">The <see cref="Tag"/> name.</param>
		/// <param name="type">Restrict results to search by <see cref="TagType"/> (can be general, artist, copyright, or character).</param>
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
		public virtual Task<TagRelated[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			throw new NotImplementedException($"Method { nameof(TagRelatedAsync) } is not implemented yet.");
		}

		#endregion Tag

		#region Wiki

		/// <summary>
		/// Search for <see cref="Wiki"/> by title.
		/// </summary>
		/// <param name="title"><see cref="Wiki"/> title.</param>
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
		public virtual Task<Wiki[]> WikiListAsync(string title)
		{
			throw new NotImplementedException($"Method { nameof(WikiListAsync) } is not implemented yet.");
		}

		#endregion Wiki

		#endregion Public Method
	}
}
