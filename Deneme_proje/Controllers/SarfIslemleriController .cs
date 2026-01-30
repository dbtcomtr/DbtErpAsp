using System;

using Microsoft.AspNetCore.Mvc;

namespace Deneme_proje.Controllers
{
    public class SarfIslemleriController : Controller
    {
        // GET: SarfIslemleri/SarfCikisTanimla
        [AllowAnonymous]
        public ActionResult SarfCikisTanimla()
        {
            return View();
        }
    }
}