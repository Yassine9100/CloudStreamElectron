using Newtonsoft.Json;
using System.Collections.Generic;

namespace CloudStreamForms.Core.BaseProviders
{
	public class AnimeFeverBaseProvider
	{
		public struct AnimeFeverSearchPoster
		{
			public string source;
			public int width;
			public int height;
		}

		public struct AnimeFeverSearchPoster2
		{
			public int id;
			public string disk_name;
			public int file_size;
			public string content_type;
			public object title;
			public object description;
			public string field;
			public int sort_order;
			public string created_at;
			public string updated_at;
			public string path;
			public string extension;
		}

		public struct AnimeFeverSearchLogo
		{
			public int id;
			public string disk_name;
			public int file_size;
			public string content_type;
			public object title;
			public object description;
			public string field;
			public int sort_order;
			public string created_at;
			public string updated_at;
			public string path;
			public string extension;
		}

		public struct AnimeFeverSearchDatum
		{
			public string name;
			public string alt_name;
			public int id;
			public string slug;

			/*
            public string uid;
            public object anilist_id;
            public string description;
            public object trailer;
            public string status;
            public string type;
            public int episode_count;
            public string parental_rating;
            public string release_date;
            public string end_date;
            public string broadcast;
            public string premiered;
            public string episode_at;
            public string created_at;
            public string updated_at;
            public int episodes_count;
            public bool is_collected;
            public object collection_status;*/
			public List<AnimeFeverSearchPoster> posters;
			/*
            public object backgrounds;
            public AnimeFeverSearchPoster2 poster;
            public AnimeFeverSearchLogo logo;
            public object background;*/
		}

		public struct AnimeFeverSearchInfo
		{
			//public int? current_page;
			public List<AnimeFeverSearchDatum> data;
			/*
            public string first_page_url;
            public int? from;
            public int? last_page;
            public string last_page_url;
            public object next_page_url;
            public string path;
            public int? per_page;
            public object prev_page_url;
            public int? to;
            public int? total;*/
		}

		public struct AnimeFeverEpisodeImage
		{
			/*
            public int id;
            public string disk_name;
            public int file_size;
            public string content_type;
            public object title;
            public object description;
            public string field;
            public int sort_order;
            public string created_at;
            public string updated_at;
            public string path;
            public string extension;*/
		}

		public struct AnimeFeverEpisodeVideoMeta
		{
			public List<string> audio_languages;
			public string status;
			public long download_size;
		}

		public struct AnimeFeverEpisodeDatum
		{
			public int id;
			//public string title;
			public string slug;
			public string number;
			public AnimeFeverEpisodeVideoMeta video_meta;

			/*
            public int duration;
            public AnimeFeverEpisodeImage image;
            public int is_filler;
            public int is_recap;
            public bool watched;
            public object progress;*/
		}

		public struct AnimeFeverEpisodeLinks
		{
			/*
            public string first;
            public string last;
            public object prev;
            public object next;*/
		}

		public struct AnimeFeverEpisodeMeta
		{
			/*
            public int current_page;
            public int from;
            public int last_page;
            public string path;
            public int per_page;
            public int to;
            public int total;*/
		}

		public struct AnimeFeverEpisodeInfo
		{
			public List<AnimeFeverEpisodeDatum> data;
			public AnimeFeverEpisodeLinks links;
			public AnimeFeverEpisodeMeta meta;
		}

		static readonly string[] headerValue = new string[] { "animefever", "cloudflare" };
		static readonly string[] headerName = new string[] { "AF-Access-API", "server-provider" };
		public AnimeFeverSearchInfo? GetSearchResults(string search, bool isMovie)
		{
			/*   webRequest.Headers.Add("AF-Access-API", "animefever");
            webRequest.Headers.Add("server-provider", "cloudflare");*/
			string qry = $"https://www.animefever.tv/api/anime/shows?search={search}&sortBy=name+asc&type[]={(isMovie ? "Movie" : "TV")}&hasVideos=true&hasMultiAudio=false&page=1";
			string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
				headerName: headerName, headerValue: headerValue);
			if (d == "") {
				return null;
			}
			return JsonConvert.DeserializeObject<AnimeFeverSearchInfo>(d);
		}

		public AnimeFeverEpisodeInfo? GetAnimeFeverEpisodeInfo(int id, string slug)
		{
			string qry = $"https://www.animefever.tv/api/anime/details/episodes?id={id}-{slug}";
			string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
				headerName: headerName, headerValue: headerValue);
			if (d == "") {
				return null;
			}
			return JsonConvert.DeserializeObject<AnimeFeverEpisodeInfo>(d);
		}

		public string GetAnimeFeverEpisodeStream(int id)
		{
			string qry = $"  https://www.animefever.tv/video/{id}/stream.m3u8";
			string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
				headerName: headerName, headerValue: headerValue);

			return d;
		}

		readonly CloudStreamCore core;
		public AnimeFeverBaseProvider(CloudStreamCore _core)
		{
			core = _core;
		}
	}
}
