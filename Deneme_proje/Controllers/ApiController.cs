// Controllers/ApiController.cs
using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Models;
using Deneme_proje.Repository;
using System.Text;
using System.Text.Json;

namespace Deneme_proje.Controllers
{
    [AllowAnonymous]
    public class ApiController : BaseController
    {
        private readonly ApiRepository _apiRepository;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<ApiController> _logger;

        public ApiController(
            ApiRepository apiRepository,
            DatabaseSelectorService dbSelectorService,
            ILogger<ApiController> logger)
        {
            _apiRepository = apiRepository;
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        // API Ayarları sayfası
        public IActionResult ApiAyarlari()
        {
            try
            {
                string kullaniciAdi = User.Identity?.Name;

                // ✅ Sadece SRV kullanıcısı görebilir
                if (kullaniciAdi != "SRV")
                {
                    TempData["ErrorMessage"] = "Bu sayfaya sadece SRV kullanıcısı erişebilir.";
                    return RedirectToAction("Dashboard", "Crm");
                }

                // Sistem geneli ayarları getir
                var apiAyarlari = _apiRepository.GetMikroApiAyarlari();

                var model = new ApiAyarlariViewModel
                {
                    ApiKey = _apiRepository.GetApiKey(),
                    KullaniciKodu = _apiRepository.GetKullaniciKodu(),
                    ServerAddress = _apiRepository.GetServerAddress(),
                    BaseUrl = _apiRepository.GetBaseUrl(),
                    MikroSifre = "", // Güvenlik için şifreyi boş göster
                    Aktif = apiAyarlari?.Aktif ?? true,
                    MevcutKayitVar = apiAyarlari != null,
                    SonGuncellemeTarihi = apiAyarlari?.GuncellemeTarihi,
                    GuncelleyenKullanici = apiAyarlari?.GuncelleyenKullanici
                };

                ViewBag.KullaniciAdi = kullaniciAdi;
                ViewBag.SecilenDatabase = HttpContext.Session.GetString("SelectedDatabase") ?? "Seçilmedi";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Ayarları sayfası yüklenirken hata");
                TempData["ErrorMessage"] = $"Sayfa yüklenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Dashboard", "Crm");
            }
        }

        // API Ayarlarını kaydet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApiAyarlari(ApiAyarlariViewModel model)
        {
            try
            {
                string kullaniciAdi = User.Identity?.Name;

                // ✅ Sadece SRV kullanıcısı kaydedebilir
                if (kullaniciAdi != "SRV")
                {
                    TempData["ErrorMessage"] = "Bu işlemi sadece SRV kullanıcısı yapabilir.";
                    return RedirectToAction("Dashboard", "Crm");
                }

                if (string.IsNullOrWhiteSpace(model.MikroSifre))
                {
                    TempData["ErrorMessage"] = "Mikro Şifre alanı boş olamaz.";

                    model.ApiKey = _apiRepository.GetApiKey();
                    model.KullaniciKodu = _apiRepository.GetKullaniciKodu();
                    model.ServerAddress = _apiRepository.GetServerAddress();
                    model.BaseUrl = _apiRepository.GetBaseUrl();
                    ViewBag.KullaniciAdi = kullaniciAdi;
                    ViewBag.SecilenDatabase = HttpContext.Session.GetString("SelectedDatabase") ?? "Seçilmedi";
                    return View(model);
                }

                // Mevcut kaydı al (ID için)
                var mevcutAyar = _apiRepository.GetMikroApiAyarlari();

                var apiAyarlari = new MikroApiAyarlari
                {
                    Id = mevcutAyar?.Id ?? 0,
                    MikroSifre = model.MikroSifre.Trim(),
                    Aktif = model.Aktif,
                    GuncelleyenKullanici = kullaniciAdi
                };

                var result = _apiRepository.SaveOrUpdateMikroApiAyarlari(apiAyarlari);

                if (result)
                {
                    _logger.LogInformation($"✅ Mikro API ayarları başarıyla kaydedildi. Kaydeden: {kullaniciAdi}");
                    TempData["SuccessMessage"] = "API ayarları başarıyla kaydedildi. Artık tüm kullanıcılar bu şifreyi kullanabilir.";
                }
                else
                {
                    _logger.LogError($"❌ API ayarları kaydedilemedi");
                    TempData["ErrorMessage"] = "API ayarları kaydedilirken bir hata oluştu.";
                }

                return RedirectToAction("ApiAyarlari");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ API Ayarları kaydedilirken kritik hata");
                TempData["ErrorMessage"] = $"Kayıt sırasında bir hata oluştu: {ex.Message}";

                model.ApiKey = _apiRepository.GetApiKey();
                model.KullaniciKodu = _apiRepository.GetKullaniciKodu();
                model.ServerAddress = _apiRepository.GetServerAddress();
                model.BaseUrl = _apiRepository.GetBaseUrl();
                ViewBag.KullaniciAdi = User.Identity?.Name;
                ViewBag.SecilenDatabase = HttpContext.Session.GetString("SelectedDatabase") ?? "Seçilmedi";
                return View(model);
            }
        }

        // API Test bağlantısı (Herkes kullanabilir)
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                _logger.LogInformation("=== API Bağlantı Testi Başlıyor ===");

                var apiAyarlari = _apiRepository.GetMikroApiAyarlari();

                if (apiAyarlari == null || string.IsNullOrWhiteSpace(apiAyarlari.MikroSifre))
                {
                    return Json(new { success = false, message = "API şifresi tanımlanmamış." });
                }

                if (!apiAyarlari.Aktif)
                {
                    return Json(new { success = false, message = "API entegrasyonu aktif değil." });
                }

                string firmaKodu = HttpContext.Session.GetString("SelectedDatabase") ?? "";
                if (string.IsNullOrEmpty(firmaKodu))
                {
                    return Json(new { success = false, message = "Veritabanı seçilmemiş." });
                }

                string encryptedPassword = _apiRepository.GetEncryptedPassword(apiAyarlari.MikroSifre);

                var testRequest = new MikroApiRequest
                {
                    Mikro = new MikroApiData
                    {
                        FirmaKodu = firmaKodu,
                        CalismaYili = DateTime.Now.Year.ToString(),
                        ApiKey = _apiRepository.GetApiKey(),
                        KullaniciKodu = _apiRepository.GetKullaniciKodu(),
                        Sifre = encryptedPassword,
                        evraklar = new List<Evrak>()
                    }
                };

                string apiUrl = _apiRepository.GetApiUrl("VerilenTeklifKaydet");

                // ✅ PascalCase kullan, escape karakterlerini engelle
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // ✅ PascalCase için null
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // ✅ + karakterini escape etme
                };

                var jsonContent = JsonSerializer.Serialize(testRequest, jsonOptions);

                _logger.LogInformation($"Gönderilecek JSON:\n{jsonContent}");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response Status: {(int)response.StatusCode}");
                _logger.LogInformation($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "✅ API bağlantısı başarılı!",
                        details = new
                        {
                            statusCode = (int)response.StatusCode,
                            responseContent = responseContent
                        }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"❌ API Hatası: {response.StatusCode}",
                        details = new
                        {
                            statusCode = (int)response.StatusCode,
                            errorContent = responseContent,
                            sentJson = jsonContent
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API test hatası");
                return Json(new
                {
                    success = false,
                    message = $"❌ Hata: {ex.Message}"
                });
            }
        }
        
        // ✅ Teklif gönder (Tüm kullanıcılar kullanabilir - sistem şifresi ile)
        [HttpPost]
        public async Task<IActionResult> SendTeklifToMikro([FromBody] Evrak evrak)
        {
            return await SendToMikroApi("VerilenTeklifKaydet", evrak);
        }

        // Genel Mikro API gönderme metodu
        private async Task<IActionResult> SendToMikroApi(string endpointName, dynamic data)
        {
            try
            {
                var apiAyarlari = _apiRepository.GetMikroApiAyarlari();

                if (apiAyarlari == null || string.IsNullOrWhiteSpace(apiAyarlari.MikroSifre))
                {
                    return Json(new { success = false, message = "API şifresi tanımlanmamış." });
                }

                if (!apiAyarlari.Aktif)
                {
                    return Json(new { success = false, message = "API entegrasyonu aktif değil." });
                }

                string encryptedPassword = _apiRepository.GetEncryptedPassword(apiAyarlari.MikroSifre);
                string firmaKodu = HttpContext.Session.GetString("SelectedDatabase") ?? "";

                var apiRequest = new MikroApiRequest
                {
                    Mikro = new MikroApiData
                    {
                        FirmaKodu = firmaKodu,
                        CalismaYili = DateTime.Now.Year.ToString(),
                        ApiKey = _apiRepository.GetApiKey(),
                        KullaniciKodu = _apiRepository.GetKullaniciKodu(),
                        Sifre = encryptedPassword,
                        evraklar = data is Evrak ? new List<Evrak> { data } : new List<Evrak>()
                    }
                };

                string apiUrl = _apiRepository.GetApiUrl(endpointName);

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // ✅ PascalCase + UnsafeRelaxedJsonEscaping
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Mikro API'ye istek: {apiUrl}");
                _logger.LogInformation($"JSON: {jsonContent}");

                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "İşlem başarılı!", response = responseContent });
                }
                else
                {
                    return Json(new { success = false, message = $"API hatası: {response.StatusCode}", response = responseContent });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{endpointName} işlemi sırasında hata");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }
}