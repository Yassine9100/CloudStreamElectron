using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class HdmProvider : BloatFreeMovieProvider
	{
		public override string Name => "Hdm";
		public override bool NullMetadata => true;
		public HdmProvider(CloudStreamCore _core) : base(_core) { }

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			return null;
		}

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			if (isMovie) {
				string _name = ActiveMovie.title.name.Replace(":", "");
				string d = DownloadString($"https://hdm.to/src/player/?v=https://1o.to/{_name}.mp4");
				string key = FindHTML(d, "/playlist.m3u8?", "\"");
				if (key != "") {
					var s = $"https://hls.1o.to/vod/{_name}/playlist.m3u8?{key}";
					string verify = DownloadString(s, repeats: 1);
					if (verify != "") { // IF NAME IS WRONG OR IT DOSENT EXIT IT WILL GIVE A 404
						AddPotentialLink(normalEpisode, s, "Hdm HD", 5, "");
					}
				}
			}
		}
	}
}