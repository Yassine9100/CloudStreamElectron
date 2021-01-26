/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
    class YesMoviesBFProvider : BloatFreeMovieProvider
    {
        public override string Name => "YesMovies";
        public YesMoviesBFProvider(CloudStreamCore _core) : base(_core) { }
        
        public override object StoreData(bool isMovie, TempThread tempThred)
        {
            return base.StoreData(isMovie, tempThred);
        }

        public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
        {
            base.LoadLink(metadata, episode, season, normalEpisode, isMovie, tempThred);
        }
    }
}
*/
using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class YesMoviesBFProvider : BloatFreeMovieProvider
	{
		public override string Name => "YesMovies";
		public YesMoviesBFProvider(CloudStreamCore _core) : base(_core) { }
		const string SiteUrl = "https://yesmovies.mom";

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			try {
				string rinput = ToDown(ActiveMovie.title.name, replaceSpace: "+");
				string yesmovies = $"{SiteUrl}/search/?keyword={rinput.Replace("+", "-")}";

				string d = DownloadString(yesmovies, tempThred);
				if (!GetThredActive(tempThred)) { return null; }; // COPY UPDATE PROGRESS
				int counter = 0;
				const string lookfor = "data-url=\"";
				var yesmoviessSeasonDatas = new List<YesmoviessSeasonData>();

				while ((d.Contains(lookfor)) && counter < 100) {
					counter++;
					string url = FindHTML(d, lookfor, "\"");
					string remove = "class=\"ml-mask jt\" title=\"";
					string title = FindHTML(d, remove, "\"");
					string movieUrl = SiteUrl + "/movie/" + FindHTML(d, $"<a href=\"{SiteUrl}/movie/", "\"");
					d = RemoveOne(d, remove);

					int seasonData = 1;
					for (int i = 0; i < 100; i++) {
						if (title.Contains(" - Season " + i)) {
							seasonData = i;
						}
					}
					string realtitle = title.Replace(" - Season " + seasonData, "");
					string _d = DownloadString(url, tempThred);
					if (!GetThredActive(tempThred)) { return null; }; // COPY UPDATE PROGRESS
					string imdbData = FindHTML(_d, "IMDb: ", "<").Replace("\n", "").Replace(" ", "").Replace("	", "");
					//  string year = FindHTML(_d, "<div class=\"jt-info\">", "<").Replace("\n", "").Replace(" ", "").Replace("	", "").Replace("	", "");

					string s1 = ActiveMovie.title.rating;
					string s2 = imdbData;
					if (s2.ToLower() == "n/a") {
						continue;
					}

					if (!s1.Contains(".")) { s1 += ".0"; }
					if (!s2.Contains(".")) { s2 += ".0"; }

					int i1 = int.Parse(s1.Replace(".", ""));
					int i2 = int.Parse(s2.Replace(".", ""));
					//activeMovie.title.year.Substring(0, 4) == year
					if (ToDown(ActiveMovie.title.name, replaceSpace: "") == ToDown(realtitle, replaceSpace: "") && (i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1)) {
						yesmoviessSeasonDatas.Add(new YesmoviessSeasonData() { url = movieUrl, id = seasonData });
					}
				}
				return yesmoviessSeasonDatas;
			}
			catch (Exception) {
				return null;
			}
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			var yesmoviessSeasonDatas = (List<YesmoviessSeasonData>)metadata;
			for (int i = 0; i < yesmoviessSeasonDatas.Count; i++) {
				if (yesmoviessSeasonDatas[i].id == (isMovie ? 1 : season)) {
					string url = yesmoviessSeasonDatas[i].url;
					int _episode = normalEpisode + 1;
					string d = DownloadString(url.Replace("watching.html", "") + "watching.html");

					string movieId = FindHTML(d, "var movie_id = \'", "\'");
					if (movieId == "") return;

					d = DownloadString($"{SiteUrl}/ajax/v2_get_episodes/{movieId}");
					if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

					string episodeId = FindHTML(d, "title=\"Episode " + _episode + "\" class=\"btn-eps\" episode-id=\"", "\"");
					if (episodeId == "") return;
					d = DownloadString($"{SiteUrl}/ajax/load_embed/mov{episodeId}");

					if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

					string embedededUrl = FindHTML(d, "\"embed_url\":\"", "\"").Replace("\\", "") + "=EndAll";
					string __url = FindHTML(embedededUrl, "id=", "=EndAll");
					if (__url == "") return;
					embedededUrl = "https://video.opencdn.co/api/?id=" + __url;
					d = DownloadString(embedededUrl);

					if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
					string link = FindHTML(d, "\"link\":\"", "\"").Replace("\\", "").Replace("//", "https://").Replace("https:https:", "https:");
					d = DownloadString(link);

					if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

					string secondLink = FindHTML(d, "https://vidnode.net/download?id=", "\"");
					if (secondLink != "") {
						d = DownloadString("https://vidnode.net/download?id=" + secondLink);
						if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
						core.GetVidNode(d, normalEpisode);
					}
					LookForFembedInString(tempThred, normalEpisode, d);
				}
			}

		}
	}
}