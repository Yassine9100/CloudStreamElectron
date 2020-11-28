using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class AnimeSimpleBloatFreeProvider : BloatFreeBaseAnimeProvider
	{
		public AnimeSimpleBloatFreeProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "AnimeSimple";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			string d = DownloadString(episodeLink);
			string json = FindHTML(d, "var json = ", "</");
			const string lookFor = "\"id\":\"";

			while (json.Contains(lookFor)) {
				string id = FindHTML(json, lookFor, "\"");
				json = RemoveOne(json, lookFor);
				string host = FindHTML(json, "host\":\"", "\"");
				string type = FindHTML(json, "type\":\"", "\"");
				if ((type == "dubbed" && isDub) || (type == "subbed" && !isDub)) {
					if (host == "mp4upload") {
						AddMp4(id, normalEpisode, tempThred);
					}
					else if (host == "trollvid") {
						core.AddTrollvid(id, normalEpisode, episodeLink, tempThred, " Simple");
					}
					else if (host == "vidstreaming") {
						AddEpisodesFromMirrors(tempThred, DownloadString("https://vidstreaming.io//streaming.php?id=" + id), normalEpisode, "", " Simple");
					}
				}
			}
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			return Search(malData.engName);
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			List<AnimeSimpleTitle> data = (List<AnimeSimpleTitle>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			foreach (var subData in data) {
				if (ms.MalId == subData.malId) {
					var ep = GetAnimeSimpleEpisodes(subData.id);
					if (ep.HasValue) {
						var _val = ep.Value;
						for (int i = 0; i < _val.subbedEpisodes; i++) {
							setData.subEpisodes.Add(_val.urls[i]);
						}
						for (int i = 0; i < _val.dubbedEpisodes; i++) {
							setData.dubEpisodes.Add(_val.urls[i]);
						}
					}
				}
			}

			return setData;
		}

		public struct AnimeSimpleTitle
		{
			public int malId;
			public string title;
			public string japName;
			public string id;
		}

		public List<AnimeSimpleTitle> Search(string search)
		{
			List<AnimeSimpleTitle> titles = new List<AnimeSimpleTitle>();
			string d = DownloadString("https://ww1.animesimple.com/search?q=" + search);
			const string lookFor = "\" title=\"";
			while (d.Contains(lookFor)) {
				//string name = FindHTML(d, lookFor, "\"");
				d = RemoveOne(d, lookFor);
				string href = FindHTML(d, "href=\"", "\"");
				var _title = GetAnimeSimpleTitle(href);
				if (_title.HasValue) {
					titles.Add(_title.Value);
				}
			}
			return titles;
		}

		public AnimeSimpleTitle? GetAnimeSimpleTitle(string url)
		{
			try {
				string _d = DownloadString(url);
				if (!_d.IsClean()) return null;
				string malId = FindHTML(_d, "https://myanimelist.net/anime/", "\"");
				string title = FindHTML(_d, "media-heading\">", "<");
				string japName = FindHTML(_d, "text-muted\">", "<");
				string id = FindHTML(_d, "value=\"", "\"");
				return new AnimeSimpleTitle() { japName = japName, title = title, malId = int.Parse(malId), id = id };
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		public struct AnimeSimpleEpisodes
		{
			public int dubbedEpisodes;
			public int subbedEpisodes;
			public string[] urls;
		}

		AnimeSimpleEpisodes? GetAnimeSimpleEpisodes(string id)
		{
			try {
				string _d = DownloadString("https://ww1.animesimple.com/request?anime-id=" + id + "&epi-page=1&top=10000&bottom=1");
				if (!_d.IsClean()) return null;
				const string lookFor = "href=\"";

				int dubbedEpisodes = 0;
				int subbedEpisodes = 0;
				List<string> urls = new List<string>();
				while (_d.Contains(lookFor)) {
					try {
						string url = FindHTML(_d, lookFor, "\"");
						_d = RemoveOne(_d, lookFor);
						urls.Add(url);
						string subDub = FindHTML(_d, "success\">", "<");
						bool isDub = subDub.Contains("Dubbed");
						bool isSub = subDub.Contains("Subbed");

						int.TryParse(FindHTML(_d, "</i> Episode ", "<"), out int episode);
						if (episode == 0) {
							error("ANISIMPLE EPISODE::: " + _d);
						}
						else {
							if (isDub) {
								dubbedEpisodes = episode;
							}
							if (isSub) {
								subbedEpisodes = episode;
							}
						}
					}
					catch (Exception _ex) {
						error(_ex);
					}
				}
				return new AnimeSimpleEpisodes() { urls = urls.ToArray(), dubbedEpisodes = dubbedEpisodes, subbedEpisodes = subbedEpisodes };
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}
	}
}
