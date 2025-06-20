using Microsoft.Data.SqlClient; // SqlConnection için yeni ad alanı
using System.Data;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // ILogger ekleyin
using Deneme_proje.Models;
using static Deneme_proje.Models.Entities;
using static Deneme_proje.Models.SirketDurumuEntites;

using Deneme_proje.Helpers;
using System.Configuration;
namespace Deneme_proje.Repository

{
    public class FaturaRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<FaturaRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FaturaRepository(
            DatabaseSelectorService dbSelectorService,
            ILogger<FaturaRepository> logger,
            IHttpContextAccessor httpContextAccessor) // Constructor'a ekleme
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<Deneme_proje.Models.Entities.FaturaViewModel> GetFaturaData(string cariUnvani, float ticariFaiz)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
SELECT 
    cha_evrakno_sira AS [EvrakNo],
    cari_unvan1 AS [CariUnvani],
    cari_kod AS [CariKodu],
   cha_tarihi AS [FaturaTarihi],

    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    chk_Alacakvade AS [AlacakVade],
    chk_BorcVade AS [FaturaVadeTarihi],
    (CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT) - 
     CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT)) AS [FaturaVadesi],
    cha_meblag AS [FaturaTutari],
    chk_Tutar AS [TaksitTutar],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT) AS [AlacakVadeTarihiSayi],
    66.24 AS [FaizOrani],
    (ISNULL(chk_Tutar, 0) * CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT)) AS [BorcTutari]
FROM 
    CARI_HAREKET_BORC_ALACAK_ESLEME 
LEFT JOIN 
    CARI_HESAPLAR ON chk_ChKodu = cari_kod
LEFT JOIN 
    CARI_HESAP_HAREKETLERI ON chk_Borc_uid = cha_Guid
	WHERE 
    cari_kod LIKE '120%' AND  cha_meblag IS NOT NULL AND cha_tarihi IS NOT NULL AND cha_evrak_tip IN (29, 63)
";

                // Eğer cariUnvani boş değilse, LIKE ifadesi ile kısmi eşleşme yapın
                if (!string.IsNullOrEmpty(cariUnvani))
                {
                    query += " WHERE cari_unvan1 LIKE @CariUnvani";
                }

                var parameters = new { CariUnvani = "%" + cariUnvani + "%" };

                try
                {
                    var results = connection.Query<Deneme_proje.Models.Entities.FaturaViewModel>(query, parameters).ToList();
                    _logger.LogInformation($"Results Count: {results.Count}");

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<TedarikciFaturaViewModel> GetTedarikciFaturaData(string cariUnvani, float ticariFaiz)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
SELECT 
    cha_evrakno_sira AS [EvrakNo],
    cha_kod AS [CariKodu],
    cari_unvan1 AS [CariUnvani],
    cha_tarihi AS [FaturaTarihi],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    chk_BorcVade AS [BorcVade],
    chk_Alacakvade AS [FaturaVadeTarihi],
    (CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT) - 
     CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT)) AS [FaturaVadesi],
    cha_meblag AS [FaturaTutari],
    chk_Tutar AS [OdemeTutar],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT) AS [BorcVadeTarihiSayi],
    66.24 AS [FaizOrani],
    (ISNULL(chk_Tutar, 0) * CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT)) AS [BorcTutari]
FROM 
    CARI_HAREKET_BORC_ALACAK_ESLEME 
LEFT JOIN 
    CARI_HESAPLAR ON chk_ChKodu = cari_kod
LEFT JOIN 
    CARI_HESAP_HAREKETLERI ON chk_Alc_uid = cha_Guid
	WHERE 
    cari_kod LIKE '320%' AND  cha_meblag IS NOT NULL AND cha_tarihi IS NOT NULL  AND cha_evrak_tip IN (29, 0)

";

                // Eğer cariUnvani boş değilse, LIKE ifadesi ile kısmi eşleşme yapın
                if (!string.IsNullOrEmpty(cariUnvani))
                {
                    query += " WHERE cari_unvan1 LIKE @CariUnvani";
                }

                var parameters = new { CariUnvani = "%" + cariUnvani + "%" };

                try
                {
                    var results = connection.Query<TedarikciFaturaViewModel>(query, parameters).ToList();
                    _logger.LogInformation($"Results Count: {results.Count}");

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KrediDetayViewModel> GetKrediDetayData()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_Finans_KrediDetayTL";

                try
                {
                    var results = connection.Query<KrediDetayViewModel>(
                        procedureName,
                        commandType: CommandType.StoredProcedure).ToList();

                    _logger.LogInformation($"Results Count: {results.Count}");
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KrediDetayModel> GetKrediDetay()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_Finans_KrediDetay_AyBazli";

                try
                {
                    connection.Open();
                    var results = connection.Query<KrediDetayModel>(
                        procedureName,
                        commandType: CommandType.StoredProcedure).ToList();

                    _logger.LogInformation($"Results Count: {results.Count}");

                    foreach (var result in results)
                    {
                        _logger.LogInformation($"Yıl: {result.Yıl}, Ay: {result.Ay}, VadeTarihi: {result.VadeTarihi}, TaksitAnapara: {result.TaksitAnapara}, TaksitFaiz: {result.TaksitFaiz}, TaksitBSMV: {result.TaksitBSMV}");
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KrediDetay> GetKrediDetayListesi()
        {
            var krediDetayListesi = new List<KrediDetay>();

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("dbo.DBT_Finans_KrediDetayTL", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            krediDetayListesi.Add(new KrediDetay
                            {
                                Banka = reader["Banka"].ToString(),
                                AnaPara = reader.IsDBNull(reader.GetOrdinal("AnaPara"))
                                          ? 0
                                          : Convert.ToDecimal(reader["AnaPara"]),
                                //Aokf = reader.IsDBNull(reader.GetOrdinal("Aokf"))
                                //       ? 0
                                //       : Convert.ToDecimal(reader["Aokf"])
                            });
                        }
                    }
                }
            }

            return krediDetayListesi;
        }
        public IEnumerable<KrediDetayi> GetKrediDetayListByBankCode(string bankCode)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
        SELECT 
            krsoztaksit_sozkodu, 
            krsoztaksit_vade, 
            krsoztaksit_taksit, 
            krsoztaksit_anapara, 
            krsoztaksit_faiz, 
            krsoztaksit_bsmv,
            krsoz_sozbankakodu,
            ban_ismi,
            krsoztaksit_faizorani,
            krsoztaksit_kalananapara 
        FROM KREDI_SOZLESMESI_TANIMLARI
        LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
        LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
        WHERE krsoz_sozbankakodu = @BankCode";

                var krediDetayListesi = connection.Query<KrediDetayi>(sql, new { BankCode = bankCode }).ToList();

                return krediDetayListesi;
            }
        }

        // FaturaRepository.cs dosyasında GetKrediDetayList metodunu güncelleyin
        public IEnumerable<IGrouping<string, KrediDetayi>> GetKrediDetayList()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var bugun = DateTime.Now; // Bugünkü tarih

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
SELECT 
    krsoztaksit_sozkodu, 
    krsoztaksit_vade, 
    krsoztaksit_taksit, 
    krsoztaksit_anapara, 
    krsoztaksit_odenen_ana, 
    krsoztaksit_faiz, 
    krsoztaksit_bsmv,
    krsoz_sozbankakodu,
    ban_ismi,
    krsoztaksit_odemeevraksira,
    krsoztaksit_faizorani,
    krsoztaksit_kalananapara, 
    (krsoztaksit_taksit - krsoztaksit_odenen_ana) AS kalan
FROM KREDI_SOZLESMESI_TANIMLARI
LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
WHERE krsoztaksit_vade >= @Bugun -- Bugünden sonraki vadeli taksitler
  AND (krsoztaksit_taksit - krsoztaksit_odenen_ana) > 10
ORDER BY krsoztaksit_vade -- Vadeye göre sıralama
";

                var krediDetayListesi = connection.Query<KrediDetayi>(sql, new { Bugun = bugun }).ToList();

                // Group by bank name (ban_ismi) instead of bank code
                var groupedData = krediDetayListesi
                    .GroupBy(x => x.ban_ismi) // Grouping by Bank Name
                    .ToList();

                return groupedData;
            }
        }

        public IEnumerable<CariBakiyeYaslandirma> GetCariMusteriYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar, bool includeZeroBalances = false)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.DBT_sp_Cari_Musteri_Yaslandirma_WEB", connection))
                {
                    command.CommandTimeout = 120;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CariIlkKod", cariIlkKod);
                    command.Parameters.AddWithValue("@CariSonKod", cariSonKod);
                    command.Parameters.AddWithValue("@CariKodYapisi", cariKodYapisi);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi.HasValue ? (object)raporTarihi.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@HangiHesaplar", hangiHesaplar);
                    command.Parameters.AddWithValue("@IncludeZeroBalances", includeZeroBalances);

                    using (var reader = command.ExecuteReader())
                    {
                        var result = new List<CariBakiyeYaslandirma>();
                        while (reader.Read())
                        {
                            result.Add(new CariBakiyeYaslandirma
                            {
                                MusteriKodu = reader.GetString(0),
                                Unvan = reader.GetString(1),
                                VadesiGecenBakiye = reader.GetDouble(2),
                                VadesiGecmisBakiye = reader.GetDouble(3),
                                ToplamBakiye = reader.GetDouble(4),
                                BakiyeTipi = reader.GetString(5),
                                Gun30 = reader.GetDouble(6),
                                Gun60 = reader.GetDouble(7),
                                Gun90 = reader.GetDouble(8),
                                Gun120 = reader.GetDouble(9),
                                gecmisGun30 = reader.GetDouble(10),
                                gecmisGun60 = reader.GetDouble(11),
                                gecmisGun90 = reader.GetDouble(12),
                                gecmisGun120 = reader.GetDouble(13),
                                GunUstu120 = reader.GetDouble(14),
                                eksi120GunUstu = reader.GetDouble(reader.GetOrdinal("eksi120GunUstu"))
                            });
                        }
                        return result;
                    }
                }
            }
        }

        public IEnumerable<CariBakiyeYaslandirma> GetCariTedarikciYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar, bool includeZeroBalances = false)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.DBT_sp_Cari_Tedarikci_Yaslandirma_WEB", connection))
                {
                    command.CommandTimeout = 120;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CariIlkKod", cariIlkKod);
                    command.Parameters.AddWithValue("@CariSonKod", cariSonKod);
                    command.Parameters.AddWithValue("@CariKodYapisi", cariKodYapisi);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi.HasValue ? (object)raporTarihi.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@HangiHesaplar", hangiHesaplar);
                    command.Parameters.AddWithValue("@IncludeZeroBalances", includeZeroBalances);

                    using (var reader = command.ExecuteReader())
                    {
                        var result = new List<CariBakiyeYaslandirma>();
                        while (reader.Read())
                        {
                            result.Add(new CariBakiyeYaslandirma
                            {
                                MusteriKodu = reader.GetString(0),
                                Unvan = reader.GetString(1),
                                VadesiGecenBakiye = reader.GetDouble(2),
                                VadesiGecmisBakiye = reader.GetDouble(3),
                                ToplamBakiye = reader.GetDouble(4),
                                BakiyeTipi = reader.GetString(5),
                                Gun30 = reader.GetDouble(6),
                                Gun60 = reader.GetDouble(7),
                                Gun90 = reader.GetDouble(8),
                                Gun120 = reader.GetDouble(9),
                                gecmisGun30 = reader.GetDouble(10),
                                gecmisGun60 = reader.GetDouble(11),
                                gecmisGun90 = reader.GetDouble(12),
                                gecmisGun120 = reader.GetDouble(13),
                                GunUstu120 = reader.GetDouble(14),
                                eksi120GunUstu = reader.GetDouble(reader.GetOrdinal("eksi120GunUstu"))
                            });
                        }
                        return result;
                    }
                }
            }
        }
        public IEnumerable<StockMovement> GetStokYaslandirma(string stockCode, DateTime reportDate, int? depoNo = null)
        {
            var stockMovements = new List<StockMovement>();
            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("dbo.stok_yaslandirma_WEB", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StokKod", (object)stockCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RaporTarihi", reportDate);
                        command.Parameters.AddWithValue("@DepoNo", (object)depoNo ?? DBNull.Value);
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stockMovement = new StockMovement
                                {
                                    MsgS0088 = reader.GetGuid(reader.GetOrdinal("#msg_S_0088")),
                                    MsgS0078 = reader.GetString(reader.GetOrdinal("msg_S_0078")),
                                    MsgS0870 = reader.GetString(reader.GetOrdinal("msg_S_0870")),
                                    DepoAdi = reader.GetString(reader.GetOrdinal("dep_adi")),
                                    BirimAdi = reader.GetString(reader.GetOrdinal("sto_birim1_ad")),

                                    // Güvenli double okuma
                                    MsgS0165 = GetSafeDouble(reader, "msg_S_0165"),

                                    StokEvraknoSeri = reader.GetString(reader.GetOrdinal("sth_evrakno_seri")),
                                    StokEvraknoSira = reader.GetInt32(reader.GetOrdinal("sth_evrakno_sira")),

                                    // Güvenli double okuma
                                    StokMiktar = GetSafeDouble(reader, "sth_miktar"),
                                    StokTutar = GetSafeDouble(reader, "sth_tutar"),

                                    StokTarih = reader.GetDateTime(reader.GetOrdinal("sth_tarih")),

                                    // Güvenli double okuma
                                    CumulativeQuantity = GetSafeDouble(reader, "CumulativeQuantity"),
                                    StoktaGirenMiktar = GetSafeDouble(reader, "Stokta_Giren_Miktar"),
                                    Days0To30 = GetSafeDouble(reader, "Days0To30"),
                                    Days31To60 = GetSafeDouble(reader, "Days31To60"),
                                    Days61To90 = GetSafeDouble(reader, "Days61To90"),
                                    Days90Plus = GetSafeDouble(reader, "Days90Plus"),
                                    NumericDate = GetSafeDouble(reader, "NumericDate"),
                                    AltDovizKuru = GetSafeDouble(reader, "sth_alt_doviz_kuru"),

                                    // Negatif stok kontrolü (bool olarak)
                                    IsNegativeStock = reader.GetString(reader.GetOrdinal("sth_evrakno_seri")) == "NEG-STOCK"
                                };
                                stockMovements.Add(stockMovement);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GetStokYaslandirma Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            return stockMovements;
        }

        // Güvenli double okuma helper metodu - sınıfınıza ekleyin
        private double GetSafeDouble(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                var value = reader.GetValue(ordinal);

                if (value == null || value == DBNull.Value)
                    return 0.0;

                // Farklı numeric türleri handle et
                switch (value)
                {
                    case double d:
                        return d;
                    case float f:
                        return (double)f;
                    case decimal dec:
                        return (double)dec;
                    case int i:
                        return (double)i;
                    case long l:
                        return (double)l;
                    case short s:
                        return (double)s;
                    case byte b:
                        return (double)b;
                    default:
                        // String olarak parse etmeyi dene
                        if (double.TryParse(value.ToString(), out double result))
                            return result;
                        return 0.0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSafeDouble error for column {columnName}: {ex.Message}");
                return 0.0;
            }
        }

        // Alternatif olarak, Convert.ToDouble kullanabilirsiniz
        private double GetSafeDoubleAlternative(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                var value = reader.GetValue(ordinal);

                if (value == null || value == DBNull.Value)
                    return 0.0;

                return Convert.ToDouble(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSafeDoubleAlternative error for column {columnName}: {ex.Message}");
                return 0.0;
            }
        }


        public IEnumerable<Depo> GetDepoList()
        {
            var depoList = new List<Depo>();

            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("SELECT dep_no, dep_adi FROM DEPOLAR", connection))
                    {
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var depo = new Depo
                                {
                                    DepoNo = reader.GetInt32(reader.GetOrdinal("dep_no")),
                                    DepoAdi = reader.GetString(reader.GetOrdinal("dep_adi"))
                                };

                                depoList.Add(depo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata işlemleri
                Console.WriteLine($"Hata: {ex.Message}");
            }

            return depoList;
        }

        public List<StockAging> GetStockAging(string stokKod, DateTime? raporTarihi)
        {
            var stockAgingList = new List<StockAging>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("stok_yaslandirma_WEB", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StokKod", stokKod ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi ?? (object)DBNull.Value);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // Sütun adlarını kontrol edin
                        int guidIndex = reader.GetOrdinal("#msg_S_0088"); // GUID sütunu
                        int stokKodIndex = reader.GetOrdinal("msg_S_0078");
                        int stokIsimIndex = reader.GetOrdinal("msg_S_0870");
                        int evrakSeriIndex = reader.GetOrdinal("sth_evrakno_seri");
                        int evrakSiraIndex = reader.GetOrdinal("sth_evrakno_sira");
                        int miktarIndex = reader.GetOrdinal("sth_miktar");
                        int tutarIndex = reader.GetOrdinal("sth_tutar");
                        int tarihIndex = reader.GetOrdinal("sth_tarih");
                        int cumulativeQuantityIndex = reader.GetOrdinal("CumulativeQuantity");
                        int stoktaGirenMiktarIndex = reader.GetOrdinal("Stokta_Giren_Miktar");
                        int stokMiktarIndex = reader.GetOrdinal("msg_S_0165");
                        int days0To30Index = reader.GetOrdinal("Days0To30");
                        int days31To60Index = reader.GetOrdinal("Days31To60");
                        int days61To90Index = reader.GetOrdinal("Days61To90");
                        int days90PlusIndex = reader.GetOrdinal("Days90Plus");

                        while (reader.Read())
                        {
                            var stockAging = new StockAging
                            {
                                Guid = reader.IsDBNull(guidIndex) ? Guid.Empty : reader.GetGuid(guidIndex),
                                StokKod = reader.IsDBNull(stokKodIndex) ? null : reader.GetString(stokKodIndex),
                                StokIsim = reader.IsDBNull(stokIsimIndex) ? null : reader.GetString(stokIsimIndex),
                                EvrakSeri = reader.IsDBNull(evrakSeriIndex) ? null : reader.GetString(evrakSeriIndex),
                                EvrakSira = reader.IsDBNull(evrakSiraIndex) ? 0 : reader.GetInt32(evrakSiraIndex),
                                Miktar = reader.IsDBNull(miktarIndex) ? 0f : (float)reader.GetDouble(miktarIndex),
                                Tutar = reader.IsDBNull(tutarIndex) ? 0f : (float)reader.GetDouble(tutarIndex),
                                Tarih = reader.GetDateTime(tarihIndex),
                                CumulativeQuantity = reader.IsDBNull(cumulativeQuantityIndex) ? 0f : (float)reader.GetDouble(cumulativeQuantityIndex),
                                StoktaGirenMiktar = reader.IsDBNull(stoktaGirenMiktarIndex) ? 0f : (float)reader.GetDouble(stoktaGirenMiktarIndex),
                                StokMiktar = reader.IsDBNull(stokMiktarIndex) ? 0f : (float)reader.GetDouble(stokMiktarIndex),
                                Days0To30 = reader.IsDBNull(days0To30Index) ? 0f : (float)reader.GetDouble(days0To30Index),
                                Days31To60 = reader.IsDBNull(days31To60Index) ? 0f : (float)reader.GetDouble(days31To60Index),
                                Days61To90 = reader.IsDBNull(days61To90Index) ? 0f : (float)reader.GetDouble(days61To90Index),
                                Days90Plus = reader.IsDBNull(days90PlusIndex) ? 0f : (float)reader.GetDouble(days90PlusIndex),
                            };

                            stockAgingList.Add(stockAging);
                        }
                    }
                }
            }

            return stockAgingList;
        }

        // Stok isimlerini getiren metot
        //public List<string> GetAllStockNames()
        //{
        //    var stockNames = new List<string>();

        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        using (var command = new SqlCommand("SELECT DISTINCT msg_S_0078 FROM StockTable", connection))
        //        {
        //            connection.Open();
        //            using (var reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    stockNames.Add(reader.GetString(0));
        //                }
        //            }
        //        }
        //    }

        //    return stockNames;
        //}

        public IEnumerable<StockCodeAndName> GetStockCodesAndNames()
        {
            var stockCodesAndNames = new List<StockCodeAndName>();

            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("dbo.GetStockCodesAndNames", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stockCodeAndName = new StockCodeAndName
                                {
                                    StockCode = reader.GetString(reader.GetOrdinal("StockCode")),
                                    StockName = reader.GetString(reader.GetOrdinal("StockName"))
                                };

                                stockCodesAndNames.Add(stockCodeAndName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                Console.WriteLine("Error: " + ex.Message);
            }

            return stockCodesAndNames;
        }
        public IEnumerable<MusteriCekViewModel> GetMusteriCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_MusteriCekleri_TariheGore 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<MusteriCekViewModel>(query, parameters);
            }
        }

        // Firma Çekleri Verisini Al
        public IEnumerable<FirmaCekViewModel> GetFirmaCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_FirmaCekleri_TariheGore 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<FirmaCekViewModel>(query, parameters);
            }
        }
        public IEnumerable<VadesiGecmisCekViewModel> VadesiGecmisFirmaCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_VadesiGecenFirmaCekleri 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<VadesiGecmisCekViewModel>(query, parameters);
            }
        }

        public IEnumerable<VadesiGecmisCekViewModel> VadesiGecenMusteriCekler(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_VadesiGecenMusteriCekler 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<VadesiGecmisCekViewModel>(query, parameters);
            }
        }

        public IEnumerable<MusteriKrediKartlari> GetMusteriKrediKartlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_Finans_MusteriKrediKartlari_WEB 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<MusteriKrediKartlari>(query, parameters);
            }
        }

        public IEnumerable<FirmaKrediKartlari> GetFirmaKrediKartlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_Finans_FirmaKrediKartlari 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<FirmaKrediKartlari>(query, parameters);
            }
        }
        public IEnumerable<ArtiBakiyeFaturaViewModel> GetArtiBakiyeFaturaMusteri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslangicTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }; // Parametreler doğru isimde ve formatta olmalı
                var query = @"
            EXEC dbo.DBT_Arti_Bakiye_Fatura_Musteri 
                @BaslangicTarihi = @BaslangicTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<ArtiBakiyeFaturaViewModel>(query, parameters);
            }
        }
        public IEnumerable<ArtiBakiyeFaturaViewModel> GetArtiBakiyeFaturaTedarikci(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslangicTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }; // Parametreler doğru isimde ve formatta olmalı
                var query = @"
            EXEC dbo.DBT_Arti_Bakiye_Fatura_Tedarikci 
                @BaslangicTarihi = @BaslangicTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<ArtiBakiyeFaturaViewModel>(query, parameters);
            }
        }
        public IEnumerable<StokFoy> GetStokFoy(string stokkod, DateTime? devirtarih, DateTime? ilktar, DateTime? sontar, byte dovizCinsi, string depolarstr)
        {
            var stokFoyList = new List<StokFoy>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.sp_StokFoy", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@stokkod", stokkod);
                    command.Parameters.AddWithValue("@devirtarih", devirtarih.HasValue ? (object)devirtarih.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ilktar", ilktar.HasValue ? (object)ilktar.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@sontar", sontar.HasValue ? (object)sontar.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@DovizCinsi", dovizCinsi);
                    command.Parameters.AddWithValue("@Depolarstr", depolarstr);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stokFoyList.Add(new StokFoy
                            {
                                KayitNo = reader["KayitNo"] as Guid?,
                                Tarih = reader["Tarih"] as DateTime?,
                                TarihGunSayisi = reader.GetInt32(reader.GetOrdinal("TarihGunSayisi")),
                                FaturaTarihi = reader["FaturaTarihi"] as DateTime?,
                                SeriNo = reader["SeriNo"].ToString(),
                                SiraNo = reader["SiraNo"] as int?,
                                EvrakTipi = reader["EvrakTipi"].ToString(),
                                HareketCinsi = reader["HareketCinsi"].ToString(),
                                Tipi = reader["Tipi"].ToString(),
                                GirisCikis = reader.GetInt32(reader.GetOrdinal("GirisCikis")),
                                Ni = reader["Ni"].ToString(),
                                BelgeNo = reader["BelgeNo"].ToString(),
                                BelgeTarihi = reader["BelgeTarihi"] as DateTime?,
                                DepoAdi = reader["DepoAdi"].ToString(),
                                NakliyeHedefDepo = reader["NakliyeHedefDepo"].ToString(),
                                KarsiDepoAdi = reader["KarsiDepoAdi"].ToString(),
                                PartiNo = reader["PartiNo"].ToString(),
                                LotNo = reader["LotNo"] as int?,
                                IsMerkeziKodu = reader["IsMerkeziKodu"].ToString(),
                                ProjeKodu = reader["ProjeKodu"].ToString(),
                                ProjeAdi = reader["ProjeAdi"].ToString(),
                                Stokta_Giren_Miktar = Convert.ToDouble(reader["Stokta_Giren_Miktar"]),
                                BirimAdi = reader["BirimAdi"].ToString(),
                                AltDovizKuru = Convert.ToDouble(reader["AltDovizKuru"]),
                                BrutBirimFiyati = Convert.ToDouble(reader["BrutBirimFiyati"]),
                                NetBirimFiyati = Convert.ToDouble(reader["NetBirimFiyati"]),
                                BrutTutar = Convert.ToDouble(reader["BrutTutar"]),
                                NetTutar = Convert.ToDouble(reader["NetTutar"]),
                                GirenMiktar = Convert.ToDouble(reader["GirenMiktar"]),
                                CikanMiktar = Convert.ToDouble(reader["CikanMiktar"]),
                                KalanMiktar = Convert.ToDouble(reader["KalanMiktar"])
                            });
                        }
                    }
                }
            }

            return stokFoyList; // Boş bile olsa her zaman bir liste döndür
        }

        public List<KrediDetayi> GetKrediDetay(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT 
                krsoztaksit_sozkodu, 
                krsoztaksit_vade, 
                krsoztaksit_taksit, 
                krsoztaksit_anapara, 
                krsoztaksit_odenen_ana, 
                krsoztaksit_faiz, 
                krsoztaksit_bsmv,
                krsoz_sozbankakodu,
                ban_ismi,
                krsoztaksit_odemeevraksira,
                krsoztaksit_faizorani,
                (krsoztaksit_taksit - krsoztaksit_odenen_ana) AS kalan -- Kalan anapara hesaplaması
            FROM KREDI_SOZLESMESI_TANIMLARI
            LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
            LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
            WHERE krsoztaksit_vade BETWEEN @BaslamaTarihi AND @BitisTarihi";

                return connection.Query<KrediDetayi>(sql, new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }).ToList();
            }
        }

        public IEnumerable<CiroRaporuDepoBazli> GetCiroRaporuDepoBazli(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_RAPOR_CiroRaporuDepoBazlı 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<CiroRaporuDepoBazli>(query, parameters);
            }
        }

        public IEnumerable<EnCokSatilanUrunler> GetEnCokSatilanUrunler(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_MO_SatisMiktarinaGoreEnCokSatilanUrunler 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<EnCokSatilanUrunler>(query, parameters);
            }
        }

        public IEnumerable<SatilanMalinKarlilikveMaliyet> GetSatilanMalinKarlilikveMaliyet(DateTime baslamaTarihi, DateTime bitisTarihi, string depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new
                {
                    BaslamaTarihi = baslamaTarihi,
                    BitisTarihi = bitisTarihi,
                    DepoNo = depoNo
                };

                var query = @"
                    EXEC dbo.DBT_RAPOR_SatilanMalinKarlilikveMaliyetRaporu 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi,
                        @DepoNo = @DepoNo";

                return connection.Query<SatilanMalinKarlilikveMaliyet>(query, parameters);
            }
        }
        public IEnumerable<StokRaporuViewModel> GetStokRaporu(int? anaGrup, int? reyonKodu, int depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "sp_StokRaporu";

                var parameters = new
                {
                    AnaGrup = anaGrup,
                    ReyonKodu = reyonKodu,
                    DepoNo = depoNo
                };

                try
                {
                    var results = connection.Query<StokRaporuViewModel>(
                        procedureName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok raporu alınırken hata oluştu.");
                    throw;
                }
            }
        }

        // İş emri durumunu güncelleyen metod
        private string GetCurrentUserNo()
        {
            // Öncelikle session'dan kullanıcı numarasını almayı deneyin
            var userNo = _httpContextAccessor.HttpContext?.Session.GetString("UserNo");

            if (string.IsNullOrEmpty(userNo))
            {
                // Eğer session'da yoksa, kimlik adını kullanın
                userNo = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            }

            _logger.LogInformation($"Mevcut Kullanıcı Numarası: {userNo}");
            return userNo ?? string.Empty;
        }

        // Mevcut GetIsEmirleri metodunu güncelle
        public IEnumerable<IsEmriModel> GetIsEmirleri()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = HasProductionPermission()
                    ? GetFullProductionQuery()
                    : GetLimitedProductionQuery();

                try
                {
                    return connection.Query<IsEmriModel>(query);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş emirleri getirilirken hata oluştu");
                    throw;
                }
            }
        }

        public bool HasProductionPermission()
        {
            var userNo = GetCurrentUserNo();
            return MenuHelper.KullaniciYetkisiVarMi("Fatura", "UretIsEmri", userNo);
        }

        private string GetFullProductionQuery()
        {
            return @"
SELECT 
      i.is_Guid,
      i.is_Kod,
      i.is_Ismi,
      i.is_EmriDurumu,
      i.is_BaslangicTarihi,
      i.is_Emri_PlanBitisTarihi,
      rtp.RtP_PlanlananIsMerkezi AS IsMerkezi,
      im.IsM_Aciklama AS IsMerkeziAciklama,
      upl.upl_kodu AS UrunKodu,
      s.sto_isim AS UrunAdi,
      s.sto_yabanci_isim AS YabanciIsim,
      s.sto_kisa_ismi AS KisaIsim,
      s.sto_birim1_ad AS Birim1Ad,
      s.sto_birim2_ad AS Birim2Ad,
      s.sto_birim2_katsayi AS Birim2Katsayi,
      upl.upl_miktar AS Miktar
  FROM ISEMIRLERI i 
  LEFT JOIN URETIM_MALZEME_PLANLAMA upl 
      ON upl.upl_isemri = i.is_Kod 
      AND upl.upl_uretim_tuket = 1
  LEFT JOIN STOKLAR s 
      ON s.sto_kod = upl.upl_kodu
  LEFT JOIN URETIM_ROTA_PLANLARI rtp
      ON rtp.RtP_IsEmriKodu = i.is_Kod 
      AND rtp.RtP_UrunKodu = upl.upl_kodu 
      AND rtp.RtP_SatirNo = 0
  LEFT JOIN IS_MERKEZLERI im
      ON im.IsM_Kodu = rtp.RtP_PlanlananIsMerkezi
  WHERE i.is_EmriDurumu IN (0, 1)
      AND upl.upl_kodu IS NOT NULL
      AND i.is_Emri_PlanBitisTarihi >= CAST(GETDATE() AS DATE)
  ORDER BY upl.upl_kodu ASC, i.is_BaslangicTarihi DESC;
    ";
        }


        private string GetLimitedProductionQuery()
        {
            return @"
SELECT 
      i.is_Guid,
      i.is_Kod,
      i.is_Ismi,
      i.is_EmriDurumu,
      i.is_BaslangicTarihi,
      i.is_Emri_PlanBitisTarihi,
      rtp.RtP_PlanlananIsMerkezi AS IsMerkezi,
      im.IsM_Aciklama AS IsMerkeziAciklama,
      upl.upl_kodu AS UrunKodu,
      s.sto_isim AS UrunAdi,
      s.sto_yabanci_isim AS YabanciIsim,
      s.sto_kisa_ismi AS KisaIsim,
      s.sto_birim1_ad AS Birim1Ad,
      s.sto_birim2_ad AS Birim2Ad,
      s.sto_birim2_katsayi AS Birim2Katsayi,
      upl.upl_miktar AS Miktar
  FROM ISEMIRLERI i 
  LEFT JOIN URETIM_MALZEME_PLANLAMA upl 
      ON upl.upl_isemri = i.is_Kod 
      AND upl.upl_uretim_tuket = 1
  LEFT JOIN STOKLAR s 
      ON s.sto_kod = upl.upl_kodu
  LEFT JOIN URETIM_ROTA_PLANLARI rtp
      ON rtp.RtP_IsEmriKodu = i.is_Kod 
      AND rtp.RtP_UrunKodu = upl.upl_kodu 
      AND rtp.RtP_SatirNo = 0
  LEFT JOIN IS_MERKEZLERI im
      ON im.IsM_Kodu = rtp.RtP_PlanlananIsMerkezi
  WHERE i.is_EmriDurumu IN (0, 1)
     AND rtp.RtP_PlanlananIsMerkezi = '010'
      AND upl.upl_kodu IS NOT NULL
      AND i.is_Emri_PlanBitisTarihi >= CAST(GETDATE() AS DATE)
  ORDER BY upl.upl_kodu ASC, i.is_BaslangicTarihi DESC;

    ";
        }


        // İş emri durumunu güncelleyen metod
        public bool UpdateIsEmriDurumu(string isEmriKodu, int yeniDurum)
        {
            if (yeniDurum != 0 && yeniDurum != 1)
            {
                _logger.LogWarning("Geçersiz iş emri durumu: {YeniDurum}", yeniDurum);
                return false;
            }

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                UPDATE ISEMIRLERI 
                SET is_EmriDurumu = @YeniDurum
                WHERE is_Kod = @IsEmriKodu";

                try
                {
                    var parameters = new { IsEmriKodu = isEmriKodu, YeniDurum = yeniDurum };
                    var affectedRows = connection.Execute(query, parameters);

                    _logger.LogInformation(
                        "İş emri durumu güncellendi. Kod: {IsEmriKodu}, Yeni Durum: {YeniDurum}, Etkilenen Kayıt: {AffectedRows}",
                        isEmriKodu, yeniDurum, affectedRows);

                    return affectedRows > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "İş emri durumu güncellenirken hata oluştu. Kod: {IsEmriKodu}, Yeni Durum: {YeniDurum}",
                        isEmriKodu, yeniDurum);
                    throw;
                }
            }

        }
        public IEnumerable<MusteriRiskAnalizi> GetMusteriRiskAnalizi(DateTime? raporTarihi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_sp_MusteriRiskAnalizi";
                var parameters = new { RaporTarihi = raporTarihi ?? DateTime.Now };

                try
                {
                    var sonuclar = connection.Query<MusteriRiskAnalizi>(
                        procedureName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return sonuclar;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Müşteri risk analizi alınırken hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<SiparisDetayViewModel> GetSiparisDetay(DateTime? baslamaTarihi = null, DateTime? bitisTarihi = null)
        {
            // Tarihler belirtilmemişse, başlangıç tarihi bugünden 15 gün öncesi, bitiş tarihi bugün olacak
            baslamaTarihi ??= DateTime.Now.AddDays(-100);
            bitisTarihi ??= DateTime.Now;
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                // Önce siparişleri getirelim
                var parameters = new
                {
                    sip_tip = 0,
                    sip_cins = 0,
                    AcikmiKapalimi = "Açık",
                    MusteriKodu = (string)null,
                    DepoNo = 3,
                    BaslamaTarihi = baslamaTarihi,
                    BitisTarihi = bitisTarihi,
                    EvrakSeri = (string)null,
                    EvrakSira = (string)null,
                    BelgeNo = (string)null,
                    yk_kodu = (string)null
                };
                var query = "EXEC DBT_MO_SiparisDetayGetir2 @sip_tip, @sip_cins, @AcikmiKapalimi, @MusteriKodu, @DepoNo, @BaslamaTarihi, @BitisTarihi, @EvrakSeri, @EvrakSira, @BelgeNo, @yk_kodu";
                var siparisler = connection.Query<SiparisDetayViewModel>(query, parameters)
                    .OrderByDescending(s => s.EvrakSira) // Evrak sırasına göre büyükten küçüğe sıralama
                    .ToList();

                // Siparişler boşsa erken dönüş
                if (!siparisler.Any())
                {
                    return siparisler;
                }

                // Gelen siparişlerin evrak numaralarını topla
                var evrakNumaralari = siparisler.Select(s => s.EvrakSira).Distinct().ToList();

                // Sadece bu evrak numaraları için açıklamaları getir
                var evrakListesiQuery = $@"
            SELECT ea.egk_evr_sira, ea.egk_evracik8, ea.egk_evracik9, ea.egk_evracik10 
            FROM EVRAK_ACIKLAMALARI ea 
            WHERE ea.egk_evr_sira IN ({string.Join(",", evrakNumaralari)})";

                var evrakAciklamalari = connection.Query(evrakListesiQuery).ToList();

                // Her sipariş için, aynı evrak numarasına sahip TÜMM açıklamaları kontrol et
                foreach (var siparis in siparisler)
                {
                    // Bu siparişin evrak numarasına sahip TÜM açıklamaları bul
                    var evrakBilgileri = evrakAciklamalari
                        .Where(e => (int)e.egk_evr_sira == siparis.EvrakSira)
                        .ToList();

                    if (evrakBilgileri.Any())
                    {
                        // Öncelikle 'Basladi' durumuna sahip olanı ara
                        var basladiDurumu = evrakBilgileri.FirstOrDefault(e => e.egk_evracik10?.ToString() == "Basladi");

                        if (basladiDurumu != null)
                        {
                            siparis.IslemDurumu = basladiDurumu.egk_evracik10?.ToString();
                            siparis.RampaBilgisi = basladiDurumu.egk_evracik9?.ToString();
                        }
                        else
                        {
                            // 'Basladi' yoksa, herhangi bir değeri al
                            var herhangiAciklama = evrakBilgileri.First();
                            siparis.IslemDurumu = herhangiAciklama.egk_evracik10?.ToString();
                            siparis.RampaBilgisi = herhangiAciklama.egk_evracik9?.ToString();
                        }
                    }
                }

                return siparisler;
            }
        }
        public IEnumerable<StokHareket> GetStokHareketleri(string siparisGuid)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
    SELECT 
        sh.sth_stok_kod AS StokKodu,
        sh.sth_miktar AS Miktar,
        sh.sth_parti_kodu AS PartiKodu,
        s.sto_isim AS StokAdi,
        sh.sth_cari_kodu AS CariKodu,
        sh.sth_evrakno_seri AS EvrakSeri,
        sh.sth_evrakno_sira AS EvrakSira,
        sh.sth_sip_uid AS SiparisGuid
    FROM STOK_HAREKETLERI sh WITH(NOLOCK)
    LEFT JOIN STOKLAR s WITH(NOLOCK) ON s.sto_kod = sh.sth_stok_kod
    WHERE sh.sth_sip_uid IN (
        SELECT sip_Guid 
        FROM SIPARISLER 
        WHERE sip_evrakno_sira = (
            SELECT sip_evrakno_sira 
            FROM SIPARISLER 
            WHERE sip_Guid = @SiparisGuid
        )
    )";

                try
                {
                    connection.Open();

                    // Doğrudan StokHareket olarak sorgula ve maple
                    var result = connection.Query<StokHareket>(query, new { SiparisGuid = siparisGuid }).ToList();

                    // Sonuçları günlüğe kaydet
                    _logger.LogInformation($"Bulunan stok hareketleri sayısı: {result.Count}");
                    if (result.Any())
                    {
                        _logger.LogDebug($"İlk stok hareketi: {System.Text.Json.JsonSerializer.Serialize(result.First())}");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Stok hareketleri getirilirken hata oluştu. SiparisGuid: {siparisGuid}");
                    throw;
                }
            }
        }

        public RampUpdateResult UpdateSiparisDurum(int evrakSira, Guid siparisGuid, string rampaBilgisi, string islemDurumu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if ramp is already in use by another order
                        var checkQuery = @"
                    SELECT COUNT(1) 
                    FROM EVRAK_ACIKLAMALARI 
                    WHERE egk_evracik9 = @rampaBilgisi 
                    AND egk_evr_sira != @evrakSira
                    AND egk_evracik10 != 'Bitti'";

                        var rampInUse = connection.QuerySingle<int>(checkQuery,
                            new { rampaBilgisi, evrakSira },
                            transaction) > 0;

                        if (rampInUse)
                        {
                            transaction.Rollback();
                            return new RampUpdateResult
                            {
                                Success = false,
                                Message = $"Rampa {rampaBilgisi} başka bir sipariş tarafından kullanılıyor."
                            };
                        }

                        // Determine update query based on the process status
                        string updateQuery;
                        object queryParams;

                        if (islemDurumu == "Bitti")
                        {
                            // When status is "Bitti", keep the existing ramp information
                            updateQuery = @"
                        UPDATE EVRAK_ACIKLAMALARI 
                        SET egk_evracik8 = @siparisGuid,
                            egk_evracik10 = @islemDurumu
                        WHERE egk_evr_sira = @evrakSira";

                            queryParams = new
                            {
                                evrakSira,
                                siparisGuid,
                                islemDurumu
                            };
                        }
                        else
                        {
                            // For other statuses, update ramp information
                            updateQuery = @"
                        UPDATE EVRAK_ACIKLAMALARI 
                        SET egk_evracik8 = @siparisGuid,
                            egk_evracik9 = @rampaBilgisi,
                            egk_evracik10 = @islemDurumu
                        WHERE egk_evr_sira = @evrakSira";

                            queryParams = new
                            {
                                evrakSira,
                                siparisGuid,
                                rampaBilgisi,
                                islemDurumu
                            };
                        }

                        // Execute the update
                        var result = connection.Execute(updateQuery, queryParams, transaction);

                        if (result > 0)
                        {
                            transaction.Commit();
                            return new RampUpdateResult
                            {
                                Success = true,
                                Message = "Sipariş durumu başarıyla güncellendi."
                            };
                        }
                        else
                        {
                            transaction.Rollback();
                            return new RampUpdateResult
                            {
                                Success = false,
                                Message = "Güncelleme başarısız. Sipariş bulunamadı."
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new RampUpdateResult
                        {
                            Success = false,
                            Message = $"Hata oluştu: {ex.Message}"
                        };
                    }
                }
            }
        }
        public string UretIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("videojet2micro2025", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@isemri", isEmriKodu);
                    command.Parameters.AddWithValue("@stokkodu", urunKodu);
                    command.Parameters.AddWithValue("@depo", depoNo);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string barkod = reader.GetString(reader.GetOrdinal("barkod"));
                            string makine = reader.GetString(reader.GetOrdinal("makine"));
                            return $"Barkod: {barkod}, Makine: {makine}";
                        }
                        return "Üretim yapıldı fakat sonuç alınamadı.";
                    }
                }
            }
        }
        // Model sınıfı ekleyin

        public BilançoViewModel GetKasaToplami()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
    'Kasa' AS HesapAdi,
   SUM([msg_S_0101\T] )- SUM([msg_S_0102\T]) AS ToplamBakiye
FROM dbo.fn_MuhHesapFoy(0, 2025, N'100', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        // FaturaRepository sınıfına eklenecek metot
        public BilançoViewModel GetGelecekAylaraAitGider()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Gelecek Aylara Ait Gider' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'180', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetDigerCesitliAlacaklar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Diğer Çeşitli Alacaklar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'136', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetIsAvanslari()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'İş Avansları' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'195', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }

        public BilançoViewModel GetDevredenKdv()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Devreden KDV' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'190', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetFinansalKiralamaBorclar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Finansal Kiralama İşlem Borçlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'301', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetErtelenmisFinansalKiralama()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ertelenmiş Finansal Kiralama Maliyeti(-)' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'302', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetDigerMaliBorclar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Diğer Mali Borçlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'309', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public List<BilançoViewModel> GetDigerMaliBorclarDetay()
        {
            var detayList = new List<BilançoViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Bu sorgu firma/hesap detaylarını getirecek
                using (var command = new SqlCommand(@"
        SELECT 
            ISNULL(mh.[#msg_S_0134], 'Diğer') AS HesapAdi, -- HESAP İSMİ sütunu 
            mh.[#msg_S_0133] AS HesapKodu, -- HESAP KODU sütunu
            SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'309', '20241231', '20250101', '20251231', N'') mh
        GROUP BY mh.[#msg_S_0134], mh.[#msg_S_0133]
        HAVING SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) <> 0
        ORDER BY ToplamBakiye DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detayItem = new BilançoViewModel
                            {
                                HesapAdi = reader.GetString(0),
                                HesapKodu = reader.GetString(1),
                                ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            };
                            detayList.Add(detayItem);
                        }
                    }
                }
            }

            return detayList;
        }
        public BilançoViewModel GetAlinanDepozitoVeTeminatlar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Alınan Depozito Ve Teminatlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'326', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }

        public List<BilançoViewModel> GetVerilenDepozitoVeTeminatlarDetay()
        {
            var detayList = new List<BilançoViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Bu sorgu firma/hesap detaylarını getirecek
                using (var command = new SqlCommand(@"
        SELECT 
            ISNULL(mh.[#msg_S_0134], 'Diğer') AS HesapAdi, -- HESAP İSMİ sütunu 
            mh.[#msg_S_0133] AS HesapKodu, -- HESAP KODU sütunu
            SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'126', '20241231', '20250101', '20251231', N'') mh
        GROUP BY mh.[#msg_S_0134], mh.[#msg_S_0133]
        HAVING SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) <> 0
        ORDER BY ToplamBakiye DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detayItem = new BilançoViewModel
                            {
                                HesapAdi = reader.GetString(0),
                                HesapKodu = reader.GetString(1),
                                ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            };
                            detayList.Add(detayItem);
                        }
                    }
                }
            }

            return detayList;
        }
        public BilançoViewModel GetVerilenDepozitoVeTeminatlar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Verilen Depozito Ve Teminatlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'126', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }


        public BilançoViewModel GetPersonelBorclari()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Personel Borçları' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'335', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetOdenecekVergiVeFonlar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ödenecek Vergi Ve Fonlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'360', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetOdenecekSosyalGuvenlikKesintileri()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ödenecek Sosyal Güvenlik Kesintileri' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'361', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetOdenecekDigerYukumlulukler()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ödenecek Diğer Yükümlülükler' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'369', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetGelecekAylaraAitGelirGiderTahmini()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Gelecek Aylara Ait Gelir Ve Gider Tahmini' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'381', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetOrtaklaraBorclar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ortaklara Borçlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'431', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetVerilenSiparisAvanslari()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
        SELECT 
            'Verilen Sipariş Avansları' AS HesapAdi,
            '159' AS HesapKodu,
            SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'159', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.HesapKodu = reader.GetString(1);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public List<BilançoViewModel> GetVerilenSiparisAvanslariDetay()
        {
            var detayList = new List<BilançoViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // This query assumes that company details are stored in a field related to the account movement
                // Adjust the SQL query according to your database structure
                using (var command = new SqlCommand(@"
        SELECT 
            ISNULL(mh.[#msg_S_0134], 'Diğer') AS HesapAdi, -- Using HESAP İSMİ column
            mh.[#msg_S_0133] AS HesapKodu, -- Using HESAP KODU column
            SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'159', '20241231', '20250101', '20251231', N'') mh
        GROUP BY mh.[#msg_S_0134], mh.[#msg_S_0133]
        HAVING SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) <> 0
        ORDER BY ToplamBakiye DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detayItem = new BilançoViewModel
                            {
                                HesapAdi = reader.GetString(0),
                                HesapKodu = reader.GetString(1),
                                ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            };
                            detayList.Add(detayItem);
                        }
                    }
                }
            }

            return detayList;
        }
        public BilançoViewModel GetOrtaklardanAlacaklar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
        SELECT 
            'Ortaklardan Alacaklar' AS HesapAdi,
            '131' AS HesapKodu,
            SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'131', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.HesapKodu = reader.GetString(1);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public List<BilançoViewModel> GetOrtaklardanAlacaklarDetay()
        {
            var detayList = new List<BilançoViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // This query assumes that company details are stored in a field related to the account movement
                // Adjust the SQL query according to your database structure
                using (var command = new SqlCommand(@"
        SELECT 
            ISNULL(mh.[#msg_S_0134], 'Diğer') AS HesapAdi, -- Using HESAP İSMİ column
            mh.[#msg_S_0133] AS HesapKodu, -- Using HESAP KODU column
            SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'131', '20241231', '20250101', '20251231', N'') mh
        GROUP BY mh.[#msg_S_0134], mh.[#msg_S_0133]
        HAVING SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) <> 0
        ORDER BY ToplamBakiye DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detayItem = new BilançoViewModel
                            {
                                HesapAdi = reader.GetString(0),
                                HesapKodu = reader.GetString(1),
                                ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            };
                            detayList.Add(detayItem);
                        }
                    }
                }
            }

            return detayList;
        }
        public BilançoViewModel GetPersoneldenAlacaklar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Personelden Alacaklar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'135', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }

        public BilançoViewModel GetDigerStoklar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
        SELECT 
            'Diğer Stoklar' AS HesapAdi,
            '157' AS HesapKodu,
            SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'157', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.HesapKodu = reader.GetString(1);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public List<BilançoViewModel> GetDigerStoklarDetay()
        {
            var detayList = new List<BilançoViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // This query assumes that company details are stored in a field related to the account movement
                // Adjust the SQL query according to your database structure
                using (var command = new SqlCommand(@"
        SELECT 
            ISNULL(mh.[#msg_S_0134], 'Diğer') AS HesapAdi, -- Using HESAP İSMİ column
            mh.[#msg_S_0133] AS HesapKodu, -- Using HESAP KODU column
            SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) AS ToplamBakiye
        FROM dbo.fn_MuhHesapFoy(0, 2025, N'157', '20241231', '20250101', '20251231', N'') mh
        GROUP BY mh.[#msg_S_0134], mh.[#msg_S_0133]
        HAVING SUM(mh.[msg_S_0101\T]) - SUM(mh.[msg_S_0102\T]) <> 0
        ORDER BY ToplamBakiye DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detayItem = new BilançoViewModel
                            {
                                HesapAdi = reader.GetString(0),
                                HesapKodu = reader.GetString(1),
                                ToplamBakiye = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            };
                            detayList.Add(detayItem);
                        }
                    }
                }
            }

            return detayList;
        }
        public List<MusteriCekOzet> GetMusteriCekleriOzet()
        {
            // Bugünden başlayarak 6 ay sonrasına kadar olan çekleri alalım
            var baslangicTarihi = DateTime.Now;
            var bitisTarihi = DateTime.Now.AddMonths(6);

            var cekler = new List<MusteriCek>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslangicTarihi, BitisTarihi = bitisTarihi };
                var query = @"EXEC dbo.DBT_MusteriCekleri_TariheGore 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi";

                cekler = connection.Query<MusteriCek>(query, parameters).ToList();
            }

            // Pozisyonlarına göre gruplandır
            var ozet = cekler
                .GroupBy(c => c.Pozisyon)
                .Select(g => new MusteriCekOzet
                {
                    Pozisyon = g.Key,
                    ToplamTutar = g.Sum(c => c.Kalan),
                    Detaylar = g.OrderByDescending(c => c.Tutar).Take(5).ToList() // Her kategori için en yüksek tutarlı 5 çek
                })
                .ToList();

            return ozet;
        }
        public BilançoViewModel GetPersonelAvanslari()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Personel Avansları' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'196', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetPesinOdenenVergiVeFon()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Peşin Ödenen Vergi ve Fonlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'193', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetSayimVeTesellumNoksanlari()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Sayım Ve Tesellüm Noksanları' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'197', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetDigerBorclar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Diğer Borçlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'33', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetDigerCesitliBorclar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Diğer Çeşitli Borçlar' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'336', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }
        public BilançoViewModel GetDigerYukumlulukler()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ödenecek Diğer Yükümlülükler' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'369', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }

        public BilançoViewModel GetSupheliTicariAlacaklar()
        {
            var bilançoItem = new BilançoViewModel();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT 
                'Ödenecek Diğer Yükümlülükler' AS HesapAdi,
                SUM([msg_S_0101\T]) - SUM([msg_S_0102\T]) AS ToplamBakiye
            FROM dbo.fn_MuhHesapFoy(0, 2025, N'128', '20241231', '20250101', '20251231', N'')", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bilançoItem.HesapAdi = reader.GetString(0);
                            bilançoItem.ToplamBakiye = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        }
                    }
                }
            }

            return bilançoItem;
        }

        public List<BankaHesapViewModel> GetBankaHesaplari()
        {
            var bankaHesaplar = new List<BankaHesapViewModel>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
            SELECT TOP 100 PERCENT
ban_Guid AS [#msg_S_0088] /* KAYIT NO */ ,
ban_kod AS [msg_S_0078] /* KODU */ ,
ban_ismi AS [msg_S_0070] /* İSMİ */ ,
ban_firma_no  AS [msg_S_1263] /* FİRMA NO */ ,
ban_sube AS [msg_S_0822] /* ŞUBE */ ,
ban_hesapno AS [msg_S_0771] /* HESAP NO */ ,
dbo.fn_DovizSembolu(ban_doviz_cinsi) AS [msg_S_0849] /* DÖVİZ CİNSİ */ ,
CASE
WHEN Cari_F10da_detay = 1 Then dbo.fn_CariHesapAnaDovizBakiye(ban_firma_no,2,ban_kod,'','',1,NULL,NULL,0,0,0,0,0)
WHEN Cari_F10da_detay = 2 Then dbo.fn_CariHesapAlternatifDovizBakiye(ban_firma_no,2,ban_kod,'','',1,NULL,NULL,0,0,0,0,0)
WHEN Cari_F10da_detay = 3 Then dbo.fn_CariHesapOrjinalDovizBakiye(ban_firma_no,2,ban_kod,'','',1,NULL,NULL,0,0,0,0,0)
WHEN Cari_F10da_detay = 4 Then dbo.fn_CariHareketSayisi(2,ban_kod,'')
END AS [msg_S_1530] /* BAKİYE / HAREKET SAYISI */ ,
ban_TCMB_Kodu AS [#msg_S_0843] /* TCMB BANKALAR KODU */
FROM dbo.BANKALAR WITH (NOLOCK)
LEFT OUTER JOIN dbo.vw_Gendata ON 1=1
WHERE ban_kod LIKE '102%'
ORDER BY ban_kod", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var bankaHesap = new BankaHesapViewModel
                            {
                                BankaKodu = reader["msg_S_0078"].ToString(),
                                BankaAdi = reader["msg_S_0070"].ToString(),
                                Sube = reader["msg_S_0822"].ToString(),
                                HesapNo = reader["msg_S_0771"].ToString(),
                                DovizCinsi = reader["msg_S_0849"].ToString(),
                                Bakiye = reader.IsDBNull(reader.GetOrdinal("msg_S_1530")) ? 0 : Convert.ToDouble(reader["msg_S_1530"])
                            };
                            bankaHesaplar.Add(bankaHesap);
                        }
                    }
                }
            }

            return bankaHesaplar;
        }

        public IEnumerable<SiparisYuklemeRampaViewModel> GetSiparisYuklemeRampaDetaylari(string rampaBilgisi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var query = @"
WITH SiparisBilgileri AS (
    SELECT 
        s.sip_Guid,
        s.sip_evrakno_sira,
        s.sip_stok_kod,
        s.sip_miktar,
        st.sto_isim,
        ch.cari_unvan1,
        e.egk_evracik10
    FROM SIPARISLER s
    JOIN EVRAK_ACIKLAMALARI e ON s.sip_evrakno_sira = e.egk_evr_sira
    JOIN STOKLAR st ON st.sto_kod = s.sip_stok_kod
    JOIN CARI_HESAPLAR ch ON ch.cari_kod = s.sip_musteri_kod
    WHERE e.egk_evracik9 = @RampaBilgisi 
      AND e.egk_evracik10 = 'Basladi'
),
StokHareketleri AS (
    SELECT 
        sth_stok_kod,
        sth_miktar,
        sth_evrakno_sira
    FROM STOK_HAREKETLERI 
    WHERE sth_sip_uid IN (
        SELECT sip_Guid FROM SIPARISLER s
        JOIN EVRAK_ACIKLAMALARI e ON s.sip_evrakno_sira = e.egk_evr_sira
        WHERE e.egk_evracik9 = @RampaBilgisi AND e.egk_evracik10 = 'Basladi'
    )
)
SELECT DISTINCT
    sb.sip_evrakno_sira AS EvrakSira,
    sb.sip_stok_kod AS UrunKodu,
    sb.sto_isim AS UrunAdi,
    sb.sip_miktar AS ToplamSiparisMiktari,
    COALESCE((
        SELECT SUM(sh.sth_miktar)
        FROM StokHareketleri sh
        WHERE sh.sth_stok_kod = sb.sip_stok_kod
    ), 0) AS YuklenenMiktar,
    sb.sip_miktar - COALESCE((
        SELECT SUM(sh.sth_miktar)
        FROM StokHareketleri sh
        WHERE sh.sth_stok_kod = sb.sip_stok_kod
    ), 0) AS KalanMiktar,
    sb.cari_unvan1 AS CariUnvan,
    sb.egk_evracik10 AS SiparisDurumu,
    CASE WHEN EXISTS (
        SELECT 1 
        FROM StokHareketleri sh 
        WHERE sh.sth_stok_kod = sb.sip_stok_kod
    ) THEN sb.sip_stok_kod ELSE NULL END AS YuklenenStokKodu,
    COALESCE((
        SELECT MAX(sh.sth_evrakno_sira) 
        FROM StokHareketleri sh 
        WHERE sh.sth_stok_kod = sb.sip_stok_kod AND sh.sth_evrakno_sira > 0
    ), 
    (
        -- Sipariş için herhangi bir irsaliye varsa, onu al
        SELECT MAX(sh.sth_evrakno_sira) 
        FROM STOK_HAREKETLERI sh
        WHERE sh.sth_sip_uid IN (SELECT sip_Guid FROM SiparisBilgileri)
        AND sh.sth_evrakno_sira > 0
    ), 0) AS IrsaliyeNo
FROM SiparisBilgileri sb
ORDER BY sb.cari_unvan1, sb.sip_evrakno_sira, sb.sip_stok_kod;";

                try
                {
                    var parameters = new { RampaBilgisi = rampaBilgisi };
                    var results = connection.Query<SiparisYuklemeRampaViewModel>(query, parameters).ToList();

                    // Loglama
                    _logger.LogInformation($"Bulunan kayıt sayısı: {results.Count}");

                    // İrsaliye numarası olmayan kayıtlar için bütün siparişin irsaliye numarasını kontrol edelim
                    if (results.Any())
                    {
                        // En yüksek değerli irsaliye numarasını bulalım (0 olmayan)
                        int maxIrsaliyeNo = 0;

                        foreach (var item in results)
                        {
                            int irsaliyeInt;
                            // IrsaliyeNo'nun tipine göre dönüşüm
                            if (item.IrsaliyeNo != null)
                            {
                                if (int.TryParse(item.IrsaliyeNo.ToString(), out irsaliyeInt) && irsaliyeInt > 0)
                                {
                                    maxIrsaliyeNo = Math.Max(maxIrsaliyeNo, irsaliyeInt);
                                }
                            }
                        }

                        // Eğer geçerli bir irsaliye numarası bulunursa ve bazı kayıtlarda 0 veya null varsa onları düzeltelim
                        if (maxIrsaliyeNo > 0)
                        {
                            foreach (var result in results)
                            {
                                int currentIrsaliye = 0;
                                if (result.IrsaliyeNo != null && int.TryParse(result.IrsaliyeNo.ToString(), out currentIrsaliye) && currentIrsaliye <= 0)
                                {
                                    result.IrsaliyeNo = maxIrsaliyeNo;
                                    _logger.LogInformation($"İrsaliye numarası güncellendi: Ürün={result.UrunAdi}, " +
                                        $"Yeni İrsaliye No={maxIrsaliyeNo}");
                                }
                            }
                        }
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{rampaBilgisi} için sipariş yükleme detayları alınırken hata oluştu");
                    throw;
                }
            }
        }
        //public List<ArtiBakiyeliMusteriViewModel> GetArtiBakiyeliMusteriFaturaları()
        //{
        //    var connectionString = _dbSelectorService.GetConnectionString();
        //    var result = new List<ArtiBakiyeliMusteriViewModel>();
        //    var musteriBakiyeDict = new Dictionary<string, ArtiBakiyeliMusteriViewModel>();

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();

        //        // Stored procedure'u çağır - bugünden 1 yıl sonrasına kadar olan faturaları getir
        //        var baslamaTarihi = DateTime.Now;
        //        var bitisTarihi = DateTime.Now.AddYears(1);

        //        var parameters = new { BaslangicTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
        //        var query = "EXEC dbo.DBT_Arti_Bakiye_Fatura_Musteri_Vadesiz @BaslangicTarihi, @BitisTarihi";

        //        var faturalar = connection.Query<CapanmamisFaturaDetayViewModel>(query, parameters).ToList();

        //        // Faturaları müşterilere göre grupla
        //        foreach (var fatura in faturalar)
        //        {
        //            if (!musteriBakiyeDict.ContainsKey(fatura.MusteriKodu))
        //            {
        //                musteriBakiyeDict[fatura.MusteriKodu] = new ArtiBakiyeliMusteriViewModel
        //                {
        //                    CariKodu = fatura.MusteriKodu,
        //                    CariUnvani = fatura.Unvan,
        //                    ToplamBorc = 0,
        //                    KapanmamisFaturalar = new List<KapanmamisFaturaViewModel>()
        //                };
        //            }

        //            // Toplamı güncelle
        //            musteriBakiyeDict[fatura.MusteriKodu].ToplamBorc += fatura.Bakiye;

        //            // Faturayı ekle
        //            musteriBakiyeDict[fatura.MusteriKodu].KapanmamisFaturalar.Add(new KapanmamisFaturaViewModel
        //            {
        //                FaturaNo = fatura.FaturaNo,
        //                VadeTarihi = fatura.Vade,
        //                KalanTutar = fatura.Bakiye
        //            });
        //        }

        //        // Dictionary'den listeye çevir
        //        result = musteriBakiyeDict.Values.ToList();
        //    }

        //    return result;
        //}
        public List<ArtiBakiyeliTedarikciViewModel> GetArtiBakiyeliTedarikciFaturalari(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var result = new List<ArtiBakiyeliTedarikciViewModel>();
            var tedarikciBakiyeDict = new Dictionary<string, ArtiBakiyeliTedarikciViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Stored procedure'u çağır
                var parameters = new { BaslangicTarihi = baslangicTarihi, BitisTarihi = bitisTarihi };
                var query = "EXEC dbo.DBT_Arti_Bakiye_Fatura_Tedarikci_Vadesiz @BaslangicTarihi, @BitisTarihi";

                var faturalar = connection.Query<TedarikciFaturaDetayViewModel>(query, parameters).ToList();

                // Faturaları tedarikçilere göre grupla
                foreach (var fatura in faturalar)
                {
                    if (!tedarikciBakiyeDict.ContainsKey(fatura.TedarikciKodu))
                    {
                        tedarikciBakiyeDict[fatura.TedarikciKodu] = new ArtiBakiyeliTedarikciViewModel
                        {
                            CariKodu = fatura.TedarikciKodu,
                            CariUnvani = fatura.Unvan,
                            ToplamBorc = 0,
                            KapanmamisFaturalar = new List<KapanmamisFaturaViewModel>()
                        };
                    }

                    // Toplamı güncelle
                    tedarikciBakiyeDict[fatura.TedarikciKodu].ToplamBorc += fatura.Bakiye;

                    // Faturayı ekle
                    tedarikciBakiyeDict[fatura.TedarikciKodu].KapanmamisFaturalar.Add(new KapanmamisFaturaViewModel
                    {
                        FaturaNo = fatura.FaturaNo,
                        VadeTarihi = fatura.Vade,
                        KalanTutar = fatura.Bakiye
                    });
                }

                // Dictionary'den listeye çevir
                result = tedarikciBakiyeDict.Values.ToList();
            }

            return result;
        }

        // Stored procedure'dan gelen veri için model

        public class CapanmamisFaturaDetayViewModel
        {
            public string MusteriKodu { get; set; }
            public string Unvan { get; set; }
            public string FaturaNo { get; set; }
            public DateTime Vade { get; set; }
            public decimal Bakiye { get; set; }
        }
        //public List<EksiBakiyeliMusteriViewModel> GetEksiBakiyeliFaturalar()
        //{
        //    var connectionString = _dbSelectorService.GetConnectionString();
        //    var musteriBakiyeDict = new Dictionary<string, EksiBakiyeliMusteriViewModel>();

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();

        //        // Prosedürdeki doğru görünümü ve kolon adlarını kullanarak sorgu
        //        var query = @"
        //    WITH CariBakiyeler AS (
        //        SELECT 
        //            cha.cha_kod,
        //            SUM(
        //                cha.CHA_CARI_MEBLAG_ANA * 
        //                CASE 
        //                    WHEN cha.CHA_CARI_BORC_ALACAK_TIP = 0 THEN 1.0 
        //                    ELSE -1.0 
        //                END
        //            ) AS Bakiye
        //        FROM dbo.CARI_HESAP_HAREKETLERI_VIEW_WITH_INDEX_02 cha
        //        WHERE 
        //            cha.cha_kod LIKE '120%'
        //            AND cha.cha_evrak_tip IN (29, 63)
        //        GROUP BY cha.cha_kod
        //        HAVING SUM(
        //            cha.CHA_CARI_MEBLAG_ANA * 
        //            CASE 
        //                WHEN cha.CHA_CARI_BORC_ALACAK_TIP = 0 THEN 1.0 
        //                ELSE -1.0 
        //            END
        //        ) < 0
        //    )
        //    SELECT
        //        cb.cha_kod AS MusteriKodu,
        //        ch.cari_unvan1 AS Unvan,
        //        cb.Bakiye
        //    FROM CariBakiyeler cb
        //    JOIN CARI_HESAPLAR ch ON cb.cha_kod = ch.cari_kod";

        //        try
        //        {
        //            var musteriler = connection.Query<dynamic>(query).ToList();

        //            foreach (var musteri in musteriler)
        //            {
        //                // Fatura detayları için ayrı sorgu
        //                var faturaQuery = @"
        //            SELECT
        //                cha.cha_evrakno_seri + '-' + CAST(cha.cha_evrakno_sira AS VARCHAR) AS FaturaNo,
        //                cha.CHA_CARI_MEBLAG_ANA * 
        //                CASE 
        //                    WHEN cha.CHA_CARI_BORC_ALACAK_TIP = 0 THEN 1.0 
        //                    ELSE -1.0 
        //                END AS Bakiye
        //            FROM dbo.CARI_HESAP_HAREKETLERI_VIEW_WITH_INDEX_02 cha
        //            WHERE 
        //                cha.cha_kod = @CariKodu
        //                AND cha.cha_evrak_tip IN (29, 63)
        //            ORDER BY cha.cha_tarihi DESC";

        //                var faturalar = connection.Query<dynamic>(faturaQuery, new { CariKodu = musteri.MusteriKodu }).ToList();

        //                var musteriModel = new EksiBakiyeliMusteriViewModel
        //                {
        //                    CariKodu = musteri.MusteriKodu,
        //                    CariUnvani = musteri.Unvan,
        //                    ToplamAlacak = Math.Abs((decimal)musteri.Bakiye),
        //                    EksiBakiyeliFaturalar = new List<EksiBakiyeliFaturaViewModel>()
        //                };

        //                foreach (var fatura in faturalar)
        //                {
        //                    musteriModel.EksiBakiyeliFaturalar.Add(new EksiBakiyeliFaturaViewModel
        //                    {
        //                        FaturaNo = fatura.FaturaNo,
        //                        AlacakTutar = Math.Abs((decimal)fatura.Bakiye)
        //                    });
        //                }

        //                musteriBakiyeDict[musteri.MusteriKodu] = musteriModel;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Eksi bakiyeli müşteriler alınırken hata oluştu");

        //            // Alternatif, daha basit bir sorgu deneyelim
        //            var basitQuery = @"
        //        SELECT 
        //            ch.cari_kod AS MusteriKodu,
        //            ch.cari_unvan1 AS Unvan,
        //            SUM(chhv.doviz_meblag) AS Bakiye
        //        FROM CARI_HESAPLAR ch
        //        JOIN CARI_HESAP_HAREKETLERI_VADE chhv ON ch.cari_kod = chhv.cha_kod
        //        WHERE 
        //            ch.cari_kod LIKE '120%'
        //        GROUP BY ch.cari_kod, ch.cari_unvan1
        //        HAVING SUM(chhv.doviz_meblag) < 0";

        //            var basitMusteriler = connection.Query<dynamic>(basitQuery).ToList();

        //            foreach (var musteri in basitMusteriler)
        //            {
        //                musteriBakiyeDict[musteri.MusteriKodu] = new EksiBakiyeliMusteriViewModel
        //                {
        //                    CariKodu = musteri.MusteriKodu,
        //                    CariUnvani = musteri.Unvan,
        //                    ToplamAlacak = Math.Abs((decimal)musteri.Bakiye),
        //                    EksiBakiyeliFaturalar = new List<EksiBakiyeliFaturaViewModel>()
        //                };
        //            }
        //        }
        //    }

        //    return musteriBakiyeDict.Values.ToList();
        //}
        public List<EksiBakiyeliTedarikciViewModel> GetEksiBakiyeliTedarikciFaturalari()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var tedarikciler = new List<EksiBakiyeliTedarikciViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var query = @"
            SELECT 
                cari_kod AS CariKodu,
                cari_unvan1 AS CariUnvani,
                dbo.fn_CariHesapAnaDovizBakiye(
                    '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                    0, 0, 0, 0
                ) AS AnaDovizBakiyesi
            FROM dbo.CARI_HESAPLAR WITH (NOLOCK)
            WHERE cari_kod LIKE '320%' AND
                  dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) < 0
            ORDER BY cari_kod";

                try
                {
                    tedarikciler = connection.Query<EksiBakiyeliTedarikciViewModel>(query).ToList();

                    foreach (var tedarikci in tedarikciler)
                    {
                        tedarikci.EksiBakiyeliFaturalar = new List<EksiBakiyeliFaturaViewModel>();
                    }

                    _logger.LogInformation($"Eksi bakiyeli tedarikçi sayısı: {tedarikciler.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eksi bakiyeli tedarikçiler alınırken hata oluştu: {message}", ex.Message);
                }
            }

            return tedarikciler;
        }

        public List<ArtiBakiyeliTedarikciViewModel> GetArtiBakiyeliTedarikciFaturalari()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var result = new List<ArtiBakiyeliTedarikciViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Eksi bakiyelilerdeki sorguya benzer yaklaşım kullanılıyor
                var query = @"
            SELECT 
                cari_kod AS CariKodu,
                cari_unvan1 AS CariUnvani,
                dbo.fn_CariHesapAnaDovizBakiye(
                    '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                    0, 0, 0, 0
                ) AS AnaDovizBakiyesi
            FROM dbo.CARI_HESAPLAR WITH (NOLOCK)
            WHERE cari_kod LIKE '320%' AND
                  dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) > 0
            ORDER BY dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) DESC";

                try
                {
                    var tedarikciler = connection.Query<dynamic>(query).ToList();

                    foreach (var tedarikci in tedarikciler)
                    {
                        result.Add(new ArtiBakiyeliTedarikciViewModel
                        {
                            CariKodu = tedarikci.CariKodu,
                            CariUnvani = tedarikci.CariUnvani,
                            ToplamBorc = (decimal)tedarikci.AnaDovizBakiyesi
                            // KapanmamisFaturalar listesi zaten boş oluşturulduğu için detay gerekmez
                        });
                    }

                    _logger.LogInformation($"Artı bakiyeli tedarikçi sayısı: {result.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Artı bakiyeli tedarikçiler alınırken hata oluştu: {message}", ex.Message);
                }
            }

            return result;
        }

        public List<ArtiBakiyeliMusteriViewModel> GetArtiBakiyeliMusteriFaturaları()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var result = new List<ArtiBakiyeliMusteriViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Eksi bakiyelilerdeki sorguya benzer yaklaşım
                var query = @"
            SELECT 
                cari_kod AS CariKodu,
                cari_unvan1 AS CariUnvani,
                dbo.fn_CariHesapAnaDovizBakiye(
                    '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                    0, 0, 0, 0
                ) AS AnaDovizBakiyesi
            FROM dbo.CARI_HESAPLAR WITH (NOLOCK)
            WHERE cari_kod LIKE '120%' AND
                  dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) > 0
            ORDER BY dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) DESC";

                try
                {
                    var musteriler = connection.Query<dynamic>(query).ToList();

                    foreach (var musteri in musteriler)
                    {
                        result.Add(new ArtiBakiyeliMusteriViewModel
                        {
                            CariKodu = musteri.CariKodu,
                            CariUnvani = musteri.CariUnvani,
                            ToplamBorc = (decimal)musteri.AnaDovizBakiyesi
                            // KapanmamisFaturalar listesi zaten boş oluşturulduğu için detay gerekmez
                        });
                    }

                    _logger.LogInformation($"Artı bakiyeli müşteri sayısı: {result.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Artı bakiyeli müşteriler alınırken hata oluştu: {message}", ex.Message);
                }
            }

            return result;
        }

        public List<EksiBakiyeliMusteriViewModel> GetEksiBakiyeliFaturalar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var result = new List<EksiBakiyeliMusteriViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Daha basit ve doğrudan bir sorgu kullanılıyor
                var query = @"
            SELECT 
                cari_kod AS CariKodu,
                cari_unvan1 AS CariUnvani,
                ABS(dbo.fn_CariHesapAnaDovizBakiye(
                    '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                    0, 0, 0, 0
                )) AS ToplamAlacak
            FROM dbo.CARI_HESAPLAR WITH (NOLOCK)
            WHERE cari_kod LIKE '120%' AND
                  dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  ) < 0
            ORDER BY ABS(dbo.fn_CariHesapAnaDovizBakiye(
                      '', 0, cari_kod, '', '', NULL, NULL, NULL, 0,
                      0, 0, 0, 0
                  )) DESC";

                try
                {
                    var musteriler = connection.Query<EksiBakiyeliMusteriViewModel>(query).ToList();

                    // Fatura detaylarını almadan doğrudan sonuçları döndür
                    foreach (var musteri in musteriler)
                    {
                        // EksiBakiyeliFaturalar listesi zaten constructor'da oluşturuluyor
                        result.Add(musteri);
                    }

                    _logger.LogInformation($"Eksi bakiyeli müşteri sayısı: {result.Count}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eksi bakiyeli müşteriler alınırken hata oluştu");

                    // Hata durumunda boş liste döndür
                    return new List<EksiBakiyeliMusteriViewModel>();
                }
            }
        }
        public List<StokDepoViewModel> GetStokDepoDagilimi()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Stok kodu filtresi olmadan, doğrudan tüm stokları getiren sorgu
                var query = @"
            SELECT 
                s.sto_kod AS StokKodu,
                s.sto_isim AS StokAdi,
                sh.sth_giris_depo_no AS DepoNo,
                d.dep_adi AS DepoAdi,
                SUM(CASE WHEN sh.sth_tip = 0 THEN sh.sth_miktar ELSE -sh.sth_miktar END) AS DepoMiktari,
                s.sto_birim1_ad AS BirimAdi
            FROM 
                STOKLAR s
            JOIN 
                STOK_HAREKETLERI sh ON s.sto_kod = sh.sth_stok_kod
            LEFT JOIN 
                DEPOLAR d ON sh.sth_giris_depo_no = d.dep_no
            GROUP BY 
                s.sto_kod, s.sto_isim, sh.sth_giris_depo_no, d.dep_adi, s.sto_birim1_ad
            HAVING 
                SUM(CASE WHEN sh.sth_tip = 0 THEN sh.sth_miktar ELSE -sh.sth_miktar END) > 0
            ORDER BY 
                s.sto_kod, sh.sth_giris_depo_no";

                var stokDepoData = connection.Query<StokDepoViewModel>(query).ToList();

                return stokDepoData;
            }
        }

        // FaturaRepository.cs içine aşağıdaki metodu ekleyin
        public StokDetayModel GetStokDetay(string stokKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT 
                sto_kod AS StokKodu,
                sto_isim AS Isim,
                sto_yabanci_isim AS YabanciIsim,
                sto_kisa_ismi AS KisaIsim,
                sto_birim1_ad AS Birim1Ad,
                sto_birim2_ad AS Birim2Ad,
                sto_birim2_katsayi AS Birim2Katsayi
            FROM STOKLAR
            WHERE sto_kod = @StokKodu";

                var stokDetay = connection.QueryFirstOrDefault<StokDetayModel>(query, new { StokKodu = stokKodu });

                if (stokDetay != null)
                {
                    // Karakter sınırlamalarını burada uygulayabiliriz
                    stokDetay.YabanciIsim = LimitString(stokDetay.YabanciIsim, 12);
                    stokDetay.KisaIsim = LimitString(stokDetay.KisaIsim, 6);
                }

                return stokDetay;
            }
        }

        // String'i belirli bir uzunluğa sınırlayan yardımcı metot
        private string LimitString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
        public List<StokMaliyetViewModel> GetStokMaliyetBilgileri()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Stok miktarı yerine sadece maliyet bilgilerini getiren sorgu
                var query = @"
        SELECT 
            s.sto_kod AS StokKodu,
            s.sto_isim AS StokAdi,
            sh.sth_giris_depo_no AS DepoNo,
            d.dep_adi AS DepoAdi,
            SUM(CASE WHEN sh.sth_tip = 0 THEN sh.sth_miktar ELSE -sh.sth_miktar END) AS DepoMiktari,
            SUM(CASE WHEN sh.sth_tip = 0 THEN sh.sth_tutar ELSE -sh.sth_tutar END) AS DepoMaliyeti,
            s.sto_birim1_ad AS BirimAdi
        FROM 
            STOKLAR s
        JOIN 
            STOK_HAREKETLERI sh ON s.sto_kod = sh.sth_stok_kod
        LEFT JOIN 
            DEPOLAR d ON sh.sth_giris_depo_no = d.dep_no
        GROUP BY 
            s.sto_kod, s.sto_isim, sh.sth_giris_depo_no, d.dep_adi, s.sto_birim1_ad
        HAVING 
            SUM(CASE WHEN sh.sth_tip = 0 THEN sh.sth_miktar ELSE -sh.sth_miktar END) > 0
        ORDER BY 
            s.sto_kod, sh.sth_giris_depo_no";

                var stokMaliyetData = connection.Query<StokMaliyetViewModel>(query).ToList();
                return stokMaliyetData;
            }
        }
        public IEnumerable<SorumluKod> GetSorumluKodlari(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            { 
                string query = @"
  SELECT DISTINCT
      som_kod AS SorumluKodu,
      som_isim AS SorumluAdi
  FROM SORUMLULUK_MERKEZLERI
      ";

                var parameters = new { BaslangicTarihi = baslangicTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<SorumluKod>(query, parameters);
            }
        }

        public IEnumerable<MusteriAcikFaturaViewModel> GetMusteriAcikFaturalar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var bugunTarihi = DateTime.Now.Date;
            var sonuclar = new List<MusteriAcikFaturaViewModel>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Müşterileri getirme sorgusu
                    var musteriSorgusu = @"
                    SELECT 
                        ch.cari_kod AS MusteriKodu,
                        ch.cari_unvan1 AS Unvan,
                        SUM(CASE WHEN cha.CHA_VADE_TARIHI < @BugunTarihi THEN cha.CHA_CARI_MEBLAG_ANA ELSE 0 END) AS VadesiGecmisBakiye,
                        SUM(CASE WHEN cha.CHA_VADE_TARIHI = @BugunTarihi THEN cha.CHA_CARI_MEBLAG_ANA ELSE 0 END) AS BugunOdenmesiGereken,
                        SUM(CASE WHEN cha.CHA_VADE_TARIHI > @BugunTarihi THEN cha.CHA_CARI_MEBLAG_ANA ELSE 0 END) AS GelecekVadeliFaturalar,
                        SUM(cha.CHA_CARI_MEBLAG_ANA) AS ToplamBorc
                    FROM CARI_HESAPLAR ch
                    JOIN CARI_HESAP_HAREKETLERI cha ON ch.cari_kod = cha.cha_kod
                    WHERE 
                        ch.cari_kod LIKE '120%' 
                        AND cha.CHA_CARI_BORC_ALACAK_TIP = 0  -- Borç hareketleri
                        AND cha.CHA_CARI_MEBLAG_ANA > 0
                        AND cha.cha_evrak_tip IN (29, 63)     -- Satış ve Hizmet Faturaları
                    GROUP BY ch.cari_kod, ch.cari_unvan1
                    HAVING SUM(cha.CHA_CARI_MEBLAG_ANA) > 0  -- Sadece borç bakiyesi olan cariler
                    ORDER BY ch.cari_unvan1";

                    var musteriler = connection.Query(musteriSorgusu, new { BugunTarihi = bugunTarihi }).ToList();

                    foreach (var musteri in musteriler)
                    {
                        // Müşteri faturalarını getirme sorgusu
                        var faturaSorgusu = @"
                        SELECT 
                            cha.cha_evrakno_seri + '-' + CAST(cha.cha_evrakno_sira AS VARCHAR) AS FaturaNo,
                            cha.cha_tarihi AS FaturaTarihi,
                            cha.CHA_VADE_TARIHI AS VadeTarihi,
                            cha.CHA_CARI_MEBLAG_ANA AS Tutar
                        FROM CARI_HESAP_HAREKETLERI cha
                        WHERE 
                            cha.cha_kod = @MusteriKodu
                            AND cha.CHA_CARI_BORC_ALACAK_TIP = 0  -- Borç hareketleri
                            AND cha.cha_evrak_tip IN (29, 63)     -- Satış ve Hizmet Faturaları
                            AND cha.CHA_CARI_MEBLAG_ANA > 0        -- Sadece pozitif tutarlar
                        ORDER BY cha.CHA_VADE_TARIHI";

                        var faturalar = connection.Query<FaturaViewModel>(faturaSorgusu,
                            new { MusteriKodu = musteri.MusteriKodu }).ToList();

                        // Müşteri ve faturalarını modele ekle
                        var musteriModel = new MusteriAcikFaturaViewModel
                        {
                            MusteriKodu = musteri.MusteriKodu,
                            Unvan = musteri.Unvan,
                            VadesiGecmisBakiye = musteri.VadesiGecmisBakiye,
                            BugunOdenmesiGereken = musteri.BugunOdenmesiGereken,
                            GelecekVadeliFaturalar = musteri.GelecekVadeliFaturalar,
                            ToplamBorc = musteri.ToplamBorc,
                            Faturalar = faturalar
                        };

                        sonuclar.Add(musteriModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri açık faturaları alınırken hata oluştu");
                throw;
            }

            return sonuclar;
        }
        // FaturaRepository.cs içindeki GetHataliUretimler metodunu güncelleyin
        // FaturaRepository.cs içindeki GetHataliUretimler metodunu güncelleyin
        // FaturaRepository.cs içindeki GetHataliUretimler metodunu güncelleyin
        // FaturaRepository.cs içindeki GetHataliUretimler metodunu güncelleyin
        public List<HataliUretimViewModel> GetHataliUretimler(DateTime baslangicTarihi, DateTime bitisTarihi, string stokArama = "")
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var query = @"
SELECT 
    sh.sth_Guid AS StokHareketGuid,
    sh.sth_stok_kod AS StokKodu,
    s.sto_isim AS StokAdi,
    pl.pl_partikodu AS PartiKodu,
    pl.pl_lotno AS LotNo,
    sh.sth_miktar AS Miktar,
    s.sto_birim1_ad AS BirimAdi,
    pl.pl_uretim_tar AS UretimTarihi,
    sh.sth_create_date AS IslemTarihi
FROM STOK_HAREKETLERI sh
JOIN STOKLAR s ON sh.sth_stok_kod = s.sto_kod
LEFT JOIN PARTILOT pl 
    ON sh.sth_parti_kodu = pl.pl_partikodu 
    AND sh.sth_lot_no = pl.pl_lotno 
    AND sh.sth_stok_kod = pl.pl_stokkodu
WHERE 
    sh.sth_tip = 0 
    AND sh.sth_evraktip = 12
    AND sh.sth_cins = 7
    AND sh.sth_create_date >= @BaslangicTarihi 
    AND sh.sth_create_date <= @BitisTarihi
    AND (@StokArama IS NULL OR @StokArama = '' OR 
         sh.sth_stok_kod LIKE '%' + @StokArama + '%' OR 
         s.sto_isim LIKE '%' + @StokArama + '%')
    AND NOT EXISTS (
        SELECT 1 
        FROM STOK_HAREKETLERI sh2 
        WHERE sh2.sth_tip = 1
          AND sh2.sth_parti_kodu = sh.sth_parti_kodu
          AND sh2.sth_lot_no = sh.sth_lot_no
          AND sh2.sth_stok_kod = sh.sth_stok_kod
    )
ORDER BY sh.sth_create_date DESC";

                var parameters = new
                {
                    BaslangicTarihi = baslangicTarihi,
                    BitisTarihi = bitisTarihi,
                    StokArama = string.IsNullOrWhiteSpace(stokArama) ? null : stokArama.Trim()
                };

                try
                {
                    var results = connection.Query<HataliUretimViewModel>(query, parameters).ToList();

                    string aramaInfo = string.IsNullOrWhiteSpace(stokArama) ? "" : $", Arama: '{stokArama}'";
                    _logger.LogInformation($"Hatalı üretim sayısı: {results.Count} " +
                        $"(sth_create_date aralığı: {baslangicTarihi:dd.MM.yyyy HH:mm} - {bitisTarihi:dd.MM.yyyy HH:mm}{aramaInfo})");

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hatalı üretimler listelenirken hata oluştu");
                    throw;
                }
            }
        }
        public int HataliUretimleriSil(List<string> seciliUretimler)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var erpConnectionString = ConnectionHelper.GetConnectionString("ERPDatabase");
            int silinenKayitSayisi = 0;

            // ========= KULLANICI BİLGİSİ ALMA BÖLÜMÜ =========
            // Birkaç farklı yöntemle kullanıcı ID'sini almaya çalışıyoruz
            int? silenKullaniciId = null;

            try
            {
                // 1. Session kontrolü ve session'dan alma
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session != null)
                {
                    // string tipinden alıp parse etmeyi deneyelim
                    var userNoStr = _httpContextAccessor.HttpContext.Session.GetString("UserNo");
                    if (!string.IsNullOrEmpty(userNoStr) && int.TryParse(userNoStr, out int userNo))
                    {
                        silenKullaniciId = userNo;
                        _logger.LogInformation($"Session string'den kullanıcı no: {silenKullaniciId}");
                    }
                    else
                    {
                        // Int32 olarak almayı deneyelim
                        silenKullaniciId = _httpContextAccessor.HttpContext.Session.GetInt32("UserNo");
                        _logger.LogInformation($"Session Int32'den kullanıcı no: {silenKullaniciId}");
                    }

                    // Hala bulunamadıysa Session elemanlarını logla
                    if (silenKullaniciId == null)
                    {
                        _logger.LogWarning("Session'da UserNo bulunamadı. Session içerikleri:");
                        foreach (var key in _httpContextAccessor.HttpContext.Session.Keys)
                        {
                            _logger.LogWarning($"- {key}: {_httpContextAccessor.HttpContext.Session.GetString(key)}");
                        }
                    }
                }

                // 2. Claim'den almayı dene (Session yoksa veya Session'da yoksa)
                if (silenKullaniciId == null && _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var userNoClaim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserNo");
                    if (userNoClaim != null && int.TryParse(userNoClaim.Value, out int userNo))
                    {
                        silenKullaniciId = userNo;
                        _logger.LogInformation($"Claim'den kullanıcı no: {silenKullaniciId}");
                    }
                }

                // 3. Username'i al, veritabanından UserNo'yu bul
                if (silenKullaniciId == null && _httpContextAccessor.HttpContext?.User?.Identity?.Name != null)
                {
                    var username = _httpContextAccessor.HttpContext.User.Identity.Name;
                    _logger.LogInformation($"Kullanıcı adı: {username}");

                    // Veritabanından user_no'yu bul
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var query = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";
                        var foundUserNo = conn.QueryFirstOrDefault<int?>(query, new { username });

                        if (foundUserNo.HasValue)
                        {
                            silenKullaniciId = foundUserNo.Value;
                            _logger.LogInformation($"Veritabanından kullanıcı no: {silenKullaniciId}");
                        }
                    }
                }

                // 4. Hiçbir yöntemle bulamazsak 1 olarak varsayalım (default admin)
                if (silenKullaniciId == null)
                {
                    silenKullaniciId = 1;
                    _logger.LogWarning("Kullanıcı no bulunamadı, varsayılan olarak 1 kullanılıyor.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı no alınırken hata oluştu: {Message}", ex.Message);
                silenKullaniciId = 1; // Hata durumunda yine 1 olarak varsayalım
            }

            // ========= KAYIT SİLME İŞLEMİ BÖLÜMÜ =========
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Çift tıklama koruması: daha önce silinmiş stok hareketleri filtreleniyor
                        var filteredGuidList = new List<string>();
                        foreach (var guidStr in seciliUretimler)
                        {
                            if (!Guid.TryParse(guidStr, out Guid stokHareketGuid))
                            {
                                _logger.LogWarning($"Geçersiz GUID formatı: {guidStr}");
                                continue;
                            }

                            // Bu GUID daha önce silinmiş mi kontrol et
                            using (var erpConnection = new SqlConnection(erpConnectionString))
                            {
                                erpConnection.Open();

                                var existingRecord = erpConnection.QueryFirstOrDefault<int>(
                                    "SELECT COUNT(1) FROM silinen_barkodlar WHERE stok_hareket_guid = @StokHareketGuid",
                                    new { StokHareketGuid = stokHareketGuid });

                                if (existingRecord > 0)
                                {
                                    _logger.LogInformation($"GUID {guidStr} daha önce silinmiş, işlem atlanıyor.");
                                    continue;
                                }
                            }

                            filteredGuidList.Add(guidStr);
                        }

                        foreach (var guidStr in filteredGuidList)
                        {
                            if (!Guid.TryParse(guidStr, out Guid stokHareketGuid))
                            {
                                continue; // Zaten yukarıda kontrol ettik
                            }

                            // İlgili stok hareketini bul
                            var stokHareket = connection.QueryFirstOrDefault<StokHareketSilModel>(
                                @"SELECT 
                            sth_stok_kod AS StokKodu, 
                            sth_parti_kodu AS PartiKodu, 
                            sth_lot_no AS LotNo,
                            sth_miktar AS Miktar,
                            (SELECT sto_isim FROM STOKLAR WHERE sto_kod = sth_stok_kod) AS StokAdi,
                            (SELECT TOP 1 bar_kodu FROM BARKOD_TANIMLARI 
                             WHERE bar_stokkodu = sth_stok_kod 
                             AND bar_partikodu = sth_parti_kodu 
                             AND bar_lotno = sth_lot_no) AS BarkodNo
                        FROM STOK_HAREKETLERI WHERE sth_Guid = @StokHareketGuid",
                                new { StokHareketGuid = stokHareketGuid },
                                transaction);

                            if (stokHareket == null)
                            {
                                _logger.LogWarning($"Stok hareketi bulunamadı: {guidStr}");
                                continue;
                            }

                            // Silinen kaydı ERP veritabanına kaydet
                            using (var erpConnection = new SqlConnection(erpConnectionString))
                            {
                                erpConnection.Open();

                                _logger.LogInformation($"Silinen barkod kaydı yapılıyor: StokKodu={stokHareket.StokKodu}, UserNo={silenKullaniciId}");

                                erpConnection.Execute(
                                    @"INSERT INTO silinen_barkodlar 
                              (stok_kodu, stok_adi, miktar, silinme_tarihi, silen_kullanici_id, 
                               barkod_no, parti_kodu, lot_no, stok_hareket_guid, silinme_nedeni)
                              VALUES 
                              (@StokKodu, @StokAdi, @Miktar, GETDATE(), @SilenKullaniciId, 
                               @BarkodNo, @PartiKodu, @LotNo, @StokHareketGuid, 'Hatalı Üretim')",
                                    new
                                    {
                                        stokHareket.StokKodu,
                                        stokHareket.StokAdi,
                                        stokHareket.Miktar,
                                        SilenKullaniciId = silenKullaniciId,
                                        stokHareket.BarkodNo,
                                        stokHareket.PartiKodu,
                                        stokHareket.LotNo,
                                        StokHareketGuid = stokHareketGuid
                                    });
                            }

                            // 1. İlgili barkod tanımlarını sil
                            int silinenBarkodSayisi = connection.Execute(
                                @"DELETE FROM BARKOD_TANIMLARI 
                          WHERE bar_stokkodu = @StokKodu 
                          AND bar_partikodu = @PartiKodu 
                          AND bar_lotno = @LotNo",
                                stokHareket,
                                transaction);

                            _logger.LogInformation($"Silinen barkod sayısı: {silinenBarkodSayisi} ({stokHareket.StokKodu}, {stokHareket.PartiKodu}, {stokHareket.LotNo})");

                            // 2. Stok hareketini sil
                            int silinenStokHareketi = connection.Execute(
                                "DELETE FROM STOK_HAREKETLERI WHERE sth_Guid = @StokHareketGuid",
                                new { StokHareketGuid = stokHareketGuid },
                                transaction);

                            _logger.LogInformation($"Silinen stok hareketi: {silinenStokHareketi} (GUID: {stokHareketGuid})");

                            // 3. Parti lot kaydını sil (eğer başka stok hareketleri tarafından kullanılmıyorsa)
                            if (!string.IsNullOrEmpty(stokHareket.PartiKodu) && stokHareket.LotNo.HasValue)
                            {
                                // Parti lot kontrolü
                                bool partiKullaniliyor = connection.QueryFirstOrDefault<bool>(
                                    @"SELECT CASE WHEN EXISTS (
                                SELECT 1 FROM STOK_HAREKETLERI 
                                WHERE sth_stok_kod = @StokKodu 
                                AND sth_parti_kodu = @PartiKodu 
                                AND sth_lot_no = @LotNo) 
                              THEN 1 ELSE 0 END",
                                    stokHareket,
                                    transaction);

                                if (!partiKullaniliyor)
                                {
                                    int silinenPartiLot = connection.Execute(
                                        @"DELETE FROM PARTILOT 
                                  WHERE pl_stokkodu = @StokKodu 
                                  AND pl_partikodu = @PartiKodu 
                                  AND pl_lotno = @LotNo",
                                        stokHareket,
                                        transaction);

                                    _logger.LogInformation($"Silinen parti lot: {silinenPartiLot} ({stokHareket.StokKodu}, {stokHareket.PartiKodu}, {stokHareket.LotNo})");
                                }
                            }

                            silinenKayitSayisi++;
                        }

                        transaction.Commit();
                        return silinenKayitSayisi;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Hatalı üretimler silinirken hata oluştu: {Message}", ex.Message);
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }



        public List<SilinenBarkodViewModel> GetSilinenBarkodlar(DateTime? baslangicTarihi = null, DateTime? bitisTarihi = null, string stokKodu = null)
        {
            // ERP veritabanı bağlantısı ConnectionHelper üzerinden alınıyor
            var erpConnectionString = ConnectionHelper.GetConnectionString("ERPDatabase");
            var normalConnectionString = _dbSelectorService.GetConnectionString(); // Mikro DB bağlantısı

            using (var connection = new SqlConnection(erpConnectionString))
            {
                connection.Open();

                // Silinen barkodlar tablosundan verileri çek
                var query = @"
            SELECT 
                sb.id AS Id,
                sb.stok_kodu AS StokKodu,
                sb.stok_adi AS StokAdi,
                sb.miktar AS Miktar,
                sb.silinme_tarihi AS SilinmeTarihi,
                sb.silen_kullanici_id AS SilenKullaniciId,
                sb.barkod_no AS BarkodNo,
                sb.parti_kodu AS PartiKodu,
                sb.lot_no AS LotNo,
                sb.stok_hareket_guid AS StokHareketGuid,
                sb.silinme_nedeni AS SilinmeNedeni
            FROM 
                silinen_barkodlar sb
            WHERE 
                1=1";

                var parameters = new DynamicParameters();

                if (baslangicTarihi.HasValue)
                {
                    query += " AND sb.silinme_tarihi >= @BaslangicTarihi";
                    parameters.Add("BaslangicTarihi", baslangicTarihi.Value);
                }

                if (bitisTarihi.HasValue)
                {
                    query += " AND sb.silinme_tarihi <= @BitisTarihi";
                    parameters.Add("BitisTarihi", bitisTarihi.Value.AddDays(1).AddSeconds(-1)); // Günün sonuna kadar
                }

                if (!string.IsNullOrEmpty(stokKodu))
                {
                    query += " AND sb.stok_kodu LIKE @StokKodu";
                    parameters.Add("StokKodu", "%" + stokKodu + "%");
                }

                query += " ORDER BY sb.silinme_tarihi DESC";

                try
                {
                    var results = connection.Query<SilinenBarkodViewModel>(query, parameters).ToList();
                    _logger.LogInformation($"Silinen barkod sayısı: {results.Count}");

                    // Kullanıcı adlarını ayrıca mikro veri tabanından al
                    if (results.Any())
                    {
                        Dictionary<int, string> kullaniciAdlari = new Dictionary<int, string>();

                        // Tüm kullanıcı ID'lerini topla
                        var kullaniciIdler = results
                            .Where(r => r.SilenKullaniciId.HasValue && r.SilenKullaniciId.Value > 0)
                            .Select(r => r.SilenKullaniciId.Value)
                            .Distinct()
                            .ToList();

                        if (kullaniciIdler.Any())
                        {
                            try
                            {
                                using (var mikroConn = new SqlConnection(normalConnectionString))
                                {
                                    mikroConn.Open();
                                    string kullaniciQuery = @"
                                SELECT 
                                    User_no, 
                                    User_name 
                                FROM 
                                    KULLANICILAR 
                                WHERE 
                                    User_no IN @KullaniciIdler";

                                    var kullanicilar = mikroConn.Query<dynamic>(kullaniciQuery, new { KullaniciIdler = kullaniciIdler });

                                    foreach (var kullanici in kullanicilar)
                                    {
                                        if (kullanici.User_no != null && !string.IsNullOrEmpty(kullanici.User_name))
                                        {
                                            kullaniciAdlari[kullanici.User_no] = kullanici.User_name;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Kullanıcı adları alınırken hata oluştu: {Message}", ex.Message);
                            }
                        }

                        // Sonuçlara kullanıcı adlarını ekle
                        foreach (var result in results)
                        {
                            if (result.SilenKullaniciId.HasValue && result.SilenKullaniciId.Value > 0)
                            {
                                if (kullaniciAdlari.ContainsKey(result.SilenKullaniciId.Value))
                                {
                                    result.KullaniciAdi = kullaniciAdlari[result.SilenKullaniciId.Value];
                                }
                                else
                                {
                                    result.KullaniciAdi = $"Kullanıcı #{result.SilenKullaniciId}";
                                }
                            }
                            else
                            {
                                result.KullaniciAdi = "Bilinmiyor";
                            }
                        }
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Silinen barkodlar listelenirken hata oluştu: {Message}", ex.Message);

                    // Hata durumunda basit bir sorgu ile yeniden dene
                    try
                    {
                        var simpleQuery = @"
                    SELECT 
                        id AS Id,
                        stok_kodu AS StokKodu,
                        stok_adi AS StokAdi,
                        miktar AS Miktar,
                        silinme_tarihi AS SilinmeTarihi,
                        silen_kullanici_id AS SilenKullaniciId,
                        'Bilinmiyor' AS KullaniciAdi,
                        barkod_no AS BarkodNo,
                        parti_kodu AS PartiKodu,
                        lot_no AS LotNo,
                        stok_hareket_guid AS StokHareketGuid,
                        silinme_nedeni AS SilinmeNedeni
                    FROM 
                        silinen_barkodlar
                    ORDER BY silinme_tarihi DESC";

                        var simpleResults = connection.Query<SilinenBarkodViewModel>(simpleQuery).ToList();
                        _logger.LogInformation($"Basit sorgu ile silinen barkod sayısı: {simpleResults.Count}");
                        return simpleResults;
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Basit sorgu ile silinen barkodlar listelenirken de hata oluştu");
                        return new List<SilinenBarkodViewModel>();
                    }
                }

            }
        }

        // FaturaRepository.cs içine eklenecek metotlar

        // Malzeme planlamayı getir
        public IEnumerable<MalzemePlanlama> GetMalzemePlanlama(string isEmriKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT 
            upl.upl_kodu AS StokKodu,
            s.sto_isim AS StokAdi,
            s.sto_birim1_ad AS BirimAdi,
            upl.upl_miktar AS PlanlananMiktar,
            -- Tüketilen miktarı stok hareketlerinden hesapla
            ISNULL((
                SELECT SUM(sh.sth_miktar) 
                FROM STOK_HAREKETLERI sh 
                WHERE sh.sth_belge_no = @IsEmriKodu 
                AND sh.sth_stok_kod = upl.upl_kodu 
                AND sh.sth_tip = 1  -- Çıkış hareketi
                AND sh.sth_cins = 7 -- Tüketim hareketi
            ), 0) AS TuketilenMiktar,
            -- Kalan miktarı hesapla
            upl.upl_miktar - ISNULL((
                SELECT SUM(sh.sth_miktar) 
                FROM STOK_HAREKETLERI sh 
                WHERE sh.sth_belge_no = @IsEmriKodu 
                AND sh.sth_stok_kod = upl.upl_kodu 
                AND sh.sth_tip = 1 
                AND sh.sth_cins = 7
            ), 0) AS KalanMiktar
        FROM URETIM_MALZEME_PLANLAMA upl
        INNER JOIN STOKLAR s ON s.sto_kod = upl.upl_kodu
        WHERE upl.upl_isemri = @IsEmriKodu 
            AND upl.upl_uretim_tuket = 0  -- Sadece tüketilen malzemeler
        ORDER BY s.sto_isim";

                try
                {
                    return connection.Query<MalzemePlanlama>(query, new { IsEmriKodu = isEmriKodu });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Malzeme planlaması alınırken hata oluştu. İş Emri: {IsEmriKodu}", isEmriKodu);
                    throw;
                }
            }
        }

        // Malzeme tüketimi yap
        public string MalzemeTuketimi(string isEmriKodu, List<TuketimItem> tuketimListesi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("MalzemeTuketimi", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // JSON formatına çevir
                    var jsonListesi = System.Text.Json.JsonSerializer.Serialize(
                        tuketimListesi.Select(t => new { stokKodu = t.StokKodu, miktar = t.Miktar })
                    );

                    command.Parameters.AddWithValue("@isemri", isEmriKodu);
                    command.Parameters.AddWithValue("@tuketim_listesi", jsonListesi);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string durum = reader.GetString("durum");
                            string evrakNo = reader.GetString("evrak_no");
                            int malzemeSayisi = reader.GetInt32("tuketilen_malzeme_sayisi");

                            return $"Tüketim başarılı! Evrak No: {evrakNo}, {malzemeSayisi} adet malzeme tüketildi.";
                        }
                        return "Tüketim yapıldı fakat sonuç alınamadı.";
                    }
                }
            }
        }

     
        public class MusteriAcikFaturaViewModel
        {
            public string MusteriKodu { get; set; }
            public string Unvan { get; set; }
            public decimal VadesiGecmisBakiye { get; set; }
            public decimal BugunOdenmesiGereken { get; set; }
            public decimal GelecekVadeliFaturalar { get; set; }
            public decimal ToplamBorc { get; set; }
            public List<FaturaViewModel> Faturalar { get; set; } = new List<FaturaViewModel>();
        }

        public class FaturaViewModel
        {
            public string FaturaNo { get; set; }
            public DateTime FaturaTarihi { get; set; }
            public DateTime VadeTarihi { get; set; }
            public decimal Tutar { get; set; }
        }
    }
}
