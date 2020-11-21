using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using BooruDex.Exceptions;
using BooruDex.Models;

using Litdex.Security.RNG;
using Litdex.Security.RNG.PRNG;

using Newtonsoft.Json;

namespace BooruDex.Booru
{
	/// <summary>
	/// Base class for all booru client.
	/// </summary>
	public abstract class Booru
	{
		#region Member

		/// <summary>
		/// Browser
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
		/// Max tags to use for search a post. 
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
		/// Base object for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		public Booru(string domain) : this(domain, null, new JSF32())
		{
			
		}

		/// <summary>
		/// Base object for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		public Booru(string domain, HttpClient httpClient = null) : this(domain, httpClient, new JSF32())
		{
			
		}

		/// <summary>
		/// Base object for booru client.
		/// </summary>
		/// <param name="domain">URL of booru based sites.</param>
		/// <param name="httpClient">Client for sending and receive http response.</param>
		/// <param name="rng">Random generator for random post.</param>
		public Booru(string domain, HttpClient httpClient, IRNG rng)
		{
			this._BaseUrl = new Uri(domain, UriKind.Absolute);
			this.HttpClient = httpClient;
			this._RNG = rng;
			this._Authentication = false;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		}

		/// <summary>
		/// Release all resource that this object hold.
		/// </summary>
		~Booru()
		{
			
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

		/// <summary>
		/// Gets or sets whether this booru contains explicit content or not.
		/// </summary>
		public bool IsSafe { set; get; }

		/// <summary>
		/// Gets or sets maximum page number for booru.
		/// </summary>
		public byte PageLimit 
		{
			set
			{ 
				if (value < 10)
				{
					this._PageLimit = 10;
				}
				else
				{
					this._PageLimit = value;
				}
			}
			get
			{
				return this._PageLimit;
			}
		}

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
						return this.DeserializeJsonFromStream<T>(stream);
					}

					throw new HttpResponseException(
						$"Unexpected error occured.\n" +
						$"Status code = { response.StatusCode }\n" +
						$"Content = { await this.DeserializeStringFromStreamAsync(stream) }.");
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
		/// Deserializes the JSON structure into an instance of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="stream"></param>
		/// <returns>The instance of <typeparamref name="T"/> being deserialized.</returns>
		protected T DeserializeJsonFromStream<T>(Stream stream)
		{
			if (stream == null || stream.CanRead == false)
			{
				return default(T);
			}

			using (var sr = new StreamReader(stream))
			using (JsonReader reader = new JsonTextReader(sr))
			{
				JsonSerializer serializer = new JsonSerializer();
				return serializer.Deserialize<T>(reader);
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
				using (var sr = new StreamReader(stream))
				{
					return await sr.ReadToEndAsync();
				}
			}

			return null;
		}

		/// <summary>
		/// Create base API call url. 
		/// </summary>
		/// <param name="query">Categories.</param>
		/// <returns></returns>
		protected abstract string CreateBaseApiCall(string query);

		/// <summary>
		/// Convert string rating to <see cref="Rating"/>.
		/// </summary>
		/// <param name="rating">String rating</param>
		/// <returns></returns>
		protected Rating ConvertRating(string rating)
		{
			switch (rating.ToString()[0])
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

		#endregion Protected Method

		#region Public Method

		/// <summary>
		/// Login with booru username and password.
		/// </summary>
		/// <param name="username">Your username.</param>
		/// <param name="password">Your password.</param>
		/// <returns></returns>
		public bool Authenticate(string username, string password)
		{
			throw new NotImplementedException($"Method { nameof(Authenticate) } is not implemented yet.");
			this._Username = username;
			this._Password = this._PasswordSalt.Replace("{}", password);

			return false;
		}

		#region Artist

		/// <summary>
		/// Get a list of artists.
		/// </summary>
		/// <param name="name">The name (or a fragment of the name) of the artist.</param>
		/// <param name="page">The page number.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Artist[]> ArtistListAsync(string name, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(ArtistListAsync) } is not implemented yet.");
		}

		#endregion Artist

		#region Pool

		/// <summary>
		/// Search a pool.
		/// </summary>
		/// <param name="title">The title of pool.</param>
		/// <param name="page">Tha page number.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Pool[]> PoolList(string title, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(PoolList) } is not implemented yet.");
		}

		/// <summary>
		/// Get list of post inside the pool.
		/// </summary>
		/// <param name="poolId">The <see cref="Pool"/> id.</param>
		/// <param name="page">The page number.</param>
		/// <returns></returns>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Post[]> PoolPostList(uint poolId, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(PoolPostList) } is not implemented yet.");
		}

		#endregion Pool

		#region Post

		/// <summary>
		/// Get a list of <see cref="Post"/>.
		/// </summary>
		/// <param name="limit">How many <see cref="Post"/> to retrieve.</param>
		/// <param name="page">The page number.</param>
		/// <param name="tags">The tags to search for.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		/// <exception cref="SearchNotFoundException"></exception>
		public virtual Task<Post[]> PostListAsync(uint limit, string[] tags, uint page = 0)
		{
			throw new NotImplementedException($"Method { nameof(PostListAsync) } is not implemented yet.");
		}

		/// <summary>
		/// Search a single random post from booru with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		/// <exception cref="SearchNotFoundException"></exception>
		public virtual Task<Post> GetRandomPostAsync(string[] tags = null)
		{
			throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
		}

		/// <summary>
		/// Search some post from booru with the given tags.
		/// </summary>
		/// <param name="tags"><see cref="Tag"/> to search.</param>
		/// <param name="limit">How many post to retrieve.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Post[]> GetRandomPostAsync(uint limit, string[] tags = null)
		{
			throw new NotImplementedException($"Method { nameof(GetRandomPostAsync) } is not implemented yet.");
		}

		#endregion Post

		#region Tag

		/// <summary>
		/// Get a list of tag that contains 
		/// </summary>
		/// <param name="name">The tag names to query.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Tag[]> TagListAsync(string name)
		{
			throw new NotImplementedException($"Method { nameof(TagListAsync) } is not implemented yet.");
		}

		/// <summary>
		/// Get a list of related tags.
		/// </summary>
		/// <param name="name">The tag names to query.</param>
		/// <param name="type">Restrict results to tag type (can be general, artist, copyright, or character).</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<TagRelated[]> TagRelatedAsync(string name, TagType type = TagType.General)
		{
			throw new NotImplementedException($"Method { nameof(TagRelatedAsync) } is not implemented yet.");
		}

		#endregion Tag

		#region Wiki

		/// <summary>
		/// Search a wiki content.
		/// </summary>
		/// <param name="title">Wiki title.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="HttpResponseException"></exception>
		public virtual Task<Wiki[]> WikiListAsync(string title)
		{
			throw new NotImplementedException($"Method { nameof(WikiListAsync) } is not implemented yet.");
		}

		#endregion Wiki

		#endregion Public Method
	}
}
