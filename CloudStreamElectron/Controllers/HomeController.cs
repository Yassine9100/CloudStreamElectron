using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CloudStreamElectron.Models;
using static CloudStreamElectron.Controllers.StaticData;
using Newtonsoft.Json;
using CloudStreamForms.Core;
using Microsoft.AspNetCore.Http;

namespace CloudStreamElectron.Controllers
{
	public static class StaticData
	{
		public static Dictionary<Guid, CloudStreamCore> cores = new Dictionary<Guid, CloudStreamCore>();
	}

	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			if (HybridSupport.IsElectronActive) {
				Electron.IpcMain.On("search", (object s) => {
					var mainWindow = Electron.WindowManager.BrowserWindows.First();
					string search = s.ToString();
					Electron.IpcMain.Send(mainWindow, "returnsearch", "Hello", "World");
					// Electron.Dialog.ShowErrorBox("dada", search);
				});
				Electron.IpcMain.On("hello", (args) => {
					//Electron.App.Quit();
				});
			}

			return View();
		} 

		[Route("Search")]
		[HttpGet]   //use or not works same
					//[ValidateAntiForgeryToken]
		public async Task<string> Search(string search, string guid)
		{
			var core = CoreHolder.GetCore(guid);
			await core.QuickSearch(search, blocking: true);

			return (JsonConvert.SerializeObject(core.activeSearchResults.ToArray()));
		}

		[Route("Setup")]
		[HttpGet]   //use or not works same
					//[ValidateAntiForgeryToken]
		public IActionResult Setup(string guid)
		{
			return Json(CoreHolder.CheckGuid(guid));
			/*
			Guid guid = Guid.NewGuid();
			cores.Add(guid, new CloudStreamCore());
			return Json(guid);*/
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
