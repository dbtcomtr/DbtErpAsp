using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Deneme_proje.Repository;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class DiokiController : Controller
    {
        private readonly DiokiRepository _repository;

        public DiokiController(DiokiRepository repository)
        {
            _repository = repository;
        }

        // Ana sayfa görünümünü döndüren Index metodu
        public IActionResult Index()
        {
            var barkodTanimi = _repository.KullaniciBilgisiyleBarkodTanimlariniGetir(); // Genişletilmiş veriyi çekiyoruz
            return View(barkodTanimi);
        }

        [AllowAnonymous]
        public IActionResult ÜretimListesi()
        {
            var barkodTanimi = _repository.KullaniciBilgisiyleBarkodTanimlariniGetir(); // Genişletilmiş veriyi çekiyoruz
            return View(barkodTanimi);
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetMarkalar()
        {
            var markalar = _repository.GetMarkalar();
            return Json(markalar);
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetModeller(string markaKodu)
        {
            var modeller = _repository.GetModeller(markaKodu);
            return Json(modeller);
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetKisaIsimler(string markaKodu, string modelKodu, string ambalajKodu)
        {
            var isimler = _repository.GetKisaIsimler(markaKodu, modelKodu, ambalajKodu);
            return Json(isimler);
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetAmbalajKodlari(string markaKodu, string modelKodu)
        {
            var ambalajKodlari = _repository.GetAmbalajKodlari(markaKodu, modelKodu);
            return Json(ambalajKodlari);
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult ExecuteVideojet2Micro(string kisaIsim, int depo, int miktar, int lotNo)
        {
            try
            {
                // Kısa isime göre stokkodu al
                string stokkodu = _repository.GetStokKodByKisaIsim(kisaIsim);
                if (string.IsNullOrEmpty(stokkodu))
                {
                    return Json(new { success = false, message = "Stok kodu bulunamadı." });
                }

                // İş emrini fn_IsEmriOperasyon fonksiyonu ile al
                string isEmri = _repository.GetIsemriByFn(stokkodu);
                if (string.IsNullOrEmpty(isEmri))
                {
                    return Json(new { success = false, message = $"'{kisaIsim}' için aktif iş emri bulunamadı." });
                }

                // Prosedürü çalıştır
                var (barkod, makine) = _repository.ExecuteVideojet2Micro(isEmri, stokkodu, depo, miktar, lotNo);

                // Kullanıcı numarasını session'dan al
                string userNo = HttpContext.Session.GetString("UserNo");
                if (!string.IsNullOrEmpty(userNo) && !string.IsNullOrEmpty(barkod))
                {
                    // Barkod üzerine kullanıcı numarasını kaydet
                    _repository.BarkodKullaniciGuncelle(barkod, userNo);
                }

                return Json(new
                {
                    success = true,
                    barkod,
                    makine,
                    isEmri // İş emri bilgisini de dön
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecuteVideojet2Micro işlemi sırasında hata oluştu: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Yeni eklenen "Hatalı Üretim" işaretleme metodu
        [HttpPost]
        [AllowAnonymous]
        public JsonResult UretimiHataliOlarakIsaretle(string barkod)
        {
            try
            {
                if (string.IsNullOrEmpty(barkod))
                {
                    return Json(new { success = false, message = "Barkod bilgisi gereklidir." });
                }

                _repository.BarkodHataliOlarakIsaretle(barkod);

                return Json(new { success = true, message = "Üretim hatalı olarak işaretlendi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Üretim hatalı olarak işaretlenirken hata oluştu: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}