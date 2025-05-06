using System.Data.SqlClient;
using Dapper;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using static Deneme_proje.Models.CrmEntities;

namespace Deneme_proje.Controllers
{
    public class CrmController : BaseController
	{
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<CrmController> _logger;


        public CrmController(IConfiguration configuration, DatabaseSelectorService dbSelectorService, ILogger<CrmController> logger)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }
        public IActionResult Dashboard()
        {
            return View();
        }
      
        public IActionResult Firsatlar()
        {
            var connectionString = _configuration.GetConnectionString("ERPDatabase");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                    SELECT 
                        Firsat_Guid,
                        Firsat_Adi AS Adi,
                        Firma_Adi AS Firma,
                        Email,
                        Telefon,
                        Tutar,
                        Etiketler,
                        Atanan_Kisi,
                        Durum,
                        Kaynak,
                        Son_Iletisim_Tarihi,
                        Olusturulma_Tarihi
                    FROM CRM_FIRSATLAR
                    ORDER BY Olusturulma_Tarihi DESC";

                    var firsatListesi = connection.Query<Firsat>(query).ToList();
                    return View(firsatListesi);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fırsat listesi çekilirken hata oluştu");
                ViewBag.ErrorMessage = "Fırsat listesi yüklenirken bir hata oluştu.";
                return View(new List<Firsat>());
            }
        }

        // Yeni fırsat ekleme metodu
        [HttpPost]
        public IActionResult FirsatEkle(Firsat firsat)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Firsatlar");
            }

            var connectionString = _configuration.GetConnectionString("ERPDatabase");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                    INSERT INTO CRM_FIRSATLAR 
                    (Firsat_Adi, Firma_Adi, Email, Telefon, Tutar, Etiketler, 
                    Atanan_Kisi, Durum, Kaynak, Son_Iletisim_Tarihi, 
                    Adres, Pozisyon, Sehir, Ilce, Ulke, 
                    Website, Posta_Kodu, Varsayilan_Dil, Aciklama)
                    VALUES 
                    (@Firsat_Adi, @Firma_Adi, @Email, @Telefon, @Tutar, @Etiketler, 
                    @Atanan_Kisi, @Durum, @Kaynak, 
                    @Son_Iletisim_Tarihi, 
                    @Adres, @Pozisyon, @Sehir, @Ilce, @Ulke, 
                    @Website, @Posta_Kodu, @Varsayilan_Dil, @Aciklama)";

                    connection.Execute(query, firsat);

                    return RedirectToAction("Firsatlar");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fırsat eklenirken hata oluştu");
                ViewBag.ErrorMessage = "Fırsat eklenirken bir hata oluştu.";
                return RedirectToAction("Firsatlar");
            }
        }
    
    

        //[HttpGet]
        //public IActionResult FirsatDetay(Guid id)
        //{
        //    var connectionString = _dbSelectorService.GetConnectionString();
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        try
        //        {
        //            var query = "SELECT * FROM CRM_FIRSATLAR WHERE Firsat_Guid = @Id";
        //            var firsat = connection.QueryFirstOrDefault<Firsat>(query, new { Id = id });

        //            if (firsat == null)
        //                return NotFound();

        //            return Json(new { success = true, data = firsat });
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Fırsat detayı getirilirken hata oluştu");
        //            return Json(new { success = false, message = "Fırsat detayı getirilirken bir hata oluştu." });
        //        }
        //    }
        //}

        //[HttpPost]
        //public IActionResult FirsatGuncelle([FromBody] Firsat firsat)
        //{
        //    var connectionString = _dbSelectorService.GetConnectionString();
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        try
        //        {
        //            var query = @"
        //            UPDATE CRM_FIRSATLAR SET 
        //                Firsat_Adi = @Firsat_Adi,
        //                Firma_Adi = @Firma_Adi,
        //                Email = @Email,
        //                Telefon = @Telefon,
        //                Tutar = @Tutar,
        //                Etiketler = @Etiketler,
        //                Atanan_Kisi = @Atanan_Kisi,
        //                Durum = @Durum,
        //                Kaynak = @Kaynak,
        //                Son_Iletisim_Tarihi = @Son_Iletisim_Tarihi,
        //                Adres = @Adres,
        //                Pozisyon = @Pozisyon,
        //                Sehir = @Sehir,
        //                Ilce = @Ilce,
        //                Ulke = @Ulke,
        //                Website = @Website,
        //                Posta_Kodu = @Posta_Kodu,
        //                Varsayilan_Dil = @Varsayilan_Dil,
        //                Aciklama = @Aciklama
        //            WHERE Firsat_Guid = @Firsat_Guid";

        //            var result = connection.Execute(query, firsat);

        //            return Json(new { success = true, message = "Fırsat başarıyla güncellendi." });
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Fırsat güncellenirken hata oluştu");
        //            return Json(new { success = false, message = "Fırsat güncellenirken bir hata oluştu." });
        //        }
        //    }
        //}

        //[HttpPost]
        //public IActionResult FirsatSil(Guid id)
        //{
        //    var connectionString = _dbSelectorService.GetConnectionString();
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        try
        //        {
        //            var query = "DELETE FROM CRM_FIRSATLAR WHERE Firsat_Guid = @Id";
        //            var result = connection.Execute(query, new { Id = id });

        //            return Json(new { success = true, message = "Fırsat başarıyla silindi." });
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Fırsat silinirken hata oluştu");
        //            return Json(new { success = false, message = "Fırsat silinirken bir hata oluştu." });
        //        }
        //    }
        //}


        public IActionResult Teklifler()
        {
            return View();
        }

        public IActionResult YeniTeklif()
        {
            return View();
        }

        public IActionResult Musteriler()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT 
                ch.cari_unvan1 AS Musteri,
                '' AS Tur,
                ch.cari_kod AS TicariKodu,
                ISNULL(ss.sktr_ismi, 'Tanımsız') AS Sektor,
                ISNULL(p.per_adi, 'Tanımsız') AS Temsilci
            FROM 
                CARI_HESAPLAR ch
            LEFT JOIN 
                STOK_SEKTORLERI ss ON ch.cari_sektor_kodu = ss.sktr_kod
            LEFT JOIN 
                PERSONELLER p ON ch.cari_temsilci_kodu = p.per_kod";

                try
                {
                    var musteriListesi = connection.Query(query).ToList();
                    return View(musteriListesi);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Müşteri listesi çekilirken hata oluştu");
                    ViewBag.ErrorMessage = "Müşteri listesi yüklenirken bir hata oluştu.";
                    return View(new List<dynamic>());
                }
            }
        }

        public IActionResult MusteriEkle()
        {
            return View();
        }
        public IActionResult Aktiviteler()
        {
            return View();
        }
        public IActionResult AktiviteEkle()
        {
            return View();
        }
  
      public IActionResult Stoklar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
        SELECT 
    s.sto_kod AS StokKodu, 
    s.sto_isim AS StokAdi, 
    ISNULL(ag.san_isim, 'Tanımsız') AS AnaGrupAdi, 
    ISNULL(altg1.sta_isim, 'Tanımsız') AS AltGrup1Adi
FROM 
    STOKLAR s
LEFT JOIN 
    STOK_ANA_GRUPLARI ag ON s.sto_anagrup_kod = ag.san_kod
LEFT JOIN 
    STOK_ALT_GRUPLARI altg1 ON s.sto_altgrup_kod = altg1.sta_kod
      ";

                try
                {
                    var stokListesi = connection.Query(query).ToList();
                    return View(stokListesi);
                }
                catch (Exception ex)
                {
                    // Hata detaylarını logla
                    _logger.LogError(ex, "Stok listesi çekilirken hata oluştu");
                    ViewBag.ErrorMessage = "Stok listesi yüklenirken bir hata oluştu.";
                    return View(new List<dynamic>());
                }
            }
        }
        public IActionResult StokEkle()
        {
            return View();
        }
        public IActionResult Siparisler()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT 
                CONVERT(VARCHAR(10), s.sip_tarih, 104) AS Tarih,
                ISNULL(vt.tkl_belge_no, '') AS TeklifNo,
                ISNULL(ch.cari_unvan1, '') AS Musteri,
                '' AS IrsaliyeDurum,
                '' AS FaturaDurum,
                '' AS AraToplam,
                '' AS Indirim,
                '' AS Kdv,
                ISNULL(s.sip_tutar, 0) AS GenelToplam,
                ISNULL(s.sip_durumu, '') AS Durum
            FROM 
                SIPARISLER s
            LEFT JOIN 
                VERILEN_TEKLIFLER vt ON s.sip_teklif_uid = vt.tkl_guid
            LEFT JOIN 
                CARI_HESAPLAR ch ON s.sip_musteri_kod = ch.cari_kod";

                try
                {
                    var siparisListesi = connection.Query(query).ToList();
                    return View(siparisListesi);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sipariş listesi çekilirken hata oluştu");
                    ViewBag.ErrorMessage = "Sipariş listesi yüklenirken bir hata oluştu.";
                    return View(new List<dynamic>());
                }
            }
        }
        public IActionResult SiparisEkle()
        {
            return View();
        }

        public IActionResult CariAnaliz()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetBakiyeDetay()
        {
            var detay = new
            {
                ToplamBakiye = 100000.50m,
                Detaylar = new[]
                {
            new Dictionary<string, object>
            {
                {"FaturaNo", "FA-2024-001"},
                {"Tarih", "15.02.2024"},
                {"Vade", "15.03.2024"},
                {"Tutar", 35000.25m},
                {"Aciklama", "Şubat ayı mal alım faturası"}
            },
            new Dictionary<string, object>
            {
                {"FaturaNo", "FA-2024-002"},
                {"Tarih", "10.03.2024"},
                {"Vade", "10.04.2024"},
                {"Tutar", 45000.75m},
                {"Aciklama", "Mart ayı mal alım faturası"}
            },
            new Dictionary<string, object>
            {
                {"FaturaNo", "FA-2024-003"},
                {"Tarih", "05.04.2024"},
                {"Vade", "05.05.2024"},
                {"Tutar", 20000.50m},
                {"Aciklama", "Nisan ayı mal alım faturası"}
            }
        }
            };

            return Json(detay);
        }

        [HttpGet]
        public JsonResult GetAylikBakiyeDetay()
        {
            var aylikDetay = new[]
            {
        new Dictionary<string, object> { {"Ay", "Şubat 2024"}, {"Bakiye", 35000.25m} },
        new Dictionary<string, object> { {"Ay", "Mart 2024"}, {"Bakiye", 45000.75m} },
        new Dictionary<string, object> { {"Ay", "Nisan 2024"}, {"Bakiye", 20000.50m} },
        new Dictionary<string, object> { {"Ay", "Mayıs 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Haziran 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Temmuz 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Ağustos 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Eylül 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Ekim 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Kasım 2024"}, {"Bakiye", 0m} },
        new Dictionary<string, object> { {"Ay", "Aralık 2024"}, {"Bakiye", 0m} }
    };

            return Json(aylikDetay);
        }

    }
}
