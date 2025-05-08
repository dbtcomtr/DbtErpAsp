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
        private readonly IConfiguration _configuration; private readonly DatabaseSelectorService _dbSelectorService; private readonly byte[] encryptionKey; // Derived from configuration
        public LoginController(IConfiguration configuration, DatabaseSelectorService dbSelectorService)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            // Derive encryption key using PBKDF2
            string keyString = _configuration["EncryptionSettings:Key"] ?? "YourSecretKey123!"; // Ensure this is set securely in appsettings.json
            encryptionKey = DeriveKey(keyString, 32); // 32 bytes for AES-256
        }

        // Derive a secure key using PBKDF2
        private byte[] DeriveKey(string password, int keySize)
        {
            byte[] salt = Encoding.UTF8.GetBytes("FixedSalt12345678"); // In production, use a unique, random salt per user and store it
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize);
            }
        }

        // Şifre encryption metodu
        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = encryptionKey;
                    aes.GenerateIV(); // Generate a random IV for each encryption

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                        // Prepend IV to the encrypted data
                        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
                        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifreleme hatası: {ex.Message}");
                return null;
            }
        }

        // Şifre decryption metodu
        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword)) return null;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedPassword);
                using (var aes = Aes.Create())
                {
                    // Extract IV (first 16 bytes)
                    byte[] iv = new byte[16];
                    byte[] cipher = new byte[fullCipher.Length - 16];
                    Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                    Buffer.BlockCopy(fullCipher, 16, cipher, 0, fullCipher.Length - 16);

                    aes.Key = encryptionKey;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre çözme hatası: {ex.Message}");
                return null;
            }
        }

        public IActionResult Index()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

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
                                HttpContext.Session.SetString("Version", reader["versiyon"].ToString());
                                return RedirectToAction("LoginKullanici");
                            }
                        }
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Login Controller Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

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

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Web_Kullanici WHERE kullanici_adi = @username";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@username", username);

                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string storedEncryptedPassword = reader["sifre"]?.ToString();
                            string storedPassword = DecryptPassword(storedEncryptedPassword);

                            if (storedPassword != password)
                            {
                                ViewBag.Message = "Geçersiz kullanıcı adı veya şifre";
                                return View();
                            }

                            string selectedVersion = reader["versiyon"]?.ToString() ?? "V16";
                            string ipAddress = reader["ip_adresi"]?.ToString();
                            string dbUsername = reader["db_kullaniciadi"]?.ToString();
                            string dbPassword = DecryptPassword(reader["db_sifre"]?.ToString());
                            string defaultDatabase = reader["db_varsayilan"]?.ToString();

                            string dynamicConnectionString = selectedVersion == "V16"
                                ? $"Server={ipAddress};Database=MikroDB_V16;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;"
                                : $"Server={ipAddress};Database=MikroDesktop;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;";

                            UpdateAppSettings(
                                selectedVersion == "V16" ? "MikroDB_V16" : "MikroDesktop",
                                dynamicConnectionString
                            );

                            HttpContext.Session.SetString("Username", username);
                            HttpContext.Session.SetString("IsAuthenticated", "true");

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
                System.Diagnostics.Debug.WriteLine("Login Controller Post Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

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
                string version = HttpContext.Session.GetString("Version");
                if (string.IsNullOrEmpty(version))
                {
                    return RedirectToAction("Index");
                }

                HttpContext.Session.SetString("SelectedVersion", version);

                string defaultDatabase = await GetAdminDefaultDatabase();
                HttpContext.Session.SetString("SelectedDatabase", defaultDatabase);

                string baseConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                using (SqlConnection connection = new SqlConnection(baseConnectionString))
                {
                    await connection.OpenAsync();

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
                                string storedEncryptedPassword = reader["Sifre"] != DBNull.Value ? reader["Sifre"].ToString() : null;

                                if (!girisYetkisi)
                                {
                                    ModelState.AddModelError("", "Sisteme giriş yetkiniz bulunmamaktadır.");
                                    return View();
                                }

                                if (string.IsNullOrEmpty(storedEncryptedPassword))
                                {
                                    ViewBag.RequirePasswordSetup = true;
                                    ViewBag.TempUsername = username;
                                    HttpContext.Session.SetString("TempUserNo", userNo);
                                    Response.Headers.Add("X-Require-Password-Setup", "true");
                                    return View();
                                }

                                string storedPassword = DecryptPassword(storedEncryptedPassword);
                                if (storedPassword != password)
                                {
                                    ModelState.AddModelError("", "Geçersiz şifre");
                                    return View();
                                }
                            }
                        }
                    }

                    string tableName = "KULLANICILAR";
                    string query = $"SELECT * FROM {tableName} WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                HttpContext.Session.SetString("Username", username);
                                HttpContext.Session.SetString("IsAuthenticated", "true");
                                HttpContext.Session.SetString("UserNo", userNo);

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
                                    System.Diagnostics.Debug.WriteLine($"Veritabanı bağlantı hatası: {dbEx.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Giriş hatası: {ex.Message}");
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

                string userNo = HttpContext.Session.GetString("TempUserNo");
                if (string.IsNullOrEmpty(userNo))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın." });
                }

                string encryptedPassword = EncryptPassword(model.NewPassword);
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    return Json(new { success = false, message = "Şifre şifreleme işlemi başarısız." });
                }

                // Şifreyi veritabanına kaydet
                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();
                    string updateQuery = "UPDATE KullaniciYonetimi SET Sifre = @Sifre WHERE User_no = @User_no";
                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Sifre", encryptedPassword);
                        command.Parameters.AddWithValue("@User_no", userNo);
                        int result = await command.ExecuteNonQueryAsync();
                        if (result <= 0)
                        {
                            return Json(new { success = false, message = "Şifre kaydedilemedi. Kullanıcı bulunamadı." });
                        }
                    }
                }

                // LoginKullanici'deki kullanıcı adını al
                string username = HttpContext.Session.GetString("TempUsername");

                // Eğer TempUsername boşsa, userNo kullanarak veritabanından kullanıcı adını almaya çalış
                if (string.IsNullOrEmpty(username))
                {
                    string baseConnectionString = _configuration.GetConnectionString("MikroDB_V16");
                    using (SqlConnection connection = new SqlConnection(baseConnectionString))
                    {
                        await connection.OpenAsync();
                        string userQuery = "SELECT User_name FROM KULLANICILAR WHERE User_no = @userNo";
                        using (SqlCommand command = new SqlCommand(userQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            var result = await command.ExecuteScalarAsync();
                            if (result != null)
                            {
                                username = result.ToString();
                            }
                            else
                            {
                                // Hala bulunamadıysa, default bir değer kullan
                                username = "Kullanici_" + userNo;
                            }
                        }
                    }
                }
                else
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
            new Claim(ClaimTypes.Name, username), // Artık username null olmayacak
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

                        if (defaultDatabase == null)
                        {
                            string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                            return GetFirstAvailableDatabase(version);
                        }

                        return defaultDatabase.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin varsayılan veritabanı hatası: {ex.Message}");
                string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                return GetFirstAvailableDatabase(version);
            }
        }

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
                System.Diagnostics.Debug.WriteLine($"İlk kullanılabilir veritabanı hatası: {ex.Message}");
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
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                if (username.ToLower() == "admin")
                {
                    string connectionString = _configuration.GetConnectionString("ERPDatabase");
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "SELECT sifre FROM Web_Kullanici WHERE kullanici_adi = @username";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@username", username);

                        await connection.OpenAsync();
                        var storedEncryptedPassword = await command.ExecuteScalarAsync();

                        if (storedEncryptedPassword == null)
                        {
                            return Json(new { success = false, message = "Şifre bulunamadı." });
                        }

                        string storedPassword = DecryptPassword(storedEncryptedPassword.ToString());
                        if (storedPassword != password)
                        {
                            return Json(new { success = false, message = "Geçersiz şifre." });
                        }
                    }
                }
                else
                {
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
                        var storedEncryptedPassword = await command.ExecuteScalarAsync();

                        if (storedEncryptedPassword == null)
                        {
                            return Json(new { success = false, message = "Şifre bulunamadı." });
                        }

                        string storedPassword = DecryptPassword(storedEncryptedPassword.ToString());
                        if (storedPassword != password)
                        {
                            return Json(new { success = false, message = "Geçersiz şifre." });
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Şifre doğrulama hatası: {ex.Message}");
                return Json(new { success = false, message = $"Doğrulama hatası: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Username))
                {
                    return BadRequest(new { success = false, message = "Kullanıcı adı boş olamaz." });
                }

                string selectedVersion = "V16";
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
                    }
                }

                string mikroDbConnectionString = _configuration.GetConnectionString("MikroDB_V16");
                string userNo = null;
                using (SqlConnection connection = new SqlConnection(mikroDbConnectionString))
                {
                    await connection.OpenAsync();
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

                string dynamicConnectionString = _configuration.GetConnectionString("DynamicDatabase");
                string email = null;
                using (SqlConnection connection = new SqlConnection(dynamicConnectionString))
                {
                    await connection.OpenAsync();
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

                string token = GenerateResetToken();
                DateTime expiry = DateTime.UtcNow.AddHours(24);

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

                try
                {
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
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderDisplayName = _configuration["EmailSettings:SenderDisplayName"];
                var appUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

                string resetUrl = $"{appUrl}/Login/ResetPassword?token={token}&username={Uri.EscapeDataString(username)}";

                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    delegate { return true; };

                string subject = "Şifre Sıfırlama Talebi";

                string body = $@"Şifre Sıfırlama

Sayın Kullanıcı,

Hesabınız için bir şifre sıfırlama talebinde bulundunuz. Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayınız:

Şifrenizi Sıfırlayın

Ayrıca, aşağıdaki bağlantıyı tarayıcınıza kopyalayıp yapıştırabilirsiniz:

{resetUrl}

Bu bağlantı 24 saat boyunca geçerlidir. Eğer bu şifre sıfırlama talebini siz yapmadıysanız, lütfen bu e-postayı dikkate almayınız.

Bu otomatik bir e-postadır, lütfen yanıtlamayınız.

 ";
                try
                {
                    using (var client = new SmtpClient(smtpServer)
                    {
                        Port = smtpPort,
                        Credentials = new NetworkCredential(senderEmail, senderPassword),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 30000
                    })
                    {
                        using (var mailMessage = new MailMessage
                        {
                            From = new MailAddress(senderEmail, senderDisplayName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        })
                        {
                            mailMessage.To.Add(email);
                            await client.SendMailAsync(mailMessage);
                            System.Diagnostics.Debug.WriteLine($"Şifre sıfırlama e-postası gönderildi: {email}");
                        }
                    }
                }
                catch (Exception sendEx)
                {
                    System.Diagnostics.Debug.WriteLine($"E-posta gönderme hatası: {sendEx.Message}");
                    if (sendEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"İç Hata: {sendEx.InnerException.Message}");
                    }
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
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }

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

                string encryptedPassword = EncryptPassword(model.NewPassword);
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    return BadRequest(new { success = false, message = "Şifre şifreleme işlemi başarısız." });
                }

                string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                string userNo = null;

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

                    bool userExists = false;
                    string checkUserQuery = "SELECT 1 FROM KullaniciYonetimi WHERE User_no = @userNo";

                    using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userNo", userNo);
                        var result = await command.ExecuteScalarAsync();
                        userExists = result != null;
                    }

                    if (!userExists)
                    {
                        string insertUserQuery = "INSERT INTO KullaniciYonetimi (User_no, Sifre, GirisYetkisi) VALUES (@userNo, @newPassword, 1)";
                        using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            command.Parameters.AddWithValue("@newPassword", encryptedPassword);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        string updatePasswordQuery = @"
                        UPDATE KullaniciYonetimi 
                        SET Sifre = @newPassword
                        WHERE User_no = @userNo";

                        using (SqlCommand command = new SqlCommand(updatePasswordQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            command.Parameters.AddWithValue("@newPassword", encryptedPassword);
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