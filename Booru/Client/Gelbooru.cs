﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BooruDex.Booru.Client
{
	public class Gelbooru : Template.Gelbooru
	{
		#region Constructor & Destructor

		/// <summary>
		/// Create <see cref="Gelbooru"/> client object.
		/// </summary>
		/// <param name="httpClient">Http client for sending request and recieving response.</param>
		public Gelbooru(HttpClient httpClient = null) : base("http://gelbooru.com/", httpClient)
		{

		}

		#endregion Constructor & Destructor
	}
}