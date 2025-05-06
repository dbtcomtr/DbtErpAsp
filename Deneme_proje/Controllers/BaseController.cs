using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Deneme_proje.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var selectedVersion = HttpContext.Session.GetString("SelectedVersion");

            if (string.IsNullOrEmpty(selectedVersion))
            {
                selectedVersion = "V16"; // Varsayılan versiyon
            }

            try
            {
                var databases = GetDatabases(selectedVersion);
                ViewBag.Databases = databases;
            }
            catch (Exception ex)
            {
                ViewBag.Databases = new List<string>();
                TempData["ErrorMessage"] = $"Veritabanları yüklenirken bir hata oluştu: {ex.Message}";
            }

            base.OnActionExecuting(filterContext);
        }


        private List<string> GetDatabases(string version)
        {
            var databases = new List<string>();

            // IConfiguration nesnesine doğrudan erişim
            var configuration = HttpContext.RequestServices.GetService<IConfiguration>();
            string connectionString = version == "V16"
                ? configuration.GetConnectionString("MikroDB_V16")
                : configuration.GetConnectionString("MikroDesktop");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                const string query = "SELECT DB_kod FROM VERI_TABANLARI";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        databases.Add(reader["DB_kod"].ToString());
                    }
                }
            }

            return databases;
        }


        private string GetConnectionStringByVersion(string version, IConfiguration configuration)
        {
            return version switch
            {
                "V16" => configuration.GetConnectionString("MikroDB_V16"),
                "V17" => configuration.GetConnectionString("MikroDesktop"),
                _ => throw new ArgumentException("Geçersiz versiyon bilgisi.")
            };
        }


        [HttpPost]
        public IActionResult SelectDatabase(string databaseName)
        {
            if (!string.IsNullOrEmpty(databaseName))
            {
                try
                {
                    // Seçilen veritabanını session'a kaydet
                    HttpContext.Session.SetString("SelectedDatabase", databaseName);

                    // Seçilen versiyonu al
                    var selectedVersion = HttpContext.Session.GetString("SelectedVersion") ?? "V16";

                    // DatabaseSelectorService'i kullanarak bağlantı ayarlarını güncelle
                    var dbSelectorService = HttpContext.RequestServices.GetRequiredService<DatabaseSelectorService>();
                    dbSelectorService.GetConnectionString();

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Veritabanı seçimi sırasında bir hata oluştu: {ex.Message}";
                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["ErrorMessage"] = "Lütfen geçerli bir veritabanı seçin.";
            return RedirectToAction("Index", "Home");
        }

        private void UpdateConnectionString(string version, string databaseName)
        {
            // appsettings.json dosyasının yolunu belirleyin
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

            // appsettings.json'u oku
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            // Versiyona göre doğru bağlantı dizgesini güncelle
            string connectionStringKey = version == "V16" ? "MikroDB_V16" : "MikroDesktop";
            string baseConnectionString = jsonObj["ConnectionStrings"][connectionStringKey];

            // Yeni veritabanı adını ekle
            if (!baseConnectionString.Contains("Database="))
            {
                baseConnectionString += $";Database={databaseName}";
            }
            else
            {
                baseConnectionString = System.Text.RegularExpressions.Regex.Replace(
                    baseConnectionString,
                    @"Database=[^;]*",
                    $"Database={databaseName}"
                );
            }

            // Güncellenmiş bağlantı dizgesini JSON nesnesine yaz
            jsonObj["ConnectionStrings"][connectionStringKey] = baseConnectionString;

            // Güncellenmiş JSON'u appsettings.json'a yaz
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            System.IO.File.WriteAllText(appSettingsPath, output);
        }



    }
}