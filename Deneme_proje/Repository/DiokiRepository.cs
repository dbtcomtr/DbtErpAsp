using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Deneme_proje.Models.DiokiEntities;
using System.Linq;

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

        // ESKİ METOD - YEDEKLEMEYİ KORUYORUZ
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
                    return connection.Query<string>(sqlQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Marka data.");
                    throw;
                }
            }
        }

        // YENİ - İŞ MERKEZİ FİLTRELİ MARKA LİSTESİ
        public IEnumerable<string> GetMarkalarWithIsMerkeziFilter(string userNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo);

            using (var connection = new SqlConnection(connectionString))
            {
                string sqlQuery;
                object parameters;

                if (yetkiliIsMerkezleri.Any())
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_marka_kodu
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND TRIM(s.sto_marka_kodu) <> '' 
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi IN @IsMerkezleri";

                    parameters = new { IsMerkezleri = yetkiliIsMerkezleri };
                }
                else
                {
                    sqlQuery = @"
                        SELECT DISTINCT sto_marka_kodu
                        FROM STOKLAR
                        WHERE sto_cins = 4 AND TRIM(sto_marka_kodu) <> ''";

                    parameters = new { };
                }

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş merkezi filtreli marka listesi alınırken hata oluştu.");
                    throw;
                }
            }
        }

        // YENİ - İŞ MERKEZİ FİLTRELİ MODEL LİSTESİ
        public IEnumerable<string> GetModeller(string markaKodu, string userNo, string isMerkezi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo);

            using (var connection = new SqlConnection(connectionString))
            {
                string sqlQuery;
                object parameters;

                if (!string.IsNullOrEmpty(isMerkezi))
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_model_kodu
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_pasif_fl = 0 
                            AND TRIM(s.sto_model_kodu) <> ''
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi = @IsMerkezi";

                    parameters = new { MarkaKodu = markaKodu, IsMerkezi = isMerkezi };
                }
                else if (yetkiliIsMerkezleri.Any())
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_model_kodu
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_pasif_fl = 0 
                            AND TRIM(s.sto_model_kodu) <> ''
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi IN @IsMerkezleri";

                    parameters = new { MarkaKodu = markaKodu, IsMerkezleri = yetkiliIsMerkezleri };
                }
                else
                {
                    sqlQuery = @"
                        SELECT DISTINCT sto_model_kodu
                        FROM STOKLAR
                        WHERE sto_cins = 4 
                            AND sto_marka_kodu = @MarkaKodu 
                            AND sto_pasif_fl = 0 
                            AND TRIM(sto_model_kodu) <> ''";

                    parameters = new { MarkaKodu = markaKodu };
                }

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş merkezi filtreli model listesi alınırken hata oluştu.");
                    throw;
                }
            }
        }

        // YENİ - İŞ MERKEZİ FİLTRELİ AMBALAJ KODLARI
        public IEnumerable<string> GetAmbalajKodlari(string markaKodu, string modelKodu, string userNo, string isMerkezi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo);

            using (var connection = new SqlConnection(connectionString))
            {
                string sqlQuery;
                object parameters;

                if (!string.IsNullOrEmpty(isMerkezi))
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_ambalaj_kodu
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_model_kodu = @ModelKodu 
                            AND s.sto_pasif_fl = 0
                            AND TRIM(s.sto_ambalaj_kodu) <> ''
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi = @IsMerkezi";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, IsMerkezi = isMerkezi };
                }
                else if (yetkiliIsMerkezleri.Any())
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_ambalaj_kodu
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_model_kodu = @ModelKodu 
                            AND s.sto_pasif_fl = 0
                            AND TRIM(s.sto_ambalaj_kodu) <> ''
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi IN @IsMerkezleri";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, IsMerkezleri = yetkiliIsMerkezleri };
                }
                else
                {
                    sqlQuery = @"
                        SELECT DISTINCT sto_ambalaj_kodu
                        FROM STOKLAR
                        WHERE sto_cins = 4 
                            AND sto_marka_kodu = @MarkaKodu 
                            AND sto_model_kodu = @ModelKodu 
                            AND sto_pasif_fl = 0
                            AND TRIM(sto_ambalaj_kodu) <> ''";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu };
                }

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş merkezi filtreli ambalaj kodları listesi alınırken hata oluştu.");
                    throw;
                }
            }
        }

        // YENİ - İŞ MERKEZİ FİLTRELİ KISA İSİMLER
        public IEnumerable<string> GetKisaIsimler(string markaKodu, string modelKodu, string ambalajKodu, string userNo, string isMerkezi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo);

            using (var connection = new SqlConnection(connectionString))
            {
                string sqlQuery;
                object parameters;

                if (!string.IsNullOrEmpty(isMerkezi))
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_kisa_ismi
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_model_kodu = @ModelKodu 
                            AND s.sto_ambalaj_kodu = @AmbalajKodu
                            AND TRIM(s.sto_kisa_ismi) <> '' 
                            AND s.sto_pasif_fl = 0
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi = @IsMerkezi";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, AmbalajKodu = ambalajKodu, IsMerkezi = isMerkezi };
                }
                else if (yetkiliIsMerkezleri.Any())
                {
                    sqlQuery = @"
                        SELECT DISTINCT s.sto_kisa_ismi
                        FROM STOKLAR s
                        INNER JOIN URETIM_ROTA_PLANLARI rtp ON s.sto_kod = rtp.RtP_UrunKodu AND rtp.RtP_SatirNo = 0
                        INNER JOIN ISEMIRLERI ie ON rtp.RtP_IsEmriKodu = ie.is_Kod
                        WHERE s.sto_cins = 4 
                            AND s.sto_marka_kodu = @MarkaKodu 
                            AND s.sto_model_kodu = @ModelKodu 
                            AND s.sto_ambalaj_kodu = @AmbalajKodu
                            AND TRIM(s.sto_kisa_ismi) <> '' 
                            AND s.sto_pasif_fl = 0
                            AND ie.is_EmriDurumu IN (0, 1)
                            AND rtp.RtP_PlanlananIsMerkezi IN @IsMerkezleri";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, AmbalajKodu = ambalajKodu, IsMerkezleri = yetkiliIsMerkezleri };
                }
                else
                {
                    sqlQuery = @"
                        SELECT DISTINCT sto_kisa_ismi
                        FROM STOKLAR
                        WHERE sto_cins = 4 
                            AND sto_marka_kodu = @MarkaKodu 
                            AND sto_model_kodu = @ModelKodu 
                            AND TRIM(sto_kisa_ismi) <> '' 
                            AND sto_pasif_fl = 0 
                            AND sto_ambalaj_kodu = @AmbalajKodu";

                    parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, AmbalajKodu = ambalajKodu };
                }

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş merkezi filtreli kısa isim listesi alınırken hata oluştu.");
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

        public (string Barkod, string Makine) ExecuteVideojet2Micro(string isemri, string stokkodu, int depo, int miktar, string partiKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@isemri", isemri);
                parameters.Add("@stokkodu", stokkodu);
                parameters.Add("@depo", depo);
                parameters.Add("@miktar", miktar);
                parameters.Add("@parti_kodu", partiKodu);

                try
                {
                    var result = connection.QuerySingle(@"EXEC dbo.videojet2micro @isemri, @stokkodu, @depo, @miktar, @parti_kodu", parameters);
                    return (result.barkod, result.makine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"videojet2micro prosedürü çalıştırılırken hata oluştu: {ex.Message}");
                    throw;
                }
            }
        }

        // ESKİ METOD - YEDEKLEMEYİ KORUYORUZ
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

        // YENİ - İŞ MERKEZİ BAZLI İŞ EMRİ BULMA
        public string GetIsemriByIsMerkezi(string stokkodu, string isMerkezi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
                    SELECT TOP 1 ie.is_Kod
                    FROM ISEMIRLERI ie
                    INNER JOIN URETIM_ROTA_PLANLARI rtp ON ie.is_Kod = rtp.RtP_IsEmriKodu 
                        AND rtp.RtP_SatirNo = 0
                    WHERE 
                     ie.is_EmriDurumu IN (0, 1)
                        AND rtp.RtP_PlanlananIsMerkezi = @IsMerkezi
                    ORDER BY ie.is_create_date DESC";

                try
                {
                    var isEmri = connection.QueryFirstOrDefault<string>(query,
                        new { StokKodu = stokkodu, IsMerkezi = isMerkezi });

                    if (string.IsNullOrEmpty(isEmri))
                    {
                        _logger.LogWarning($"Stok kodu '{stokkodu}' ve iş merkezi '{isMerkezi}' için aktif iş emri bulunamadı.");
                        return null;
                    }

                    _logger.LogInformation($"Stok kodu '{stokkodu}' ve iş merkezi '{isMerkezi}' için bulunan iş emri: {isEmri}");
                    return isEmri;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Stok kodu '{stokkodu}' ve iş merkezi '{isMerkezi}' için iş emri aranırken hata oluştu.");
                    throw;
                }
            }
        }

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

        // GÜNCELLEME: Açıklama parametresi eklendi - Artık ERP veritabanında ayrı tabloda
        public void BarkodHataliOlarakIsaretle(string barkod, string aciklama, string userNo)
        {
            // 1. Ana veritabanında bar_special3'ü güncelle
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

            // 2. ERP veritabanında açıklamayı kaydet
            HataliAciklamaKaydet(barkod, aciklama, userNo);
        }

        // YENİ: Hatalıyı kaldırma metodu - ERP veritabanındaki açıklamayı da siler
        public void BarkodHataliyiKaldir(string barkod)
        {
            // 1. Ana veritabanında bar_special3'ü temizle
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
                    UPDATE BARKOD_TANIMLARI 
                    SET bar_special3 = NULL
                    WHERE bar_kodu = @Barkod";
                var parameters = new { Barkod = barkod };
                try
                {
                    connection.Execute(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Barkod hatalı durumu kaldırılırken hata oluştu.");
                    throw;
                }
            }

            // 2. ERP veritabanındaki açıklamayı sil (soft delete)
            HataliAciklamaSil(barkod);
        }

        // YENİ: ERP veritabanına açıklama kaydet
        private void HataliAciklamaKaydet(string barkod, string aciklama, string userNo)
        {
            var erpConnectionString = _dbSelectorService.GetERPConnectionString();
            if (string.IsNullOrEmpty(erpConnectionString))
            {
                _logger.LogError("ERPDatabase connection string bulunamadı");
                throw new Exception("ERPDatabase connection string bulunamadı");
            }

            using (var connection = new SqlConnection(erpConnectionString))
            {
                // Önce var mı kontrol et
                var checkQuery = @"
                    SELECT COUNT(*) 
                    FROM BARKOD_HATALI_ACIKLAMALAR 
                    WHERE bar_kodu = @BarkodKodu AND aktif = 1";

                var exists = connection.ExecuteScalar<int>(checkQuery, new { BarkodKodu = barkod }) > 0;

                if (exists)
                {
                    // Varsa güncelle
                    var updateQuery = @"
                        UPDATE BARKOD_HATALI_ACIKLAMALAR
                        SET aciklama = @Aciklama,
                            guncellenme_tarihi = GETDATE()
                        WHERE bar_kodu = @BarkodKodu
                        AND aktif = 1";

                    connection.Execute(updateQuery, new { BarkodKodu = barkod, Aciklama = aciklama });
                    _logger.LogInformation($"Barkod {barkod} için açıklama güncellendi");
                }
                else
                {
                    // Yoksa ekle
                    var insertQuery = @"
                        INSERT INTO BARKOD_HATALI_ACIKLAMALAR (bar_kodu, aciklama, olusturan_user)
                        VALUES (@BarkodKodu, @Aciklama, @KullaniciNo)";

                    connection.Execute(insertQuery, new { BarkodKodu = barkod, Aciklama = aciklama, KullaniciNo = userNo });
                    _logger.LogInformation($"Barkod {barkod} için yeni açıklama eklendi");
                }
            }
        }

        // YENİ: ERP veritabanından açıklama sil (soft delete)
        private void HataliAciklamaSil(string barkod)
        {
            var erpConnectionString = _dbSelectorService.GetERPConnectionString();
            if (string.IsNullOrEmpty(erpConnectionString))
            {
                _logger.LogWarning("ERPDatabase connection string bulunamadı, açıklama silinemedi");
                return; // Hata fırlatma, sadece uyar
            }

            using (var connection = new SqlConnection(erpConnectionString))
            {
                var sqlQuery = @"
                    UPDATE BARKOD_HATALI_ACIKLAMALAR
                    SET aktif = 0,
                        guncellenme_tarihi = GETDATE()
                    WHERE bar_kodu = @BarkodKodu
                    AND aktif = 1";

                try
                {
                    var affectedRows = connection.Execute(sqlQuery, new { BarkodKodu = barkod });
                    if (affectedRows > 0)
                    {
                        _logger.LogInformation($"Barkod {barkod} için açıklama silindi");
                    }
                    else
                    {
                        _logger.LogWarning($"Barkod {barkod} için silinecek açıklama bulunamadı");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Barkod {barkod} için açıklama silinirken hata oluştu");
                    // Hata fırlatma, sadece logla
                }
            }
        }

        // YENİ: Barkod için açıklama getir
        public string GetHataliAciklama(string barkod)
        {
            var erpConnectionString = _dbSelectorService.GetERPConnectionString();
            if (string.IsNullOrEmpty(erpConnectionString))
            {
                _logger.LogWarning("ERPDatabase connection string bulunamadı");
                return string.Empty;
            }

            try
            {
                using (var connection = new SqlConnection(erpConnectionString))
                {
                    var sqlQuery = @"
                        SELECT TOP 1 aciklama
                        FROM BARKOD_HATALI_ACIKLAMALAR
                        WHERE bar_kodu = @BarkodKodu
                        AND aktif = 1
                        ORDER BY olusturma_tarihi DESC";

                    var aciklama = connection.QueryFirstOrDefault<string>(sqlQuery, new { BarkodKodu = barkod });
                    return aciklama ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Barkod {barkod} için açıklama alınırken hata oluştu");
                return string.Empty;
            }
        }

        public class PersonelBilgisi
        {
            public string Ad { get; set; }
            public string Soyad { get; set; }
        }

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

        public class GenisletilmisBarkodTanimi : BarkodTanimi
        {
            public string KullaniciNo { get; set; }
            public string PersonelAdi { get; set; }
            public string PersonelSoyadi { get; set; }
            public string HataliDurum { get; set; }
            public string HataliAciklama { get; set; }  // YENİ
            public string IsMerkezi { get; set; }
            public DateTime UretimTarihi { get; set; }
            public int SiraNo { get; set; }
            public int AyniLottanToplam { get; set; }
            public string StokAdi { get; set; }
            public string KisaIsim { get; set; }
            public string Miktar { get; set; }
            public string ModelKodu { get; set; }
        }

        // GÜNCELLEME: İş merkezi bazlı liste + hatalı durum filtresi + ERP'den açıklama getirme
        public IEnumerable<GenisletilmisBarkodTanimi> KullaniciBilgisiyleBarkodTanimlariniGetir(
            string userNo = null,
            DateTime? baslangicTarihi = null,
            DateTime? bitisTarihi = null,
            string isMerkezi = null,
            string hataliDurum = null)  // YENİ PARAMETRE
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            // Kullanıcının yetkili olduğu iş merkezlerini al
            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo ?? "");

            using (var connection = new SqlConnection(connectionString))
            {
                var sqlQuery = @"
WITH BarkodSiraNo AS (
    SELECT 
        b.bar_kodu,
        b.bar_partikodu,
        ROW_NUMBER() OVER (
            PARTITION BY b.bar_partikodu 
            ORDER BY b.bar_create_date
        ) AS SiraNo,
        COUNT(*) OVER (
            PARTITION BY b.bar_partikodu
        ) AS AyniLottanToplam
    FROM BARKOD_TANIMLARI b
    WHERE b.bar_special1 = 'EMF'
)
SELECT DISTINCT
    b.bar_kodu, 
    b.bar_stokkodu, 
    b.bar_partikodu, 
    b.bar_lotno,
    ISNULL(pl.pl_kod5, '') AS Miktar,
    b.bar_special2 AS KullaniciNo,
    b.bar_special3 AS HataliDurum,
    '' AS HataliAciklama,
    p.per_adi AS PersonelAdi,
    p.per_soyadi AS PersonelSoyadi,
    s.sto_isim AS StokAdi,
    s.sto_kisa_ismi AS KisaIsim,
    s.sto_model_kodu AS ModelKodu,
    ISNULL(rtp.RtP_PlanlananIsMerkezi, '') AS IsMerkezi,
    b.bar_create_date AS UretimTarihi,
    bs.SiraNo,
    bs.AyniLottanToplam
FROM BARKOD_TANIMLARI b
LEFT JOIN PERSONELLER p 
    ON b.bar_special2 = p.per_userno 
   AND b.bar_special2 <> ''
LEFT JOIN STOKLAR s 
    ON b.bar_stokkodu = s.sto_kod
LEFT JOIN PARTILOT pl 
    ON b.bar_partikodu = pl.pl_partikodu 
   AND b.bar_lotno = pl.pl_lotno
   AND b.bar_stokkodu = pl.pl_stokkodu
LEFT JOIN (
    SELECT RtP_UrunKodu, RtP_PlanlananIsMerkezi
    FROM (
        SELECT 
            RtP_UrunKodu,
            RtP_PlanlananIsMerkezi,
            ROW_NUMBER() OVER (
                PARTITION BY RtP_UrunKodu
                ORDER BY RtP_lastup_date DESC, RtP_SatirNo
            ) AS rn
        FROM URETIM_ROTA_PLANLARI
        WHERE RtP_SatirNo = 0
    ) x
    WHERE rn = 1
) rtp
    ON b.bar_stokkodu = rtp.RtP_UrunKodu
LEFT JOIN BarkodSiraNo bs 
    ON b.bar_kodu = bs.bar_kodu
WHERE 1 = 1";

                var conditions = new List<string>();
                var parameters = new DynamicParameters();

                // İŞ MERKEZİ YETKİSİ KONTROLÜ - Kullanıcının yetkili olduğu iş merkezlerindeki tüm üretimleri göster
                if (!string.IsNullOrEmpty(isMerkezi))
                {
                    // Kullanıcı özellikle bir merkez seçtiyse
                    conditions.Add("rtp.RtP_PlanlananIsMerkezi = @IsMerkezi");
                    parameters.Add("@IsMerkezi", isMerkezi);
                }
                else if (yetkiliIsMerkezleri.Any())
                {
                    // Seçim yoksa → yetkili olduğu tüm merkezler
                    conditions.Add("rtp.RtP_PlanlananIsMerkezi IN @YetkiliIsMerkezleri");
                    parameters.Add("@YetkiliIsMerkezleri", yetkiliIsMerkezleri);
                }

                if (baslangicTarihi.HasValue)
                {
                    conditions.Add("b.bar_create_date >= @BaslangicTarihi");
                    parameters.Add("@BaslangicTarihi", baslangicTarihi.Value);
                }

                if (bitisTarihi.HasValue)
                {
                    conditions.Add("b.bar_create_date <= @BitisTarihi");
                    parameters.Add("@BitisTarihi", bitisTarihi.Value.AddDays(1).AddSeconds(-1));
                }

                if (!string.IsNullOrEmpty(isMerkezi))
                {
                    conditions.Add("rtp.RtP_PlanlananIsMerkezi = @IsMerkezi");
                    parameters.Add("@IsMerkezi", isMerkezi);
                }

                // YENİ: Hatalı durum filtresi
                if (!string.IsNullOrEmpty(hataliDurum))
                {
                    if (hataliDurum == "hatali")
                    {
                        conditions.Add("b.bar_special3 = '001'");
                    }
                    else if (hataliDurum == "normal")
                    {
                        conditions.Add("(b.bar_special3 IS NULL OR b.bar_special3 <> '001')");
                    }
                }

                if (conditions.Count > 0)
                {
                    sqlQuery += " AND " + string.Join(" AND ", conditions);
                }

                sqlQuery += " ORDER BY b.bar_create_date DESC, b.bar_kodu DESC";

                try
                {
                    var results = connection.Query<GenisletilmisBarkodTanimi>(sqlQuery, parameters).ToList();

                    // Hatalı olanlar için ERP veritabanından açıklamaları al
                    var hataliKayitlar = results.Where(x => x.HataliDurum == "001").ToList();
                    if (hataliKayitlar.Any())
                    {
                        var erpConnectionString = _dbSelectorService.GetERPConnectionString();
                        if (!string.IsNullOrEmpty(erpConnectionString))
                        {
                            using (var erpConnection = new SqlConnection(erpConnectionString))
                            {
                                var barkodlar = hataliKayitlar.Select(x => x.bar_kodu).ToList();

                                var aciklamaQuery = @"
                                    SELECT bar_kodu, aciklama
                                    FROM BARKOD_HATALI_ACIKLAMALAR
                                    WHERE bar_kodu IN @Barkodlar
                                    AND aktif = 1";

                                var aciklamalar = erpConnection.Query<(string bar_kodu, string aciklama)>(
                                    aciklamaQuery,
                                    new { Barkodlar = barkodlar }
                                ).ToDictionary(x => x.bar_kodu, x => x.aciklama);

                                // Açıklamaları kayıtlara ekle
                                foreach (var kayit in hataliKayitlar)
                                {
                                    if (aciklamalar.TryGetValue(kayit.bar_kodu, out var aciklama))
                                    {
                                        kayit.HataliAciklama = aciklama;
                                    }
                                }
                            }
                        }
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kullanıcı bilgisiyle barkod tanımları getirilirken hata oluştu.");
                    throw;
                }
            }
        }

        public GenisletilmisBarkodTanimi GetBarkodBilgileri(string barkod)
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
                ISNULL(pl.pl_kod5, '') AS Miktar,
                b.bar_special2 AS KullaniciNo,
                b.bar_special3 AS HataliDurum,
                '' AS HataliAciklama,
                p.per_adi AS PersonelAdi,
                p.per_soyadi AS PersonelSoyadi,
                s.sto_isim AS StokAdi,
                s.sto_kisa_ismi AS KisaIsim,
                s.sto_model_kodu AS ModelKodu,
                ISNULL(rtp.RtP_PlanlananIsMerkezi, '') AS IsMerkezi,
                b.bar_create_date AS UretimTarihi
            FROM BARKOD_TANIMLARI b
            LEFT JOIN PERSONELLER p ON b.bar_special2 = p.per_userno
            LEFT JOIN STOKLAR s ON b.bar_stokkodu = s.sto_kod
            LEFT JOIN PARTILOT pl 
                ON b.bar_partikodu = pl.pl_partikodu 
               AND b.bar_lotno = pl.pl_lotno
               AND b.bar_stokkodu = pl.pl_stokkodu
            LEFT JOIN URETIM_ROTA_PLANLARI rtp ON b.bar_stokkodu = rtp.RtP_UrunKodu 
                AND rtp.RtP_SatirNo = 0
            WHERE b.bar_kodu = @Barkod";

                try
                {
                    var result = connection.QueryFirstOrDefault<GenisletilmisBarkodTanimi>(sqlQuery, new { Barkod = barkod });

                    // Eğer hatalı ise ERP'den açıklamayı getir
                    if (result != null && result.HataliDurum == "001")
                    {
                        result.HataliAciklama = GetHataliAciklama(barkod);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Barkod bilgileri getirilirken hata oluştu.");
                    throw;
                }
            }
        }

        public List<string> GetKullaniciIsMerkezleri(string userNo)
        {
            if (string.IsNullOrEmpty(userNo))
            {
                _logger.LogWarning("GetKullaniciIsMerkezleri: UserNo boş veya null");
                return new List<string>();
            }

            try
            {
                var erpConnectionString = _dbSelectorService.GetERPConnectionString();
                if (string.IsNullOrEmpty(erpConnectionString))
                {
                    _logger.LogError("ERPDatabase connection string bulunamadı");
                    return new List<string>();
                }

                using (var connection = new SqlConnection(erpConnectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT IsMerkezleri 
                        FROM KullaniciYonetimi 
                        WHERE User_no = @UserNo";

                    var isMerkezleriStr = connection.QueryFirstOrDefault<string>(query, new { UserNo = userNo });

                    if (string.IsNullOrEmpty(isMerkezleriStr))
                    {
                        _logger.LogInformation($"Kullanıcı {userNo} için iş merkezi yetkisi bulunamadı");
                        return new List<string>();
                    }

                    var result = isMerkezleriStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    _logger.LogInformation($"Kullanıcı {userNo} için {result.Count} iş merkezi yetkisi bulundu: {string.Join(", ", result)}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kullanıcı {userNo} için iş merkezi yetkileri alınırken hata oluştu");
                return new List<string>();
            }
        }

        public string GetIsEmriIsMerkezi(string isEmriKodu, string stokKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
                    SELECT TOP 1 rtp.RtP_PlanlananIsMerkezi
                    FROM URETIM_ROTA_PLANLARI rtp
                    WHERE rtp.RtP_IsEmriKodu = @IsEmriKodu 
                        AND rtp.RtP_UrunKodu = @StokKodu 
                        AND rtp.RtP_SatirNo = 0";

                try
                {
                    var isMerkezi = connection.QueryFirstOrDefault<string>(query,
                        new { IsEmriKodu = isEmriKodu, StokKodu = stokKodu });

                    _logger.LogInformation($"İş emri {isEmriKodu} - Stok {stokKodu} için iş merkezi: {isMerkezi ?? "Bulunamadı"}");
                    return isMerkezi ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"İş emri {isEmriKodu} için iş merkezi alınırken hata oluştu");
                    return string.Empty;
                }
            }
        }

        public bool KullaniciIsMerkeziYetkisiVarMi(string userNo, string isMerkezi)
        {
            if (string.IsNullOrEmpty(userNo) || string.IsNullOrEmpty(isMerkezi))
            {
                return false;
            }

            var yetkiliIsMerkezleri = GetKullaniciIsMerkezleri(userNo);

            if (!yetkiliIsMerkezleri.Any())
            {
                return true;
            }

            return yetkiliIsMerkezleri.Contains(isMerkezi);
        }
    }
}