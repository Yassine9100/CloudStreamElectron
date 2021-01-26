using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class MonkeyStreamProvider : BloatFreeMovieProvider
	{
		public MonkeyStreamProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "MonkeyStream";

		public override object StoreData(bool isMovie, CloudStreamCore.TempThread tempThred)
		{
			try {
				string search = ActiveMovie.title.name;
				string year = ActiveMovie.title.year[0..4];
				string d = DownloadString("https://www.monkeystream.net/search?q=" + search);
				const string lookFor = "<div class=\"movie-title\">";
				while (d.Contains(lookFor)) {
					d = RemoveOne(d, lookFor);
					string _href = FindHTML(d, "href=\"", "\"");
					d = RemoveOne(d, "title=\"");
					string _title = FindHTML(d, "\">", "<");
					string _year = FindHTML(d, "movie-item\">", "<");
					if (_year == year && _title == search) {
						return _href;
					}
				}
				return null;
			}
			catch {
				return null;
			}
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			string mainUrl = (string)metadata;
			if (!mainUrl.IsClean()) return;
			string d = DownloadString(mainUrl, referer: "https://www.monkeystream.net/search?q=" + ActiveMovie.title.name, tempThred: tempThred);
			const string lookFor = "?key=";
			while (d.Contains(lookFor)) {
				string key = FindHTML(d, lookFor, "\"");
				d = RemoveOne(d, lookFor);
				string title = FindHTML(d, "server mr10\">", "<");

				if (title.Contains("MonkeyEmbed")) {
					string _d = DownloadString(mainUrl + "?key=" + key, referer: "https://www.monkeystream.net/search?q=iron+man", tempThred: tempThred);

					string apiSource = FindHTML(_d, "https://www.monkeyembed.xyz/v/", "\"");
					if (apiSource != "") {
						string apiPost = core.PostRequest("https://www.monkeyembed.xyz/api/source/" + apiSource, "https://www.monkeyembed.xyz/v/" + apiSource, $"r={mainUrl}&d=www.monkeyembed.xyz");
						const string _lookFor = "\"file\":\"";
						while (apiPost.Contains(_lookFor)) {
							string file = FindHTML(apiPost, _lookFor, "\"").Replace("\\", "");
							apiPost = RemoveOne(apiPost, _lookFor);
							string label = FindHTML(apiPost, "\"label\":\"", "\"");
							AddPotentialLink(normalEpisode, file, "MonkeyEmbed", 5, label);
						}
					}
				}
			}
		}
	}
}
