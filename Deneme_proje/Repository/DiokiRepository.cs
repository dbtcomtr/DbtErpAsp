using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Deneme_proje.Models.DiokiEntities;

namespace Deneme_proje.Repository
{
    public class DiokiRepository
    {
		private readonly DatabaseSelectorService _dbSelectorService;
		private readonly ILogger<DiokiRepository> _logger;

		public DiokiRepository(DatabaseSelectorService dbSelectorService, ILogger<DiokiRepository> logger)
		{
			_dbSelectorService = dbSelectorService;
			_logger = logger;
		}


		// Örnek bir method: Markaları getiren bir sorgu
		public IEnumerable<string> GetMarkalar()
		{
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
				var sqlQuery = @"
        SELECT DISTINCT sto_marka_kodu
        FROM STOKLAR
        WHERE sto_cins = 4 AND TRIM(sto_marka_kodu) <> ''";

				try
				{
					// Burada connection doğru şekilde kullanılıyor
					return connection.Query<string>(sqlQuery);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "An error occurred while retrieving Marka data.");
					throw;
				}
			}
		}

		public IEnumerable<string> GetModeller(string markaKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
		SELECT DISTINCT sto_model_kodu
		FROM STOKLAR
		WHERE sto_cins = 4 AND sto_marka_kodu = @MarkaKodu AND sto_pasif_fl=0 AND TRIM(sto_model_kodu) <> ''";

                var parameters = new { MarkaKodu = markaKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Model data.");
                    throw;
                }
            }
        }
        public IEnumerable<string> GetAmbalajKodlari(string markaKodu, string modelKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT DISTINCT sto_ambalaj_kodu
            FROM STOKLAR
            WHERE sto_cins = 4 
              AND sto_marka_kodu = @MarkaKodu 
              AND sto_model_kodu = @ModelKodu 
AND sto_pasif_fl=0
              AND TRIM(sto_ambalaj_kodu) <> ''";

                var parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Ambalaj Kodları data.");
                    throw;
                }
            }
        }
        public IEnumerable<string> GetKisaIsimler(string markaKodu, string modelKodu, string ambalajKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT DISTINCT sto_kisa_ismi
            FROM STOKLAR
            WHERE sto_cins = 4 
              AND sto_marka_kodu = @MarkaKodu 
              AND sto_model_kodu = @ModelKodu 
              AND TRIM(sto_kisa_ismi) <> '' 
              AND sto_pasif_fl = 0 
              AND sto_ambalaj_kodu = @AmbalajKodu";

                var parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, AmbalajKodu = ambalajKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Kısa İsim data.");
                    throw;
                }
            }
        }





        public string GetStokKodByKisaIsim(string kisaIsim)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
		SELECT sto_kod
		FROM STOKLAR
		WHERE sto_kisa_ismi = @KisaIsim AND TRIM(sto_kod) <> ''";

                var parameters = new { KisaIsim = kisaIsim };

                try
                {
                    return connection.QuerySingleOrDefault<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Stok Kod data.");
                    throw;
                }
            }
        }
        public (string Barkod, string Makine) ExecuteVideojet2Micro(string isemri, string stokkodu, int depo, int miktar, int lotNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@isemri", isemri);
                parameters.Add("@stokkodu", stokkodu);
                parameters.Add("@depo", depo);
                parameters.Add("@miktar", miktar);
                parameters.Add("@lot_no", lotNo);

                try
                {
                    var result = connection.QuerySingle(@"EXEC dbo.videojet2micro @isemri, @stokkodu, @depo, @miktar, @lot_no", parameters);
                    return (result.barkod, result.makine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"videojet2micro prosedürü çalıştırılırken hata oluştu: {ex.Message}");
                    throw;
                }
            }
        }
        public string GetActiveIsemriForStok(string stokkodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
            SELECT TOP 1 is_Kod
            FROM ISEMIRLERI ie
            INNER JOIN ISEMRI_MALZEME_DURUMLARI imd ON ie.is_Kod = imd.ish_isemri
            WHERE imd.ish_stokhizm_gid_kod = @StokKodu
            AND ie.is_EmriDurumu = 1 -- Aktif iş emirleri
            ORDER BY ie.is_BaslangicTarihi DESC";

                var parameters = new { StokKodu = stokkodu };

                try
                {
                    return connection.QueryFirstOrDefault<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Stok kodu '{stokkodu}' için aktif iş emri aranırken hata oluştu.");
                    throw;
                }
            }
        }

        public string GetIsemriByFn(string stokkodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(@"
            SELECT TOP 1 [msg_S_0349] 
            FROM dbo.fn_IsEmriOperasyon(255, NULL, NULL, 0, 2, N'', N'', N'', N'', N'', N'') 
            WHERE msg_S_0352 = @StokKodu AND #msg_S_0355 = 'Aktif'
            ORDER BY [msg_S_0351] DESC", (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@StokKodu", stokkodu);

                    try
                    {
                        var result = command.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            _logger.LogWarning($"Stok kodu '{stokkodu}' için aktif iş emri bulunamadı.");
                            return null;
                        }
                        _logger.LogInformation($"Stok kodu '{stokkodu}' için bulunan iş emri: {result}");
                        return result.ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Stok kodu '{stokkodu}' için iş emri aranırken hata oluştu.");
                        throw;
                    }
                }
            }
        }
        // DiokiRepository.cs dosyasına eklenecek metotlar

        // Barkod bilgisini kullanıcı numarası ile güncelleme
        public void BarkodKullaniciGuncelle(string barkod, string userNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
            UPDATE BARKOD_TANIMLARI 
            SET bar_special2 = @UserNo
            WHERE bar_kodu = @Barkod";

                var parameters = new { Barkod = barkod, UserNo = userNo };

                try
                {
                    connection.Execute(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Barkod kullanıcı bilgisi güncellenirken hata oluştu.");
                    throw;
                }
            }
        }

        // Barkodu hatalı olarak işaretle
        public void BarkodHataliOlarakIsaretle(string barkod)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
            UPDATE BARKOD_TANIMLARI 
            SET bar_special3 = '001'
            WHERE bar_kodu = @Barkod";

                var parameters = new { Barkod = barkod };

                try
                {
                    connection.Execute(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Barkod hatalı olarak işaretlenirken hata oluştu.");
                    throw;
                }
            }
        }

        // Personel bilgilerini tutan sınıf
        public class PersonelBilgisi
        {
            public string Ad { get; set; }
            public string Soyad { get; set; }
        }

        // Kullanıcı numarasına göre personel bilgisini getir
        public PersonelBilgisi KullaniciNoyaGorePersonelGetir(string userNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
            SELECT per_adi AS Ad, per_soyadi AS Soyad
            FROM PERSONELLER
            WHERE per_userno = @UserNo";

                var parameters = new { UserNo = userNo };

                try
                {
                    return connection.QueryFirstOrDefault<PersonelBilgisi>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Personel bilgisi getirilirken hata oluştu.");
                    throw;
                }
            }
        }

        // Genişletilmiş Barkod Tanımı sınıfı
        public class GenisletilmisBarkodTanimi : BarkodTanimi
        {
            public string KullaniciNo { get; set; }
            public string PersonelAdi { get; set; }
            public string PersonelSoyadi { get; set; }
            public string HataliDurum { get; set; }
        }

        // Barkod tanımlarını kullanıcı bilgileriyle birlikte getir
        public IEnumerable<GenisletilmisBarkodTanimi> KullaniciBilgisiyleBarkodTanimlariniGetir()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
            SELECT 
    b.bar_kodu, 
    b.bar_stokkodu, 
    b.bar_partikodu, 
    b.bar_lotno,
    b.bar_special2 AS KullaniciNo,
    b.bar_special3 AS HataliDurum,
    p.per_adi AS PersonelAdi,
    p.per_soyadi AS PersonelSoyadi
FROM BARKOD_TANIMLARI b
LEFT JOIN PERSONELLER p ON b.bar_special2 = p.per_userno AND b.bar_special2 <> ''
GROUP BY 
    b.bar_kodu, 
    b.bar_stokkodu, 
    b.bar_partikodu, 
    b.bar_lotno,
    b.bar_special2,
    b.bar_special3,
    p.per_adi,
    p.per_soyadi";

                try
                {
                    return connection.Query<GenisletilmisBarkodTanimi>(sqlQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kullanıcı bilgisiyle barkod tanımları getirilirken hata oluştu.");
                    throw;
                }
            }
        }
        public IEnumerable<BarkodTanimi> GetBarkodTanimi()
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT bar_kodu, bar_stokkodu, bar_partikodu, bar_lotno
            FROM BARKOD_TANIMLARI";

                try
                {
                    return connection.Query<BarkodTanimi>(sqlQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Barkod Tanımı data.");
                    throw;
                }
            }
        }
    }
}

        

