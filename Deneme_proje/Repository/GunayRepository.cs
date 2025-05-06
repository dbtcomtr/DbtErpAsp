using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // SqlConnection buradan
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using static Deneme_proje.Models.GunayEntities;
using NuGet.Protocol.Plugins;
using static Deneme_proje.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Deneme_proje.Repository
{
    public class GunayRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<GunayRepository> _logger;

        public GunayRepository(DatabaseSelectorService dbSelectorService, ILogger<GunayRepository> logger)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }



        public IEnumerable<FiloKiralamaViewModel> GetFiloKiralamaData(DateTime startDate, DateTime endDate, string srmrkodu, string databaseName = "ERPDatabase")
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
                    SELECT 
                        cha_srmrkkodu AS ChaSrmrkodu,
                        cha_tarihi AS ChaTarihi,
                        cha_kod AS ChaKod,
                        cha_kasa_hizkod AS ChaKasaHizkod,
                        hiz_isim AS HizIsim,
                        cha_meblag AS ChaMeblag,
                        SUM(cha_miktari) AS TotalMiktar,
                        cha_aciklama AS BirGunluk,
                        cha_aciklama AS IkiYediGun,
                        cha_aciklama AS YediOnBesGun,
                        cha_aciklama AS OnBesTenFazla
                    FROM CARI_HESAP_HAREKETLERI 
                    LEFT JOIN HIZMET_HESAPLARI ON hiz_kod = cha_kasa_hizkod
                    WHERE cha_srmrkkodu = @Srmrkodu
                        AND cha_tarihi >= @StartDate
                        AND cha_tarihi <= @EndDate
                        AND cha_kasa_hizkod = '600.04'
                    GROUP BY cha_srmrkkodu, cha_kasa_hizkod, cha_kod, cha_meblag, cha_tarihi, hiz_isim, cha_aciklama";

                var parameters = new
                {
                    Srmrkodu = srmrkodu,
                    StartDate = startDate,
                    EndDate = endDate
                };

                try
                {
                    return connection.Query<FiloKiralamaViewModel>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving FiloKiralama data.");
                    throw;
                }
            }
        }

        public IEnumerable<Sorumlu> GetTumSorumluKodlari(DateTime? baslangicTarihi = null, DateTime? bitisTarihi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT 
                som_kod AS SorumluKodu,
                som_isim AS SorumluAdi
            FROM [dbo].[SORUMLULUK_MERKEZLERI]";

                try
                {
                    return connection.Query<Sorumlu>(query);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tüm sorumluluk merkezi kodları getirilirken hata oluştu.");
                    return Enumerable.Empty<Sorumlu>();
                }
            }
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
    ISNULL(cha_tarihi, GETDATE()) AS [FaturaTarihi],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    ISNULL(chk_Alacakvade, GETDATE()) AS [AlacakVade],
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
    ISNULL(cha_tarihi, GETDATE()) AS [FaturaTarihi],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    ISNULL(chk_BorcVade, GETDATE()) AS [BorcVade],
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



        public IEnumerable<IGrouping<string, KrediDetayi>> GetKrediDetayList()
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
WHERE   (krsoztaksit_taksit - krsoztaksit_odenen_ana)  > 10 -- Filter for anapara greater than 10
";

                var krediDetayListesi = connection.Query<KrediDetayi>(sql).ToList();

                // Group by bank name (ban_ismi) instead of bank code
                var groupedData = krediDetayListesi
                    .GroupBy(x => x.ban_ismi) // Grouping by Bank Name
                    .ToList();

                return groupedData;
            }
        }



        public IEnumerable<CariBakiyeYaslandirma> GetCariMusteriYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar)
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

        public IEnumerable<CariBakiyeYaslandirma> GetCariTedarikciYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar)
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
                        command.Parameters.AddWithValue("@DepoNo", (object)depoNo ?? DBNull.Value); // Depo numarası parametresi ekleme

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
                                    MsgS0165 = reader.GetDouble(reader.GetOrdinal("msg_S_0165")),
                                    StokEvraknoSeri = reader.GetString(reader.GetOrdinal("sth_evrakno_seri")),
                                    StokEvraknoSira = reader.GetInt32(reader.GetOrdinal("sth_evrakno_sira")),
                                    StokMiktar = reader.GetDouble(reader.GetOrdinal("sth_miktar")),
                                    StokTutar = reader.GetDouble(reader.GetOrdinal("sth_tutar")),
                                    StokTarih = reader.GetDateTime(reader.GetOrdinal("sth_tarih")),
                                    CumulativeQuantity = reader.GetDouble(reader.GetOrdinal("CumulativeQuantity")),
                                    StoktaGirenMiktar = reader.GetDouble(reader.GetOrdinal("Stokta_Giren_Miktar")),
                                    Days0To30 = reader.GetDouble(reader.GetOrdinal("Days0To30")),
                                    Days31To60 = reader.GetDouble(reader.GetOrdinal("Days31To60")),
                                    Days61To90 = reader.GetDouble(reader.GetOrdinal("Days61To90")),
                                    Days90Plus = reader.GetDouble(reader.GetOrdinal("Days90Plus")),
                                    NumericDate = reader.GetDouble(reader.GetOrdinal("NumericDate")),
                                    AltDovizKuru = reader.GetDouble(reader.GetOrdinal("sth_alt_doviz_kuru"))
                                };

                                stockMovements.Add(stockMovement);
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

            return stockMovements;
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
        public IEnumerable<OtokocViewModel> OtokocData()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "OTOKOC";

                try
                {
                    return connection.Query<OtokocViewModel>(
                        procedureName,
                        commandType: CommandType.StoredProcedure).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving stock movement data.");
                    throw;
                }
            }
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

        public List<AgirVasitaSatisViewModel> GetAgirVasitaSatisData(
    DateTime startDate,
    DateTime endDate,
    List<string> selectedScopes)
        {
            var result = new List<AgirVasitaSatisViewModel>();
            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Seçilen kapsamları birleştir
                    string scopesParam = selectedScopes != null && selectedScopes.Count > 0
                        ? string.Join(",", selectedScopes)
                        : null;

                    using (SqlCommand command = new SqlCommand("DBT_AgirVasitaSatis", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Parametreleri ekle
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        // Seçilen kapsamlar parametresi
                        if (!string.IsNullOrEmpty(scopesParam))
                        {
                            command.Parameters.AddWithValue("@SelectedScopes", scopesParam);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@SelectedScopes", DBNull.Value);
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new AgirVasitaSatisViewModel
                                {
                                    Yil = reader["Yil"] != DBNull.Value ? Convert.ToInt32(reader["Yil"]) : 0,
                                    Ay = reader["Ay"] != DBNull.Value ? Convert.ToInt32(reader["Ay"]) : 0,
                                    AnaGrup = reader["AnaGrup"] != DBNull.Value ? reader["AnaGrup"].ToString() : string.Empty,
                                    StokKodu = reader["StokKodu"] != DBNull.Value ? reader["StokKodu"].ToString() : string.Empty,
                                    StokAdi = reader["StokAdi"] != DBNull.Value ? reader["StokAdi"].ToString() : string.Empty,
                                    SatisMiktari = reader["SatisMiktari"] != DBNull.Value ? Convert.ToDecimal(reader["SatisMiktari"]) : 0,
                                    SatisTutari = reader["SatisTutari"] != DBNull.Value ? Convert.ToDecimal(reader["SatisTutari"]) : 0,
                                    IslemSayisi = reader["IslemSayisi"] != DBNull.Value ? Convert.ToInt32(reader["IslemSayisi"]) : 0
                                };
                                result.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ağır Vasıta Satış verileri alınırken hata oluştu.");
            }
            return result;
        }
    }
}