using CloudStreamForms.Core.BaseProviders;
using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class DubbedAnimeMovieProvider : BloatFreeMovieProvider
	{
		readonly DubbedAnimeBaseProvider provider;
		public override string Name => "DubbedAnime";
		public override bool NullMetadata => true;
		public override bool HasTvSeries => false;
		public override bool HasAnimeMovie => true;
		public override bool HasMovie => false;
		public DubbedAnimeMovieProvider(CloudStreamCore _core) : base(_core) { provider = new DubbedAnimeBaseProvider(_core); provider.FishMainMovies(); }

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			if (DubbedAnimeBaseProvider.hasSearchedMovies && DubbedAnimeBaseProvider.movies.Count > 0) {
				foreach (var mov in DubbedAnimeBaseProvider.movies) {
					var baseName = ToDown(mov.name);
					if (baseName == ToDown(ActiveMovie.title.name) || baseName == ToDown(ActiveMovie.title.ogName)) {
						var ep = provider.GetDubbedAnimeEpisode(mov.slug);
						if (ep.HasValue) {
							provider.AddMirrors(ep.Value, normalEpisode);
							return;
						}
					}
				}
			}
		}
	}
}
