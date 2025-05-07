using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace Deneme_proje.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly string encryptionKey = "YourSecretKey123!"; // Güvenli bir key kullanın

        public LoginController(IConfiguration configuration, DatabaseSelectorService dbSelectorService)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
        }

        // Şifre encryption metodu
        private string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;

            try
            {
                using (var aes = Aes.Create())
                {
                    using (var md5 = MD5.Create())
                    {
                        aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                        aes.IV = new byte[16];
                    }

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Şifre decryption metodu
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword)) return null;

            try
            {
                using (var aes = Aes.Create())
                {
                    using (var md5 = MD5.Create())
                    {
                        aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                        aes.IV = new byte[16];
                    }

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IActionResult Index()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Admin kullanıcı bağlantı bilgilerini kontrol et
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"SELECT versiyon, ip_adresi, db_kullaniciadi, db_sifre 
                    FROM Web_Kullanici 
                    WHERE kullanici_adi = 'admin'";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool hasConnectionInfo =
                                !string.IsNullOrEmpty(reader["versiyon"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["ip_adresi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_kullaniciadi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_sifre"]?.ToString());

                            if (hasConnectionInfo)
                            {
                                // Bağlantı bilgileri doluysa direkt LoginKullanici'ya yönlendir
                                HttpContext.Session.SetString("Version", reader["versiyon"].ToString());
                                return RedirectToAction("LoginKullanici");
                            }
                        }
                    }
                }

                // Bağlantı bilgileri eksikse normal Index sayfasını göster
                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Login Controller Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Store the error message in ViewData
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["StackTrace"] = ex.StackTrace;

                return View("~/Views/Shared/ErrorView.cshtml");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Admin kullanıcı kontrolü ve yetkilendirme
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"SELECT versiyon, ip_adresi, db_kullaniciadi, db_sifre, db_varsayilan 
            FROM Web_Kullanici 
            WHERE kullanici_adi = 'admin'";

                    SqlCommand command = new SqlCommand(query, connection);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool hasConnectionInfo =
                                !string.IsNullOrEmpty(reader["versiyon"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["ip_adresi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_kullaniciadi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_sifre"]?.ToString());

                            if (hasConnectionInfo)
                            {
                                HttpContext.Session.SetString("Version", reader["versiyon"].ToString());
                                return RedirectToAction("LoginKullanici");
                            }
                        }
                    }
                }

                // Normal kullanıcı girişi
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Web_Kullanici WHERE kullanici_adi = @username AND sifre = @password";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Kullanıcı doğrulandı, dinamik veritabanı bağlantısını güncelle
                            string selectedVersion = reader["versiyon"]?.ToString() ?? "V16"; // Varsayılan versiyon
                            string ipAddress = reader["ip_adresi"]?.ToString();
                            string dbUsername = reader["db_kullaniciadi"]?.ToString();
                            string dbPassword = DecryptPassword(reader["db_sifre"]?.ToString());
                            string defaultDatabase = reader["db_varsayilan"]?.ToString();

                            // Dinamik bağlantı stringini oluştur
                            string dynamicConnectionString = selectedVersion == "V16"
                                ? $"Server={ipAddress};Database=MikroDB_V16;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;"
                                : $"Server={ipAddress};Database=MikroDesktop;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;";

                            // Bağlantı dizesini appsettings.json'da güncelle
                            UpdateAppSettings(
                                selectedVersion == "V16" ? "MikroDB_V16" : "MikroDesktop",
                                dynamicConnectionString
                            );

                            // Session'a gerekli bilgileri kaydet
                            HttpContext.Session.SetString("Username", username);
                            HttpContext.Session.SetString("IsAuthenticated", "true");

                            // Cookie Authentication ekleyin
                            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim("SelectedVersion", selectedVersion)
                    };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3650)
                            };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            TempData["ShowModal"] = true;
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ViewBag.Message = "Geçersiz kullanıcı adı veya şifre";
                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Login Controller Post Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Store the error message in ViewData
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["StackTrace"] = ex.StackTrace;

                return View("~/Views/Shared/ErrorView.cshtml");
            }
        }


        [HttpPost]
        public async Task<IActionResult> LoginKullanici(string username, string password)
        {
            try
            {
                // Version bilgisini kontrol et
                string version = HttpContext.Session.GetString("Version");
                if (string.IsNullOrEmpty(version))
                {
                    return RedirectToAction("Index");
                }

                // Version bilgisini SelectedVersion olarak da kaydet
                HttpContext.Session.SetString("SelectedVersion", version);

                // Admin kullanıcının varsayılan veritabanını bul
                string defaultDatabase = await GetAdminDefaultDatabase();

                // Varsayılan veritabanını session'a kaydet
                HttpContext.Session.SetString("SelectedDatabase", defaultDatabase);

                // Connection string'i seç
                string baseConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                using (SqlConnection connection = new SqlConnection(baseConnectionString))
                {
                    await connection.OpenAsync();

                    // Önce kullanıcının User_no'sunu al
                    string userNoQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";
                    string userNo = null;

                    using (SqlCommand userNoCommand = new SqlCommand(userNoQuery, connection))
                    {
                        userNoCommand.Parameters.AddWithValue("@username", username);
                        var result = await userNoCommand.ExecuteScalarAsync();
                        if (result != null)
                        {
                            userNo = result.ToString();
                        }
                        else
                        {
                            ModelState.AddModelError("", "Geçersiz kullanıcı adı");
                            return View();
                        }
                    }

                    // Kullanıcının giriş yetkisini ve şifre bilgisini kontrol et
                    string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                    using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                    {
                        await erpConnection.OpenAsync();
                        string yetkiQuery = "SELECT GirisYetkisi, Sifre FROM KullaniciYonetimi WHERE User_no = @User_no";

                        using (SqlCommand yetkiCommand = new SqlCommand(yetkiQuery, erpConnection))
                        {
                            yetkiCommand.Parameters.AddWithValue("@User_no", userNo);

                            using (SqlDataReader reader = await yetkiCommand.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync())
                                {
                                    ModelState.AddModelError("", "Kullanıcı yetkisi bulunamadı.");
                                    return View();
                                }

                                bool girisYetkisi = reader["GirisYetkisi"] != DBNull.Value && (bool)reader["GirisYetkisi"];
                                string storedPassword = reader["Sifre"] != DBNull.Value ? reader["Sifre"].ToString() : null;

                                // Giriş yetkisi kontrolü
                                if (!girisYetkisi)
                                {
                                    ModelState.AddModelError("", "Sisteme giriş yetkiniz bulunmamaktadır.");
                                    return View();
                                }

                                // Şifre kontrolü
                                if (string.IsNullOrEmpty(storedPassword))
                                {
                                    // Şifre belirlenmemiş, kullanıcının şifre belirlemesi gerekiyor
                                    // ViewBag ile bilgi gönder
                                    ViewBag.RequirePasswordSetup = true;
                                    ViewBag.TempUsername = username;
                                    HttpContext.Session.SetString("TempUserNo", userNo);

                                    // Özel header ile bilgi gönder (JavaScript tarafında kullanabilmek için)
                                    Response.Headers.Add("X-Require-Password-Setup", "true");

                                    return View();
                                }
                                else if (storedPassword != password)
                                {
                                    // Şifre yanlış
                                    ModelState.AddModelError("", "Geçersiz şifre");
                                    return View();
                                }
                                // Şifre doğru, devam et
                            }
                        }
                    }

                    // Kullanıcı girişi için ana sorgu - MikroDB bağlantısı
                    string tableName = "KULLANICILAR";
                    string query = $"SELECT * FROM {tableName} WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Kullanıcı doğrulandı
                                HttpContext.Session.SetString("Username", username);
                                HttpContext.Session.SetString("IsAuthenticated", "true");
                                HttpContext.Session.SetString("UserNo", userNo);

                                // Cookie Authentication ekleme
                                var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, username),
                            new Claim("UserNo", userNo),
                            new Claim("SelectedVersion", HttpContext.Session.GetString("SelectedVersion")),
                            new Claim("SelectedDatabase", HttpContext.Session.GetString("SelectedDatabase"))
                        };

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3650)
                                };

                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);

                                try
                                {
                                    var finalConnectionString = _dbSelectorService.GetConnectionString();
                                    return RedirectToAction("Index", "Home");
                                }
                                catch (Exception dbEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Database connection error: {dbEx.Message}");
                                    ModelState.AddModelError("", $"Veritabanı bağlantı hatası: {dbEx.Message}");
                                    return View();
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("", "Kullanıcı bilgileri bulunamadı.");
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                ModelState.AddModelError("", $"Bağlantı hatası oluştu: {ex.Message}");
                return View();
            }
        }


        [HttpPost]
        public async Task<IActionResult> SavePassword([FromBody] PasswordModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Şifreler eşleşmiyor veya boş" });
                }

                // Geçici olarak saklanan kullanıcı numarasını al
                string userNo = HttpContext.Session.GetString("TempUserNo");
                if (string.IsNullOrEmpty(userNo))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın." });
                }

                // Şifreyi veritabanına kaydet
                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string updateQuery = "UPDATE KullaniciYonetimi SET Sifre = @Sifre WHERE User_no = @User_no";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Sifre", model.NewPassword);
                        command.Parameters.AddWithValue("@User_no", userNo);

                        int result = await command.ExecuteNonQueryAsync();

                        if (result <= 0)
                        {
                            return Json(new { success = false, message = "Şifre kaydedilemedi. Kullanıcı bulunamadı." });
                        }
                    }
                }

                // Kullanıcı bilgilerini session'a kaydet
                string username = HttpContext.Session.GetString("TempUsername");
                if (!string.IsNullOrEmpty(username))
                {
                    HttpContext.Session.Remove("TempUsername");
                    HttpContext.Session.SetString("Username", username);
                }

                HttpContext.Session.Remove("TempUserNo");
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("UserNo", userNo);

                // Cookie Authentication ekleme
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("UserNo", userNo),
            new Claim("SelectedVersion", HttpContext.Session.GetString("SelectedVersion") ?? "V16"),
            new Claim("SelectedDatabase", HttpContext.Session.GetString("SelectedDatabase") ?? "")
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3650)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Json(new { success = true, message = "Şifre başarıyla kaydedildi" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password save error: {ex.Message}");
                return Json(new { success = false, message = $"Şifre kaydedilirken bir hata oluştu: {ex.Message}" });
            }
        }


        // Yeni eklenen metot
        private async Task<string> GetAdminDefaultDatabase()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT db_varsayilan FROM Web_Kullanici WHERE kullanici_adi = 'admin'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        var defaultDatabase = await command.ExecuteScalarAsync();

                        // Null kontrolü ve ToString() çevirimi
                        if (defaultDatabase == null)
                        {
                            // Eğer varsayılan veritabanı yoksa, ilk veritabanını bul
                            string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                            return GetFirstAvailableDatabase(version);
                        }

                        return defaultDatabase.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin default database error: {ex.Message}");

                // Hata durumunda varsayılan versiyon için ilk veritabanını bul
                string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                return GetFirstAvailableDatabase(version);
            }
        }

        // Yardımcı metot
        private string GetFirstAvailableDatabase(string version)
        {
            try
            {
                string connectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 DB_kod FROM VERI_TABANLARI ORDER BY DB_kod";

                    using (var command = new SqlCommand(query, connection))
                    {
                        var firstDb = command.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(firstDb))
                        {
                            throw new Exception("Hiç veritabanı bulunamadı");
                        }

                        return firstDb;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First available database error: {ex.Message}");
                throw;
            }
        }

        public IActionResult LoginKullanici()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Version")))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateConnectionInfo([FromBody] ConnectionInfo model)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");
                string username = HttpContext.Session.GetString("Username");

                // Şifreyi encrypt et
                string encryptedPassword = model.DbPassword != null
                    ? EncryptPassword(model.DbPassword)
                    : null;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Web_Kullanici 
           SET versiyon = @version,
               ip_adresi = @ipAddress,
               db_kullaniciadi = @dbUsername,
               db_sifre = @dbPassword,
               db_varsayilan = @defaultDb
           WHERE kullanici_adi = @username";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@version", model.Version ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ipAddress", model.IpAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@dbUsername", model.DbUsername ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@dbPassword", encryptedPassword ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@defaultDb", model.DefaultDatabase ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@username", username);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    if (!string.IsNullOrEmpty(model.Version))
                    {
                        HttpContext.Session.SetString("SelectedVersion", model.Version);
                    }

                    if (!string.IsNullOrEmpty(model.DefaultDatabase))
                    {
                        HttpContext.Session.SetString("SelectedDatabase", model.DefaultDatabase);
                    }

                    // Kullanıcı yeni veritabanı bilgilerini güncellediyse Claims'i güncelle
                    if (!string.IsNullOrEmpty(username))
                    {
                        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("SelectedVersion", model.Version ?? HttpContext.Session.GetString("SelectedVersion") ?? "V16"),
                    new Claim("SelectedDatabase", model.DefaultDatabase ?? HttpContext.Session.GetString("SelectedDatabase") ?? "")
                };

                        var userNoClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserNo");
                        if (userNoClaim != null)
                        {
                            claims.Add(new Claim("UserNo", userNoClaim.Value));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3650)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                    }
                }

                return Ok(new { success = true, message = "Bağlantı bilgileri güncellendi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Oturumu ve Cookie kimlik doğrulamayı temizle
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        private void UpdateAppSettings(string key, string connectionString)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["ConnectionStrings"][key] = connectionString;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            System.IO.File.WriteAllText(appSettingsPath, output);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPassword(string password)
        {
            try
            {
                // Get current user's username from claims
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // Check if the user is admin
                if (username.ToLower() == "admin")
                {
                    // For admin, verify against Web_Kullanici table
                    string connectionString = _configuration.GetConnectionString("ERPDatabase");
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "SELECT sifre FROM Web_Kullanici WHERE kullanici_adi = @username";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@username", username);

                        await connection.OpenAsync();
                        var storedPassword = await command.ExecuteScalarAsync();

                        if (storedPassword == null || storedPassword.ToString() != password)
                        {
                            return Json(new { success = false, message = "Geçersiz şifre." });
                        }
                    }
                }
                else
                {
                    // For regular users, verify against KullaniciYonetimi table
                    string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;
                    if (string.IsNullOrEmpty(userNo))
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgisi bulunamadı." });
                    }

                    string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                    using (SqlConnection connection = new SqlConnection(erpConnectionString))
                    {
                        string query = "SELECT Sifre FROM KullaniciYonetimi WHERE User_no = @User_no";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@User_no", userNo);

                        await connection.OpenAsync();
                        var storedPassword = await command.ExecuteScalarAsync();

                        if (storedPassword == null || storedPassword.ToString() != password)
                        {
                            return Json(new { success = false, message = "Geçersiz şifre." });
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password verification error: {ex.Message}");
                return Json(new { success = false, message = $"Doğrulama hatası: {ex.Message}" });
            }
        }
        // Şifre sıfırlama bağlantısı gönderme işlemi
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Username))
                {
                    return BadRequest(new { success = false, message = "Kullanıcı adı boş olamaz." });
                }

                // Admin kullanıcı için versiyon bilgisini almaya çalış
                string selectedVersion = "V16"; // Varsayılan olarak V16

                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                        string query = "SELECT versiyon FROM Web_Kullanici WHERE kullanici_adi = 'admin'";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            if (result != null && !string.IsNullOrEmpty(result.ToString()))
                            {
                                selectedVersion = result.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Versiyon bilgisi alınamadı: {ex.Message}");
                        // Varsayılan V16 kullanılacak
                    }
                }

                // Kullanıcılar tablosundan userNo'yu alabilmek için MikroDB_V16 bağlantısı kullan
                string mikroDbConnectionString = _configuration.GetConnectionString("MikroDB_V16");

                // KULLANICILAR tablosundan User_no'yu bul
                string userNo = null;
                using (SqlConnection connection = new SqlConnection(mikroDbConnectionString))
                {
                    await connection.OpenAsync();

                    // Kullanıcı numarasını bul
                    string findUserQuery = "SELECT User_no FROM KULLANICILAR WHERE user_name = @username";

                    using (SqlCommand command = new SqlCommand(findUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", model.Username);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Kullanıcı bulunamadı. Lütfen geçerli bir kullanıcı adı giriniz." });
                        }

                        userNo = result.ToString();
                    }
                }

                // KullaniciYonetimi tablosunda kullanıcının varlığını kontrol et
                bool userExists = false;
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT 1 FROM KullaniciYonetimi WHERE User_no = @userNo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();
                        userExists = result != null;
                    }

                    // Kullanıcı KullaniciYonetimi tablosunda yoksa ekle
                    if (!userExists)
                    {
                        string insertQuery = "INSERT INTO KullaniciYonetimi (User_no, GirisYetkisi) VALUES (@userNo, 1)";
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            await command.ExecuteNonQueryAsync();
                            userExists = true;
                        }
                    }
                }

                // Dinamik bağlantı bilgisini direkt olarak al, veritabanı seçimini atlayarak
                string dynamicConnectionString = _configuration.GetConnectionString("DynamicDatabase");

                // Şimdi dinamik veritabanında kullanıcının e-posta adresini bul
                string email = null;
                using (SqlConnection connection = new SqlConnection(dynamicConnectionString))
                {
                    await connection.OpenAsync();

                    // E-posta adresini al
                    string query = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE Per_UserNo = @userNo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || string.IsNullOrEmpty(result.ToString()))
                        {
                            return NotFound(new { success = false, message = "Kullanıcı için e-posta adresi bulunamadı." });
                        }

                        email = result.ToString();
                    }
                }

                // PasswordResetTokens tablosunu kontrol et ve gerekirse oluştur
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    string checkTableQuery = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PasswordResetTokens')
                BEGIN
                    CREATE TABLE PasswordResetTokens (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        User_no VARCHAR(50) NOT NULL,
                        Token VARCHAR(100) NOT NULL,
                        ExpiryDate DATETIME NOT NULL,
                        IsUsed BIT NOT NULL DEFAULT 0,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END";

                    using (SqlCommand command = new SqlCommand(checkTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Şifre sıfırlama tokenı oluştur
                string token = GenerateResetToken();
                DateTime expiry = DateTime.UtcNow.AddHours(24); // 24 saat geçerli

                // Token'ı veritabanına kaydet
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                IF EXISTS (SELECT 1 FROM PasswordResetTokens WHERE User_no = @userNo)
                    UPDATE PasswordResetTokens SET Token = @token, ExpiryDate = @expiry, IsUsed = 0
                    WHERE User_no = @userNo
                ELSE
                    INSERT INTO PasswordResetTokens (User_no, Token, ExpiryDate, IsUsed)
                    VALUES (@userNo, @token, @expiry, 0)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        command.Parameters.AddWithValue("@token", token);
                        command.Parameters.AddWithValue("@expiry", expiry);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // E-posta gönderme işlemini özel bir try-catch ile sarmalayın
                try
                {
                    // Şifre sıfırlama bağlantısını e-posta ile gönder
                    await SendPasswordResetEmail(email, token, model.Username);
                }
                catch (Exception mailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"E-POSTA_HATASI: {mailEx.Message}");

                    if (mailEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"E-POSTA_IC_HATA: {mailEx.InnerException.Message}");
                    }

                    System.Diagnostics.Debug.WriteLine($"E-POSTA_STACK_TRACE: {mailEx.StackTrace}");

                    return StatusCode(500, new
                    {
                        success = false,
                        message = $"E-posta gönderimi sırasında bir hata oluştu: {mailEx.Message}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre sıfırlama hatası: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç hata: {ex.InnerException.Message}");
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Şifre sıfırlama işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyiniz."
                });
            }
        }

        // Benzersiz bir şifre sıfırlama tokenı oluştur
        private string GenerateResetToken()
        {
            byte[] tokenData = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }
            return Convert.ToBase64String(tokenData).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }

        private async Task SendPasswordResetEmail(string email, string token, string username)
        {
            try
            {
                // SMTP ayarlarını konfigürasyondan al
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderDisplayName = _configuration["EmailSettings:SenderDisplayName"];
                var appUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

                // Resetleme URL'ini oluştur
                string resetUrl = $"{appUrl}/Login/ResetPassword?token={token}&username={Uri.EscapeDataString(username)}";

                // SSL güvenlik protokolünü ayarla
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                // ÖNEMLİ: Sertifika doğrulamasını geçici olarak atla (EmailNotificationService'ten alındı)
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    delegate { return true; };

                // E-posta başlığı
                string subject = "Şifre Sıfırlama Talebi";

                // E-posta içeriği
                string body = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 650px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 15px; border-bottom: 3px solid #007bff; }}
        .header h1 {{ color: #007bff; margin: 0; }}
        .content {{ padding: 20px 0; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
        .button:hover {{ background-color: #0056b3; }}
        .note {{ color: #6c757d; font-size: 12px; margin-top: 20px; }}
        .footer {{ font-size: 12px; color: #6c757d; margin-top: 30px; border-top: 1px solid #e9ecef; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Şifre Sıfırlama</h1>
        </div>
        <div class=""content"">
            <p>Sayın Kullanıcı,</p>
            
            <p>Hesabınız için bir şifre sıfırlama talebinde bulundunuz. Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayınız:</p>
            
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{resetUrl}"" class=""button"">Şifrenizi Sıfırlayın</a>
            </p>
            
            <p>Ayrıca, aşağıdaki bağlantıyı tarayıcınıza kopyalayıp yapıştırabilirsiniz:</p>
            <p style=""word-break: break-all;"">{resetUrl}</p>
            
            <p class=""note"">Bu bağlantı 24 saat boyunca geçerlidir. Eğer bu şifre sıfırlama talebini siz yapmadıysanız, lütfen bu e-postayı dikkate almayınız.</p>
        </div>
        <div class=""footer"">
            <p>Bu otomatik bir e-postadır, lütfen yanıtlamayınız.</p>
        </div>
    </div>
</body>
</html>";

                try
                {
                    // SMTP istemcisini oluştur
                    using (var client = new SmtpClient(smtpServer)
                    {
                        Port = smtpPort,
                        Credentials = new NetworkCredential(senderEmail, senderPassword),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 30000 // 30 saniye timeout (arttırıldı)
                    })
                    {
                        // E-posta mesajını oluştur
                        using (var mailMessage = new MailMessage
                        {
                            From = new MailAddress(senderEmail, senderDisplayName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        })
                        {
                            // Alıcı e-posta adresini ekle
                            mailMessage.To.Add(email);

                            // E-postayı gönder
                            await client.SendMailAsync(mailMessage);
                            System.Diagnostics.Debug.WriteLine($"Şifre sıfırlama e-postası gönderildi: {email}");
                        }
                    }
                }
                catch (Exception sendEx)
                {
                    // Gönderim sırasındaki hatayı detaylı logla
                    System.Diagnostics.Debug.WriteLine($"E-posta gönderme hatası: {sendEx.Message}");

                    // İç içe hata varsa onu da logla
                    if (sendEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"İç Hata: {sendEx.InnerException.Message}");
                    }

                    // Hatayı yeniden fırlat
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"E-posta gönderme hatası: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç hata: {ex.InnerException.Message}");
                }

                throw;
            }
            finally
            {
                // Güvenlik için sertifika doğrulamasını geri yükle
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }

        // Şifre sıfırlama sayfası
        [HttpGet]
        public IActionResult ResetPassword(string token, string username)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
            {
                ViewBag.ErrorMessage = "Geçersiz şifre sıfırlama bağlantısı.";
                return View();
            }

            ViewBag.Token = token;
            ViewBag.Username = username;
            return View();
        }

        // Şifre sıfırlama işlemi
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Username))
                {
                    return BadRequest(new { success = false, message = "Geçersiz şifre sıfırlama bağlantısı." });
                }

                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
                {
                    return BadRequest(new { success = false, message = "Şifreler eşleşmiyor veya boş." });
                }

                // Token'ın geçerliliğini kontrol et
                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                string userNo = null;

                // KULLANICILAR tablosundan User_no'yu bul
                string mikroDbConnectionString = _configuration.GetConnectionString("MikroDB_V16");
                using (SqlConnection mikroDbConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroDbConnection.OpenAsync();

                    string findUserQuery = "SELECT User_no FROM KULLANICILAR WHERE user_name = @username";

                    using (SqlCommand command = new SqlCommand(findUserQuery, mikroDbConnection))
                    {
                        command.Parameters.AddWithValue("@username", model.Username);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
                        }

                        userNo = result.ToString();
                    }
                }

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    // Token'ın geçerliliğini kontrol et
                    string tokenQuery = @"
                SELECT 1 FROM PasswordResetTokens 
                WHERE User_no = @userNo 
                AND Token = @token 
                AND ExpiryDate > @now
                AND IsUsed = 0";

                    using (SqlCommand command = new SqlCommand(tokenQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        command.Parameters.AddWithValue("@token", model.Token);
                        command.Parameters.AddWithValue("@now", DateTime.UtcNow);

                        var isValid = await command.ExecuteScalarAsync();

                        if (isValid == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı."
                            });
                        }
                    }

                    // Token'ı kullanıldı olarak işaretle
                    string updateTokenQuery = @"
                UPDATE PasswordResetTokens 
                SET IsUsed = 1
                WHERE User_no = @userNo AND Token = @token";

                    using (SqlCommand command = new SqlCommand(updateTokenQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        command.Parameters.AddWithValue("@token", model.Token);
                        await command.ExecuteNonQueryAsync();
                    }

                    // KullaniciYonetimi tablosunda kullanıcı kaydının varlığını kontrol et
                    bool userExists = false;
                    string checkUserQuery = "SELECT 1 FROM KullaniciYonetimi WHERE User_no = @userNo";

                    using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();
                        userExists = result != null;
                    }

                    // Kullanıcı yoksa ekle, varsa güncelle
                    if (!userExists)
                    {
                        string insertUserQuery = "INSERT INTO KullaniciYonetimi (User_no, Sifre, GirisYetkisi) VALUES (@userNo, @newPassword, 1)";
                        using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            command.Parameters.AddWithValue("@newPassword", model.NewPassword);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Şifreyi güncelle
                        string updatePasswordQuery = @"
                    UPDATE KullaniciYonetimi 
                    SET Sifre = @newPassword
                    WHERE User_no = @userNo";

                        using (SqlCommand command = new SqlCommand(updatePasswordQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            command.Parameters.AddWithValue("@newPassword", model.NewPassword);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre sıfırlama hatası: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç hata: {ex.InnerException.Message}");
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Şifre sıfırlama işlemi sırasında bir hata oluştu."
                });
            }
        }

    }
    public class ForgotPasswordModel
    {
        public string Username { get; set; }
    }

    public class ResetPasswordModel
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
    public class UserConnectionInfo
    {
        public string Version { get; set; }
        public string IpAddress { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DefaultDb { get; set; }
    }

    public class ConnectionInfo
    {
        public string Version { get; set; }
        public string IpAddress { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DefaultDatabase { get; set; }
    }

    public class PasswordModel
    {
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}