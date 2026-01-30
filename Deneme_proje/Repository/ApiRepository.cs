// Repository/ApiRepository.cs
using Dapper;
using Deneme_proje.Models;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Deneme_proje.Repository
{
    public class ApiRepository
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<ApiRepository> _logger;

        public ApiRepository(
            IConfiguration configuration,
            DatabaseSelectorService dbSelectorService,
            ILogger<ApiRepository> logger)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        private string ERPConnectionString => _configuration.GetConnectionString("ERPDatabase");

        // Sabit değerleri appsettings'ten al
        public string GetApiKey() => _configuration["MikroApi:ApiKey"];
        public string GetKullaniciKodu() => _configuration["MikroApi:KullaniciKodu"];
        public string GetBaseUrl() => _configuration["MikroApi:BaseUrl"];

        // Endpoint URL'lerini al
        public string GetApiUrl(string endpointName)
        {
            var serverAddress = GetServerAddress();
            var baseUrl = string.Format(_configuration["MikroApi:BaseUrl"], serverAddress);
            var endpoint = _configuration[$"MikroApi:Endpoints:{endpointName}"];
            return baseUrl + endpoint;
        }

        // MD5 şifreleme
        public string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Bugünkü tarih + boşluk + şifre MD5
        public string GetEncryptedPassword(string mikroSifre)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string combined = today + " " + mikroSifre; // ✅ Boşluk eklendi

            _logger.LogInformation($"MD5 için birleştirilen: {combined}");

            return GetMD5Hash(combined);
        }

        // ✅ Sistem geneli API ayarlarını getir (tek kayıt)
        public MikroApiAyarlari GetMikroApiAyarlari()
        {
            using var connection = new SqlConnection(ERPConnectionString);

            var query = @"
                SELECT TOP 1
                    id as Id,
                    mikro_sifre as MikroSifre,
                    aktif as Aktif,
                    guncelleme_tarihi as GuncellemeTarihi,
                    guncelleyen_kullanici as GuncelleyenKullanici
                FROM MikroApiAyarlari 
                ORDER BY id DESC";

            return connection.QueryFirstOrDefault<MikroApiAyarlari>(query);
        }

        // ✅ Sistem geneli API ayarlarını kaydet/güncelle
        public bool SaveOrUpdateMikroApiAyarlari(MikroApiAyarlari model)
        {
            using var connection = new SqlConnection(ERPConnectionString);

            try
            {
                if (string.IsNullOrWhiteSpace(model.MikroSifre))
                {
                    throw new ArgumentException("Mikro Şifre boş olamaz.");
                }

                if (model.MikroSifre.Length > 500)
                {
                    throw new ArgumentException("Mikro Şifre 500 karakterden uzun olamaz.");
                }

                _logger.LogInformation($"Mikro API ayarları kaydediliyor. Şifre uzunluğu: {model.MikroSifre.Length}");

                // Mevcut kayıt var mı kontrol et
                var existing = GetMikroApiAyarlari();

                if (existing != null)
                {
                    // Güncelleme
                    var updateQuery = @"
                        UPDATE MikroApiAyarlari 
                        SET 
                            mikro_sifre = @MikroSifre,
                            aktif = @Aktif,
                            guncelleme_tarihi = GETDATE(),
                            guncelleyen_kullanici = @GuncelleyenKullanici
                        WHERE id = @Id";

                    connection.Execute(updateQuery, model);
                    _logger.LogInformation($"Mikro API ayarları güncellendi. Güncelleyen: {model.GuncelleyenKullanici}");
                }
                else
                {
                    // Yeni kayıt
                    var insertQuery = @"
                        INSERT INTO MikroApiAyarlari 
                        (mikro_sifre, aktif, guncelleme_tarihi, guncelleyen_kullanici)
                        VALUES 
                        (@MikroSifre, @Aktif, GETDATE(), @GuncelleyenKullanici)";

                    connection.Execute(insertQuery, model);
                    _logger.LogInformation($"Yeni Mikro API ayarları kaydedildi. Kaydeden: {model.GuncelleyenKullanici}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mikro API ayarları kaydedilirken hata oluştu");
                throw;
            }
        }

        // Server adresini connection string'den al
        public string GetServerAddress()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DynamicDatabase");
                var builder = new SqlConnectionStringBuilder(connectionString);
                return builder.DataSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server adresi alınırken hata");
                return "localhost";
            }
        }
    }
}