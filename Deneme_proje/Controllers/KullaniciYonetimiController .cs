using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using static Deneme_proje.Models.YonetimEntities;

namespace Deneme_proje.Controllers
{
    public class KullaniciYonetimiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _mikroDbConnection;
        private readonly string _dynamicDbConnection; // Yeni eklenecek değişken

        public KullaniciYonetimiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ERPDatabase");
            _mikroDbConnection = _configuration.GetConnectionString("MikroDB_V16");
            _dynamicDbConnection = _configuration.GetConnectionString("DynamicDatabase"); // Burada tanımlıyoruz
        }
        public async Task<IActionResult> Index()
        {
            var kullanicilar = new List<KullaniciListViewModel>();

            try
            {
                // Veritabanı bağlantı bilgilerini al
                var dynamicDbConnectionString = _configuration.GetConnectionString("DynamicDatabase");
                var builder = new SqlConnectionStringBuilder(dynamicDbConnectionString);
                var dynamicDbName = builder.InitialCatalog;

                // Mikro veritabanına bağlan
                using (var connection = new SqlConnection(_mikroDbConnection))
                {
                    await connection.OpenAsync();

                    // Kullanıcıları ve personel e-postalarını tek sorguda al
                    var query = $@"SELECT DISTINCT 
                            k.User_no, 
                            k.User_name, 
                            k.User_LongName, 
                            p.per_PERSMailAddress as Email, 
                            ISNULL(ky.GirisYetkisi, 1) as GirisYetkisi 
                        FROM KULLANICILAR k 
                        LEFT JOIN [{dynamicDbName}].dbo.PERSONELLER p 
                            ON k.User_no = p.per_Userno
                        LEFT JOIN [DBT_ERP].dbo.KullaniciYonetimi ky 
                            ON k.User_no = ky.User_no
                        ORDER BY k.User_no"; // Sıralama ekledim

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // E-posta değerini kontrol et
                                var emailValue = reader["Email"];
                                var emailString = emailValue != DBNull.Value ? emailValue.ToString() : string.Empty;

                                // Kullanıcıyı listeye ekle
                                kullanicilar.Add(new KullaniciListViewModel
                                {
                                    UserNo = reader["User_no"].ToString(),
                                    UserName = reader["User_name"].ToString(),
                                    LongName = reader["User_LongName"].ToString(),
                                    Email = emailString,
                                    GirisYetkisi = Convert.ToBoolean(reader["GirisYetkisi"])
                                });
                            }
                        }
                    }
                }

                return View(kullanicilar);
            }
            catch (Exception ex)
            {
                // Daha açıklayıcı hata mesajı oluştur
                var errorViewModel = new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = $"Kullanıcı listesi alınırken hata oluştu: {ex.Message}"
                };
                return View("Error", errorViewModel);
            }
        }
        [AllowAnonymous]
        public async Task<IActionResult> UpdateYetki(string userNo, bool girisYetkisi)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"IF EXISTS (SELECT 1 FROM KullaniciYonetimi WHERE User_no = @User_no)
                        UPDATE KullaniciYonetimi 
                        SET GirisYetkisi = @girisYetkisi 
                        WHERE User_no = @User_no
                        ELSE
                        INSERT INTO KullaniciYonetimi (User_no, GirisYetkisi) 
                        VALUES (@User_no, @girisYetkisi)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@User_no", userNo);
                        command.Parameters.AddWithValue("@girisYetkisi", girisYetkisi);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UpdateYetkiModel
        {
            public string UserNo { get; set; }
            public bool GirisYetkisi { get; set; }
        }
    }
}