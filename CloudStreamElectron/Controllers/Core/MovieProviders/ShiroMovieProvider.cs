using CloudStreamForms.Core.AnimeProviders;
using CloudStreamForms.Core.BaseProviders;
using System;
using System.Linq;
using static CloudStreamForms.Core.BaseProviders.ShiroBaseProvider;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class ShiroMovieProvider : BloatFreeMovieProvider
	{
		public ShiroBaseProvider shiroBase;
		public string Token => shiroBase.Token;
		public override string Name => shiroBase._name;
		public override bool HasMovie => true;
		public override bool HasAnimeMovie => true;
		public override bool HasTvSeries => false;
		public override bool NullMetadata => false;

		public ShiroMovieProvider(CloudStreamCore _core, string siteUrl, string name) : base(_core)
		{
			shiroBase = new ShiroBaseProvider(_core, siteUrl, name, true);
		}

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			if (!isMovie) return null;
			RealShiroItem?[] items = new RealShiroItem?[2] { null, null };
			bool returnNull = true;
			var bData = shiroBase.StoreData(tempThred, ToDown(ActiveMovie.title.name).Replace("  ", ""));
			foreach (var data in bData) {
				if (data.year != ActiveMovie.title.year.Substring(0, 4)) {
					continue;
				}
				if (data.synonyms.Contains(data.name) || ToDown(data.name) == ToDown(ActiveMovie.title.name) || ToDown(data.name) == ToDown(ActiveMovie.title.ogName)) {
					returnNull = false;
					items[data.isDub ? 1 : 0] = data;
				}
			}
			return returnNull ? null : items;
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			foreach (var item in (RealShiroItem?[])metadata) {
				if (item != null) {
					shiroBase.LoadLink($"https://ani.api-web.site/anime-episode/slug/{item.Value.slug}-episode-1?token={Token}", normalEpisode, tempThred, item.Value.isDub, $"{item.Value.slug}-episode-1");
				}
			}
		}
	}
}
