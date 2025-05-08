using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Deneme_proje.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class ProfilController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;

        public ProfilController(IConfiguration configuration, DatabaseSelectorService dbSelectorService)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {//
            try
            {
                // Giriş yapmış kullanıcının bilgilerini al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;
                string username = User.Identity.Name;

                if (string.IsNullOrEmpty(userNo) || string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                // Kullanıcının e-posta adresini al
                string email = null;
                string dynamicConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(dynamicConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE Per_UserNo = @userNo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();

                        if (result != null && !string.IsNullOrEmpty(result.ToString()))
                        {
                            email = result.ToString();
                        }
                    }
                }

                // Kullanıcı modelini doldur
                var model = new ProfilViewModel
                {
                    UserNo = userNo,
                    Username = username,
                    Email = email ?? "Belirtilmemiş"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profil görüntüleme hatası: {ex.Message}");
                ViewData["ErrorMessage"] = "Profil bilgileri yüklenirken bir hata oluştu.";
                return View("Error");
            }
        }
        [AllowAnonymous] // Güvenlik için AllowAnonymous yerine Authorize kullanıyoruz
        [HttpPost]
        public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Geçersiz bilgiler." });
                }

                // Kullanıcı bilgilerini al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;

                if (string.IsNullOrEmpty(userNo))
                {
                    return BadRequest(new { success = false, message = "Kullanıcı bilgisi bulunamadı." });
                }

                // Mevcut şifreyi doğrula
                var sifreDogrulaModel = new SifreDogrulaModel { Sifre = model.MevcutSifre };
                var sifreDogrulaResult = await SifreDogrula(sifreDogrulaModel);

                // IActionResult'ı OkObjectResult olarak cast et
                if (sifreDogrulaResult is OkObjectResult okResult)
                {
                    var response = okResult.Value as dynamic; // Veya bir anonim tip/dictionary olarak işleyebilirsiniz
                    if (response == null || !(bool)response.success)
                    {
                        return BadRequest(new { success = false, message = response?.message?.ToString() ?? "Şifre doğrulama başarısız." });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "Şifre doğrulama işlemi başarısız." });
                }

                // LoginController'dan şifre şifreleme metoduna erişmek için instance oluştur
                var loginController = new LoginController(_configuration, _dbSelectorService);

                // Yeni şifreyi şifrele
                string encryptedYeniSifre = loginController.EncryptPassword(model.YeniSifre);
                if (string.IsNullOrEmpty(encryptedYeniSifre))
                {
                    return BadRequest(new { success = false, message = "Yeni şifre şifreleme işlemi başarısız." });
                }

                // Şifreyi güncelle
                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string updateQuery = "UPDATE KullaniciYonetimi SET Sifre = @yeniSifre WHERE User_no = @userNo";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        command.Parameters.AddWithValue("@yeniSifre", encryptedYeniSifre);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre değiştirme hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Şifre değiştirme işlemi sırasında bir hata oluştu." });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SifreDogrula([FromBody] SifreDogrulaModel model)
        {
            try
            {
                // Kullanıcı bilgilerini al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;

                if (string.IsNullOrEmpty(userNo))
                {
                    return BadRequest(new { success = false, message = "Kullanıcı bilgisi bulunamadı." });
                }

                // LoginController'dan şifre çözme metoduna erişmek için instance oluştur
                var loginController = new LoginController(_configuration, _dbSelectorService);

                // Veritabanındaki şifreyi al ve çöz
                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                string storedEncryptedPassword = null;

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT Sifre FROM KullaniciYonetimi WHERE User_no = @userNo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();
                        storedEncryptedPassword = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(storedEncryptedPassword))
                    {
                        return BadRequest(new { success = false, message = "Kullanıcı şifresi bulunamadı." });
                    }

                    // Şifreyi çöz
                    string storedPassword = loginController.DecryptPassword(storedEncryptedPassword);
                    if (storedPassword == null)
                    {
                        return BadRequest(new { success = false, message = "Şifre çözme işlemi başarısız." });
                    }

                    // Şifreyi plain text olarak karşılaştır
                    bool sifreDogruMu = storedPassword == model.Sifre;
                    return Ok(new { success = sifreDogruMu });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre doğrulama hatası: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Şifre doğrulama işlemi sırasında bir hata oluştu." });
            }
        }
    }

    // Model sınıfları
    public class ProfilViewModel
    {
        public string UserNo { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class SifreDegistirModel
    {
        public string MevcutSifre { get; set; }
        public string YeniSifre { get; set; }
        public string SifreTekrari { get; set; }
    }

    public class SifreDogrulaModel
    {
        public string Sifre { get; set; }
    }
}