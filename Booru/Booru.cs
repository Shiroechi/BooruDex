using System;
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
		/// Determine <see cref="Booru"/> API access.
		/// </summary>
		protected BooruApi _BooruApi;

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
			this._BooruApi = new BooruApi();
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
					this.AddHttpHeader();
				}
			}
			get
			{
				if (this._HttpClient == null)
				{
					this._HttpClient = _LazyHttpClient.Value;
					this.AddHttpHeader();
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
		public bool WikiApi { protected set; get; }

		#endregion Booru API Settings

		#endregion Properties

		#region Private Method

		private static readonly Lazy<HttpClient> _LazyHttpClient = new Lazy<HttpClient>(() =>
		{
			var http = new HttpClient();
			return http;
		});

		public void AddHttpHeader()
		{
			if (this._HttpClient == null)
			{
				return;
			}

			if (this._HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				this.HttpClient.DefaultRequestHeaders.Add(
					"User-Agent",
					"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36");
			}
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
		protected Task<string> DeserializeStringFromStreamAsync(Stream stream)
		{
			if (stream != null)
			{
				using (var sr = new StreamReader(stream, Encoding.UTF8))
				{
					return sr.ReadToEndAsync();
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
		protected async Task<uint> GetPostCountAsync(string url)
		{
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
		public virtual Task<Artist[]> ArtistListAsync(string name, uint page = 0, bool sort = false)
		{
			throw new NotImplementedException($"Method { nameof(ArtistListAsync) } is not implemented yet.");
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
		public virtual Task<Pool[]> PoolList(string title, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(PoolList) } is not implemented yet.");
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
		public virtual Task<Post[]> PoolPostList(uint poolId)
		{
			throw new NotImplementedException($"Method { nameof(PoolPostList) } is not implemented yet.");
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
		public virtual Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(PostListAsync) } is not implemented yet.");
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
		public virtual Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
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
		public virtual Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
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
