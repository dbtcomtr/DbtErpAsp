using Deneme_proje;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

[ApiController]
[Route("api/[controller]")]

public class DatabaseController : Controller
{
    private readonly DatabaseSelectorService _dbSelectorService;
    private readonly IConfiguration _configuration;



    public DatabaseController(DatabaseSelectorService dbSelectorService, IConfiguration configuration)
    {
        _dbSelectorService = dbSelectorService;
        _configuration = configuration;
    }

    [HttpPost("dynamic-connect")]
    public IActionResult DynamicConnect([FromBody] DatabaseConnectionRequest request)
    {
        try
        {
            // Session'dan seçilen versiyon bilgisini al
            var selectedVersion = HttpContext.Session.GetString("SelectedVersion");

            if (string.IsNullOrEmpty(selectedVersion))
            {
                return BadRequest(new { success = false, message = "Versiyon seçimi yapılmamış." });
            }

            // Dinamik bağlantı stringini oluştur
            string dynamicConnectionString = $"Server={request.IpAddress};User Id={request.Username};Password={request.Password};Encrypt=True;TrustServerCertificate=True;";

            // Seçilen versiyona göre bağlantı stringini oluştur ve güncelle
            string connectionStringToUpdate;
            string connectionStringKey;

            if (selectedVersion == "V16")
            {
                connectionStringToUpdate = $"Server={request.IpAddress};Database=MikroDB_V16;User Id={request.Username};Password={request.Password};Encrypt=True;TrustServerCertificate=True;";
                connectionStringKey = "MikroDB_V16";
            }
            else if (selectedVersion == "V17")
            {
                connectionStringToUpdate = $"Server={request.IpAddress};Database=MikroDesktop;User Id={request.Username};Password={request.Password};Encrypt=True;TrustServerCertificate=True;";
                connectionStringKey = "MikroDesktop";
            }
            else
            {
                return BadRequest(new { success = false, message = "Geçersiz versiyon seçimi." });
            }

            // appsettings.json dosyasını güncelle
            UpdateAppSettings(connectionStringKey, connectionStringToUpdate);

            // Dinamik bağlantıyı test et
            using (var connection = new SqlConnection(dynamicConnectionString))
            {
                connection.Open();
            }

            // Seçilen veritabanı bağlantısını test et
            using (var connection = new SqlConnection(connectionStringToUpdate))
            {
                connection.Open();
            }

            return Ok(new
            {
                success = true,
                message = "Bağlantı başarılı!",
                redirectUrl = Url.Action("Index", "Home") // Home/Index'e yönlendirme URL'si
            });
        }
        catch (SqlException ex)
        {
            return BadRequest(new { success = false, message = $"SQL Hatası: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Genel Hata: {ex.Message}" });
        }
    }


    [HttpPost("select-database")]
    public IActionResult SelectDatabase([FromForm] string databaseName)
    {
        try
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Bir veritabanı seçmelisiniz.");
            }

            // Seçilen veritabanını Session'a kaydet
            HttpContext.Session.SetString("SelectedDatabase", databaseName);

            // JSON formatında başarılı mesaj döndür
            return Ok(new
            {
                success = true,
                message = "Veritabanı başarıyla seçildi."
            });
        }
        catch (Exception ex)
        {
            // Hata durumunda JSON formatında hata mesajı döndür
            return BadRequest(new
            {
                success = false,
                message = $"Hata: {ex.Message}"
            });
        }
    }




    private void UpdateAppSettings(string key, string connectionString)
    {
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        if (!System.IO.File.Exists(appSettingsPath))
        {
            throw new FileNotFoundException("appsettings.json bulunamadı.");
        }

        var json = System.IO.File.ReadAllText(appSettingsPath);

        try
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["ConnectionStrings"][key] = connectionString;

            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            System.IO.File.WriteAllText(appSettingsPath, output);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("appsettings.json dosyası güncellenemedi. JSON formatı hatalı olabilir.", ex);
        }
    }

    [HttpGet("GetDatabase")]
    public IActionResult GetDatabase(string version)
    {
        try
        {
            var databases = GetDatabases(version);
            return Ok(databases); // JSON formatında veritabanı isimlerini döndür
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    private List<string> GetDatabases(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            throw new ArgumentException("Versiyon bilgisi sağlanmadı.");
        }

        var databases = new List<string>();
        string connectionString = GetConnectionStringByVersion(version);

        using (var connection = new SqlConnection(connectionString))
        {
            const string query = "SELECT DB_kod FROM VERI_TABANLARI"; // VERI_TABANLARI tablosu sorgulanıyor
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        databases.Add(reader["DB_kod"].ToString());
                    }
                }
            }
        }

        return databases;
    }
    private string GetConnectionStringByVersion(string version)
    {
        return version switch
        {
            "V16" => _configuration.GetConnectionString("MikroDB_V16"),
            "V17" => _configuration.GetConnectionString("MikroDesktop"),
            _ => throw new ArgumentException("Geçersiz versiyon bilgisi.")
        };
    }

    [HttpPost("SetVersion")]
    public IActionResult SetVersion([FromBody] VersionRequest request)
    {
        if (string.IsNullOrEmpty(request.Version))
        {
            return BadRequest(new { success = false, message = "Versiyon bilgisi boş olamaz." });
        }

        // Versiyonu Session'a kaydet
        HttpContext.Session.SetString("SelectedVersion", request.Version);

        return Ok(new { success = true, message = "Versiyon bilgisi kaydedildi." });
    }





    // Versiyon isteği modeli
    public class VersionRequest
    {
        public string Version { get; set; }
    }



    public class DatabaseConnectionRequest
    {
        public string IpAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}