using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Dapper;
using Deneme_proje.Models;
using static Deneme_proje.Models.CrmEntities;
using Microsoft.Extensions.Configuration;  // ✅ System.Configuration DEĞIL, Microsoft.Extensions.Configuration

namespace Deneme_proje.Repository
{
    public class CrmRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _context;
      

        // ✅ DOĞRU CONSTRUCTOR
        public CrmRepository(DatabaseSelectorService dbSelectorService, IConfiguration configuration, IHttpContextAccessor context)
        {
            _dbSelectorService = dbSelectorService;
            _configuration = configuration;  // ✅ PARAMETRE'den alıyor
            _context = context;
        
        }

        private string ConnectionString => _dbSelectorService.GetConnectionString();
        private string ErpConnectionString => _configuration.GetConnectionString("ERPDatabase");
        #region Teklifler

        // Teklif listesi getir


        // Dashboard istatistikleri için de aynı mapping
        public TeklifIstatistikleri GetTeklifIstatistikleri()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
        SELECT 
            COUNT(DISTINCT tkl_evrakno_sira) as ToplamTeklif,
            COUNT(DISTINCT CASE WHEN ISNULL(tkl_durumu, '0') IN ('0', '1') THEN tkl_evrakno_sira END) as AcikTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '2' THEN tkl_evrakno_sira END) as KazanilanTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '3' THEN tkl_evrakno_sira END) as KaybedilenTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '4' THEN tkl_evrakno_sira END) as ErtelelenTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '5' THEN tkl_evrakno_sira END) as IptalEdilenTeklifler,
            SUM(tkl_Alisfiyati) as ToplamTutar
        FROM VERILEN_TEKLIFLER
        WHERE (
            CASE 
                WHEN ISDATE(tkl_evrak_tarihi) = 1 
                THEN CAST(tkl_evrak_tarihi AS DATETIME)
                ELSE tkl_create_date
            END >= DATEADD(MONTH, -12, GETDATE())
        )";

            return connection.QuerySingle<TeklifIstatistikleri>(query);
        }

        // Teklif detay getir
        //public TeklifDetayModel GetTeklifDetay(int evrakSiraNo)
        //{
        //    using var connection = new SqlConnection(ConnectionString);

        //    var teklifQuery = @"
        //SELECT 
        //    vt.tkl_evrakno_sira,
        //    MIN(vt.tkl_cari_kod) as tkl_cari_kod,
        //    MIN(vt.tkl_evrak_tarihi) as tkl_evrak_tarihi,
        //    MIN(vt.tkl_belge_no) as tkl_belge_no,
        //    MIN(vt.tkl_Gecerlilik_Sures) as tkl_Gecerlilik_Sures,
        //    MIN(vt.tkl_Sorumlu_Kod) as tkl_Sorumlu_Kod,
        //    MIN(vt.tkl_Aciklama) as tkl_Aciklama,
        //    MIN(vt.tkl_durumu) as tkl_durumu,
        //    SUM(vt.tkl_Alisfiyati) as tkl_Alisfiyati,
        //    MIN(ch.cari_unvan1) as CariAdi,
        //    MIN(cp.cari_per_adi + ' ' + cp.cari_per_soyadi) as HazirlayanAdi
        //FROM VERILEN_TEKLIFLER vt
        //LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
        //LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON vt.tkl_Sorumlu_Kod = cp.cari_per_kod
        //WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
        //GROUP BY vt.tkl_evrakno_sira;

        //SELECT 
        //    vt.tkl_stok_kod as StokKod,
        //    s.sto_isim as StokAdi,
        //    vt.tkl_miktar as Miktar,
        //    vt.tkl_Alisfiyati as BirimFiyat,
        //    vt.tkl_Alisfiyati as IndirimliFiyat -- Adjust if you have a separate discount field
        //FROM VERILEN_TEKLIFLER vt
        //LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
        //WHERE vt.tkl_evrakno_sira = @EvrakSiraNo";

        //    using var multi = connection.QueryMultiple(teklifQuery, new { EvrakSiraNo = evrakSiraNo });
        //    var teklif = multi.ReadSingleOrDefault<TeklifDetayModel>();
        //    if (teklif != null)
        //    {
        //        teklif.Urunler = multi.Read<TeklifUrunModel>().ToList();
        //    }

        //    return teklif;
        //}

        // CrmRepository.cs - Sadece değişen metodlar

        // ✅ DÜZELTME: KDV sistemi eklendi
        // tkl_Aciklama → Teklif konusu (tüm satırlar için aynı)
        // tkl_special1 → Satır açıklaması (her satır için ayrı)
        // tkl_special2 → KDV oranı (0, 1, 10, 20, 30...) ⭐
        // tkl_vergi_pntr → KDV pointer (4 sabit)
        // tkl_vergi → KDV tutarı (hesaplanan)

        // YeniTeklifKaydet metodundaki INSERT sorgusunu değiştirin:
        //        public bool YeniTeklifKaydet(YeniTeklifModel teklif)
        //        {
        //            using var connection = new SqlConnection(ConnectionString);
        //            try
        //            {
        //                connection.Open();
        //                using var transaction = connection.BeginTransaction();

        //                var evrakSiraQuery = "SELECT ISNULL(MAX(tkl_evrakno_sira), 0) + 1 FROM VERILEN_TEKLIFLER";
        //                var yeniEvrakSira = connection.QuerySingle<int>(evrakSiraQuery, transaction: transaction);

        //                var durumKodu = "0";
        //                if (!int.TryParse(teklif.CreateUser, out int createUserId))
        //                {
        //                    createUserId = 1;
        //                }

        //                var evrakTarihi = teklif.Tarih.ToString("yyyy-MM-dd");
        //                var baslangicTarihi = teklif.BaslangicTarihi.ToString("yyyy-MM-dd");
        //                var bitisTarihi = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("yyyy-MM-dd");

        //                // ✅ ANA TABLO INSERT QUERY
        //                var teklifQuery = @"
        //INSERT INTO VERILEN_TEKLIFLER (
        //    tkl_Guid, tkl_DBCno, tkl_SpecRECno, tkl_iptal, tkl_fileid, 
        //    tkl_hidden, tkl_kilitli, tkl_degisti, tkl_checksum, tkl_create_user, 
        //    tkl_create_date, tkl_lastup_user, tkl_lastup_date, tkl_special1, tkl_special2, 
        //    tkl_special3, tkl_firmano, tkl_subeno, tkl_stok_kod, tkl_cari_kod, 
        //    tkl_evrakno_seri, tkl_evrakno_sira, tkl_evrak_tarihi, tkl_satirno, tkl_belge_no, 
        //    tkl_belge_tarih, tkl_asgari_miktar, tkl_teslimat_suresi, tkl_baslangic_tarihi, tkl_Gecerlilik_Sures,
        //    tkl_Brut_fiyat, tkl_Odeme_Plani, tkl_Alisfiyati, tkl_karorani, tkl_miktar,
        //    tkl_Aciklama, tkl_doviz_cins, tkl_doviz_kur, tkl_alt_doviz_kur, tkl_iskonto1,
        //    tkl_iskonto2, tkl_iskonto3, tkl_iskonto4, tkl_iskonto5, tkl_iskonto6,
        //    tkl_masraf1, tkl_masraf2, tkl_masraf3, tkl_masraf4, tkl_vergi_pntr,
        //    tkl_vergi, tkl_masraf_vergi_pnt, tkl_masraf_vergi, tkl_isk_mas1, TKL_ISK_MAS2,
        //    TKL_ISK_MAS3, TKL_ISK_MAS4, TKL_ISK_MAS5, TKL_ISK_MAS6, TKL_ISK_MAS7,
        //    TKL_ISK_MAS8, TKL_ISK_MAS9, TKL_ISK_MAS10, TKL_SAT_ISKMAS1, TKL_SAT_ISKMAS2,
        //    TKL_SAT_ISKMAS3, TKL_SAT_ISKMAS4, TKL_SAT_ISKMAS5, TKL_SAT_ISKMAS6, TKL_SAT_ISKMAS7,
        //    TKL_SAT_ISKMAS8, TKL_SAT_ISKMAS9, TKL_SAT_ISKMAS10, TKL_VERGISIZ_FL, TKL_KAPAT_FL,
        //    TKL_TESLIMTURU, tkl_ProjeKodu, tkl_Sorumlu_Kod, tkl_adres_no, tkl_yetkili_uid,
        //    tkl_durumu, tkl_TedarikEdilecekCari, tkl_fiyat_liste_no, tkl_Birimfiyati,
        //    tkl_paket_kod, tkl_teslim_miktar, tkl_OnaylayanKulNo, tkl_cagrilabilir_fl,
        //    tkl_harekettipi, tkl_cari_sormerk, tkl_stok_sormerk, tkl_kapatmanedenkod,
        //    tkl_servisisemrikodu, tkl_birim_pntr, tkl_cari_tipi, tkl_HareketGrupKodu1,
        //    tkl_HareketGrupKodu2, tkl_HareketGrupKodu3, tkl_Olcu1, tkl_Olcu2,
        //    tkl_Olcu3, tkl_Olcu4, tkl_Olcu5, tkl_FormulMiktarNo, tkl_FormulMiktar,
        //    tkl_Tevkifat_turu, tkl_tevkifat_sifirlandi_fl
        //) VALUES (
        //    @TeklifGuid, 0, 0, 0, 100, 
        //    0, 0, 0, 0, @CreateUserId, 
        //    GETDATE(), @CreateUserId, GETDATE(), '', @KdvOrani, 
        //    '', 0, 0, @StokKod, @CariKod, 
        //    '', @EvrakSira, @EvrakTarihi, @SatirNo, @BelgeNo, 
        //    NULL, 0, 0, @BaslangicTarihi, @BitisTarihi,
        //    @ListeFiyat, 0, @TeklifFiyat, 0, @Miktar,
        //    @TeklifKonusu, '', 1, 0, 0,
        //    0, 0, 0, 0, 0,
        //    0, 0, 0, 0, 4,
        //    @KdvTutari, 0, 0, 0, 1,
        //    1, 1, 1, 1, 1,
        //    1, 1, 1, 0, 0,
        //    0, 0, 0, 0, 0,
        //    0, 0, 0, 0, 0,
        //    '', '', @SorumluKod, 1, '00000000-0000-0000-0000-000000000000',
        //    @DurumKodu, '', 0, 0,
        //    '', 0, 0, 0,
        //    @BirimPntr, '', '', '', '',
        //    @BirimPntr, 0, '', '', '',
        //    0, 0, 0, 0, 0,
        //    0, 0, 0, 0
        //)";

        //                // ✅ USER TABLOSUNA SATIR AÇIKLAMASI KAYIT QUERY
        //                var userTableQuery = @"
        //INSERT INTO VERILEN_TEKLIFLER_USER (
        //    Record_uid,
        //    Satir_Aciklama
        //) VALUES (
        //    @RecordUid,
        //    @SatirAciklama
        //)";

        //                for (int i = 0; i < teklif.Urunler.Count; i++)
        //                {
        //                    var urun = teklif.Urunler[i];
        //                    var teklifGuid = Guid.NewGuid();  // ✅ Bu GUID hem ana tabloda hem user tablosunda kullanılacak

        //                    decimal listeFiyat = urun.BirimFiyat;
        //                    decimal teklifFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat;

        //                    // ✅ KDV hesaplama
        //                    int kdvOrani = urun.KdvOrani;
        //                    decimal kdvTutari = (teklifFiyat * urun.Miktar * kdvOrani) / 100;

        //                    Console.WriteLine($"Ürün {i}: {urun.StokKod} - Liste={listeFiyat:N2}, Teklif={teklifFiyat:N2}, Miktar={urun.Miktar}, KDV%{kdvOrani}={kdvTutari:N2}, Açıklama={urun.Aciklama}");

        //                    // ✅ 1. ANA TABLOYA KAYDET
        //                    connection.Execute(teklifQuery, new
        //                    {
        //                        TeklifGuid = teklifGuid,
        //                        CreateUserId = createUserId,
        //                        KdvOrani = kdvOrani.ToString(),            // ✅ tkl_special2: KDV oranı
        //                        StokKod = urun.StokKod ?? "",
        //                        CariKod = teklif.CariKod ?? "",
        //                        EvrakSira = yeniEvrakSira,
        //                        EvrakTarihi = evrakTarihi,
        //                        SatirNo = i,
        //                        BelgeNo = teklif.FormNo ?? "",
        //                        BaslangicTarihi = baslangicTarihi,
        //                        BitisTarihi = bitisTarihi,
        //                        ListeFiyat = listeFiyat,
        //                        TeklifFiyat = teklifFiyat,
        //                        Miktar = urun.Miktar,
        //                        TeklifKonusu = teklif.Aciklama ?? "",      // ✅ tkl_Aciklama: Teklif konusu (tüm satırlar için aynı)
        //                        KdvTutari = kdvTutari,                     // ✅ tkl_vergi: KDV tutarı
        //                        SorumluKod = teklif.SorumluKod ?? "",
        //                        DurumKodu = durumKodu,
        //                        BirimPntr = 1
        //                    }, transaction: transaction);

        //                    // ✅ 2. SATIR AÇIKLAMASI VARSA USER TABLOSUNA KAYDET
        //                    if (!string.IsNullOrWhiteSpace(urun.Aciklama))
        //                    {
        //                        connection.Execute(userTableQuery, new
        //                        {
        //                            RecordUid = teklifGuid,                // ✅ Ana tablodaki tkl_Guid ile eşleşecek
        //                            SatirAciklama = urun.Aciklama
        //                        }, transaction: transaction);

        //                        Console.WriteLine($"  → Satır açıklaması kaydedildi: {urun.Aciklama}");
        //                    }
        //                }

        //                transaction.Commit();
        //                Console.WriteLine($"✅ Teklif başarıyla kaydedildi!");
        //                Console.WriteLine($"   Evrak Sıra: {yeniEvrakSira}");
        //                Console.WriteLine($"   Ürün Sayısı: {teklif.Urunler.Count}");
        //                Console.WriteLine($"   Açıklamalı Satır: {teklif.Urunler.Count(u => !string.IsNullOrWhiteSpace(u.Aciklama))}");
        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"❌ Teklif kaydetme hatası: {ex.Message}");
        //                if (ex.InnerException != null)
        //                {
        //                    Console.WriteLine($"❌ İç Hata: {ex.InnerException.Message}");
        //                }
        //                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        //                return false;
        //            }
        //        }


        public async Task<MikroApiResponse> YeniTeklifKaydet(YeniTeklifModel teklif)
        {
            try
            {
                Console.WriteLine("=== API ile teklif kaydediliyor ===");

                // ✅ 1. API ayarlarını ERP veritabanından al
                MikroApiAyarlari apiAyarlari;
                using (var erpConnection = new SqlConnection(ErpConnectionString))
                {
                    var query = @"
                SELECT TOP 1 
                    id as Id,
                    mikro_sifre as MikroSifre,
                    aktif as Aktif
           
                FROM MikroApiAyarlari 
                ORDER BY id DESC";

                    apiAyarlari = erpConnection.QueryFirstOrDefault<MikroApiAyarlari>(query);
                    Console.WriteLine($"✅ API ayarları ERP'den alındı - Aktif: {apiAyarlari?.Aktif}");
                }

                if (apiAyarlari == null || !apiAyarlari.Aktif)
                {
                    return new MikroApiResponse
                    {
                        Success = false,
                        Message = "Mikro API ayarları aktif değil.",
                        StatusCode = 503
                    };
                }

                // ✅ 2. appsettings.json'dan API ayarlarını al
                var apiKey = _configuration["MikroApi:ApiKey"];
                var kullaniciKodu = _configuration["MikroApi:KullaniciKodu"];
                var baseUrlTemplate = _configuration["MikroApi:BaseUrl"];
                var endpoint = _configuration["MikroApi:Endpoints:VerilenTeklifKaydet"];

                // ✅ 3. Connection string'den IP'yi parse et
                var connectionString = _configuration.GetConnectionString("DynamicDatabase");
                string apiServerIp = ParseServerIpFromConnectionString(connectionString);

                // API URL'ini oluştur
                string baseUrl = string.Format(baseUrlTemplate, apiServerIp);
                string apiUrl = baseUrl + endpoint;

                Console.WriteLine($"📡 API URL: {apiUrl}");
                Console.WriteLine($"🖥️ Server IP: {apiServerIp}");
                Console.WriteLine($"👤 Kullanıcı: {kullaniciKodu}");

                // ✅ 4. Şifreyi MD5'le şifrele
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string combined = today + " " + apiAyarlari.MikroSifre;
                string encryptedPassword;

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    encryptedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }

                Console.WriteLine($"🔐 MD5 Şifre: {encryptedPassword}");

                // ✅ 5. Firma kodunu seçilen veritabanı adından al
                var selectedDb = _context.HttpContext.Session.GetString("SelectedDatabase");

                // Eğer boşsa fallback değeri ver
                if (string.IsNullOrEmpty(selectedDb))
                    selectedDb = "MikroDesktop_TEST"; // varsayılan

                // Firma kodunu veritabanı adından çıkar
                string firmaKodu = selectedDb.Contains("_")
                    ? selectedDb.Split('_').Last()
                    : selectedDb;

                Console.WriteLine($"🏢 Firma Kodu (DB’den): {firmaKodu}");


                // ✅ 6. Evrak ve satırları oluştur
                var evrak = new Evrak
                {
                    evrak_aciklamalari = new List<EvrakAciklama>(),
                    satirlar = new List<Satir>()
                };

                foreach (var urun in teklif.Urunler)
                {
                    decimal teklifFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat;
                    decimal kdvTutari = (teklifFiyat * urun.Miktar * urun.KdvOrani) / 100;

                    var satir = new Satir
                    {
                        tkl_evrak_tarihi = teklif.Tarih.ToString("dd.MM.yyyy"),
                        tkl_evrakno_seri = "",
                        tkl_belge_no = teklif.FormNo,
                        tkl_cari_kod = teklif.CariKod,
                        tkl_cari_tipi = "0",
                        tkl_cari_sormerk = "",
                        tkl_stok_kod = urun.StokKod,
                        tkl_stok_sormerk = "",
                        tkl_Aciklama = urun.Aciklama ?? "",
                        tkl_Alisfiyati = teklifFiyat,
                        tkl_Brut_fiyat = urun.BirimFiyat,
                        tkl_miktar = urun.Miktar,
                        tkl_baslangic_tarihi = teklif.BaslangicTarihi.ToString("dd.MM.yyyy"),
                        tkl_Gecerlilik_Sures = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("dd.MM.yyyy"),
                        tkl_special2 = urun.KdvOrani.ToString(),
                        tkl_vergi = kdvTutari,
                        tkl_birim_pntr = 1,
                        tkl_vergi_pntr = 4,
                        tkl_harekettipi = 0,
                        tkl_karorani = 0,
                        tkl_ProjeKodu = ""
                    };

                    evrak.satirlar.Add(satir);
                }

                // ✅ 7. API Request oluştur
                var apiRequest = new MikroApiRequest
                {
                    Mikro = new MikroApiData
                    {
                        FirmaKodu = firmaKodu,
                        CalismaYili = DateTime.Now.Year.ToString(),
                        ApiKey = apiKey,
                        KullaniciKodu = kullaniciKodu,
                        Sifre = encryptedPassword,
                        evraklar = new List<Evrak> { evrak }
                    }
                };

                // ✅ 8. API'ye gönder
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Console.WriteLine($"📤 JSON (ilk 500 karakter): {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");

                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 API Response Status: {response.StatusCode}");
                Console.WriteLine($"📥 API Response: {responseContent}");

                return new MikroApiResponse
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Başarılı" : $"Hata: {response.StatusCode}",
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hata: {ex.Message}");
                return new MikroApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 500
                };
            }
        }

        private string ParseServerIpFromConnectionString(string connectionString)
        {
            try
            {
                // "Server=192.168.2.100;Database=..." formatından IP'yi çıkar
                var parts = connectionString.Split(';');
                var serverPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase));

                if (serverPart != null)
                {
                    var server = serverPart.Split('=')[1].Trim();

                    // Eğer "SERVER\INSTANCE" formatındaysa, sadece IP'yi al
                    if (server.Contains('\\'))
                    {
                        server = server.Split('\\')[0];
                    }

                    Console.WriteLine($"🔍 Connection string'den parse edilen IP: {server}");
                    return server;
                }

                Console.WriteLine("⚠️ Connection string'de Server bulunamadı, varsayılan IP kullanılıyor");
                return "192.168.2.100"; // Varsayılan
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ IP parse hatası: {ex.Message}, varsayılan IP kullanılıyor");
                return "192.168.2.100"; // Varsayılan
            }
        }

        // ✅ TeklifGuncelle metodunu da aynı şekilde güncelleyin
        //        public bool TeklifGuncelle(int evrakSiraNo, YeniTeklifModel teklif)
        //        {
        //            using var connection = new SqlConnection(ConnectionString);

        //            try
        //            {
        //                connection.Open();
        //                using var transaction = connection.BeginTransaction();

        //                Console.WriteLine($"[TeklifGuncelle] Başlangıç - EvrakSira: {evrakSiraNo}");

        //                if (!int.TryParse(teklif.CreateUser, out int updateUserId))
        //                {
        //                    updateUserId = 1;
        //                }

        //                var evrakTarihi = teklif.Tarih.ToString("yyyy-MM-dd");
        //                var baslangicTarihi = teklif.BaslangicTarihi.ToString("yyyy-MM-dd");
        //                var bitisTarihi = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("yyyy-MM-dd");

        //                // Eski satırları sil
        //                var deleteQuery = @"DELETE FROM VERILEN_TEKLIFLER WHERE tkl_evrakno_sira = @EvrakSiraNo";
        //                var deletedCount = connection.Execute(deleteQuery, new { EvrakSiraNo = evrakSiraNo }, transaction: transaction);
        //                Console.WriteLine($"[TeklifGuncelle] {deletedCount} eski satır silindi");

        //                // ✅ USER TABLOSINDAN DA SİL
        //                var deleteUserQuery = @"
        //            DELETE FROM VERILEN_TEKLIFLER_USER 
        //            WHERE Record_uid IN (
        //                SELECT tkl_Guid FROM VERILEN_TEKLIFLER WHERE tkl_evrakno_sira = @EvrakSiraNo
        //            )";
        //                connection.Execute(deleteUserQuery, new { EvrakSiraNo = evrakSiraNo }, transaction: transaction);

        //                // ✅ ANA TABLO INSERT
        //                var insertQuery = @"
        //INSERT INTO VERILEN_TEKLIFLER (
        //    tkl_Guid, tkl_DBCno, tkl_SpecRECno, tkl_iptal, tkl_fileid, tkl_hidden, 
        //    tkl_kilitli, tkl_degisti, tkl_checksum, tkl_create_user, tkl_create_date, 
        //    tkl_lastup_user, tkl_lastup_date, tkl_durumu, tkl_special1, tkl_special2, tkl_special3,
        //    tkl_firmano, tkl_subeno, tkl_stok_kod, tkl_cari_kod, tkl_evrakno_seri,
        //    tkl_evrakno_sira, tkl_evrak_tarihi, tkl_satirno, tkl_belge_no, tkl_belge_tarih,
        //    tkl_asgari_miktar, tkl_teslimat_suresi, tkl_baslangic_tarihi, tkl_Gecerlilik_Sures,
        //    tkl_Brut_fiyat, tkl_Odeme_Plani, tkl_Alisfiyati, tkl_karorani, tkl_miktar,
        //    tkl_Aciklama, tkl_doviz_cins, tkl_doviz_kur, tkl_alt_doviz_kur, tkl_iskonto1,
        //    tkl_iskonto2, tkl_iskonto3, tkl_iskonto4, tkl_iskonto5, tkl_iskonto6,
        //    tkl_masraf1, tkl_masraf2, tkl_masraf3, tkl_masraf4, tkl_vergi_pntr,
        //    tkl_vergi, tkl_masraf_vergi_pnt, tkl_masraf_vergi, tkl_isk_mas1, TKL_ISK_MAS2,
        //    TKL_ISK_MAS3, TKL_ISK_MAS4, TKL_ISK_MAS5, TKL_ISK_MAS6, TKL_ISK_MAS7,
        //    TKL_ISK_MAS8, TKL_ISK_MAS9, TKL_ISK_MAS10, TKL_SAT_ISKMAS1, TKL_SAT_ISKMAS2,
        //    TKL_SAT_ISKMAS3, TKL_SAT_ISKMAS4, TKL_SAT_ISKMAS5, TKL_SAT_ISKMAS6, TKL_SAT_ISKMAS7,
        //    TKL_SAT_ISKMAS8, TKL_SAT_ISKMAS9, TKL_SAT_ISKMAS10, TKL_VERGISIZ_FL, TKL_KAPAT_FL,
        //    TKL_TESLIMTURU, tkl_ProjeKodu, tkl_Sorumlu_Kod, tkl_adres_no, tkl_yetkili_uid,
        //    tkl_TedarikEdilecekCari, tkl_fiyat_liste_no, tkl_Birimfiyati,
        //    tkl_paket_kod, tkl_teslim_miktar, tkl_OnaylayanKulNo, tkl_cagrilabilir_fl,
        //    tkl_harekettipi, tkl_cari_sormerk, tkl_stok_sormerk, tkl_kapatmanedenkod,
        //    tkl_servisisemrikodu, tkl_birim_pntr, tkl_cari_tipi, tkl_HareketGrupKodu1,
        //    tkl_HareketGrupKodu2, tkl_HareketGrupKodu3, tkl_Olcu1, tkl_Olcu2,
        //    tkl_Olcu3, tkl_Olcu4, tkl_Olcu5, tkl_FormulMiktarNo, tkl_FormulMiktar,
        //    tkl_Tevkifat_turu, tkl_tevkifat_sifirlandi_fl
        //) VALUES (
        //    @TeklifGuid, 0, 0, 0, 100, 0,
        //    0, 0, 0, @CreateUserId, GETDATE(),
        //    @UpdateUserId, GETDATE(), @DurumKodu, '', @KdvOrani, '',
        //    0, 0, @StokKod, @CariKod, '',
        //    @EvrakSira, @EvrakTarihi, @SatirNo, @BelgeNo, NULL,
        //    0, 0, @BaslangicTarihi, @BitisTarihi,
        //    @ListeFiyat, 0, @TeklifFiyat, 0, @Miktar,
        //    @TeklifKonusu, '', 1, 0, 0,
        //    0, 0, 0, 0, 0,
        //    0, 0, 0, 0, 4,
        //    @KdvTutari, 0, 0, 0, 1,
        //    1, 1, 1, 1, 1,
        //    1, 1, 1, 0, 0,
        //    0, 0, 0, 0, 0,
        //    0, '', @SorumluKod, 1, '00000000-0000-0000-0000-000000000000',
        //    '', 0, 0,
        //    '', 0, 0, 1,
        //    0, '', '', '', '',
        //    1, 0, '', '', '',
        //    0, 0, 0, 0, 0,
        //    0, 0, 0, 0
        //)";

        //                // ✅ USER TABLOSUNA SATIR AÇIKLAMASI KAYIT QUERY
        //                var userTableQuery = @"
        //INSERT INTO VERILEN_TEKLIFLER_USER (
        //    Record_uid,
        //    Satir_Aciklama
        //) VALUES (
        //    @RecordUid,
        //    @SatirAciklama
        //)";

        //                for (int i = 0; i < teklif.Urunler.Count; i++)
        //                {
        //                    var urun = teklif.Urunler[i];
        //                    var teklifGuid = Guid.NewGuid();  // ✅ Bu GUID hem ana tabloda hem user tablosunda kullanılacak

        //                    decimal listeFiyat = urun.BirimFiyat;
        //                    decimal teklifFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat;

        //                    // ✅ KDV hesaplama
        //                    int kdvOrani = urun.KdvOrani;
        //                    decimal kdvTutari = (teklifFiyat * urun.Miktar * kdvOrani) / 100;

        //                    Console.WriteLine($"Ürün {i}: {urun.StokKod} - Liste={listeFiyat:N2}, Teklif={teklifFiyat:N2}, Miktar={urun.Miktar}, KDV%{kdvOrani}={kdvTutari:N2}, Açıklama={urun.Aciklama}");

        //                    // ✅ 1. ANA TABLOYA KAYDET
        //                    connection.Execute(insertQuery, new
        //                    {
        //                        TeklifGuid = teklifGuid,
        //                        EvrakSira = evrakSiraNo,
        //                        EvrakTarihi = evrakTarihi,
        //                        BaslangicTarihi = baslangicTarihi,
        //                        BitisTarihi = bitisTarihi,
        //                        SatirNo = i,
        //                        BelgeNo = teklif.FormNo,
        //                        StokKod = urun.StokKod,
        //                        CariKod = teklif.CariKod,
        //                        ListeFiyat = listeFiyat,
        //                        TeklifFiyat = teklifFiyat,
        //                        Miktar = urun.Miktar,
        //                        TeklifKonusu = teklif.Aciklama ?? "",      // ✅ Teklif konusu (tüm satırlar için aynı)
        //                        KdvOrani = kdvOrani.ToString(),            // ✅ tkl_special2: KDV oranı (STRING)
        //                        KdvTutari = kdvTutari,                     // ✅ tkl_vergi: KDV tutarı
        //                        DurumKodu = "0",
        //                        CreateUserId = updateUserId,
        //                        UpdateUserId = updateUserId,
        //                        SorumluKod = teklif.SorumluKod ?? ""
        //                    }, transaction: transaction);

        //                    // ✅ 2. SATIR AÇIKLAMASI VARSA USER TABLOSUNA KAYDET
        //                    if (!string.IsNullOrWhiteSpace(urun.Aciklama))
        //                    {
        //                        connection.Execute(userTableQuery, new
        //                        {
        //                            RecordUid = teklifGuid,                // ✅ Ana tablodaki tkl_Guid ile eşleşecek
        //                            SatirAciklama = urun.Aciklama
        //                        }, transaction: transaction);

        //                        Console.WriteLine($"  → Satır açıklaması kaydedildi: {urun.Aciklama}");
        //                    }
        //                }

        //                transaction.Commit();
        //                Console.WriteLine($"✅ Teklif başarıyla güncellendi!");
        //                Console.WriteLine($"   Evrak Sıra: {evrakSiraNo}");
        //                Console.WriteLine($"   Ürün Sayısı: {teklif.Urunler.Count}");
        //                Console.WriteLine($"   Açıklamalı Satır: {teklif.Urunler.Count(u => !string.IsNullOrWhiteSpace(u.Aciklama))}");
        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"[TeklifGuncelle] HATA: {ex.Message}");
        //                if (ex.InnerException != null)
        //                {
        //                    Console.WriteLine($"❌ İç Hata: {ex.InnerException.Message}");
        //                }
        //                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        //                return false;
        //            }
        //        }

        // ============================================
        // ✅ 2. METOD: TeklifGuncelle
        // ============================================
        // ============================================
        // ✅ TeklifGuncelle - Mikro API ile Güncelleme
        // ============================================
        public async Task<MikroApiResponse> TeklifGuncelle(int evrakSiraNo, YeniTeklifModel teklif)
        {
            try
            {
                Console.WriteLine($"=== Teklif güncelleniyor: {evrakSiraNo} ===");

                // ✅ 1. Mevcut teklif satırlarının GUID'lerini al
                List<Guid> mevcutGuidler = new List<Guid>();
                using (var connection = new SqlConnection(ConnectionString))
                {
                    var guidQuery = @"
                SELECT tkl_Guid 
                FROM VERILEN_TEKLIFLER 
                WHERE tkl_evrakno_sira = @EvrakSiraNo
                ORDER BY tkl_satirno";

                    mevcutGuidler = connection.Query<Guid>(guidQuery, new { EvrakSiraNo = evrakSiraNo }).ToList();
                    Console.WriteLine($"✅ {mevcutGuidler.Count} mevcut satır GUID'i alındı");
                }

                // ✅ 2. API ayarlarını ERP veritabanından al
                MikroApiAyarlari apiAyarlari;
                using (var erpConnection = new SqlConnection(ErpConnectionString))
                {
                    var query = @"
                SELECT TOP 1 
                    id as Id,
                    mikro_sifre as MikroSifre,
                    aktif as Aktif
              
                FROM MikroApiAyarlari 
                ORDER BY id DESC";

                    apiAyarlari = erpConnection.QueryFirstOrDefault<MikroApiAyarlari>(query);
                    Console.WriteLine($"✅ API ayarları alındı - Aktif: {apiAyarlari?.Aktif}");
                }

                if (apiAyarlari == null || !apiAyarlari.Aktif)
                {
                    return new MikroApiResponse
                    {
                        Success = false,
                        Message = "Mikro API ayarları aktif değil.",
                        StatusCode = 503
                    };
                }

                // ✅ 3. appsettings.json'dan API ayarlarını al
                var apiKey = _configuration["MikroApi:ApiKey"];
                var kullaniciKodu = _configuration["MikroApi:KullaniciKodu"];
                var baseUrlTemplate = _configuration["MikroApi:BaseUrl"];
                var endpoint = "/VerilenTeklifDuzeltV2";  // ⬅️ DOĞRU ENDPOINT

                // ✅ 4. Connection string'den IP'yi parse et
                var connectionString = _configuration.GetConnectionString("DynamicDatabase");
                string apiServerIp = ParseServerIpFromConnectionString(connectionString);

                // API URL'ini oluştur
                string baseUrl = string.Format(baseUrlTemplate, apiServerIp);
                string apiUrl = baseUrl + endpoint;

                Console.WriteLine($"📡 API URL: {apiUrl}");

                // ✅ 5. Şifreyi MD5'le şifrele
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string combined = today + " " + apiAyarlari.MikroSifre;
                string encryptedPassword;

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    encryptedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }

                Console.WriteLine($"🔐 MD5 Şifre: {encryptedPassword}");

                // ✅ 6. Firma kodunu seçilen veritabanı adından al
                var selectedDb = _context.HttpContext.Session.GetString("SelectedDatabase");
                if (string.IsNullOrEmpty(selectedDb))
                    selectedDb = "MikroDesktop_TEST";

                string firmaKodu = selectedDb.Contains("_") ? selectedDb.Split('_').Last() : selectedDb;
                Console.WriteLine($"🏢 Firma Kodu: {firmaKodu}");

                // ✅ 7. Satırları oluştur - GUID ile eşleştir
                var evrak = new Evrak
                {
                    evrak_aciklamalari = new List<EvrakAciklama>(),
                    satirlar = new List<Satir>()
                };

                for (int i = 0; i < teklif.Urunler.Count; i++)
                {
                    var urun = teklif.Urunler[i];
                    decimal teklifFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat;
                    decimal kdvTutari = (teklifFiyat * urun.Miktar * urun.KdvOrani) / 100;

                    // ✅ Mevcut satır için GUID kullan, yeni satır için yeni GUID oluştur
                    Guid satirGuid = i < mevcutGuidler.Count ? mevcutGuidler[i] : Guid.NewGuid();

                    var satir = new Satir
                    {
                        tkl_Guid = satirGuid.ToString(),  // ⬅️ GUID eklendi
                        tkl_evrak_tarihi = teklif.Tarih.ToString("dd.MM.yyyy"),
                        tkl_evrakno_seri = "",
                        tkl_belge_no = teklif.FormNo,
                        tkl_cari_kod = teklif.CariKod,
                        tkl_cari_tipi = "0",
                        tkl_cari_sormerk = "",
                        tkl_stok_kod = urun.StokKod,
                        tkl_stok_sormerk = "",
                        tkl_Aciklama = urun.Aciklama ?? "",
                        tkl_Alisfiyati = teklifFiyat,
                        tkl_Brut_fiyat = urun.BirimFiyat,
                        tkl_miktar = urun.Miktar,
                        tkl_baslangic_tarihi = teklif.BaslangicTarihi.ToString("dd.MM.yyyy"),
                        tkl_Gecerlilik_Sures = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("dd.MM.yyyy"),
                        tkl_special2 = urun.KdvOrani.ToString(),
                        tkl_vergi = kdvTutari,
                        tkl_birim_pntr = 1,
                        tkl_vergi_pntr = 4,
                        tkl_harekettipi = 0,
                        tkl_karorani = 0,
                        tkl_ProjeKodu = ""
                    };

                    evrak.satirlar.Add(satir);

                    Console.WriteLine($"  → Satır {i + 1}: GUID={satirGuid}, Stok={urun.StokKod}, Miktar={urun.Miktar}");
                }

                // ✅ 8. API Request oluştur
                var apiRequest = new MikroApiRequest
                {
                    Mikro = new MikroApiData
                    {
                        FirmaKodu = firmaKodu,
                        CalismaYili = DateTime.Now.Year.ToString(),
                        ApiKey = apiKey,
                        KullaniciKodu = kullaniciKodu,
                        Sifre = encryptedPassword,
                        evraklar = new List<Evrak> { evrak }
                    }
                };

                // ✅ 9. API'ye gönder
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Console.WriteLine($"📤 JSON (ilk 500 karakter): {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");

                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 API Response Status: {response.StatusCode}");
                Console.WriteLine($"📥 API Response: {responseContent}");

                return new MikroApiResponse
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Teklif başarıyla güncellendi" : $"Hata: {response.StatusCode}",
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hata: {ex.Message}");
                return new MikroApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 500
                };
            }
        }
        // ✅ GetTeklifSatirlari - KDV bilgilerini special2'den al
        public IEnumerable<TeklifUrunModel> GetTeklifSatirlari(int evrakSiraNo)
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
SELECT 
    vt.tkl_stok_kod as StokKod,
    ISNULL(s.sto_isim, '') as StokAdi,
    vt.tkl_miktar as Miktar,
    vt.tkl_Brut_fiyat as BirimFiyat,
    vt.tkl_Alisfiyati as IndirimliFiyat,
    CAST(ISNULL(vt.tkl_special2, '0') AS INT) as KdvOrani,  -- ✅ KDV oranı special2'den
    vt.tkl_vergi as KdvTutari,                              -- ✅ KDV tutarı tkl_vergi'den
    (vt.tkl_miktar * vt.tkl_Alisfiyati) as Toplam,
    (vt.tkl_miktar * vt.tkl_Alisfiyati) + ISNULL(vt.tkl_vergi, 0) as ToplamKdvDahil,
    vt.tkl_satirno as SatirNo,
    ISNULL(vt.tkl_special1, '') as Aciklama,               -- ✅ Satır açıklaması
    id.Data as ImageData
FROM VERILEN_TEKLIFLER vt
LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
LEFT JOIN [dbo].[mye_ImageData] id 
    ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
       UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
ORDER BY vt.tkl_satirno";

            return connection.Query<TeklifUrunModel>(query, new { EvrakSiraNo = evrakSiraNo });
        }

        // ✅ GetTeklifDetay - Teklif konusunu ve KDV toplamını getir
        // ✅ GetTeklifDetay - USER tablosundan satır açıklamalarını da çek
        public TeklifDetayModel GetTeklifDetay(int evrakSiraNo)
        {
            using var connection = new SqlConnection(ConnectionString);

            var teklifQuery = @"
SELECT 
    vt.tkl_evrakno_sira,
    MIN(vt.tkl_cari_kod) as tkl_cari_kod,
    MIN(CONVERT(VARCHAR(10), vt.tkl_evrak_tarihi, 23)) as tkl_evrak_tarihi,
    MIN(CONVERT(VARCHAR(10), vt.tkl_baslangic_tarihi, 23)) as tkl_baslangic_tarihi,
    MIN(vt.tkl_Gecerlilik_Sures) as tkl_Gecerlilik_Sures,
    MIN(vt.tkl_belge_no) as tkl_belge_no,
    MIN(vt.tkl_Sorumlu_Kod) as tkl_Sorumlu_Kod,
    MIN(vt.tkl_Aciklama) as tkl_Aciklama,                     -- ✅ Teklif konusu
    MIN(vt.tkl_durumu) as tkl_durumu,
    SUM(vt.tkl_miktar * vt.tkl_Alisfiyati) as tkl_Alisfiyati,
    SUM(ISNULL(vt.tkl_vergi, 0)) as ToplamKdv,                -- ✅ Toplam KDV
    MIN(ch.cari_unvan1) as CariAdi,
    MIN(ISNULL(cp.cari_per_adi, '') + ' ' + ISNULL(cp.cari_per_soyadi, '')) as HazirlayanAdi
FROM VERILEN_TEKLIFLER vt
LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON vt.tkl_Sorumlu_Kod = cp.cari_per_kod
WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
GROUP BY vt.tkl_evrakno_sira";

            // ✅ USER tablosundan satır açıklamasını da çek
            var teklifSatirQuery = @"
SELECT 
    vt.tkl_stok_kod as StokKod,
    ISNULL(s.sto_isim, '') as StokAdi,
    vt.tkl_miktar as Miktar,
    vt.tkl_Brut_fiyat as BirimFiyat,
    vt.tkl_Alisfiyati as IndirimliFiyat,
    CAST(ISNULL(vt.tkl_special2, '0') AS INT) as KdvOrani,
    ISNULL(vt.tkl_vergi, 0) as KdvTutari,
    (vt.tkl_miktar * vt.tkl_Alisfiyati) as Toplam,
    (vt.tkl_miktar * vt.tkl_Alisfiyati) + ISNULL(vt.tkl_vergi, 0) as ToplamKdvDahil,
    vt.tkl_satirno as SatirNo,

    id.Data as ImageData
FROM VERILEN_TEKLIFLER vt
LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod

LEFT JOIN [dbo].[mye_ImageData] id 
    ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
       UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
ORDER BY vt.tkl_satirno";

            using var multi = connection.QueryMultiple($"{teklifQuery}; {teklifSatirQuery}",
                new { EvrakSiraNo = evrakSiraNo });

            var teklif = multi.ReadSingleOrDefault<TeklifDetayModel>();
            if (teklif != null)
            {
                teklif.Urunler = multi.Read<TeklifUrunModel>().ToList();
            }

            return teklif;
        }

        // ✅ GetTeklifler - Liste görünümü
        public IEnumerable<TeklifListeModel> GetTeklifler()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
SELECT 
    MIN(vt.tkl_Guid) as tkl_Guid,
    CAST(vt.tkl_evrakno_sira AS VARCHAR) as TeklifNo,
    MIN(vt.tkl_Aciklama) as Konu,
    MIN(ch.cari_unvan1) as Kime,
    SUM(vt.tkl_miktar * vt.tkl_Alisfiyati) as Toplam,
    SUM(ISNULL(vt.tkl_vergi, 0)) as ToplamKdv,
    MIN(CASE 
        WHEN ISDATE(vt.tkl_evrak_tarihi) = 1 
        THEN CAST(vt.tkl_evrak_tarihi AS DATETIME)
        ELSE GETDATE()
    END) as Tarih,
    MIN(CASE 
        WHEN ISDATE(vt.tkl_evrak_tarihi) = 1 
        THEN DATEADD(DAY, ISNULL(CAST(vt.tkl_Gecerlilik_Sures AS INT), 7), CAST(vt.tkl_evrak_tarihi AS DATETIME))
        ELSE DATEADD(DAY, ISNULL(CAST(vt.tkl_Gecerlilik_Sures AS INT), 7), GETDATE())
    END) as GecerlilikTarihi,
    '' as Etiketler,
    MIN(ISNULL(vt.tkl_create_date, GETDATE())) as OlusturmaTarihi,
    MIN(CASE 
        WHEN ISNULL(vt.tkl_durumu, '0') = '0' THEN 'Taslak'
        WHEN vt.tkl_durumu = '1' THEN 'Gönderildi'
        WHEN vt.tkl_durumu = '2' THEN 'Kazanıldı'
        WHEN vt.tkl_durumu = '3' THEN 'Kaybedildi'
        WHEN vt.tkl_durumu = '4' THEN 'Ertelendi'
        WHEN vt.tkl_durumu = '5' THEN 'İptal Edildi'
        ELSE 'Taslak'
    END) as Durum,
    MIN(vt.tkl_belge_no) as TeklifKonusu,
    STRING_AGG(s.sto_isim, ', ') as Urunler
FROM VERILEN_TEKLIFLER vt
LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
GROUP BY vt.tkl_evrakno_sira
ORDER BY MIN(ISNULL(vt.tkl_create_date, GETDATE())) DESC";

            return connection.Query<TeklifListeModel>(query);
        }

        #endregion
        // CrmRepository.cs dosyasındaki TeklifGuncelle metodunu güncelleyin ve GetTeklifDetay metodunu geliştirin

        #region Teklif Güncelleme Metodları

        // Mevcut GetTeklifDetay metodunu güncelleyin

        #endregion
        #region Cari Hesaplar

        // Cari hesap listesi
        public IEnumerable<CariHesapModel> GetCariHesaplar()
        {
            return GetTumCariler(); // Yeni metodu kullan
        }

        // Cari hesap detay
        public CariHesapDetayModel GetCariHesapDetay(string cariKod)
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    ch.*,
                    cp.cari_per_adi + ' ' + cp.cari_per_soyadi as TemsilciAdi
                FROM CARI_HESAPLAR ch
                LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON ch.cari_temsilci_kodu = cp.cari_per_kod
                WHERE ch.cari_kod = @CariKod";

            return connection.QueryFirstOrDefault<CariHesapDetayModel>(query, new { CariKod = cariKod });
        }

        // Cari temsilci koduna göre personel getir
        public IEnumerable<PersonelModel> GetCariPersonelleri(string cariKod)
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT DISTINCT
                    cp.cari_per_kod as PersonelKod,
                    cp.cari_per_adi + ' ' + cp.cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI cp
                INNER JOIN CARI_HESAPLAR ch ON cp.cari_per_kod = ch.cari_temsilci_kodu
                WHERE ch.cari_kod = @CariKod
                
                UNION
                
                SELECT 
                    cpt.cari_per_kod as PersonelKod,
                    cpt.cari_per_adi + ' ' + cpt.cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI cpt";

            return connection.Query<PersonelModel>(query, new { CariKod = cariKod });
        }

        #endregion

        #region Stoklar

        // Stok listesi - STOKLAR tablosundan
        // Updated GetStoklar method - only return basic stock info
        public IEnumerable<StokModel> GetStoklar()
        {
            using var connection = new SqlConnection(ConnectionString);

            // Önce hangi JOIN yönteminin çalıştığını test edelim
            var testQuery = @"
        SELECT COUNT(*) as TestCount
        FROM STOKLAR s
        INNER JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE s.sto_pasif_fl = 0 AND s.sto_iptal = 0";

            try
            {
                var testCount = connection.QuerySingle<int>(testQuery);
                Console.WriteLine($"GUID eşleştirme test sonucu: {testCount} kayıt bulundu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GUID test hatası: {ex.Message}");
            }

            // Ana sorgu - GUID formatını temizleyerek eşleştir
            var query = @"
        SELECT 
            s.sto_Guid As StukGuid,
            s.sto_kod AS StokKod,
            s.sto_isim AS StokAdi,
            s.sto_kisa_ismi AS KisaIsim,
            s.sto_birim1_ad AS Birim1,
            s.sto_birim1_katsayi AS Birim1Katsayi,
            s.sto_anagrup_kod AS AnaGrupKod,
            id.Data AS ImageData
        FROM STOKLAR s
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE s.sto_pasif_fl = 0 
            AND s.sto_iptal = 0
        ORDER BY s.sto_isim";

            var result = connection.Query<StokModel>(query).ToList();

            // Log için fotoğraflı stok sayısını say
            var fotografliStokSayisi = result.Count(s => s.ImageData != null && s.ImageData.Length > 0);
            Console.WriteLine($"Repository GetStoklar: {result.Count} stok, {fotografliStokSayisi} tanesi fotoğraflı");

            return result;
        }

        // Alternatif method - eğer yukarıdaki çalışmazsa bu deneyin
        public IEnumerable<StokModel> GetStoklar_Alternative()
        {
            using var connection = new SqlConnection(ConnectionString);

            // Farklı JOIN yöntemleri dene
            var query = @"
        SELECT 
            s.sto_Guid As StukGuid,
            s.sto_kod AS StokKod,
            s.sto_isim AS StokAdi,
            s.sto_kisa_ismi AS KisaIsim,
            s.sto_birim1_ad AS Birim1,
            s.sto_birim1_katsayi AS Birim1Katsayi,
            s.sto_anagrup_kod AS AnaGrupKod,
            id.Data AS ImageData
        FROM STOKLAR s
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON (
                s.sto_guid = id.Record_uid OR
                CAST(s.sto_guid AS VARCHAR(50)) = CAST(id.Record_uid AS VARCHAR(50)) OR
                UPPER(LTRIM(RTRIM(CAST(s.sto_guid AS VARCHAR(50))))) = UPPER(LTRIM(RTRIM(CAST(id.Record_uid AS VARCHAR(50)))))
            )
        WHERE s.sto_pasif_fl = 0 
            AND s.sto_iptal = 0
        ORDER BY s.sto_isim";

            return connection.Query<StokModel>(query);
        }

        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        public decimal GetStokSatisFiyati(string StokKod, int listeSiraNo = 1, int dovizCinsi = 0)
        {
            using var connection = new SqlConnection(ConnectionString);

            var fiyatQuery = @"
        SELECT TOP 1 sfiyat_fiyati 
        FROM STOK_SATIS_FIYAT_LISTELERI 
        WHERE sfiyat_stokkod = @StokKod 
            AND sfiyat_listesirano = @ListeSiraNo
            AND sfiyat_doviz = @DovizCinsi
            AND sfiyat_iptal = 0
        ORDER BY sfiyat_create_date DESC";

            var fiyat = connection.QueryFirstOrDefault<decimal?>(fiyatQuery, new
            {
                StokKod = StokKod,
                ListeSiraNo = listeSiraNo,
                DovizCinsi = dovizCinsi
            });

            return fiyat ?? 0;
        }

        #endregion

        #region Lookup Metodları

        // Form numarası üret
        public string GetYeniFormNumarasi()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT RIGHT('000000' + CAST(ISNULL(MAX(tkl_evrakno_sira), 0) + 1 AS VARCHAR), 6)
                FROM VERILEN_TEKLIFLER";

            return connection.QuerySingle<string>(query);
        }

        // Personel listesi
        public IEnumerable<PersonelModel> GetPersoneller()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    cari_per_kod as PersonelKod,
                    cari_per_adi + ' ' + cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI
                ORDER BY cari_per_adi, cari_per_soyadi";

            return connection.Query<PersonelModel>(query);
        }

        // Durumlar
        public IEnumerable<string> GetTeklifDurumlari()
        {
            return new List<string>
            {
                "Taslak",
                "Gönderildi",
                "Kazanıldı",
                "Kaybedildi",
                "Ertelendi",
                "İptal Edildi"
            };
        }



        #endregion

        #region Dashboard & İstatistikler

        // Dashboard için teklif istatistikleri

        // Aylık teklif grafiği için veri
        public IEnumerable<AylikTeklifGrafik> GetAylikTeklifGrafigi()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    YEAR(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ) as Yil,
                    MONTH(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ) as Ay,
                    COUNT(*) as TeklifSayisi,
                    SUM(ISNULL(tkl_Alisfiyati, 0)) as ToplamTutar
                FROM VERILEN_TEKLIFLER
                WHERE (
                    CASE 
                        WHEN ISDATE(tkl_evrak_tarihi) = 1 
                        THEN CAST(tkl_evrak_tarihi AS DATETIME)
                        ELSE tkl_create_date
                    END >= DATEADD(MONTH, -12, GETDATE())
                )
                GROUP BY 
                    YEAR(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ), 
                    MONTH(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    )
                ORDER BY Yil, Ay";

            return connection.Query<AylikTeklifGrafik>(query);
        }

        #endregion

        #region Sipariş Dönüştürme İşlemleri

        // Teklif verilerini sipariş için getir
        public TeklifSiparisViewModel GetTeklifForSiparis(int teklifNo)
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        SELECT 
            vt.tkl_evrakno_sira AS TeklifNo,
            MIN(ch.cari_unvan1) AS MusteriAdi,
            MIN(vt.tkl_cari_kod) AS MusteriKod
        FROM VERILEN_TEKLIFLER vt
        LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
        WHERE vt.tkl_evrakno_sira = @TeklifNo
        GROUP BY vt.tkl_evrakno_sira";

            var teklif = connection.QuerySingleOrDefault<TeklifSiparisViewModel>(query, new { TeklifNo = teklifNo });

            if (teklif == null)
                return null;

            // Ürünleri getir
            var urunQuery = @"
        SELECT 
            vt.tkl_stok_kod AS TeklifStokKod,
            s.sto_isim AS StokAdi,
            vt.tkl_miktar AS SiparisMiktar,
            vt.tkl_birim_pntr AS BirimPntr,
            CASE 
                WHEN s.sto_kod IS NOT NULL AND s.sto_CRM_sistemine_aktar_fl = 1 
                THEN 1 
                ELSE 0 
            END AS MikrodaVarMi,
            CASE 
                WHEN s.sto_CRM_sistemine_aktar_fl = 1 
                THEN s.sto_kod 
                ELSE NULL 
            END AS MikroStokKod
        FROM VERILEN_TEKLIFLER vt
        LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod 
            AND s.sto_iptal = 0 
            AND s.sto_pasif_fl = 0
            AND s.sto_CRM_sistemine_aktar_fl = 1
        WHERE vt.tkl_evrakno_sira = @TeklifNo
        ORDER BY vt.tkl_satirno";

            var urunler = connection.Query<TeklifUrunSiparisModel>(urunQuery, new { TeklifNo = teklifNo }).ToList();

            // Her ürün için eldeki miktarı hesapla
            foreach (var urun in urunler)
            {
                if (!string.IsNullOrEmpty(urun.MikroStokKod))
                {
                    urun.EldekiMiktar = GetEldekiMiktar(urun.MikroStokKod);
                    urun.SatinAlmaTalep = Math.Max(0, urun.SiparisMiktar - urun.EldekiMiktar);
                }
                else
                {
                    urun.EldekiMiktar = 0;
                    urun.SatinAlmaTalep = urun.SiparisMiktar;
                }
            }

            teklif.Urunler = urunler;
            return teklif;
        }

        // Eldeki miktarı hesaplama
        public decimal GetEldekiMiktar(string stokKodu)
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        SELECT dbo.fn_EldekiMiktar(@StokKodu) AS EldekiMiktar";

            var result = connection.QuerySingleOrDefault<decimal?>(query, new { StokKodu = stokKodu });
            return result ?? 0;
        }

        // Mikro stok listesini getir (CRM'e aktarılabilir olanlar)
        public IEnumerable<MikroStokModel> GetMikroStoklar()
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        SELECT 
            sto_kod AS StokKod,
            sto_isim AS StokAdi,
            sto_kisa_ismi AS KisaIsim,
            sto_birim1_ad AS Birim
        FROM STOKLAR
        WHERE sto_iptal = 0 
            AND sto_pasif_fl = 0
            AND sto_CRM_sistemine_aktar_fl = 1
        ORDER BY sto_isim";

            return connection.Query<MikroStokModel>(query);
        }

        // Teklifi siparişe dönüştür
        public SiparisKayitSonuc TekliftenSiparisOlustur(TeklifSiparisDonusturModel model)
        {
            using var connection = new SqlConnection(ConnectionString);

            try
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                // Teklif bilgilerini al
                var teklifQuery = @"
            SELECT TOP 1
                tkl_cari_kod,
                tkl_evrak_tarihi,
                tkl_doviz_cins,
                tkl_doviz_kur,
                tkl_Sorumlu_Kod,
                tkl_adres_no
            FROM VERILEN_TEKLIFLER
            WHERE tkl_evrakno_sira = @TeklifNo";

                var teklifInfo = connection.QuerySingleOrDefault<dynamic>(teklifQuery,
                    new { TeklifNo = model.TeklifNo },
                    transaction: transaction);

                if (teklifInfo == null)
                {
                    transaction.Rollback();
                    return new SiparisKayitSonuc { Success = false, Message = "Teklif bulunamadı." };
                }

                // Yeni sipariş seri ve sıra numarası al
                var siparisNoQuery = @"
            SELECT 
                ISNULL(MAX(sip_evrakno_sira), 0) + 1 
            FROM SIPARISLER";

                var yeniSiparisNo = connection.QuerySingle<int>(siparisNoQuery, transaction: transaction);

                var siparisSeri = "SIP";
                var siparisTarihi = DateTime.Now;
                var teslimTarihi = DateTime.Now.AddDays(15); // 15 gün sonra teslimat

                // Sipariş kayıt sorgusu
                var siparisInsertQuery = @"
            INSERT INTO SIPARISLER (
                sip_Guid, sip_DBCno, sip_SpecRECno, sip_iptal, sip_fileid, sip_hidden,
                sip_kilitli, sip_degisti, sip_checksum, sip_create_user, sip_create_date,
                sip_lastup_user, sip_lastup_date, sip_special1, sip_special2, sip_special3,
                sip_firmano, sip_subeno, sip_tarih, sip_teslim_tarih, sip_tip, sip_cins,
                sip_evrakno_seri, sip_evrakno_sira, sip_satirno, sip_belgeno, sip_belge_tarih,
                sip_satici_kod, sip_musteri_kod, sip_stok_kod, sip_b_fiyat, sip_miktar,
                sip_birim_pntr, sip_teslim_miktar, sip_tutar, sip_iskonto_1, sip_iskonto_2,
                sip_iskonto_3, sip_iskonto_4, sip_iskonto_5, sip_iskonto_6, sip_masraf_1,
                sip_masraf_2, sip_masraf_3, sip_masraf_4, sip_vergi_pntr, sip_vergi,
                sip_masvergi_pntr, sip_masvergi, sip_opno, sip_aciklama, sip_aciklama2,
                sip_depono, sip_OnaylayanKulNo, sip_vergisiz_fl, sip_kapat_fl, sip_promosyon_fl,
                sip_cari_sormerk, sip_stok_sormerk, sip_cari_grupno, sip_doviz_cinsi,
                sip_doviz_kuru, sip_alt_doviz_kuru, sip_adresno, sip_teslimturu,
                sip_cagrilabilir_fl, sip_durumu, sip_projekodu, sip_fiyat_liste_no,
                sip_harekettipi, sip_yetkili_uid
            ) VALUES (
                @SiparisGuid, 0, 0, 0, 100, 0,
                0, 0, 0, 1, GETDATE(),
                1, GETDATE(), '', '', '',
                0, 0, @Tarih, @TeslimTarih, 0, 0,
                @Seri, @SiraNo, @SatirNo, '', NULL,
                '', @CariKod, @StokKod, @BirimFiyat, @Miktar,
                @BirimPntr, 0, @Tutar, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 4, 0,
                0, 0, 0, @Aciklama, '',
                1, 0, 0, 0, 0,
                '', '', 0, @DovizCinsi,
                @DovizKuru, 0, @AdresNo, 0,
                0, '0', '', 0,
                0, '00000000-0000-0000-0000-000000000000'
            )";

                int satirNo = 1;
                decimal toplamTutar = 0;

                foreach (var urun in model.Urunler)
                {
                    if (string.IsNullOrEmpty(urun.MikroStokKod))
                        continue;

                    // Stok fiyatını al
                    var fiyatQuery = @"
                SELECT TOP 1 sfiyat_fiyati 
                FROM STOK_SATIS_FIYAT_LISTELERI 
                WHERE sfiyat_stokkod = @StokKod 
                    AND sfiyat_iptal = 0
                ORDER BY sfiyat_create_date DESC";

                    var birimFiyat = connection.QueryFirstOrDefault<decimal?>(fiyatQuery,
                        new { StokKod = urun.MikroStokKod },
                        transaction: transaction) ?? 0;

                    var tutar = birimFiyat * urun.SiparisMiktar;
                    toplamTutar += tutar;

                    var siparisGuid = Guid.NewGuid();

                    connection.Execute(siparisInsertQuery, new
                    {
                        SiparisGuid = siparisGuid,
                        Tarih = siparisTarihi,
                        TeslimTarih = teslimTarihi,
                        Seri = siparisSeri,
                        SiraNo = yeniSiparisNo,
                        SatirNo = satirNo,
                        CariKod = teklifInfo.tkl_cari_kod,
                        StokKod = urun.MikroStokKod,
                        BirimFiyat = birimFiyat,
                        Miktar = urun.SiparisMiktar,
                        BirimPntr = 1, // Ana birim
                        Tutar = tutar,
                        Aciklama = $"Teklif No: {model.TeklifNo} - {urun.TeklifStokKod}",
                        DovizCinsi = teklifInfo.tkl_doviz_cins ?? "",
                        DovizKuru = teklifInfo.tkl_doviz_kur ?? 1,
                        AdresNo = teklifInfo.tkl_adres_no ?? 1
                    }, transaction: transaction);

                    // Eğer satın alma talebi varsa, satın alma talebi oluştur
                    if (urun.SatinAlmaTalep > 0)
                    {
                        // Buraya satın alma talebi insert kodu eklenebilir
                        // Şimdilik sadece log tutalım
                        Console.WriteLine($"Satın alma talebi: {urun.MikroStokKod} - {urun.SatinAlmaTalep}");
                    }

                    satirNo++;
                }

                transaction.Commit();

                return new SiparisKayitSonuc
                {
                    Success = true,
                    Message = "Sipariş başarıyla oluşturuldu.",
                    SiparisNo = yeniSiparisNo,
                    ToplamTutar = toplamTutar
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sipariş oluşturma hatası: {ex.Message}");
                return new SiparisKayitSonuc
                {
                    Success = false,
                    Message = $"Sipariş oluşturulurken bir hata oluştu: {ex.Message}"
                };
            }
        }

        #endregion

        public IEnumerable<CariHesapModel> GetTumCariler()
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        -- Asıl cariler
        SELECT 
            ch.cari_kod as CariKod,
            ch.cari_unvan1 as CariAdi,
            ch.cari_temsilci_kodu as TemsilciKodu,
            ch.cari_sektor_kodu as SektorKodu,
            ch.cari_grup_kodu as GrupKodu,
            ch.cari_EMail as Email,
            ch.cari_CepTel as Telefon,
            0 as IsAdayCari
        FROM CARI_HESAPLAR ch
        WHERE ch.cari_iptal = 0
        
        UNION ALL
        
        -- Aday cariler
        SELECT 
            ac.adaycr_kod as CariKod,
            ac.adaycr_unvan1 as CariAdi,
            ac.adaycr_temsilci_kodu as TemsilciKodu,
            ac.adaycr_sektor_kodu as SektorKodu,
            ac.adaycr_grup_kodu as GrupKodu,
            ac.adaycr_EMail as Email,
            ac.adaycr_adr1_tel_no1 as Telefon,
            1 as IsAdayCari
        FROM ADAY_CARI_HESAPLAR ac
        WHERE ac.adaycr_iptal = 0
        
        ORDER BY CariAdi";

            return connection.Query<CariHesapModel>(query);
        }

        public string GetYeniAdayCariKodu()
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        SELECT TOP 1 adaycr_kod 
        FROM ADAY_CARI_HESAPLAR 
        WHERE adaycr_kod LIKE 'CRM%'
        ORDER BY adaycr_kod DESC";

            var sonKod = connection.QueryFirstOrDefault<string>(query);

            if (string.IsNullOrEmpty(sonKod))
            {
                return "CRM001";
            }

            // CRM001 -> 001 -> 1 -> 2 -> 002 -> CRM002
            var numara = int.Parse(sonKod.Substring(3));
            numara++;
            return $"CRM{numara:000}";
        }

        // Aday cari kaydet
        public bool AdayCariKaydet(AdayCariHesapModel model)
        {
            using var connection = new SqlConnection(ConnectionString);

            try
            {
                var query = @"
            INSERT INTO ADAY_CARI_HESAPLAR (
                adaycr_Guid, adaycr_DBCno, adaycr_SpecRECno, adaycr_iptal, 
                adaycr_fileid, adaycr_hidden, adaycr_kilitli, adaycr_degisti,
                adaycr_checksum, adaycr_create_user, adaycr_create_date,
                adaycr_lastup_user, adaycr_lastup_date,
                adaycr_kod, adaycr_unvan1, adaycr_unvan2,
                adaycr_sektor_kodu, adaycr_bolge_kodu, adaycr_grup_kodu,
                adaycr_temsilci_kodu, adaycr_wwwadresi, adaycr_EMail,
                adaycr_adr1_cadde, adaycr_adr1_mahalle, adaycr_adr1_sokak,
                adaycr_adr1_Semt, adaycr_adr1_Apt_No, adaycr_adr1_Daire_No,
                adaycr_adr1_posta_kodu, adaycr_adr1_ilce, adaycr_adr1_il,
                adaycr_adr1_ulke, adaycr_adr1_adres_kodu,
                adaycr_adr1_tel_ulke_kodu, adaycr_adr1_tel_bolge_kodu,
                adaycr_adr1_tel_no1,
                adaycr_yetkili1_isim, adaycr_yetkili1_dahili_telno,
                adaycr_yetkili1_email_adres, adaycr_yetkili1_cep_telno
            ) VALUES (
                @Guid, 0, 0, 0,
                100, 0, 0, 0,
                0, 1, GETDATE(),
                1, GETDATE(),
                @Kod, @Unvan1, @Unvan2,
                @SektorKodu, @BolgeKodu, @GrupKodu,
                @TemsilciKodu, @WwwAdresi, @Email,
                @Adr1Cadde, @Adr1Mahalle, @Adr1Sokak,
                @Adr1Semt, @Adr1AptNo, @Adr1DaireNo,
                @Adr1PostaKodu, @Adr1Ilce, @Adr1Il,
                @Adr1Ulke, @Adr1AdresKodu,
                @Adr1TelUlkeKodu, @Adr1TelBolgeKodu,
                @Adr1TelNo1,
                @Yetkili1Isim, @Yetkili1DahiliTelno,
                @Yetkili1EmailAdres, @Yetkili1CepTelno
            )";

                connection.Execute(query, new
                {
                    Guid = Guid.NewGuid(),
                    model.Kod,
                    Unvan1 = model.Unvan1 ?? "",
                    Unvan2 = model.Unvan2 ?? "",
                    SektorKodu = model.SektorKodu ?? "",
                    BolgeKodu = model.BolgeKodu ?? "",
                    GrupKodu = model.GrupKodu ?? "",
                    TemsilciKodu = model.TemsilciKodu ?? "",
                    WwwAdresi = model.WwwAdresi ?? "",
                    Email = model.Email ?? "",
                    Adr1Cadde = model.Adr1Cadde ?? "",
                    Adr1Mahalle = model.Adr1Mahalle ?? "",
                    Adr1Sokak = model.Adr1Sokak ?? "",
                    Adr1Semt = model.Adr1Semt ?? "",
                    Adr1AptNo = model.Adr1AptNo ?? "",
                    Adr1DaireNo = model.Adr1DaireNo ?? "",
                    Adr1PostaKodu = model.Adr1PostaKodu ?? "",
                    Adr1Ilce = model.Adr1Ilce ?? "",
                    Adr1Il = model.Adr1Il ?? "",
                    Adr1Ulke = model.Adr1Ulke ?? "TÜRKİYE",
                    Adr1AdresKodu = model.Adr1AdresKodu ?? "",
                    Adr1TelUlkeKodu = model.Adr1TelUlkeKodu ?? "",
                    Adr1TelBolgeKodu = model.Adr1TelBolgeKodu ?? "",
                    Adr1TelNo1 = model.Adr1TelNo1 ?? "",
                    Yetkili1Isim = model.Yetkili1Isim ?? "",
                    Yetkili1DahiliTelno = model.Yetkili1DahiliTelno ?? "",
                    Yetkili1EmailAdres = model.Yetkili1EmailAdres ?? "",
                    Yetkili1CepTelno = model.Yetkili1CepTelno ?? ""
                });

                Console.WriteLine($"Aday cari başarıyla kaydedildi: {model.Kod}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aday cari kaydetme hatası: {ex.Message}");
                return false;
            }
        }

        // Aday cariyi asıl cari tablosuna aktar
        public bool AdayCaridenCariyeAktar(string adayCariKod)
        {
            using var connection = new SqlConnection(ConnectionString);

            try
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                // Aday cari bilgilerini al
                var adayCariQuery = @"
            SELECT * FROM ADAY_CARI_HESAPLAR 
            WHERE adaycr_kod = @Kod AND adaycr_iptal = 0";

                var adayCari = connection.QueryFirstOrDefault<dynamic>(adayCariQuery,
                    new { Kod = adayCariKod }, transaction: transaction);

                if (adayCari == null)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Aday cari bulunamadı: {adayCariKod}");
                    return false;
                }

                // CARI_HESAPLAR tablosuna ekle
                var insertQuery = @"
            INSERT INTO CARI_HESAPLAR (
                cari_Guid, cari_DBCno, cari_SpecRECno, cari_iptal, cari_fileid,
                cari_hidden, cari_kilitli, cari_degisti, cari_checksum,
                cari_create_user, cari_create_date, cari_lastup_user, cari_lastup_date,
                cari_kod, cari_unvan1, cari_unvan2,
                cari_sektor_kodu, cari_bolge_kodu, cari_grup_kodu,
                cari_temsilci_kodu, cari_wwwadresi, cari_EMail, cari_CepTel,
                cari_hareket_tipi, cari_baglanti_tipi,
                cari_stok_alim_cinsi, cari_stok_satim_cinsi,
                cari_doviz_cinsi, cari_CRM_sistemine_aktar_fl
            ) VALUES (
                @Guid, 0, 0, 0, 100,
                0, 0, 0, 0,
                1, GETDATE(), 1, GETDATE(),
                @Kod, @Unvan1, @Unvan2,
                @SektorKodu, @BolgeKodu, @GrupKodu,
                @TemsilciKodu, @WwwAdresi, @Email, @CepTel,
                0, 0,
                0, 0,
                '', 1
            )";

                connection.Execute(insertQuery, new
                {
                    Guid = Guid.NewGuid(),
                    Kod = adayCariKod,
                    Unvan1 = adayCari.adaycr_unvan1 ?? "",
                    Unvan2 = adayCari.adaycr_unvan2 ?? "",
                    SektorKodu = adayCari.adaycr_sektor_kodu ?? "",
                    BolgeKodu = adayCari.adaycr_bolge_kodu ?? "",
                    GrupKodu = adayCari.adaycr_grup_kodu ?? "",
                    TemsilciKodu = adayCari.adaycr_temsilci_kodu ?? "",
                    WwwAdresi = adayCari.adaycr_wwwadresi ?? "",
                    Email = adayCari.adaycr_EMail ?? "",
                    CepTel = adayCari.adaycr_adr1_tel_no1 ?? ""
                }, transaction: transaction);

                // Aday cariyi pasif yap (sil)
                var deleteQuery = @"
            UPDATE ADAY_CARI_HESAPLAR 
            SET adaycr_iptal = 1, 
                adaycr_lastup_date = GETDATE() 
            WHERE adaycr_kod = @Kod";

                connection.Execute(deleteQuery, new { Kod = adayCariKod }, transaction: transaction);

                transaction.Commit();
                Console.WriteLine($"Aday cari başarıyla aktarıldı: {adayCariKod}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aday cari aktarma hatası: {ex.Message}");
                return false;
            }
        }
    }
}