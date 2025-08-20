using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using static Deneme_proje.Models.YonetimEntities;
using Deneme_proje.Repository;
using Dapper;

namespace Deneme_proje.Controllers
{
    public class KullaniciYonetimiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _mikroDbConnection;
        private readonly string _dynamicDbConnection;
        private readonly FaturaRepository _faturaRepository;
        private readonly ILogger<KullaniciYonetimiController> _logger;

        public KullaniciYonetimiController(
            IConfiguration configuration,
            FaturaRepository faturaRepository,
            ILogger<KullaniciYonetimiController> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ERPDatabase");
            _mikroDbConnection = _configuration.GetConnectionString("MikroDB_V16");
            _dynamicDbConnection = _configuration.GetConnectionString("DynamicDatabase");
            _faturaRepository = faturaRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var kullanicilar = new List<KullaniciListViewModel>();

            try
            {
                var dynamicDbConnectionString = _configuration.GetConnectionString("DynamicDatabase");
                var builder = new SqlConnectionStringBuilder(dynamicDbConnectionString);
                var dynamicDbName = builder.InitialCatalog;

                using (var connection = new SqlConnection(_mikroDbConnection))
                {
                    await connection.OpenAsync();

                    // Kullanıcıları ve iş merkezi yetkilerini getir
                    var query = $@"SELECT DISTINCT 
                            k.User_no, 
                            k.User_name, 
                            k.User_LongName, 
                            p.per_PERSMailAddress as Email, 
                            ISNULL(ky.GirisYetkisi, 1) as GirisYetkisi,
                            ISNULL(ky.IsMerkezleri, '') as IsMerkezleri
                        FROM KULLANICILAR k 
                        LEFT JOIN [{dynamicDbName}].dbo.PERSONELLER p 
                            ON k.User_no = p.per_Userno
                        LEFT JOIN [DBT_ERP].dbo.KullaniciYonetimi ky 
                            ON k.User_no = ky.User_no
                        ORDER BY k.User_no";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var emailValue = reader["Email"];
                                var emailString = emailValue != DBNull.Value ? emailValue.ToString() : string.Empty;

                                kullanicilar.Add(new KullaniciListViewModel
                                {
                                    UserNo = reader["User_no"].ToString(),
                                    UserName = reader["User_name"].ToString(),
                                    LongName = reader["User_LongName"].ToString(),
                                    Email = emailString,
                                    GirisYetkisi = Convert.ToBoolean(reader["GirisYetkisi"]),
                                    IsMerkezleri = reader["IsMerkezleri"].ToString()
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation($"Toplam {kullanicilar.Count} kullanıcı listelendi");
                return View(kullanicilar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi alınırken hata oluştu");
                var errorViewModel = new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = $"Kullanıcı listesi alınırken hata oluştu: {ex.Message}"
                };
                return View("Error", errorViewModel);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> UpdateYetki(string userNo, bool girisYetkisi)
        {
            try
            {
                if (string.IsNullOrEmpty(userNo))
                {
                    return Json(new { success = false, message = "Kullanıcı numarası gerekli" });
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        IF EXISTS (SELECT 1 FROM KullaniciYonetimi WHERE User_no = @User_no)
                            UPDATE KullaniciYonetimi 
                            SET GirisYetkisi = @girisYetkisi 
                            WHERE User_no = @User_no
                        ELSE
                            INSERT INTO KullaniciYonetimi (User_no, GirisYetkisi, IsMerkezleri) 
                            VALUES (@User_no, @girisYetkisi, '')";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@User_no", userNo);
                        command.Parameters.AddWithValue("@girisYetkisi", girisYetkisi);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation($"Kullanıcı {userNo} giriş yetkisi güncellendi: {girisYetkisi}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı yetkisi güncellenirken hata oluştu");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // İş merkezi yetkilendirme sayfası
        public IActionResult IsMerkeziYetkileri(string userNo)
        {
            try
            {
                if (string.IsNullOrEmpty(userNo))
                {
                    TempData["ErrorMessage"] = "Kullanıcı numarası gerekli";
                    return RedirectToAction("Index");
                }

                // Kullanıcı bilgilerini getir
                var kullanici = GetKullaniciDetay(userNo);
                if (kullanici == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı";
                    return RedirectToAction("Index");
                }

                // Tüm iş merkezlerini getir
                var tumIsMerkezleri = _faturaRepository.GetTumIsMerkezleri();

                // Kullanıcının mevcut yetkilerini getir
                var yetkiliIsMerkezleri = _faturaRepository.GetKullaniciIsMerkezleri(userNo);

                // İş merkezlerini işaretle
                foreach (var isMerkezi in tumIsMerkezleri)
                {
                    isMerkezi.IsSelected = yetkiliIsMerkezleri.Contains(isMerkezi.IsM_Kodu);
                }

                var viewModel = new KullaniciIsMerkeziYetkiViewModel
                {
                    UserNo = kullanici.UserNo,
                    UserName = kullanici.UserName,
                    LongName = kullanici.LongName,
                    TumIsMerkezleri = tumIsMerkezleri,
                    SeciliIsMerkezleri = yetkiliIsMerkezleri
                };

                _logger.LogInformation($"İş merkezi yetkileri sayfası açıldı - Kullanıcı: {userNo}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş merkezi yetkileri sayfası açılırken hata oluştu");
                TempData["ErrorMessage"] = $"İş merkezi yetkileri alınırken hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IsMerkeziYetkiKaydet(IsMerkeziUpdateModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserNo))
                {
                    TempData["ErrorMessage"] = "Kullanıcı numarası gerekli";
                    return RedirectToAction("Index");
                }

                // Model state kontrolü
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Geçersiz veri gönderildi";
                    return RedirectToAction("IsMerkeziYetkileri", new { userNo = model.UserNo });
                }

                var success = _faturaRepository.KullaniciIsMerkeziYetkiKaydet(model.UserNo, model.IsMerkezleri ?? new List<string>());

                if (success)
                {
                    TempData["SuccessMessage"] = "İş merkezi yetkileri başarıyla kaydedildi";
                    _logger.LogInformation($"Kullanıcı {model.UserNo} için iş merkezi yetkileri kaydedildi");
                }
                else
                {
                    TempData["ErrorMessage"] = "İş merkezi yetkileri kaydedilemedi";
                    _logger.LogWarning($"Kullanıcı {model.UserNo} için iş merkezi yetkileri kaydedilemedi");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş merkezi yetkileri kaydedilirken hata oluştu");
                TempData["ErrorMessage"] = $"İş merkezi yetkileri kaydedilirken hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private KullaniciListViewModel GetKullaniciDetay(string userNo)
        {
            try
            {
                using (var connection = new SqlConnection(_mikroDbConnection))
                {
                    connection.Open();

                    var query = @"SELECT 
                            User_no, 
                            User_name, 
                            User_LongName
                        FROM KULLANICILAR 
                        WHERE User_no = @UserNo";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserNo", userNo);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new KullaniciListViewModel
                                {
                                    UserNo = reader["User_no"].ToString(),
                                    UserName = reader["User_name"].ToString(),
                                    LongName = reader["User_LongName"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kullanıcı detayları alınırken hata oluştu - UserNo: {userNo}");
            }

            return null;
        }
    }
}