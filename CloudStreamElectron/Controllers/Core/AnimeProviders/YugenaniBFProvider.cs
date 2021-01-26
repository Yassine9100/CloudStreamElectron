using CloudStreamForms.Core.BaseProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using static CloudStreamForms.Core.BaseProviders.YugenaniBaseProvider;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class YugenaniBFProvider : BloatFreeBaseAnimeProvider
	{
		readonly YugenaniBaseProvider baseProvider;

		public YugenaniBFProvider(CloudStreamCore _core) : base(_core)
		{
			if (Settings.IsProviderActive(Name)) {
				baseProvider = new YugenaniBaseProvider();
			}
		}

		public override string Name => "Yugenani";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			if (baseProvider == null) return;
			var embed = baseProvider.GetEmbed(episodeLink);
			var embedData = baseProvider.GetEmbedData(embed);
			if (embedData.HasValue) {
				var data = embedData.Value;
				foreach (var link in data.multi) {
					AddPotentialLink(normalEpisode, link.src, "YuLink", link.size / 100, link.size >= 100 ? link.size + "p" : "");
				}

				/*foreach (var link in data.sources) { // CHECK GogoStream or StreamTape
				if (link.type.StartsWith("video/") && link.src.Length > 10) {
					print(link.src + "|");
				}
				}*/
			}
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			if (baseProvider == null) return null;
			var res = baseProvider.SearchSite(malData.engName);
			if (res.HasValue) {
				return res.Value.query;
			}
			else {
				return null;
			}
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			YuSearchItems[] data = (YuSearchItems[])storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			if (baseProvider == null) return setData;
			int index = 0;

			foreach (var subData in data) {
				string toDownTitle = ToDown(subData.fields.title);
				if (index < 2 || ToDown(ms.name) == toDownTitle || ToDown(ms.engName) == toDownTitle || ToDown(ms.japName) == toDownTitle) { // CAN BE REMOVED, BUT MIGHT BE BLOCKED DUE TO MANY REQUESTS
					var info = baseProvider.GetYuInfo(subData.pk, subData.fields.slug);
					if (!GetThredActive(tempThread)) return setData;
					if (info.HasValue) {
						var infoVal = info.Value;
						if (infoVal.malId == ms.MalId) {
							for (int i = 0; i < infoVal.subbedEps; i++) {
								setData.subEpisodes.Add($"https://yugenani.me/watch/{subData.pk}/{subData.fields.slug}/{(i + 1)}/");
							}
							for (int i = 0; i < infoVal.dubbedEps; i++) {
								setData.dubEpisodes.Add($"https://yugenani.me/watch/{subData.pk}/{subData.fields.slug}-dub/{(i + 1)}/");
							}
							return setData;
						}
					}
				}

				index++;
			}

			return setData;
		}
	}
}
