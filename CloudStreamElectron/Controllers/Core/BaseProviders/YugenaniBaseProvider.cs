using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.BaseProviders
{
	class YugenaniBaseProvider
	{
		static string lastToken;
		static string lastCfduid;
		static bool isFetching = false;

		public YugenaniBaseProvider()
		{
			if (lastToken == null && !isFetching) {
				isFetching = true;
				Thread t = new Thread(() => {
					GetYugenaniSite("https://yugenani.me/"); // INIT
					isFetching = false;
				}) { Name = nameof(GetYugenaniSite) };
				t.Start();
			}
		}

#pragma warning disable CS0649
		public struct YuVideoMulti
		{
			public string src;
			public string type;
			public int size;
		}

		public struct YuVideoSource
		{
			public int id;
			public string name;
			public string src;
			public string type;
		}

		public struct YuVideoRoot
		{
			public string message;
			//public string thumbnail;
			public YuVideoMulti[] multi;
			public YuVideoSource[] sources;
		}

		public struct YuSearchItemFields
		{
			public string title;
			public string slug;
			/*
			public string poster;
			public string type;
			public string status;
			public string premiered;
			public string score;*/
		}

		public struct YuSearchItems
		{
			public string model;
			public int pk;
			public YuSearchItemFields fields;
		}

		public struct YuSearchRoot
		{
			public YuSearchItems[] query;
		}

#pragma warning restore CS0649

		static string GetYugenaniSite(string url, out string token, out string cfduid)
		{
			const int waitTime = 10000;
			token = "";
			cfduid = "";
			try {
				HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
				//if (GetRequireCert(url)) { webRequest.ServerCertificateValidationCallback = delegate { return true; }; }

				webRequest.Method = "GET";
				webRequest.Timeout = waitTime;
				webRequest.ReadWriteTimeout = waitTime;
				webRequest.ContinueTimeout = waitTime;
				webRequest.UserAgent = USERAGENT;

				print("REQUEST::: " + url);

				using var webResponse = webRequest.GetResponse();
				try {
					foreach (var key in webResponse.Headers.AllKeys) {
						if (key == "Set-Cookie") {
							foreach (var f in webResponse.Headers.GetValues(key)) {
								print(key + "|" + f);
								if (f.Contains("csrftoken")) {
									token = FindHTML(f, "csrftoken=", ";");
								}
								if (f.Contains("cfduid")) {
									cfduid = FindHTML(f, "cfduid=", ";");
								}
							}
						}
					}
					using StreamReader httpWebStreamReader = new StreamReader(webResponse.GetResponseStream());
					try {
						return httpWebStreamReader.ReadToEnd();
					}
					catch (Exception _ex) {
						print("FATAL ERROR DLOAD3: " + _ex + "|" + url);
					}
				}
				catch (Exception) {
					return "";
				}
			}
			catch (Exception _ex) {
				error("FATAL ERROR DLOAD: \n" + url + "\n============================================\n" + _ex + "\n============================================");
			}
			return "";
		}

		static string PostYugenaniSite(string myUri, string _requestBody, string referer, string token, string cfduid, string _contentType = "application/x-www-form-urlencoded; charset=UTF-8")
		{
			try {
				HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

				webRequest.Method = "POST";
				webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
				webRequest.Headers.Add("sec-fetch-dest", "empty");
				webRequest.Headers.Add("sec-fetch-mode", "cors");
				webRequest.Headers.Add("x-csrftoken", token);
				webRequest.Headers.Add("sec-fetch-site", "same-origin");
				webRequest.Headers.Add("cookie", $"__cfduid={cfduid};csrftoken={token}");

				webRequest.CookieContainer = new CookieContainer();
				webRequest.CookieContainer.Add(new Cookie("__cfduid", cfduid, "/", ".yugenani.me"));
				webRequest.CookieContainer.Add(new Cookie("csrftoken", token, "/", "yugenani.me"));

				webRequest.Referer = referer;
				webRequest.ContentType = _contentType;
				// webRequest.Headers.Add("Host", "trollvid.net");
				webRequest.UserAgent = USERAGENT;
				webRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");
				bool done = false;
				string _res = "";
				webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
					try {
						HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
						Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

						string requestBody = _requestBody;// --- RequestHeaders ---

						byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

						postStream.Write(byteArray, 0, byteArray.Length);
						postStream.Close();


						// BEGIN RESPONSE

						_webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
							try {
								HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
								HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
								using StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream());
								try {
									_res = httpWebStreamReader.ReadToEnd();
									done = true;
								}
								catch (Exception) {
									return;
								}

							}
							catch (Exception _ex) {
								error("FATAL EX IN POST2: " + _ex);
							}
						}), _webRequest);

					}
					catch {
						error("FATAL EX IN POSTREQUEST");
					}
				}), webRequest);


				for (int i = 0; i < 1000; i++) {
					Thread.Sleep(10);
					if (done) {
						return _res;
					}
				}
				return _res;
			}
			catch (Exception _ex) {
				error("FATAL EX IN POST: " + _ex);
				return "";
			}
		}

		public string PostYugenaniSite(string myUri, string _requestBody, string referer, string _contentType = "application/x-www-form-urlencoded; charset=UTF-8")
		{
			if (lastToken == null || lastCfduid == null) {
				return "";
			}

			return PostYugenaniSite(myUri, _requestBody, referer, lastToken, lastCfduid, _contentType);
		}

		public string GetYugenaniSite(string url)
		{
			var site = GetYugenaniSite(url, out string token, out string cfduid);
			if (token.IsClean()) {
				lastToken = token;
				lastCfduid = cfduid;
			}
			return site;
		}

		public YuSearchRoot? SearchSite(string query)
		{
			try {
				string post = PostYugenaniSite("https://yugenani.me/api/search/", $"query={query}", "https://yugenani.me/").Replace("\\", "");
				string main = "{ \"query\": " + FindHTML(post, "query\": \"", "]\"") + "]}";
				print("Search::: " + post + "|\n" + main);
				return JsonConvert.DeserializeObject<YuSearchRoot>(main);
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		/// <summary>
		/// "https://yugenani.me/watch/1428/overlord/1/" or
		/// "https://yugenani.me/watch/1428/overlord-dub/1/"
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public string GetEmbed(string url)
		{
			string d = GetYugenaniSite(url);
			return FindHTML(d, "src=\"//yugenani.me/e/", "/");
		}

		public YuVideoRoot? GetEmbedData(string embed)
		{
			try {
				string post = PostYugenaniSite("https://yugenani.me/api/embed/", $"id={embed}&ac=0", $"https://yugenani.me/e/{embed}/");
				print(post);
				return JsonConvert.DeserializeObject<YuVideoRoot>(post);
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		[System.Serializable]
		public struct YuInfo
		{
			public int malId;
			public int dubbedEps;
			public int subbedEps;
		}

		public static Dictionary<int, YuInfo> cachedYuInfo = new Dictionary<int, YuInfo>();

		public YuInfo? GetYuInfo(int id, string slug)
		{
			//Episodes (Dub)</div><span class="description" style="font-size: 12px;">
			//Episodes</div><span class="description" style="font-size: 12px;">
			//mal_id":
			if (cachedYuInfo.ContainsKey(id)) return cachedYuInfo[id];

			string d = GetYugenaniSite($"https://yugenani.me/anime/{id}/{slug}/watch/");
			if (d == "") return null;
			try {
				int.TryParse(FindHTML(d, "Episodes (Dub)</div><span class=\"description\" style=\"font-size: 12px;\">", "<"), out int dubEpisodes);
				int.TryParse(FindHTML(d, "Episodes</div><span class=\"description\" style=\"font-size: 12px;\">", "<"), out int subEpisodes);
				int.TryParse(FindHTML(d, "mal_id\":", ","), out int malId);
				if (malId <= 0) return null;
				var info =  new YuInfo() {
					malId = malId,
					dubbedEps = dubEpisodes,
					subbedEps = subEpisodes,
				};
				cachedYuInfo[id] = info;
				return info;
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}
	}
}
