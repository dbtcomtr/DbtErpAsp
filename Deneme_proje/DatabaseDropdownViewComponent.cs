using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Deneme_proje.Components
{
    public class DatabaseDropdownViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;

        public DatabaseDropdownViewComponent(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IViewComponentResult Invoke(string version)
        {
            try
            {
                var databases = GetDatabases(version);
                ViewBag.Databases = databases;
                return View();
            }
            catch (Exception ex)
            {
                // Eğer hata oluşursa, kullanıcıya hata mesajını iletmek için boş bir liste gönderiyoruz.
                ViewBag.ErrorMessage = $"Veritabanı listesi alınırken bir hata oluştu: {ex.Message}";
                ViewBag.Databases = new List<string>();
                return View();
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

    try
    {
        using (var connection = new SqlConnection(connectionString))
        {
            const string query = "SELECT DB_kod FROM VERI_TABANLARI";
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
    }
    catch (SqlException sqlEx)
    {
        throw new Exception($"SQL hatası: {sqlEx.Message}");
    }
    catch (Exception ex)
    {
        throw new Exception($"Bir hata oluştu: {ex.Message}");
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
    }
}
