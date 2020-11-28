using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class ShiroBFProvider : BloatFreeBaseAnimeProvider
	{
		public string token = "";

		string GetShiroToken()
		{
			try {
				string d = DownloadString("https://shiro.is/");
				string js = FindHTML(d, "src=\"/static/js/main", "\"");
				string dScript = DownloadString("https://shiro.is/static/js/main" + js);
				return FindHTML(dScript, "token:\"", "\"");
			}
			catch (Exception _ex) {
				error(_ex);
				return "";
			}
		}

		public ShiroBFProvider(CloudStreamCore _core) : base(_core)
		{
			_core.StartThread("Shiro TokenThread", () => {
				token = GetShiroToken();
			});
		}

		public override string Name => "Shiro";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			try {
				string episodeReq = DownloadString(episodeLink);
				var epData = JsonConvert.DeserializeObject<ShiroEpisodeRoot>(episodeReq);
				foreach (var video in epData.Data.videos) {
					string before = video.host switch
					{
						"googledrive" => "https://ani.googledrive.stream/vidstreaming/vid-ad/",
						"vidstream" => "https://gogo-stream.com/ajax.php?id=", //"https://gogo-stream.com/streaming.php?id=",
						_ => "",
					};
					before += video.video_id;

					string _d = DownloadString(before).Replace("\\", "");
					var links = GetAllFilesRegex(_d);
					foreach (var link in links) {
						AddPotentialLink(normalEpisode, link.url, $"Shiro{(video.host == "googledrive" ? " GoogleDrive" : "")}", 5, link.label);
					}
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			try {
				if (token == "") {
					token = GetShiroToken();
				}
				if (token == "") {
					error("NO SHIRO TOKEN! AT " + nameof(StoreData));
					return null;
				}

				string shiroToken = GetShiroToken();
				string search = malData.engName;
				string d = DownloadString($"https://ani.api-web.site/advanced?search={search}&token={shiroToken}");

				var data = JsonConvert.DeserializeObject<ShiroRoot>(d);
				var ret = data.Data.Nav.Nav[0].Items.Where(t => t.type == "TV").Select(t => new RealShiroItem() {
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

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{

			List<RealShiroItem> data = (List<RealShiroItem>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			if (token == "") {
				error("NO SHIRO TOKEN! AT " + nameof(GetSeasonData));
				return setData;
			}
			foreach (var subData in data) {
				if ((setData.dubExists && subData.isDub) || (setData.subExists && !subData.isDub)) continue;

				bool synoExist = false;
				if (subData.synonyms != null && ms.synonyms != null && ms.synonyms.Count > 0 && subData.synonyms.Length > 0) {
					synoExist = ms.synonyms.Where(t => subData.synonyms.Contains(t)).ToArray().Length > 0;
				}

				if (ToDown(ms.engName) == ToDown(subData.name) || synoExist) {
					List<string> episodes = new List<string>();
					string slug = subData.slug;

					for (int i = 1; i <= subData.episodes; i++) {
						episodes.Add($"https://ani.api-web.site/anime-episode/slug/{slug}-episode-{i}?token={token}");
					}

					if (subData.isDub) {
						setData.dubEpisodes = episodes;
					}
					else {
						setData.subEpisodes = episodes;
					}
				}
			}

			return setData;
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
			public string Status;
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
	}
}
