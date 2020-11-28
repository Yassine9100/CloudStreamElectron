using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class AnimeParadiseBFProvider : BloatFreeBaseAnimeProvider
	{
		public const bool IsNewApi = true;

		public AnimeParadiseBFProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "AnimeParadise";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			try {
				string d = "";
#pragma warning disable 
				if (!IsNewApi) {
					string main = DownloadString(episodeLink);
					if (main == "") return;

					string fileId = FindHTML(main, "fileId=", "\"");
					if (fileId == "") return;

					d = DownloadString($"https://stream.animeparadise.cc/sources?fileId={fileId}", referer: $"https://stream.animeparadise.cc/embed.html?fileId={fileId}");
				}
				else {
					d = DownloadString(episodeLink);
				}
#pragma warning restore

				if (d == "") return;
				var videos = JsonConvert.DeserializeObject<AnimeParadiseVideoFile[]>(d);
				int prio = 10;
				foreach (var video in videos) {
					AddPotentialLink(normalEpisode, video.file, "AnimeParadise", prio, video.label);
					prio--;
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		[System.Serializable]
		public struct AnimeParadiseVideoFile
		{
			public string file;
			public string label;
			public string type;
		}

		[System.Serializable]
		struct AnimeParadiseData
		{
			public int id;
			public bool isDub;
			public int season;
			public string name;
			public string referer;
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			try {
				string search = malData.engName.Replace("-", " ");
				List<AnimeParadiseData> data = new List<AnimeParadiseData>();
				string searchQry = "https://animeparadise.cc/search.php?query=" + search;
				string d = DownloadString(searchQry, referer: (IsNewApi ? "https://animeparadise.cc/" : "https://animeparadise.cc/index.php"));
				if (d == "") {
					return null;
				}
				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(d);
				var nodes = doc.QuerySelectorAll("div.content > p");
				foreach (var item in nodes) {
					var _data = item.QuerySelector("strong > a");
					int.TryParse(_data.GetAttributeValue("href", "").Split('=')[1], out int id);
					bool isDub = item.QuerySelector("span").InnerText == "DUB";
					string name = _data.InnerText;
					int season = 1;
					for (int i = 2; i < 10; i++) {
						if (item.InnerText.Contains($"(Season {i})")) {
							season = i;
							break;
						}
					}
					if (id != 0) {
						data.Add(new AnimeParadiseData() {
							isDub = isDub,
							name = name,
							id = id,
							season = season,
							referer = searchQry,
						});
					}
				}
				return data;
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			List<AnimeParadiseData> data = (List<AnimeParadiseData>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			foreach (var subData in data) {
				if ((setData.dubExists && subData.isDub) || (setData.subExists && !subData.isDub)) continue;

				if (subData.season == ms.season &&
					(ToLowerAndReplace(ms.name, false).StartsWith(ToLowerAndReplace(subData.name, false)) ||
					 ToLowerAndReplace(ms.engName, false).StartsWith(ToLowerAndReplace(subData.name, false)))) { // THIS IS BECAUSE SEASON IS SEPERATED FROM NAME
					try {
						string d = DownloadString("https://animeparadise.cc/anime.php?s=" + subData.id, referer: subData.referer);
						var doc = new HtmlAgilityPack.HtmlDocument();
						doc.LoadHtml(d);
						var nodes = doc.QuerySelectorAll("h1.title");

						int lastEp = int.Parse(nodes[^1].InnerText);

						for (int i = 1; i <= lastEp; i++) {
							string s = IsNewApi ? $"https://animeparadise.cc/apis/fetchsources.php?s={subData.id}&ep={i}" : $"https://animeparadise.cc/watch.php?s={subData.id}&ep={i}";
							if (subData.isDub) {
								setData.dubEpisodes.Add(s);
							}
							else {
								setData.subEpisodes.Add(s);
							}
						}
					}
					catch (Exception _ex) {
						error(_ex);
					}
				}
			}

			return setData;
		}
	}
}
