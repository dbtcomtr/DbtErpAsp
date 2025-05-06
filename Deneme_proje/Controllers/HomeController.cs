using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DatabaseSelectorService _dbSelectorService;

        public HomeController(
            ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor,
            DatabaseSelectorService dbSelectorService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _dbSelectorService = dbSelectorService;
        }

        public IActionResult Index()
        {
            // Kullanıcı oturumu kontrolü
            var username = HttpContext.Session.GetString("Username");
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");

            if (string.IsNullOrEmpty(username) || isAuthenticated != "true")
            {
                // Eğer kullanıcı doğrulanmamışsa login sayfasına yönlendir
                return RedirectToAction("Index", "Login");
            }

            if (HttpContext.Session.GetString("SelectedDatabase") != null)
            {
                TempData["SelectedDatabase"] = HttpContext.Session.GetString("SelectedDatabase");
            }

            return View();
        }

        // SelectDatabase metodu BaseController'da kalacak, burada kaldırıyoruz

        public IActionResult Settings()
        {
            return View();
        }
    }
}