/*
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class AnimeNameBloatFreeProvider : BloatFreeBaseAnimeProvider
	{
		public AnimeNameBloatFreeProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "AnimeTemplate";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			base.LoadLink(episodeLink, episode, normalEpisode, tempThred, extraData, isDub);
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			return base.StoreData(year, tempThred, malData);
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			List<string> data = (List<string>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			foreach (var subData in data) {

			}

			return setData;
		}
	}
}
*/