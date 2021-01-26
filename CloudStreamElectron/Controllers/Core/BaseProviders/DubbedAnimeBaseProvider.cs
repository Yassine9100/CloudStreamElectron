using System.Collections.Generic;
using static CloudStreamForms.Core.CloudStreamCore;
using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json;
using System;

namespace CloudStreamForms.Core.BaseProviders
{
	class DubbedAnimeBaseProvider : BaseProvider
	{
		public DubbedAnimeBaseProvider(CloudStreamCore _core) : base(_core) { }

#pragma warning disable CS0649

		[System.Serializable]
		public struct DubbedSearchItem
		{
			public string name;
			public string slug;
			public bool isMovie;
		}

		[System.Serializable]
		public struct DubbedAnimeEpisode
		{
			public string rowid;
			public string title;
			public string desc;
			public string status;
			public object skips;
			public int totalEp;
			public string ep;
			public int NextEp;
			public string slug;
			public string wideImg;
			public string year;
			public string showid;
			public string Epviews;
			public string TotalViews;
			public string serversHTML;
			public string preview_img;
			public string tags;
		}

		[System.Serializable]
		public struct DubbedAnimeSearchResult
		{
			public List<DubbedAnimeEpisode> anime;
			public bool error;
			public object errorMSG;
		}

		[System.Serializable]
		public struct DubbedAnimeSearchRootObject
		{
			public DubbedAnimeSearchResult result;
		}

#pragma warning restore CS0649

		/// <summary>
		/// Null if nothing found
		/// </summary>
		/// <param name="inp"></param>
		/// <returns></returns>
		public List<DubbedSearchItem> Search(string inp)
		{
			string d = DownloadString($"https://bestdubbedanime.com/search/{inp}");
			if (!d.IsClean()) return null;
			var doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(d);
			List<DubbedSearchItem> items = new List<DubbedSearchItem>();

			var hrefs = doc.QuerySelectorAll("a.resulta");
			foreach (var q in hrefs) {
				string href = q.GetAttributeValue("href", "");
				if (href == "") continue;
				var hDiv = q.QuerySelector("> div.result");
				string name = hDiv.QuerySelector("> div.titleresults").InnerText;
				var innerHt = hDiv.QuerySelector("> div.inresult").InnerHtml.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", "");
				bool isMovie = innerHt.Length > 4;
				items.Add(new DubbedSearchItem() {
					isMovie = isMovie,
					name = name,
					slug = href.Replace("//bestdubbedanime.com/", ""),
				});
			}
			return items;
		}

		public void AddMirrors(DubbedAnimeEpisode dubbedEp, int normalEpisode)
		{
			string serverUrls = dubbedEp.serversHTML;

			const string sLookFor = "hl=\"";
			while (serverUrls.Contains(sLookFor)) {
				string baseUrl = FindHTML(dubbedEp.serversHTML, "hl=\"", "\"");
				string burl = "https://bestdubbedanime.com/xz/api/playeri.php?url=" + baseUrl + "&_=" + UnixTime;
				string _d = DownloadString(burl);

				string enlink = "\'";
				if (_d.Contains("<source src=\"")) {
					enlink = "\"";
				}
				string lookFor = "<source src=" + enlink;
				while (_d.Contains(lookFor)) {

					string vUrl = FindHTML(_d, lookFor, enlink);
					if (vUrl != "") {
						vUrl = "https:" + vUrl;
					}
					string label = FindHTML(_d, "label=" + enlink, enlink);
					AddPotentialLink(normalEpisode, vUrl, "DubbedAnime", 7, label.Replace("0p", "0") + "p");

					_d = RemoveOne(_d, lookFor);
					try {
						_d = RemoveOne(_d, "label=" + enlink);
					}
					catch (Exception _ex) {
						error(_ex);
					}
				}
				serverUrls = RemoveOne(serverUrls, sLookFor);
			}
		}

		/// <summary>
		/// Null eps for movies
		/// </summary>
		/// <param name="slug"></param>
		/// <param name="eps"></param>
		/// <returns></returns>
		public DubbedAnimeEpisode? GetDubbedAnimeEpisode(string slug, int? eps = null)
		{
			bool isMovie = eps == null;
			string url = "https://bestdubbedanime.com/" + (isMovie ? "movies/jsonMovie" : "xz/v3/jsonEpi") + ".php?slug=" + slug + (eps != null ? ("/" + eps) : "") + "&_=" + UnixTime;
			string d = DownloadString(url, referer: $"https://bestdubbedanime.com/{(isMovie ? "movies/" : "")}{slug}{(isMovie ? "" : $"/{eps}")}");
			var f = JsonConvert.DeserializeObject<DubbedAnimeSearchRootObject>(d, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
			if (f.result.error) {
				return null;
			}
			else {
				try {
					return f.result.anime[0];
				}
				catch {
					return null;
				}
			}
		}

		public static List<DubbedSearchItem> movies = new List<DubbedSearchItem>();
		public static bool hasSearchedMovies = false;
		static bool isSearchingMovies = false;
		public void FishMainMovies()
		{
			//var t = core.CreateThread(2);
			core.StartThread("DubbedMoviesThread", () => {
				if (isSearchingMovies || hasSearchedMovies) return;
				isSearchingMovies = true;
				try {
					string d = DownloadString("https://bestdubbedanime.com/movies/");

					if (d != "") {
						const string lookFor = "//bestdubbedanime.com/movies/";
						while (d.Contains(lookFor)) {
							string href = FindHTML(d, lookFor, "\"");
							d = RemoveOne(d, lookFor);
							string title = FindHTML(d, "grid_item_title\">", "<");
							movies.Add(new DubbedSearchItem() {
								isMovie = true,
								name = title,
								slug = href
							});
						}
						if (movies.Count > 0) {
							hasSearchedMovies = true;
						}
					}
				}
				catch (Exception _ex) {
					error("EX IN MAINMOV: " + _ex);
				}
				finally {
					isSearchingMovies = false;
				}
			});
		}
	}
}
