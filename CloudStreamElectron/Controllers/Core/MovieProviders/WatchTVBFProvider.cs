using System;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class WatchTVBFProvider : BloatFreeMovieProvider
	{
		public override string Name => "WatchTv";
		public override bool NullMetadata => true;
		public override bool HasMovie => false;
		public override bool HasAnimeMovie => false;
		public WatchTVBFProvider(CloudStreamCore _core) : base(_core) { }

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			try {
				string url = "https://www.tvseries.video/series/" + ToDown(activeMovie.title.name, replaceSpace: "-") + "/" + "season-" + season + "-episode-" + episode;

				string d = DownloadString(url);
				string vidId = FindHTML(d, " data-vid=\"", "\"");
				if (vidId != "") {
					d = DownloadString("https://www.tvseries.video" + vidId);
					AddEpisodesFromMirrors(tempThred, d, normalEpisode);
				}
			}
			catch (Exception _ex) {
				error("PROVIDER ERROR: " + _ex);
			}
		}
	}
}
