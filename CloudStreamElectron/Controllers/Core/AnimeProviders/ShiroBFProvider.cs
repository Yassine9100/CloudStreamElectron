using CloudStreamForms.Core.BaseProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BaseProviders.ShiroBaseProvider;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class ShiroBFProvider : BloatFreeBaseAnimeProvider
	{
		public ShiroBaseProvider shiroBase;
		public string Token => shiroBase.Token;
		private string SiteUrl => shiroBase._siteUrl;
		public override string Name => shiroBase._name;

		public ShiroBFProvider(CloudStreamCore _core, string siteUrl, string name) : base(_core)
		{
			shiroBase = new ShiroBaseProvider(_core, siteUrl, name, false);
		}

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			shiroBase.LoadLink(episodeLink, normalEpisode, tempThred, isDub, ((string[])extraData)[isDub ? 1 : 0]);
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			return shiroBase.StoreData(tempThred, malData.engName);
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			List<RealShiroItem> data = (List<RealShiroItem>)storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			if (Token == "") {
				error("NO SHIRO TOKEN! AT " + nameof(GetSeasonData));
				return setData;
			}

			string[] extraData = new string[2];
			foreach (var subData in data) {
				if ((setData.DubExists && subData.isDub) || (setData.SubExists && !subData.isDub)) continue;

				bool synoExist = false;
				if (subData.synonyms != null && ms.synonyms != null && ms.synonyms.Count > 0 && subData.synonyms.Length > 0) {
					synoExist = ms.synonyms.Where(t => subData.synonyms.Contains(t)).ToArray().Length > 0;
				}

				if (ToDown(ms.engName) == ToDown(subData.name) || synoExist) {
					List<string> episodes = new List<string>();
					string slug = subData.slug;

					for (int i = 1; i <= subData.episodes; i++) {
						episodes.Add($"https://ani.api-web.site/anime-episode/slug/{slug}-episode-{i}?token={Token}");
					}

					if (subData.isDub) {
						setData.dubEpisodes = episodes;
					}
					else {
						setData.subEpisodes = episodes;
					}
					extraData[subData.isDub ? 1 : 0] = slug;
				}
			}
			setData.extraData = extraData;

			return setData;
		}
	}
}
