using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Deneme_proje.Repository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;

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
            var userNo = HttpContext.Session.GetString("UserNo");
            var (userName, businessUnits, selectedBusinessUnit) = GetUserDetails(userNo ?? "");

            ViewBag.UserName = userName;
            ViewBag.BusinessUnits = businessUnits;
            ViewBag.SelectedBusinessUnit = selectedBusinessUnit;

            return View();
        }

        [AllowAnonymous]
        public IActionResult ÜretimListesi(DateTime? baslangicTarihi, DateTime? bitisTarihi, string isMerkezi)
        {
            var userNo = HttpContext.Session.GetString("UserNo");
            var (userName, businessUnits, selectedBusinessUnit) = GetUserDetails(userNo ?? "");

            var barkodTanimi = _repository.KullaniciBilgisiyleBarkodTanimlariniGetir(userNo, baslangicTarihi, bitisTarihi, isMerkezi);

            ViewBag.UserName = userName;
            ViewBag.BusinessUnits = businessUnits;
            ViewBag.SelectedBusinessUnit = selectedBusinessUnit;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            ViewBag.IsMerkezi = isMerkezi;

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

        // *** GÜNCELLENMIŞ - İŞ MERKEZİ PARAMETRESİ EKLENDİ ***
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetModeller(string markaKodu)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var selectedIsMerkezi = HttpContext.Session.GetString("SelectedBusinessUnit");

                var modeller = _repository.GetModeller(markaKodu, userNo ?? "", selectedIsMerkezi);
                return Json(modeller);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Model listesi alınırken hata oluştu: {ex.Message}");
                return Json(new List<string>());
            }
        }

        // *** GÜNCELLENMIŞ - İŞ MERKEZİ PARAMETRESİ EKLENDİ ***
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetAmbalajKodlari(string markaKodu, string modelKodu)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var selectedIsMerkezi = HttpContext.Session.GetString("SelectedBusinessUnit");

                var ambalajKodlari = _repository.GetAmbalajKodlari(markaKodu, modelKodu, userNo ?? "", selectedIsMerkezi);
                return Json(ambalajKodlari);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ambalaj kodları listesi alınırken hata oluştu: {ex.Message}");
                return Json(new List<string>());
            }
        }

        // *** GÜNCELLENMIŞ - İŞ MERKEZİ PARAMETRESİ EKLENDİ ***
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetKisaIsimler(string markaKodu, string modelKodu, string ambalajKodu)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var selectedIsMerkezi = HttpContext.Session.GetString("SelectedBusinessUnit");

                var isimler = _repository.GetKisaIsimler(markaKodu, modelKodu, ambalajKodu, userNo ?? "", selectedIsMerkezi);
                return Json(isimler);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kısa isim listesi alınırken hata oluştu: {ex.Message}");
                return Json(new List<string>());
            }
        }

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
                    markalar = _repository.GetMarkalarWithIsMerkeziFilter(userNo);
                }
                else
                {
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

        // *** GÜNCELLENMIŞ - İŞ MERKEZİ BAZLI İŞ EMRİ BULMA ***
        [HttpPost]
        [AllowAnonymous]
        public JsonResult ExecuteVideojet2Micro(string kisaIsim, int depo, int miktar, string lotNo)
        {
            try
            {
                string userNo = HttpContext.Session.GetString("UserNo");
                string selectedIsMerkezi = HttpContext.Session.GetString("SelectedBusinessUnit");

                string stokkodu = _repository.GetStokKodByKisaIsim(kisaIsim);
                if (string.IsNullOrEmpty(stokkodu))
                {
                    return Json(new { success = false, message = "Stok kodu bulunamadı." });
                }

                // SEÇİLİ İŞ MERKEZİNE GÖRE İŞ EMRİ BUL
                string isEmri = null;
                if (!string.IsNullOrEmpty(selectedIsMerkezi))
                {
                    isEmri = _repository.GetIsemriByIsMerkezi(stokkodu, selectedIsMerkezi);
                }
                else
                {
                    // Eğer iş merkezi seçili değilse eski metodu kullan
                    isEmri = _repository.GetIsemriByFn(stokkodu);
                }

                if (string.IsNullOrEmpty(isEmri))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"'{kisaIsim}' için {selectedIsMerkezi ?? "herhangi bir"} iş merkezinde aktif iş emri bulunamadı."
                    });
                }

                // İş emrinin iş merkezini kontrol et (güvenlik için)
                string isEmriIsMerkezi = _repository.GetIsEmriIsMerkezi(isEmri, stokkodu);

                if (!string.IsNullOrEmpty(userNo) && !string.IsNullOrEmpty(selectedIsMerkezi))
                {
                    if (!string.IsNullOrEmpty(isEmriIsMerkezi) && isEmriIsMerkezi != selectedIsMerkezi)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"İş emri farklı bir iş merkezine ait. " +
                                     $"İş Emri Merkezi: {isEmriIsMerkezi}, Seçili Merkez: {selectedIsMerkezi}"
                        });
                    }

                    bool yetkiVarMi = _repository.KullaniciIsMerkeziYetkisiVarMi(userNo, isEmriIsMerkezi);
                    if (!yetkiVarMi)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Bu ürünü üretme yetkiniz bulunmamaktadır. " +
                                     $"Ürün iş merkezi: {isEmriIsMerkezi}."
                        });
                    }
                }

                var (barkod, makine) = _repository.ExecuteVideojet2Micro(isEmri, stokkodu, depo, miktar, lotNo);

                if (!string.IsNullOrEmpty(userNo) && !string.IsNullOrEmpty(barkod))
                {
                    _repository.BarkodKullaniciGuncelle(barkod, userNo);
                }

                return Json(new
                {
                    success = true,
                    barkod,
                    makine,
                    isEmri,
                    isMerkezi = isEmriIsMerkezi // İş merkezi bilgisini de döndür
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
        public JsonResult UretimiHataliOlarakIsaretle(string barkod, string aciklama)
        {
            try
            {
                if (string.IsNullOrEmpty(barkod))
                {
                    return Json(new { success = false, message = "Barkod bilgisi gereklidir." });
                }

                if (string.IsNullOrEmpty(aciklama))
                {
                    return Json(new { success = false, message = "Hatalı açıklaması girilmesi zorunludur." });
                }

                string userNo = HttpContext.Session.GetString("UserNo");
                if (string.IsNullOrEmpty(userNo))
                {
                    userNo = "SYSTEM"; // Fallback değer
                }

                _repository.BarkodHataliOlarakIsaretle(barkod, aciklama, userNo);

                return Json(new { success = true, message = "Üretim hatalı olarak işaretlendi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Üretim hatalı olarak işaretlenirken hata oluştu: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetCurrentUserInfo()
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");

                if (string.IsNullOrEmpty(userName))
                {
                    userName = "Bilinmiyor";
                }

                var businessUnit = HttpContext.Session.GetString("SelectedBusinessUnit");

                if (string.IsNullOrEmpty(businessUnit))
                {
                    businessUnit = "Bilinmiyor";
                }

                return Json(new
                {
                    userName = userName,
                    businessUnit = businessUnit
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCurrentUserInfo hatası: {ex.Message}");
                return Json(new
                {
                    userName = "Bilinmiyor",
                    businessUnit = "Bilinmiyor"
                });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetBarkodBilgileri(string barkod)
        {
            try
            {
                var barkodBilgi = _repository.GetBarkodBilgileri(barkod);
                if (barkodBilgi == null)
                {
                    return Json(new { success = false, message = "Barkod bulunamadı." });
                }

                var userName = !string.IsNullOrEmpty(barkodBilgi.PersonelAdi) && !string.IsNullOrEmpty(barkodBilgi.PersonelSoyadi)
                    ? $"{barkodBilgi.PersonelAdi} {barkodBilgi.PersonelSoyadi}"
                    : "Bilinmiyor";

                return Json(new
                {
                    success = true,
                    barkod = barkodBilgi.bar_kodu,
                    stokKodu = barkodBilgi.bar_stokkodu,
                    stokAdi = barkodBilgi.StokAdi,
                    kisaIsim = barkodBilgi.KisaIsim,
                    modelKodu = barkodBilgi.ModelKodu,
                    miktar = barkodBilgi.Miktar,
                    partiKodu = barkodBilgi.bar_partikodu,
                    userName = userName,
                    isMerkezi = barkodBilgi.IsMerkezi
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetBarkodBilgileri hatası: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}