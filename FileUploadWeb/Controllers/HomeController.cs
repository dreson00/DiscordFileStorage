using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FileUploadWeb.Models;

namespace FileUploadWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;


    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            _logger.LogInformation($"{User.Identity.Name} entered website.");
            return RedirectToAction("FileUpload", "FileUpload");
        }
        return RedirectToAction("LoginWithDiscord", "DiscordLogin");
        //return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}