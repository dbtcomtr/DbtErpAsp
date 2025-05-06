using System.Data.SqlClient;
using Newtonsoft.Json;

public class DatabaseSelectorService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DatabaseSelectorService> _logger;

    public DatabaseSelectorService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DatabaseSelectorService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetDefaultDatabase(string version, string username)
    {
        try
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT db_varsayilan 
                                 FROM Web_Kullanici 
                                 WHERE kullanici_adi = @username";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    var defaultDb = command.ExecuteScalar()?.ToString();

                    if (string.IsNullOrEmpty(defaultDb))
                    {
                        // Varsayılan veritabanı yoksa, ilk veritabanını al
                        defaultDb = GetFirstAvailableDatabase(version);
                    }

                    return defaultDb;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Varsayılan veritabanı alınırken hata oluştu");

            // Hata durumunda ilk veritabanını al
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
            _logger.LogError(ex, "İlk veritabanı alınırken hata oluştu");
            throw;
        }
    }

    public string GetConnectionString()
    {
        // Oturumdan kullanıcı adını al
        var username = _httpContextAccessor.HttpContext.Session.GetString("Username");

        // Oturumdan versiyon bilgisini al
        var version = _httpContextAccessor.HttpContext.Session.GetString("SelectedVersion") ?? "V16";

        // Varsayılan veritabanını al
        var databaseName = _httpContextAccessor.HttpContext.Session.GetString("SelectedDatabase");

        // Eğer veritabanı seçili değilse, varsayılan veya ilk veritabanını al
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = GetDefaultDatabase(version, username);

            // Seçilen veritabanını oturuma kaydet
            _httpContextAccessor.HttpContext.Session.SetString("SelectedDatabase", databaseName);
        }

        // Versiyona göre veritabanı adını güncelle
        string fullDatabaseName = version == "V16"
            ? $"MikroDB_V16_{databaseName}"
            : $"MikroDesktop_{databaseName}";

        // Dinamik bağlantı dizesini al
        var baseConnectionString = _configuration.GetConnectionString("DynamicDatabase");

        // Bağlantı dizesini güncelle
        var connectionString = AddOrUpdateDatabaseInConnectionString(baseConnectionString, fullDatabaseName);

        // appsettings.json'ı güncelle
        UpdateAppSettings(connectionString);

        return connectionString;
    }
    // Connection string'e Database parametresini ekleme/güncelleme metodu
    private string AddOrUpdateDatabaseInConnectionString(string connectionString, string databaseName)
    {
        if (!connectionString.Contains("Database="))
        {
            // Eğer Database parametresi yoksa, ekle
            connectionString += $";Database={databaseName}";
        }
        else
        {
            // Eğer Database parametresi varsa, güncelle
            connectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Database=[^;]*",
                $"Database={databaseName}"
            );
        }
        return connectionString;
    }
    private void UpdateAppSettings(string connectionString)
    {
        // appsettings.json dosyasının yolunu belirliyoruz
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        // appsettings.json dosyasını okuyoruz
        var json = File.ReadAllText(appSettingsPath);
        dynamic jsonObj = JsonConvert.DeserializeObject(json);
        // DynamicDatabase bağlantı dizgesini güncelliyoruz
        jsonObj["ConnectionStrings"]["DynamicDatabase"] = connectionString;
        // Güncellenmiş JSON'u dosyaya yazıyoruz
        string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(appSettingsPath, output);
    }
}