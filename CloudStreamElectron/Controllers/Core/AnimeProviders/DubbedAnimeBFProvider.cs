using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;
using HtmlAgilityPack.CssSelectors.NetCore;
using HtmlAgilityPack.CssSelectors;
using static CloudStreamForms.Core.BaseProviders.DubbedAnimeBaseProvider;
using CloudStreamForms.Core.BaseProviders;
using Newtonsoft.Json;
using System;

namespace CloudStreamForms.Core.AnimeProviders
{
	class DubbedAnimeBFProvider : BloatFreeBaseAnimeProvider
	{
		readonly DubbedAnimeBaseProvider provider;
		public DubbedAnimeBFProvider(CloudStreamCore _core) : base(_core)
		{
			provider = new DubbedAnimeBaseProvider(_core);
		}

		public override string Name => "DubbedAnime";
		public override bool HasSub => false;

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			var ep = provider.GetDubbedAnimeEpisode(episodeLink, episode);
			if (ep.HasValue) {
				provider.AddMirrors(ep.Value, normalEpisode);
			}
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			return provider.Search(ActiveMovie.title.name.Replace(".","").Replace("/","/").Replace("\'", ""));
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			List<DubbedSearchItem> data = (List<DubbedSearchItem>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			foreach (var subData in data) {
				if (subData.isMovie) continue;
				// SEE https://bestdubbedanime.com/search/_
				string name = subData.name.ToLower();
				string baseName = ToDown(name);
				if (baseName.StartsWith(ToDown(ms.name)) || baseName.StartsWith(ToDown(ActiveMovie.title.name)) || baseName.StartsWith(ToDown(ActiveMovie.title.ogName))) {
					int season = 1;
					if (name.Contains("season 2") || name.Contains("2nd season") || name.Contains("second season") || (name.EndsWith(" 2") && !name.EndsWith("part 2"))) {
						season = 2;
					}
					else if (name.Contains("season 3") || name.Contains("3rd season") || (name.EndsWith(" 3") && !name.EndsWith("part 3"))) {
						season = 3;
					}
					if (ms.season != season) continue;
					int part = 1;
					if (name.Contains("part 2") || name.Contains("part ii")) {
						part = 2;
					}
					if (ms.part != part) continue;
					var ep = provider.GetDubbedAnimeEpisode(subData.slug, 1);
					if (!ep.HasValue) continue;

					for (int i = 0; i < ep.Value.totalEp; i++) {
						setData.dubEpisodes.Add(subData.slug);
					}
					break;
				}
			}

			return setData;
		}
	}
}