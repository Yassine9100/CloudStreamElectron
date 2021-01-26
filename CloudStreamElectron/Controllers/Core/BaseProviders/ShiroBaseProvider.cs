using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.BaseProviders
{
	class ShiroBaseProvider : BaseProvider
	{
		public string Token { get { return tokens.ContainsKey(_name) ? tokens[_name] : ""; } }
		static readonly Dictionary<string, string> tokens = new Dictionary<string, string>();
		static readonly Dictionary<string, bool> loadingToken = new Dictionary<string, bool>();

		public readonly string _siteUrl;
		public readonly string _name;

		private readonly bool _isMovieProvider;

		public ShiroBaseProvider(CloudStreamCore _core, string siteUrl, string name, bool isMovie) : base(_core)
		{
			_siteUrl = siteUrl;
			_name = name;
			_isMovieProvider = isMovie;

			if (!loadingToken.ContainsKey(_name)) {
				loadingToken.Add(_name, true);
				_core.StartThread("Shiro TokenThread", () => {
					tokens[_name] = GetShiroToken();
				});
			}
		}

		string GetShiroToken()
		{
			try {
				string d = DownloadString(_siteUrl);
				string js = FindHTML(d, "src=\"/static/js/main", "\"");
				string dScript = DownloadString($"{_siteUrl}/static/js/main" + js);
				return FindHTML(dScript, "token:\"", "\"");
			}
			catch (Exception _ex) {
				error(_ex);
				return "";
			}
		}

		public void LoadLink(string episodeLink, int normalEpisode, TempThread tempThred, bool isDub, string slug)
		{
			try {
				string episodeReq = DownloadString(episodeLink, referer: $"{_siteUrl}/stream/{slug}");
				var epData = JsonConvert.DeserializeObject<ShiroEpisodeRoot>(episodeReq);
				foreach (var video in epData.Data.videos) {
					string[] before = video.host switch {
						"googledrive" => new string[] { "https://ani.googledrive.stream/vidstreaming/vid-ad/" },
						"vidstream" => new string[] { "https://gogo-stream.com/ajax.php?id=", "https://ani.googledrive.stream/vidstreaming/vid/" }, //"https://gogo-stream.com/streaming.php?id=", // https://ani.googledrive.stream/vidstreaming/vid/
						_ => null,
					};
					if (before == null) continue;
					foreach (var b in before) {
						string _before = b + video.video_id;
						string _d = DownloadString(_before).Replace("\\", "");
						if (!core.GetThredActive(tempThred)) return;

						var links = GetAllFilesRegex(_d);
						foreach (var link in links) {
							AddPotentialLink(normalEpisode, link.url, $"{_name}{(video.host == "googledrive" ? " GoogleDrive" : "")}{(_isMovieProvider ? (isDub ? " (Dub)" : " (Sub)") : "")}", 15, link.label);
						}
					}
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		public List<RealShiroItem> StoreData(TempThread tempThred, string search)
		{
			try {
				if (Token == "") {
					loadingToken.Add(_name, true);
					tokens[_name] = GetShiroToken();
				}
				if (Token == "") {
					error("NO SHIRO TOKEN! AT " + nameof(StoreData) + "|" + _name);
					return null;
				}

				string shiroToken = GetShiroToken();
				string d = DownloadString($"https://ani.api-web.site/advanced?search={search}&type={(_isMovieProvider ? "movie" : "tv")}&token={shiroToken}");

				var data = JsonConvert.DeserializeObject<ShiroRoot>(d);
				var ret = data.Data.Nav.Nav[0].Items.Where(t => t.type == (_isMovieProvider ? "movie" : "TV")).Select(t => new RealShiroItem() {
					name = (t.english ?? "").Replace(" ", "") == "" ? (t.canonicalTitle ?? t.name.Replace("DUBBED", "")) : t.english,
					synonyms = t.synonyms.ToArray(),
					isDub = t.language == "dubbed",
					episodes = int.Parse(t.episodeCount),
					slug = t.slug,
					year = t.year,
				}).ToList();
				return ret;
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		[System.Serializable]
		public struct RealShiroItem
		{
			public string slug;
			public int episodes;
			public string name;
			public string[] synonyms;
			public bool isDub;
			public string year;
		}

#pragma warning disable CS0649
		public struct ShiroItem
		{
			//public string _id;
			public string slug;
			//public string aired;
			//public string banner;
			public string canonicalTitle;
			public string english;
			public string episodeCount;
			//public IList<string> genres;
			//public string image;
			//public string japanese;
			public string language;
			//public object latestEpisodeDate;
			public string name;
			//public string rating;
			//public string schedule;
			//public object sort;
			//public string status;
			public IList<string> synonyms;
			//public string synopsis;
			public string type;
			public string year;
		}
		public struct ShiroNav
		{
			//public string Name { get; set; }
			//public bool First { get; set; }
			//public bool Current { get; set; }
			//public bool Last { get; set; }
			//public int Index { get; set; }
			public IList<ShiroItem> Items;
			//	public bool Dotdotdot { get; set; }
			//	public bool Show { get; set; }
		}

		public struct HeadShiroNav
		{
			public int current;
			public IList<ShiroNav> Nav;
		}

		public struct ShiroData
		{
			public HeadShiroNav Nav;
		}

		public struct ShiroRoot
		{
			public string Status;
			public ShiroData Data;
		}

		public struct ShiroEpisodeRoot
		{
			//public string Status;
			public ShiroEpisodeData Data;
		}

		public struct ShiroEpisodeData
		{
			public IList<ShiroEpisodeVideo> videos;
		}

		public struct ShiroEpisodeVideo
		{
			public string host;
			public string video_id;
		}
#pragma warning restore CS0649
	}
}
