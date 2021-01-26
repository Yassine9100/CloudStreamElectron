using System;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class LiveMovies123BFProvider : BloatFreeMovieProvider
	{
		public override string Name => "LiveMovies123";
		public override bool NullMetadata => true;
		public LiveMovies123BFProvider(CloudStreamCore _core) : base(_core) { }

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			try {
				GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://movies123.live", tempThred);
				GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://c123movies.com", tempThred);
			}
			catch (Exception _ex) {
				error("PROVIDER ERROR: " + _ex);
			}
		}

		void GetLiveMovies123Links(int normalEpisode, int episode, int season, bool isMovie, string provider = "https://c123movies.com", TempThread tempThred = default) // https://movies123.live & https://c123movies.com
		{
			string _title = ToDown(ActiveMovie.title.name, replaceSpace: "-");

			string _url = (isMovie ? (provider + "/movies/" + _title) : (provider + "/episodes/" + _title + "-season-" + season + "-episode-" + episode));

			string d = DownloadString(_url, tempThred);
			if (!GetThredActive(tempThred)) { return; };
			string release = FindHTML(d, "Release:</strong> ", "<");
			bool succ = true;
			if (release != ActiveMovie.title.year.Substring(0, 4)) {
				succ = false;
				if (isMovie) {
					d = DownloadString(_url + "-1");
					succ = true;
				}
			}
			if (succ) {
				string live = FindHTML(d, "getlink(\'", "\'");
				if (live != "") {
					string url = provider + "/ajax/get-link.php?id=" + live + "&type=" + (isMovie ? "movie" : "tv") + "&link=sw&" + (isMovie ? "season=undefined&episode=undefined" : ("season=" + season + "&episode=" + episode));
					d = DownloadString(url, tempThred); if (!GetThredActive(tempThred)) { return; };

					string shortURL = FindHTML(d, "iframe src=\\\"", "\"").Replace("\\/", "/");
					d = DownloadString(shortURL, tempThred); if (!GetThredActive(tempThred)) { return; };
					if (!d.IsClean()) return;
					AddEpisodesFromMirrors(tempThred, d, normalEpisode);
				}
			}
		}
	}
}