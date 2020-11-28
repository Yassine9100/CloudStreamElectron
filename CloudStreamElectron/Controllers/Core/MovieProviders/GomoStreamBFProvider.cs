using Jint;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class GomoStreamBFProvider : BloatFreeMovieProvider
	{
		public override string Name => "GomoStream";
		public override bool NullMetadata => true;
		public GomoStreamBFProvider(CloudStreamCore _core) : base(_core) { }

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			string find = activeMovie.title.name.ToLower() + (activeMovie.title.movieType == MovieType.TVSeries ? "-season-" + season : "");
			find = find.Replace("\'", "-");
			Regex rgx = new Regex("[^a-zA-Z0-9 -]");
			find = rgx.Replace(find, "");

			find = find.Replace(" - ", "-").Replace(" ", "-");

			if (activeMovie.title.movieType == MovieType.TVSeries) { // ADD CORRECT FORMAT; https://gomostream.com/show/game-of-thrones/01-01
				find = find.Replace("-season-", "/");

				for (int i = 0; i < 10; i++) {
					if (find.EndsWith("/" + i)) {
						find = find.Replace("/" + i, "/0" + i);
					}
				}
				if (episode.ToString() != "-1") {
					find += "-" + episode;
				}
				for (int i = 0; i < 10; i++) {
					if (find.EndsWith("-" + i)) {
						find = find.Replace("-" + i, "-0" + i);
					}
				}
			}
			string gomoUrl = "https://" + GOMOURL + "/" + (isMovie ? "movie" : "show") + "/" + find;

			DownloadGomoSteam(gomoUrl, tempThred, normalEpisode);
		}


		/// <summary>
		/// GET GOMOSTEAM SITE MIRRORS
		/// </summary>
		/// <param name="url"></param>
		/// <param name="_tempThred"></param>
		/// <param name="episode"></param>
		void DownloadGomoSteam(string url, TempThread tempThred, int episode)
		{
			bool done = true;
			try {
				try {
					string d = "";
					if (d == "") {
						try {
							// d = DownloadString(url, tempThred, false, 2); if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS TODO CHECK
							d = DownloadString(url, tempThred, 2); if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
						}
						catch (System.Exception _ex) {
							error("Error gogo");
						}
					}

					if (d == "") {
						d = GetHTML(url);
						if (!GetThredActive(tempThred)) { return; };
					}

					if (d == "") {
						d = HTMLGet(url, "https://" + GOMOURL);
						if (!GetThredActive(tempThred)) { return; };
					}

					if (d != "") { // If not failed to connect
						debug("Passed gogo download site");

						// ----- JS EMULATION, CHECK USED BY WEBSITE TO STOP WEB SCRAPE BOTS, DID NOT STOP ME >:) -----

						string tokenCode = FindHTML(d, "var tc = \'", "'");
						string _token = FindHTML(d, "_token\": \"", "\"");
						string funct = "function _tsd_tsd_ds(" + FindHTML(d, "function _tsd_tsd_ds(", "</script>").Replace("\"", "'") + " log(_tsd_tsd_ds('" + tokenCode + "'))";
						// print(funct);
						if (funct == "function _tsd_tsd_ds( log(_tsd_tsd_ds(''))") {
							debug(d); // ERROR IN LOADING JS
						}
						string realXToken = "";
						var engine = new Engine()
						.SetValue("log", new Action<string>((a) => { realXToken = a; }));

						engine.Execute(@funct);
						if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
																	 //GetAPI(realXToken, tokenCode, _token, tempThred, episode);
						print("PAssed js test" + realXToken);
						System.Uri myUri = new System.Uri("https://" + GOMOURL + "/decoding_v3.php"); // Can't DownloadString because of RequestHeaders (Anti-bot)
						HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

						// --- Headers ---

						webRequest.Method = "POST";
						webRequest.Headers.Add("x-token", realXToken);
						webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
						webRequest.Headers.Add("DNT", "1");
						webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
						webRequest.Headers.Add("TE", "Trailers");
						webRequest.Headers.Add("Pragma", "Trailers");
						webRequest.ContentType = "application/x-www-form-urlencoded";
						done = false;
						print("Passed token");

						webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
							HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
							Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

							string requestBody = true ? ("tokenCode=" + tokenCode + "&_token=" + _token) : "type=epis&xny=hnk&id=" + tokenCode; // --- RequestHeaders ---

							byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

							postStream.Write(byteArray, 0, byteArray.Length);
							postStream.Close();
							print("PASSED TOKENPOST");

							if (!GetThredActive(tempThred)) { return; };


							// BEGIN RESPONSE
							try {
								_webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
									HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
									try {
										HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
										try {
											using StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream());
											if (!GetThredActive(tempThred)) { print(":("); return; };
											print("GOT RESPONSE:");
											string result = httpWebStreamReader.ReadToEnd();
											print("RESULT:::" + result);
											try {
												if (result != "") {

													// --------------- GOT RESULT!!!!! ---------------


													// --------------- MIRROR LINKS ---------------
													string veryURL = FindHTML(result, "https:\\/\\/verystream.com\\/e\\/", "\"");
													string gunURL = "https://gounlimited.to/" + FindHTML(result, "https:\\/\\/gounlimited.to\\/", ".html") + ".html";
													string onlyURL = "https://onlystream.tv" + FindHTML(result, "https:\\/\\/onlystream.tv", "\"").Replace("\\", "");
													//string gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
													string upstream = FindHTML(result, "https:\\/\\/upstream.to\\/embed-", "\"");
													string mightyupload = FindHTML(result, "https:\\/\\/mightyupload.com\\/embed-", "\"");//FindHTML(result, "http:\\/\\/mightyupload.com\\/", "\"").Replace("\\/", "/");
																																		  //["https:\/\/upstream.to\/embed-05mzggpp3ohg.html","https:\/\/gomo.to\/vid\/eyJ0eXBlIjoidHYiLCJzIjoiMDEiLCJlIjoiMDEiLCJpbWQiOiJ0dDA5NDQ5NDciLCJfIjoiMzQyMDk0MzQzMzE4NTEzNzY0IiwidG9rZW4iOiI2NjQ0MzkifQ,,&noneemb","https:\/\/hqq.tv\/player\/embed_player.php?vid=SGVsWVI5aUNlVTZxTTdTV09RY0x6UT09&autoplay=no",""]
													if (upstream != "") {
														string _d = DownloadString("https://upstream.to/embed-" + upstream);
														if (!GetThredActive(tempThred)) { return; };

														var links = GetAllFilesRegex(_d);
														int prio = 16;
														foreach (var link in links) {
															AddPotentialLink(episode, link.url, "HD Upstream", prio, link.label);
														}
													}

													/*
                                                    if (mightyupload != "") {
                                                        print("MIGHT: " + mightyupload);
                                                        string baseUri = "http://mightyupload.com/embed-" + mightyupload;
                                                        //string _d = DownloadString("http://mightyupload.com/" + mightyupload);
                                                        string post = "op=download1&usr_login=&id=" + (mightyupload.Replace(".html", "")) + "&fname=" + (mightyupload.Replace(".html", "") + "_play.mp4") + "&referer=&method_free=Free+Download+%3E%3E";

                                                        string _d = PostRequest(baseUri, baseUri, post, tempThred);//op=download1&usr_login=&id=k9on84m2bvr9&fname=tt0371746_play.mp4&referer=&method_free=Free+Download+%3E%3E
                                                        print("RESMIGHT:" + _d);
                                                        if (!GetThredActive(tempThred)) { return; };
                                                        string ur = FindHTML(_d, "<source src=\"", "\"");
                                                        AddPotentialLink(episode, ur, "HD MightyUpload", 16);
                                                    }*/
													/*
                                                    if (gogoStream.EndsWith(",&noneemb")) {
                                                        result = RemoveOne(result, ",&noneemb");
                                                        gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
                                                    }


                                                    gogoStream = gogoStream.Replace(",,&noneemb", "").Replace("\\", "");

                                                    */

													/*
                                                    Episode ep = activeMovie.episodes[episode];
                                                    if (ep.links == null) {
                                                        activeMovie.episodes[episode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                                                    }*/

													if (veryURL != "") {
														try {
															if (!GetThredActive(tempThred)) { return; };

															d = DownloadString("https://verystream.com/e/" + veryURL);
															if (!GetThredActive(tempThred)) { return; };

															// print(d);
															debug("-------------------- HD --------------------");
															url = "https://verystream.com/gettoken/" + FindHTML(d, "videolink\">", "<");
															debug(url);
															if (url != "https://verystream.com/gettoken/") {
																/*
                                                                if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                    // print(activeMovie.episodes[episode].Progress);
                                                                    activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 10, name = "HD Verystream" });
                                                                    linkAdded?.Invoke(null, 1);
                                                                }*/
																AddPotentialLink(episode, url, "HD Verystream", 20);
															}

															debug("--------------------------------------------");
															debug("");
														}
														catch (System.Exception) {

														}

													}
													else {
														debug("HD Verystream Link error (Read api)");
														debug("");
													}
													//   activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

													const string __lookFor = "https:\\/\\/gomo.to\\/vid\\/";
													while (result.Contains(__lookFor)) {
														string gogoStream = FindHTML(result, __lookFor, "\"");
														result = RemoveOne(result, __lookFor);
														if (gogoStream != "") {
															debug(gogoStream);
															try {
																if (!GetThredActive(tempThred)) { return; };
																string trueUrl = "https://" + GOMOURL + "/vid/" + gogoStream;
																//print(trueUrl);
																d = DownloadString(trueUrl);
																//print("-->><<__" + d);
																if (!GetThredActive(tempThred)) { return; };

																var links = GetFileFromEvalData(d);

																foreach (var link in links) {
																	AddPotentialLink(episode, link.url, "Gomoplayer", 7, link.label);
																}
															}
															catch (System.Exception) {
															}

														}
														else {
															debug("HD Viduplayer Link error (Read api)");
															debug("");
														}
													}

													// activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

													if (gunURL != "https://gounlimited.to/.html" && gunURL != "" && gunURL != "https://gounlimited.to/") {
														try {
															if (!GetThredActive(tempThred)) { return; };

															d = DownloadString(gunURL);
															if (!GetThredActive(tempThred)) { return; };

															string mid = FindHTML(d, "mp4|", "|");
															string server = FindHTML(d, mid + "|", "|");
															url = "https://" + server + ".gounlimited.to/" + mid + "/v.mp4";
															if (mid != "" && server != "") {
																/*
                                                                if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                    // print(activeMovie.episodes[episode].Progress);

                                                                    activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 8, name = "HD Go Unlimited" });
                                                                    linkAdded?.Invoke(null, 1);

                                                                }*/
																AddPotentialLink(episode, url, "HD Go Unlimited", 18);

															}
															debug("-------------------- HD --------------------");
															debug(url);

															debug("--------------------------------------------");
															debug("");
														}
														catch (System.Exception) {

														}

													}
													else {
														debug("HD Go Link error (Read api)");
														debug("");
													}
													// activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

													if (onlyURL != "" && onlyURL != "https://onlystream.tv") {
														try {
															if (!GetThredActive(tempThred)) { return; };

															d = DownloadString(onlyURL);
															if (!GetThredActive(tempThred)) { return; };

															string _url = FindHTML(d, "file:\"", "\"");

															if (_url == "") {
																_url = FindHTML(d, "src: \"", "\"");
															}

															bool valid = false;
															if (CheckIfURLIsValid(_url)) { // NEW USES JW PLAYER I THNIK, EASIER LINK EXTRACTION
																url = _url; valid = true;
															}
															else { // OLD SYSTEM I THINK
																string server = "";//FindHTML(d, "urlset|", "|");
																string mid = FindHTML(d, "logo|", "|");

																if (mid == "" || mid.Length < 10) {
																	mid = FindHTML(d, "mp4|", "|");
																}

																string prefix = FindHTML(d, "ostreamcdn|", "|");

																url = "";
																if (server != "") {
																	url = "https://" + prefix + ".ostreamcdn.com/" + server + "/" + mid + "/v/mp4"; // /index-v1-a1.m3u8 also works if you want the m3u8 file instead
																}
																else {
																	url = "https://" + prefix + ".ostreamcdn.com/" + mid + "/v/mp4";
																}

																if (mid != "" && prefix != "" && mid.Length > 10) {
																	valid = true;
																}
															}

															if (valid) {
																AddPotentialLink(episode, url, "HD Onlystream", 17);
															}
															else {
																debug(d);
																debug("FAILED URL: " + url);
															}

															debug("-------------------- HD --------------------");
															debug(url);

															debug("--------------------------------------------");
															debug("");
														}
														catch (System.Exception) {

														}

													}
													else {
														debug("HD Only Link error (Read api)");
														debug("");
													}

													done = true;
												}
												else {
													done = true;
													debug("DA FAILED");
												}
											}
											catch (Exception) {
												done = true;
											}
										}
										catch (Exception _ex) {
											error("FATAL EX IN TOKENPOST2:" + _ex);
										}
									}
									catch (Exception _ex) {
										error("FATAL EX IN TOKENPOST2:" + _ex);
									}
								}), _webRequest);
							}
							catch (Exception _ex) {
								error("FATAL EX IN TOKENPOST:" + _ex);
							}

						}), webRequest);
					}
					else {
						debug("Dident get gogo");
					}
				}
				catch (System.Exception _ex) {
					error(_ex);
				}
			}
			finally {
				while (!done) {
					Thread.Sleep(20);
				}
			}
		}
	}
}