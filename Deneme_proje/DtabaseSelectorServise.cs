using System.Data.SqlClient;
using Newtonsoft.Json;

public class DatabaseSelectorService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DatabaseSelectorService> _logger;

    // Cache değişkenleri
    private static string _cachedConnectionString;
    private static string _lastDatabaseName;
    private static readonly object _lockObject = new object();

    public DatabaseSelectorService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DatabaseSelectorService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetConnectionString()
    {
        var username = _httpContextAccessor.HttpContext.Session.GetString("Username");
        var version = _httpContextAccessor.HttpContext.Session.GetString("SelectedVersion") ?? "V16";
        var databaseName = _httpContextAccessor.HttpContext.Session.GetString("SelectedDatabase");

        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = GetDefaultDatabase(version, username);
            _httpContextAccessor.HttpContext.Session.SetString("SelectedDatabase", databaseName);
        }

        string fullDatabaseName = version == "V16"
            ? $"MikroDB_V16_{databaseName}"
            : $"MikroDesktop_{databaseName}";

        // Cache kontrolü - eğer aynı database ise dosya işlemi yapma
        lock (_lockObject)
        {
            if (_lastDatabaseName == fullDatabaseName && !string.IsNullOrEmpty(_cachedConnectionString))
            {
                return _cachedConnectionString;
            }

            var baseConnectionString = _configuration.GetConnectionString("DynamicDatabase");
            var connectionString = AddOrUpdateDatabaseInConnectionString(baseConnectionString, fullDatabaseName);

            // Sadece database değiştiyse dosyayı güncelle
            if (_lastDatabaseName != fullDatabaseName)
            {
                try
                {
                    UpdateAppSettings(connectionString);
                    _cachedConnectionString = connectionString;
                    _lastDatabaseName = fullDatabaseName;
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    // Dosya kilitlendiyse cache'den dön (eğer varsa)
                    if (!string.IsNullOrEmpty(_cachedConnectionString))
                    {
                        _logger.LogWarning("appsettings.json dosya kilidi, cache'den connection string dönülüyor");
                        return _cachedConnectionString;
                    }

                    // Cache yoksa bekleyip tekrar dene
                    Thread.Sleep(50);
                    UpdateAppSettings(connectionString);
                    _cachedConnectionString = connectionString;
                    _lastDatabaseName = fullDatabaseName;
                }
            }

            return connectionString;
        }
    }

    private void UpdateAppSettings(string connectionString)
    {
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        // Retry mekanizması
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var json = File.ReadAllText(appSettingsPath);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["ConnectionStrings"]["DynamicDatabase"] = connectionString;
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(appSettingsPath, output);
                break; // Başarılı olursa döngüden çık
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(100 * (i + 1)); // Her denemede daha uzun bekle
            }
        }
    }

    // Diğer metodlarınız aynı kalır...
    public IConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public string GetERPConnectionString()
    {
        return _configuration.GetConnectionString("ERPDatabase");
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
                        defaultDb = GetFirstAvailableDatabase(version);
                    }

                    return defaultDb;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Varsayılan veritabanı alınırken hata oluştu");
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

    private string AddOrUpdateDatabaseInConnectionString(string connectionString, string databaseName)
    {
        if (!connectionString.Contains("Database="))
        {
            connectionString += $";Database={databaseName}";
        }
        else
        {
            connectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Database=[^;]*",
                $"Database={databaseName}"
            );
        }
        return connectionString;
    }
}