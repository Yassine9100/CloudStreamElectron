using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class Embed2Provider : BloatFreeMovieProvider
	{
		public override string Name => "Embed2";
		public override bool NullMetadata => true;
		public Embed2Provider(CloudStreamCore _core) : base(_core) { }
		public override bool HasAnimeMovie => true;
		public override bool HasMovie => true;
		public override bool HasTvSeries => true;

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			return null;
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			string imdbId = ActiveMovie.title.id;
			string imdbD = DownloadString(isMovie ? $"https://www.2embed.ru/embed/imdb/movie?id={imdbId}" : $"https://www.2embed.ru/embed/imdb/tv?id={imdbId}&s={season}&e={episode}");
			const string lookFor = "data-id=\"";
			List<string> ids = new List<string>();
			while (imdbD.Contains(lookFor)) {
				ids.Add(FindHTML(imdbD, lookFor, "\""));
				imdbD = RemoveOne(imdbD, lookFor);
			}
			foreach (var id in ids) {
				string d = DownloadString($"https://www.2embed.ru/ajax/embed/play?id={id}&_token=");
				string link = FindHTML(d, "\"link\":\"", "\"");
				if (link.Contains("vidcloud.pro")) {
					string _d = DownloadString(link);
					string sources = FindHTML(_d, "sources = [", "],").Replace("\\", "");
					var links = GetAllFilesRegex(sources);
					foreach (var _link in links) {
						AddPotentialLink(normalEpisode, _link.url, "VidCloud Embed", 5, _link.label);
					}
				}
			}
		}
	}
}
