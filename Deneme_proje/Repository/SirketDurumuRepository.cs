using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using static Deneme_proje.Models.SirketDurumuEntites;
using static Deneme_proje.Models.Entities;

namespace Deneme_proje.Repository
{
    public class SirketDurumuRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<SirketDurumuRepository> _logger;

        public SirketDurumuRepository(DatabaseSelectorService dbSelectorService, ILogger<SirketDurumuRepository> logger)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        public IEnumerable<CekAnalizi> GetFirmaCekleri(string sck_sonpoz, string projeKodu, string srmMerkeziKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@sck_sonpoz", sck_sonpoz);
                parameters.Add("@ProjeKodu", projeKodu);
                parameters.Add("@SrmMerkeziKodu", srmMerkeziKodu);
                parameters.Add("@BaslamaTarihi", baslamaTarihi);
                parameters.Add("@BitisTarihi", bitisTarihi);

                try
                {
                    var result = connection.Query<CekAnalizi>(
                        "dbo.DBT_Finans_FirmaCekleri_WEB",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching firm checks");
                    throw;
                }
            }
        }

        // Bankaları al
        public IEnumerable<BankModel> GetBanks()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UPB_PB", 0);
                parameters.Add("@ban_hesap_tip", 0);

                return connection.Query<BankModel>("dbo.DBT_BankalariGetir", parameters, commandType: CommandType.StoredProcedure);
            }
        }

        // Banka detaylarını al
        public IEnumerable<BankDetailModel> GetBankDetails(string bankKodu, DateTime baslamaTarihi, DateTime bitisTarihi, int upbPb = 0, string projeKodu = null, string srmMerkeziKodu = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@cari_kod", bankKodu);
                parameters.Add("@baslamatarihi", baslamaTarihi);
                parameters.Add("@bitistarihi", bitisTarihi);
                parameters.Add("@UPB_PB", upbPb);
                parameters.Add("@ProjeKodu", projeKodu);
                parameters.Add("@SrmMerkeziKodu", srmMerkeziKodu);

                return connection.Query<BankDetailModel>(
                    "dbo.DBT_BankaFoyu_BankaKodaTariheGore",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
        }


        public IEnumerable<Stok> GetStoklar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT sto_kod AS StokKodu, sto_isim AS StokAdi FROM Stoklar";
                var result = connection.Query<Stok>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Stok verisi bulunamadı.");
                }

                return result;
            }
        }



        // GetStokHareketFoyu metodu - Stok hareketlerini getirir
        public IEnumerable<StokHareketFoyu> GetStokHareketFoyu(string stokKodu, DateTime baslamaTarihi, DateTime bitisTarihi, int paraBirimi = 0, string depolar = "")
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StokKodu", stokKodu);
                    parameters.Add("@BaslamaTarihi", baslamaTarihi);
                    parameters.Add("@BitisTarihi", bitisTarihi);
                    parameters.Add("@ParaBirimi", paraBirimi);

                    // Depo parametresinin null veya boş string olma durumunu ayrı ayrı ele al
                    if (depolar == null)
                    {
                        parameters.Add("@Depolar", DBNull.Value);
                    }
                    else if (string.IsNullOrWhiteSpace(depolar))
                    {
                        parameters.Add("@Depolar", DBNull.Value);
                    }
                    else
                    {
                        parameters.Add("@Depolar", depolar);
                    }

                    _logger.LogInformation("Gönderilen depolar parametresi: {depolar}", depolar ?? "NULL");

                    var result = connection.Query<StokHareketFoyu>(
                        "dbo.DBT_STOK_StokHareketFoyu",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );

                    return result.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok hareket verileri alınırken hata oluştu. StokKodu: {StokKodu}, Depolar: {Depolar}",
                        stokKodu, depolar ?? "NULL");
                    throw new Exception($"Stok hareket verileri alınırken bir hata oluştu: {ex.Message}", ex);
                }
            }
        }
        // GetDepolar metodu - Depo bilgilerini getirir
        public IEnumerable<Depo> GetDepolar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT dep_no AS DepoNo, dep_adi AS DepoAdi FROM Depolar";
                var result = connection.Query<Depo>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Depo verisi bulunamadı.");
                }

                return result;
            }
        }

        // GetCariKodlari metodu - Cari kodları getirir
        public IEnumerable<CariHesap> GetCariKodlari()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT DISTINCT cari_kod AS CariKod, cari_unvan1 AS CariUnvan1 FROM CARI_HESAPLAR";
                var result = connection.Query<CariHesap>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Cari kodları bulunamadı.");
                }

                return result;
            }
        }

        // GetCariHareketFoyu metodu - Cari hareketlerini getirir
        public IEnumerable<CariHareketFoyu> GetCariHareketFoyu(string firmalar, int cariCins, string cariKod, int? grupNo, DateTime? devirTar, DateTime? ilkTar, DateTime? sonTar, int odemeEmriDegerlemeDok, string somStr)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@firmalar", firmalar);
                parameters.Add("@caricins", cariCins);
                parameters.Add("@carikod", cariKod);
                parameters.Add("@grupno", grupNo);
                parameters.Add("@devirtar", devirTar);
                parameters.Add("@ilktar", ilkTar);
                parameters.Add("@sontar", sonTar);
                parameters.Add("@odemeemridegerlemedok", odemeEmriDegerlemeDok);
                parameters.Add("@SomStr", somStr);

                try
                {
                    var result = connection.Query<CariHareketFoyu>(
                        "dbo.DBT_CariHareketFoyuWeb",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching cari hareket foyu");
                    throw;
                }
            }
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
            var connectionString = _dbSelectorService.GetConnectionString(); // Bağlantı dizesini al

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
            var connectionString = _dbSelectorService.GetConnectionString(); // Bağlantı dizesini al

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
        public IEnumerable<KasaRaporuOdemeModel> GetKasaRaporuOdemeleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@BaslamaTarihi", baslamaTarihi);
                parameters.Add("@BitisTarihi", bitisTarihi);

                try
                {
                    var result = connection.Query<KasaRaporuOdemeModel>(
                        "dbo.DBT_Web_KasaRaporu_Odemeler",
                        parameters,
                        commandType: CommandType.StoredProcedure)
                        .Where(x => x.Tutar >= 1)  // 1'den küçük tutarları filtrele
                        .OrderBy(x => x.VadeTarihi);  // Tarihe göre sırala

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kasa raporu ödemeleri getirilirken hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KasaRaporuOdemeModel> GetKasaRaporuTahsilatlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@BaslamaTarihi", baslamaTarihi);
                parameters.Add("@BitisTarihi", bitisTarihi);

                try
                {
                    var result = connection.Query<KasaRaporuOdemeModel>(
                        "dbo.DBT_Web_KasaRaporu_Tahsilatlar_Deneme",
                        parameters,
                        commandType: CommandType.StoredProcedure)
                        .Where(x => x.Tutar >= 1)  // 1'den küçük tutarları filtrele
                        .OrderBy(x => x.VadeTarihi);  // Tarihe göre sırala

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kasa raporu tahsilatları getirilirken hata oluştu");
                    throw;
                }
            }
        }

        // Toplam bakiye listesini getirmek için metod
        public IEnumerable<BakiyeModel> GetToplamBakiyeListesi()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var bakiyeListesi = new List<BakiyeModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    // BANKALAR için NULL tarih parametreleri ile sorgu çalıştır
                    var bankaQuery = @"
                SELECT 
                    ban_kod AS Kod,
                    ban_ismi AS Isim,
                    ban_doviz_cinsi AS DovizKodu,
                    dbo.fn_CariHesapAnaDovizBakiye(ban_firma_no, 2, ban_kod, '', '', 1, NULL, NULL, 0, 0, 0, 0, 0) AS Bakiye
                FROM dbo.BANKALAR WITH (NOLOCK)
                WHERE ban_ismi NOT LIKE '%Çek%' AND ban_ismi NOT LIKE '%Senet%' -- Çek ve senet kelimelerini içeren banka isimlerini filtrele
                ORDER BY ban_kod";

                    var bankalar = connection.Query<BakiyeModel>(bankaQuery).ToList();
                    foreach (var banka in bankalar)
                    {
                        banka.HesapTipi = "Banka";

                        // Döviz sembolünü belirle
                        banka.DovizCinsi = GetDovizSembolu(banka.DovizKodu);

                        // 1'den küçük bakiyeleri filtrele (mutlak değer olarak)
                        if (Math.Abs(banka.Bakiye) >= 1)
                        {
                            bakiyeListesi.Add(banka);
                        }
                    }

                    // KASALAR için NULL tarih parametreleri ile sorgu çalıştır
                    var kasaQuery = @"
                SELECT 
                    kas_kod AS Kod,
                    kas_isim AS Isim,
                    0 AS DovizKodu, -- TL için 0 kodunu kullanıyoruz
                    dbo.fn_CariHesapAnaDovizBakiye(kas_firma_no, 4, kas_kod, '', '', 0, NULL, NULL, 0, 0, 0, 0, 0) AS Bakiye
                FROM dbo.KASALAR WITH (NOLOCK)
                WHERE kas_isim NOT LIKE '%Çek%' AND kas_isim NOT LIKE '%Senet%' AND kas_isim NOT LIKE '%Borç%'-- Çek ve senet kelimelerini içeren kasa isimlerini filtrele
                ORDER BY kas_kod";

                    var kasalar = connection.Query<BakiyeModel>(kasaQuery).ToList();
                    foreach (var kasa in kasalar)
                    {
                        kasa.HesapTipi = "Kasa";
                        kasa.DovizCinsi = "₺"; // Kasalar için varsayılan olarak TL sembolü

                        // 1'den küçük bakiyeleri filtrele (mutlak değer olarak)
                        if (Math.Abs(kasa.Bakiye) >= 1)
                        {
                            bakiyeListesi.Add(kasa);
                        }
                    }

                    return bakiyeListesi;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Toplam bakiye listesi alınırken hata oluştu");
                    throw;
                }
            }
        }

        // Döviz kodundan sembol üreten yardımcı metod
        private string GetDovizSembolu(int dovizKodu)
        {
            switch (dovizKodu)
            {
                case 0:
                    return "₺"; // Türk Lirası
                case 1:
                    return "$"; // Amerikan Doları
                case 2:
                    return "€"; // Euro
                case 3:
                    return "£"; // İngiliz Sterlini
                case 4:
                    return "¥"; // Japon Yeni
                case 5:
                    return "CHF"; // İsviçre Frangı
                default:
                    return ""; // Bilinmeyen döviz kodu
            }
        }
        // GetBankaLimitleri metodunu güncelleme
        // GetBankaLimitleri metodunu güncelleme
        public IEnumerable<BankaLimitViewModel> GetBankaLimitleri()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Önce banka limit bilgilerini al
                    string query = @"
                SELECT 
                    B.ban_ismi AS BankaAdi,
                    B.ban_kod AS BankaKodu,
                    BU.[Kredi_Karti_Limiti] AS KrediKartiLimiti,
                    BU.[Cek_Teminat_Limiti] AS CekTeminatLimiti,
                    BU.[Ipotek_Teminat_Limiti] AS IpotekTeminatLimiti,
                    BU.[DBS_Limiti] AS DBSLimiti,
                    BU.[Gayri_Nakdi_Limiti] AS GayriNakdiLimiti,
                    BU.[Kefalet_Karsiligi_Limit] AS KefaletKarsiligiLimit,
                    BU.[KMH_Limiti] AS KMHLimiti,
                    BU.[BCH_Limiti] AS BCHLimiti
                FROM 
                    BANKALAR B
                    LEFT JOIN BANKALAR_USER BU ON B.ban_Guid = BU.Record_uid
                ORDER BY
                    B.ban_ismi";

                    var bankaLimitleri = connection.Query<BankaLimitViewModel>(query).ToList();

                    // Şimdi her banka için teminat verilerini çek ve 1'den büyük değer olan bankaları filtrele
                    var filtrelenmisListe = new List<BankaLimitViewModel>();

                    foreach (var bankaLimit in bankaLimitleri)
                    {
                        // Firma Teminat Mektubu (sck_tip = 11)
                        var teminatMektubuQuery = @"
                    SELECT ISNULL(SUM(sck_tutar), 0) AS Toplam
                    FROM ODEME_EMIRLERI
                    WHERE sck_bankano = @BankaKodu AND sck_tip = 11 AND sck_iptal = 0";

                        // Kendi Kredi Kartımız (sck_tip = 9)
                        var kendiKrediKartiQuery = @"
                    SELECT ISNULL(SUM(sck_tutar), 0) AS Toplam
                    FROM ODEME_EMIRLERI
                    WHERE sck_bankano = @BankaKodu AND sck_tip = 9 AND sck_iptal = 0";

                        // Kendi Çekimiz (sck_tip = 2)
                        var kendiCekQuery = @"
                    SELECT ISNULL(SUM(sck_tutar), 0) AS Toplam
                    FROM ODEME_EMIRLERI
                    WHERE sck_bankano = @BankaKodu AND sck_tip = 2 AND sck_iptal = 0";
                        var krediSozlesmesiQuery = @"
    SELECT ISNULL(SUM(krsoz_kreditutari), 0) AS Toplam
    FROM KREDI_SOZLESMESI_TANIMLARI
    WHERE krsoz_sozbankakodu = @BankaKodu AND krsoz_iptal = 0";

                        bankaLimit.KrediSozlesmesiTutari = connection.QueryFirstOrDefault<decimal>(krediSozlesmesiQuery,
                            new { BankaKodu = bankaLimit.BankaKodu });


                        // Her sorgu için değerleri al
                        bankaLimit.TeminatMektubuToplam = connection.QueryFirstOrDefault<decimal>(teminatMektubuQuery,
                            new { BankaKodu = bankaLimit.BankaKodu });

                        bankaLimit.KendiKrediKartiToplam = connection.QueryFirstOrDefault<decimal>(kendiKrediKartiQuery,
                            new { BankaKodu = bankaLimit.BankaKodu });

                        bankaLimit.KendiCekToplam = connection.QueryFirstOrDefault<decimal>(kendiCekQuery,
                            new { BankaKodu = bankaLimit.BankaKodu });

                        // Eğer bankanın herhangi bir değeri 1'den büyükse, listeye ekle
                        if (bankaLimit.KrediKartiLimiti >= 1 ||
                            bankaLimit.CekTeminatLimiti >= 1 ||
                            bankaLimit.IpotekTeminatLimiti >= 1 ||
                            bankaLimit.DBSLimiti >= 1 ||
                            bankaLimit.GayriNakdiLimiti >= 1 ||
                            bankaLimit.KefaletKarsiligiLimit >= 1 ||
                            bankaLimit.KMHLimiti >= 1 ||
                            bankaLimit.BCHLimiti >= 1 ||
                            bankaLimit.TeminatMektubuToplam >= 1 ||
                            bankaLimit.KendiKrediKartiToplam >= 1 ||
                             bankaLimit.KendiCekToplam >= 1 ||
    bankaLimit.KrediSozlesmesiTutari >= 1)

                        {
                            filtrelenmisListe.Add(bankaLimit);
                        }
                    }

                    return filtrelenmisListe;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Banka limitleri alınırken hata oluştu");
                    throw;
                }
            }
        }
    }
}

// Diğer metotlar aynı şekilde dinamik bağlantı ile