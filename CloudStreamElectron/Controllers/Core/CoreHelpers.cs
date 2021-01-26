using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core
{
	public class BlotFreeProvider
	{
		[System.Serializable]
		public struct NonBloatSeasonData
		{
			public string name; // ID OF PROVIDER
			public bool SubExists => subEpisodes != null && subEpisodes.Where(t => t.IsClean()).Count() > 0;
			public bool DubExists => dubEpisodes != null && dubEpisodes.Where(t => t.IsClean()).Count() > 0;
			public List<string> subEpisodes;
			public List<string> dubEpisodes;
			public object extraData;
		}

		public class BloatFreeMovieProvider : BaseMovieProvier
		{
			public BloatFreeMovieProvider(CloudStreamCore _core) : base(_core) { }

			static readonly object metadataLock = new object();

			public override void FishMainLinkTSync(TempThread tempThread)
			{
				if (!TypeCheck()) return;
				object storedData = NullMetadata ? null : StoreData(ActiveMovie.title.IsMovie, tempThread);
				if (storedData == null && !NullMetadata) return;
				lock (metadataLock) {
					if (ActiveMovie.title.movieMetadata == null) {
						core.activeMovie.title.movieMetadata = new List<MovieMetadata>();
					}
					core.activeMovie.title.movieMetadata.Add(new MovieMetadata() { name = Name, metadata = storedData });
				}
			}

			public override void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
			{
				if (!TypeCheck()) return;
				object data = GetData(ActiveMovie.title, out bool suc);
				if (suc) {
					LoadLink(data, episode, season, normalEpisode, ActiveMovie.title.IsMovie, tempThred);
				}
			}

			bool TypeCheck()
			{
				return (ActiveMovie.title.movieType == MovieType.AnimeMovie && HasAnimeMovie) || (ActiveMovie.title.movieType == MovieType.Movie && HasMovie) || (ActiveMovie.title.movieType == MovieType.TVSeries && HasTvSeries);
			}

			public virtual bool HasMovie => true;
			public virtual bool HasTvSeries => true;
			public virtual bool HasAnimeMovie => true;
			public virtual bool NullMetadata => false;

			object GetData(Title data, out bool suc)
			{
				var list = data.movieMetadata.Where(t => (string)t.name == Name).ToList();
				suc = list.Count > 0;
				return (suc ? list[0] : new MovieMetadata()).metadata;
			}

			public virtual void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
			{
				throw new NotImplementedException();
			}

			public virtual object StoreData(bool isMovie, TempThread tempThred)
			{
				throw new NotImplementedException();
			}

		}


		public class BloatFreeBaseAnimeProvider : BaseAnimeProvider
		{
			public virtual bool NullMetadata => false;

			public BloatFreeBaseAnimeProvider(CloudStreamCore _core) : base(_core) { }

			public override int GetLinkCount(int currentSeason, bool isDub, TempThread? tempThred)
			{
				int count = 0;
				try {
					for (int q = 0; q < ActiveMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
						var list = ActiveMovie.title.MALData.seasonData[currentSeason].seasons[q].nonBloatSeasonData.Where(t => t.name == Name).ToList();
						if (list.Count > 0) {
							var ms = list[0];
							if ((ms.DubExists && isDub) || (ms.SubExists && !isDub)) {
								List<string> episodes = isDub ? ms.dubEpisodes : ms.subEpisodes;
								int maxCount = 0;
								for (int i = 0; i < episodes.Count; i++) {
									if (episodes[i].IsClean()) {
										maxCount = i + 1;
									}
								}

								count += maxCount;//(isDub ? ms.dubEpisodes.Select(t => t.IsClean(),).Count : ms.subEpisodes.Count);
							}
						}
					}
				}
				catch (Exception) {
				}
				return count;
			}

			public NonBloatSeasonData GetData(MALSeason data, out bool suc)
			{
				var list = data.nonBloatSeasonData.Where(t => t.name == Name).ToList();
				suc = list.Count > 0;
				return suc ? list[0] : new NonBloatSeasonData();
			}

			public override void GetHasDubSub(MALSeason data, out bool dub, out bool sub)
			{
				var _data = GetData(data, out bool suc);
				if (suc) {
					dub = _data.DubExists;
					sub = _data.SubExists;
				}
				else {
					dub = false;
					sub = false;
				}
			}

			public virtual NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
			{
				throw new NotImplementedException();
			}

			public virtual object StoreData(string year, TempThread tempThred, MALData malData)
			{
				throw new NotImplementedException();
			}

			public override void FishMainLink(string year, TempThread tempThred, MALData malData)
			{
				print("FF:::: <>>");
				print("NDNDNDNND;;; " + Name + "|" + year + "|" + malData.engName);
				object storedData = NullMetadata ? null : StoreData(year, tempThred, malData);
				if (storedData == null && !NullMetadata) return;
				for (int i = 0; i < ActiveMovie.title.MALData.seasonData.Count; i++) {
					for (int q = 0; q < ActiveMovie.title.MALData.seasonData[i].seasons.Count; q++) {
						try {
							MALSeason ms;
							lock (_lock) {
								ms = ActiveMovie.title.MALData.seasonData[i].seasons[q];
								ms.season = i;
								ms.part = q + 1;
							}

							NonBloatSeasonData data = GetSeasonData(ms, tempThred, year, storedData);
							data.name = Name;

							lock (_lock) {
								ms = ActiveMovie.title.MALData.seasonData[i].seasons[q];
								if (ms.nonBloatSeasonData == null) {
									ms.nonBloatSeasonData = new List<NonBloatSeasonData>();
								}
								ms.nonBloatSeasonData.Add(data);
								ActiveMovie.title.MALData.seasonData[i].seasons[q] = ms;
							}
						}
						catch (Exception _ex) {
							print("FATAL EX IN Fish " + Name + " | " + _ex);
						}
					}
				}
			}

			public virtual void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
			{
				throw new NotImplementedException();
			}

			public override void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThread tempThred)
			{
				try {
					if ((isDub && !HasDub) || (!isDub && !HasSub)) return;

					int currentep = 0;
					print("DS::::: " + ActiveMovie.title.MALData.seasonData[season].seasons.Count);
					for (int q = 0; q < ActiveMovie.title.MALData.seasonData[season].seasons.Count; q++) {
						var ms = GetData(ActiveMovie.title.MALData.seasonData[season].seasons[q], out bool suc);
						if (suc) {
							int subEp = episode - currentep; //episode - currentep;
							if ((isDub ? ms.dubEpisodes : ms.subEpisodes) != null) {
								currentep += isDub ? ms.dubEpisodes.Count : ms.subEpisodes.Count;
								if (currentep >= episode) {
									try {
										print("LOADING LINK FOR: " + Name);
										LoadLink(isDub ? ms.dubEpisodes[subEp - 1] : ms.subEpisodes[subEp - 1], subEp, normalEpisode, tempThred, ms.extraData, isDub);
									}
									catch (Exception _ex) { print("FATAL EX IN Load: " + Name + " | " + _ex); }
								}
							}
						}
					}
				}
				catch (Exception _ex) {
					error(_ex);
				}
			}
		}
	}

	public static class CoreHelpers
	{
		///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
		///<param name="items">The enumerable to search.</param>
		///<param name="predicate">The expression to test the items against.</param>
		///<returns>The index of the first matching item, or -1 if no items match.</returns>
		public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
		{
			if (items == null) throw new ArgumentNullException("items");
			if (predicate == null) throw new ArgumentNullException("predicate");

			int retVal = 0;
			foreach (var item in items) {
				if (predicate(item)) return retVal;
				retVal++;
			}
			return -1;
		}
		///<summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
		///<param name="items">The enumerable to search.</param>
		///<param name="item">The item to find.</param>
		///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
		public static int IndexOf<T>(this IEnumerable<T> items, T item) { return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i)); }

		public static string ConvertUnixTimeToString(long time)
		{
			return ConvertDateTimeToString(DateTimeOffset.FromUnixTimeSeconds(time).DateTime);
		}
		public static string ConvertDateTimeToString(DateTime time)
		{
			string txt = "";
			//3d 20h 37m
			var local = time.Subtract(DateTime.UtcNow);
			if (local.Days > 0) {
				txt += local.Days + "d ";
			}
			if (local.Hours > 0) {
				txt += local.Hours + "h ";
			}
			txt += local.Minutes + "m";
			return txt;
		}


		public static List<BasicLink> OrderHDLinks(this List<BasicLink> sort)
		{
			List<BasicLink> mirrorInfos = new List<BasicLink>();
			for (int i = 0; i < sortingList.Count; i++) {
				for (int q = 0; q < sort.Count; q++) {
					string name = sort[q].PublicName;
					if ($" {name} ".ToLower().Contains($" {sortingList[i]} ")) {
						mirrorInfos.Add(sort[q]);
					}
				}
			}
			for (int q = 0; q < sort.Count; q++) {
				var add = sort[q];
				if (!mirrorInfos.Contains(add)) {
					mirrorInfos.Add(add);
				}
			}
			return mirrorInfos;
		}
		/// <summary>
		/// Will return the result, if not match then null
		/// </summary>
		/// <param name="input"></param>
		/// <param name="match"></param>
		/// <param name="splitChar"></param>
		/// <param name="searchSplit"></param>
		/// <returns></returns>
		public static string[] GetStringRegex(string input, string match, char splitChar = '?', char searchSplit = ' ')
		{
			try {
				string[] splt = input.Split(searchSplit);
				string rex = "";

				foreach (var sp in splt) {
					string[] sps = sp.Split(splitChar);
					rex += $".*{sps[0]}(.*?){sps[1]}";
				}
				rex = rex.Replace("\"", "\\\"");

				var regex = new Regex(rex);
				if (regex.IsMatch(match)) {
					var mat = regex.Match(match);
					if (mat.Success) {
						string[] data = new string[mat.Groups.Count - 1];
						for (int i = 0; i < data.Length; i++) {
							data[i] = mat.Groups[i + 1].Value;
						}
						return data;
					}
				}
				return null;
			}
			catch (Exception) {
				return null;
			}
		}

		public static bool ContainsStuff<T>(this IList<T> list)
		{
			if (list == null) return false;
			if (list.Count == 0) return false;
			return true;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1) {
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static bool IsMovie(this MovieType mtype)
		{
			return mtype == MovieType.AnimeMovie || mtype == MovieType.Movie || mtype == MovieType.YouTube;
		}

		/// <summary>
		/// If is not null and is not ""
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static bool IsClean(this string s)
		{
			return s != null && s != "";
		}
	}
}
