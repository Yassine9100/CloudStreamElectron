using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Core.CloudStreamCore.TheMovieHelper;

namespace CloudStreamForms.Core.AnimeProviders
{
	class TheMovieBloatFreeProvider : BloatFreeBaseAnimeProvider
	{
		public TheMovieBloatFreeProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "WatchMovies";

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };

			try {
				string name = ms.name;
				var list = (List<TheMovieTitle>)storedData;

				string compare = ToDown(name, true, "");
				var end = list.Where(t => (t.href.Contains("/anime-info/")) && ToDown(t.name, true, "") == compare).OrderBy(t => { FuzzyMatch(t.name, name, out int score); return -score; }).ToArray();

				bool subExists = false;
				bool dubExists = false;
				string subUrl = "";
				string dubUrl = "";
				for (int k = 0; k < end.Length; k++) {
					if (!subExists && !end[k].isDub) {
						subExists = true;
						subUrl = end[k].href;
					}
					if (!dubExists && end[k].isDub) {
						dubExists = true;
						dubUrl = end[k].href;
					}
				}

				try {
					int maxSubbedEp = subExists ? TheMovieHelper.GetMaxEp(DownloadString(subUrl), subUrl) : 0;
					if (!GetThredActive(tempThread)) { return setData; }; // COPY UPDATE PROGRESS
					int maxDubbedEp = dubExists ? TheMovieHelper.GetMaxEp(DownloadString(dubUrl), dubUrl) : 0;
					if (!GetThredActive(tempThread)) { return setData; }; // COPY UPDATE PROGRESS 

					for (int i = 0; i < maxDubbedEp; i++) {
						setData.dubEpisodes.Add(dubUrl);
					}

					for (int i = 0; i < maxSubbedEp; i++) {
						setData.subEpisodes.Add(subUrl);
					}
					return setData;
				}
				catch (Exception _ex) {
					error("ANIME ERROROROROOR.::" + _ex);
					return setData;
				}
			}
			catch (Exception) {
				return setData;
			}
		}

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			string url = (episodeLink + "-episode-" + episode).Replace("/anime-info/", "/anime/");
			string d = DownloadString(url);
			if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
			AddEpisodesFromMirrors(tempThred, d, normalEpisode, "Watch", "");
			//  LookForFembedInString(tempThred, normalEpisode, d);
		}
		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			var list = TheMovieHelper.SearchQuary(activeMovie.title.name, core);
			if (!GetThredActive(tempThred)) { return null; }; // COPY UPDATE PROGRESS
			return list;
		}
	}
}
