using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Deneme_proje.Repository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class DiokiController : Controller
    {
        private readonly DiokiRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DiokiController(DiokiRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ERPDatabase");
        }

        // Fetch user details (name and business unit)
        //private (string UserName, string IsMerkezi) GetUserDetails(string userNo)
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(_connectionString))
        //        {
        //            connection.Open();
        //            var query = @"
        //        SELECT 
        //            p.per_adi + ' ' + p.per_soyadi AS UserName,
        //            ISNULL(ky.IsMerkezleri, '') AS IsMerkezi
        //        FROM PERSONELLER p
        //        LEFT JOIN [DBT_ERP].dbo.KullaniciYonetimi ky ON p.per_userno = ky.User_no
        //        WHERE p.per_userno = @UserNo";

        //            using (var command = new SqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@UserNo", userNo);
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        return (
        //                            reader["UserName"].ToString(),
        //                            reader["IsMerkezi"].ToString()
        //                        );
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error if needed
        //        Console.WriteLine($"Error fetching user details: {ex.Message}");
        //    }
        //    return (string.Empty, string.Empty);
        //}

        private (string UserName, List<string> BusinessUnits, string SelectedBusinessUnit) GetUserDetails(string userNo)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT 
                            p.per_adi + ' ' + p.per_soyadi AS UserName,
                            ISNULL(ky.IsMerkezleri, '') AS IsMerkezleri
                        FROM PERSONELLER p
                        LEFT JOIN [DBT_ERP].dbo.KullaniciYonetimi ky ON p.per_userno = ky.User_no
                        WHERE p.per_userno = @UserNo";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserNo", userNo);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var userName = reader["UserName"].ToString();
                                var isMerkezleriStr = reader["IsMerkezleri"].ToString();
                                var businessUnits = string.IsNullOrEmpty(isMerkezleriStr)
                                    ? new List<string>()
                                    : isMerkezleriStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(x => x.Trim())
                                                     .ToList();

                                // Seçili iş merkezini session'dan al, yoksa ilkini seç
                                var selectedBusinessUnit = HttpContext.Session.GetString("SelectedBusinessUnit");
                                if (string.IsNullOrEmpty(selectedBusinessUnit) && businessUnits.Count > 0)
                                {
                                    selectedBusinessUnit = businessUnits[0];
                                    HttpContext.Session.SetString("SelectedBusinessUnit", selectedBusinessUnit);
                                }

                                return (userName, businessUnits, selectedBusinessUnit);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kullanıcı bilgileri alınırken hata oluştu: {ex.Message}");
            }
            return (string.Empty, new List<string>(), string.Empty);
        }

        public IActionResult Index()
        {
            var barkodTanimi = _repository.KullaniciBilgisiyleBarkodTanimlariniGetir();
            var userNo = HttpContext.Session.GetString("UserNo");
            var (userName, businessUnits, selectedBusinessUnit) = GetUserDetails(userNo ?? "");

            ViewBag.UserName = userName;
            ViewBag.BusinessUnits = businessUnits;
            ViewBag.SelectedBusinessUnit = selectedBusinessUnit;

            return View(barkodTanimi);
        }

        [AllowAnonymous]
        public IActionResult ÜretimListesi()
        {
            var barkodTanimi = _repository.KullaniciBilgisiyleBarkodTanimlariniGetir();
            var userNo = HttpContext.Session.GetString("UserNo");
            var (userName, businessUnits, selectedBusinessUnit) = GetUserDetails(userNo ?? "");

            ViewBag.UserName = userName;
            ViewBag.BusinessUnits = businessUnits;
            ViewBag.SelectedBusinessUnit = selectedBusinessUnit;

            return View(barkodTanimi);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SelectBusinessUnit(string businessUnit)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var businessUnits = _repository.GetKullaniciIsMerkezleri(userNo);
                if (businessUnits.Contains(businessUnit))
                {
                    HttpContext.Session.SetString("SelectedBusinessUnit", businessUnit);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Geçersiz iş merkezi seçimi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İş merkezi seçimi sırasında hata: {ex.Message}");
                return Json(new { success = false, message = "İş merkezi seçimi sırasında hata oluştu." });
            }
        }

        // Other existing methods remain unchanged


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

        // DiokiController.cs - Güncellenmiş metodlar

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetMarkalar()
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                IEnumerable<string> markalar;

                if (!string.IsNullOrEmpty(userNo))
                {
                    // İş merkezi filtreli marka listesi
                    markalar = _repository.GetMarkalarWithIsMerkeziFilter(userNo);
                }
                else
                {
                    // Fallback: Normal marka listesi
                    markalar = _repository.GetMarkalar();
                }

                return Json(markalar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Marka listesi alınırken hata oluştu: {ex.Message}");
                return Json(new List<string>());
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult ExecuteVideojet2Micro(string kisaIsim, int depo, int miktar, int lotNo)
        {
            try
            {
                string userNo = HttpContext.Session.GetString("UserNo");

                // 1. Stok kodunu al
                string stokkodu = _repository.GetStokKodByKisaIsim(kisaIsim);
                if (string.IsNullOrEmpty(stokkodu))
                {
                    return Json(new { success = false, message = "Stok kodu bulunamadı." });
                }

                // 2. İş emrini al
                string isEmri = _repository.GetIsemriByFn(stokkodu);
                if (string.IsNullOrEmpty(isEmri))
                {
                    return Json(new { success = false, message = $"'{kisaIsim}' için aktif iş emri bulunamadı." });
                }

                // 3. İş merkezi yetki kontrolü yap
                if (!string.IsNullOrEmpty(userNo))
                {
                    string isEmriIsMerkezi = _repository.GetIsEmriIsMerkezi(isEmri, stokkodu);

                    if (!string.IsNullOrEmpty(isEmriIsMerkezi))
                    {
                        bool yetkiVarMi = _repository.KullaniciIsMerkeziYetkisiVarMi(userNo, isEmriIsMerkezi);

                        if (!yetkiVarMi)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Bu ürünü üretme yetkiniz bulunmamaktadır. " +
                                         $"Ürün iş merkezi: {isEmriIsMerkezi}. " +
                                         $"Lütfen yetkiniz olan iş merkezindeki ürünleri seçiniz."
                            });
                        }
                    }
                }

                // 4. Üretim işlemini gerçekleştir
                var (barkod, makine) = _repository.ExecuteVideojet2Micro(isEmri, stokkodu, depo, miktar, lotNo);

                // 5. Barkod kullanıcı bilgisini güncelle
                if (!string.IsNullOrEmpty(userNo) && !string.IsNullOrEmpty(barkod))
                {
                    _repository.BarkodKullaniciGuncelle(barkod, userNo);
                }

                return Json(new
                {
                    success = true,
                    barkod,
                    makine,
                    isEmri
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecuteVideojet2Micro işlemi sırasında hata oluştu: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

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