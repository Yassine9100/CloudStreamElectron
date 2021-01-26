using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class TheMovieMovieBFProvider : BloatFreeMovieProvider
	{
		public override string Name => "TheMovie";
		public TheMovieMovieBFProvider(CloudStreamCore _core) : base(_core) { }

		//public override bool HasAnimeMovie => false;

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			try {
				var list = TheMovieHelper.SearchQuary(ActiveMovie.title.name, core);
				if (!GetThredActive(tempThred)) { return null; }; // COPY UPDATE PROGRESS
				MovieType mType = ActiveMovie.title.movieType;
				string compare = ToDown(ActiveMovie.title.name, true, "");
				var watchMovieSeasonsData = new Dictionary<int, string>();

				if (mType.IsMovie()) {
					//string mustContain = mType == MovieType.AnimeMovie ? "/anime-info/" : "/series/";
					string mustContain = isMovie ? "/movie/" : "/series/";
					TheMovieHelper.TheMovieTitle[] matching = list.Where(t => ToDown(t.name, true, "") == compare && t.season == -1 && t.href.Contains(mustContain)).ToArray();
					if (matching.Length > 0) {
						TheMovieHelper.TheMovieTitle title = matching[0];

						string d = DownloadString(title.href);
						int maxEp = TheMovieHelper.GetMaxEp(d, title.href);
						if (maxEp == 0 || maxEp == 1) {
							string rEp = title.href + "/" + (maxEp - 1); //+ "-episode-" + maxEp;
							watchMovieSeasonsData[-1] = rEp;
						}
					}
				}
				else {
					var episodes = list.Where(t => !t.isDub && t.season != -1 && ToDown(t.name, true, "") == compare && t.href.Contains("/series/")).ToList().OrderBy(t => t.season).ToArray();

					for (int i = 0; i < episodes.Length; i++) {
						watchMovieSeasonsData[episodes[i].season] = episodes[i].href;
					}
				}
				return watchMovieSeasonsData;
			}
			catch { return null; }
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			try {
				var watchMovieSeasonsData = (Dictionary<int, string>)metadata;
				void GetFromUrl(string url)
				{
					string _url = url.Replace("/movie/", "/watch/").Replace("/series/", "/watch/");
					string d = DownloadString(_url, tempThred);
					if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
					AddEpisodesFromMirrors(tempThred, d, normalEpisode);
					LookForFembedInString(tempThred, normalEpisode, d);
				}

				if (isMovie) {
					if (watchMovieSeasonsData.ContainsKey(-1)) {
						GetFromUrl(watchMovieSeasonsData[-1]); //.Replace("/anime-info/", "/anime/"));
					}
				}
				else {
					if (watchMovieSeasonsData.ContainsKey(season)) {
						GetFromUrl(watchMovieSeasonsData[season] + "/" + (episode - 1));//.Replace("/anime-info/", "/anime/") + "-episode-" + episode);
					}
				}
			}
			catch (Exception _ex) {
				error("PROVIDER ERROR: " + _ex);
			}
		}
	}
}
