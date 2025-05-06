using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using static Deneme_proje.Models.SirketDurumuEntites;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using Deneme_proje.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Deneme_proje.Models.Entities;
using System.Text;
using System.Configuration;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class SirketDurumuController : BaseController
    {
        private readonly string _connectionString;
        private readonly ILogger<SirketDurumuController> _logger;
        private readonly SirketDurumuRepository _sirketDurumuRepository;
        private readonly DatabaseSelectorService _dbSelectorService;

        public SirketDurumuController(IConfiguration configuration, ILogger<SirketDurumuController> logger, SirketDurumuRepository sirketDurumuRepository, DatabaseSelectorService dbSelectorService)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
            _sirketDurumuRepository = sirketDurumuRepository;
        }

        // Çek Analiz Metodu
        public IActionResult CekAnaliz(string sck_sonpoz, string projeKodu, string srmMerkeziKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            IEnumerable<CekAnalizi> cekAnaliziList;
            IEnumerable<MusteriCekViewModel> musteriCekleriList;

            try
            {
                // Using repository methods to fetch data
                cekAnaliziList = _sirketDurumuRepository.GetFirmaCekleri(sck_sonpoz, projeKodu, srmMerkeziKodu, baslamaTarihi, bitisTarihi);
                musteriCekleriList = _sirketDurumuRepository.GetMusteriCekleri(baslamaTarihi ?? DateTime.MinValue, bitisTarihi ?? DateTime.MaxValue); // Default date range if null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çek analizi verileri alınırken bir hata oluştu.");
                return View("Error");
            }

            // Creating a ViewModel with the fetched data
            var viewModel = new CekAnaliziViewModel
            {
                CekAnaliziList = cekAnaliziList,

            };

            // Passing parameters to the view using ViewData
            ViewData["sck_sonpoz"] = sck_sonpoz;
            ViewData["projeKodu"] = projeKodu;
            ViewData["srmMerkeziKodu"] = srmMerkeziKodu;
            ViewData["baslamaTarihi"] = baslamaTarihi?.ToString("yyyy-MM-dd"); // Format the date properly
            ViewData["bitisTarihi"] = bitisTarihi?.ToString("yyyy-MM-dd");

            return View(viewModel);
        }

        // Banka Analiz Metodu
        public IActionResult BankaAnaliz(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var viewModel = new BankDetailsViewModel
            {
                Banks = _sirketDurumuRepository.GetBanks(),
                BaslamaTarihi = baslamaTarihi ?? DateTime.Now.AddMonths(-1),
                BitisTarihi = bitisTarihi ?? DateTime.Now
            };

            return View(viewModel);
        }

        // Banka Detayları Getirme Metodu
        public IActionResult Getir_Detay(string BankaKodu, DateTime? BaslamaTarihi, DateTime? BitisTarihi)
        {
            List<BankDetailModel> bankDetails;
            BaslamaTarihi ??= DateTime.Now.AddMonths(-1);
            BitisTarihi ??= DateTime.Now;

            try
            {
                bankDetails = _sirketDurumuRepository.GetBankDetails(BankaKodu, BaslamaTarihi.Value, BitisTarihi.Value, 0, null, null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka detayları alınırken bir hata oluştu.");
                return Content("Banka detayları alınırken bir hata oluştu.");
            }

            return PartialView("_BankDetailsGrid", bankDetails);
        }

        // İyileştirilmiş GetCariHareketDetay Controller Metodu
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetCariHareketDetay(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return Content("<div class='alert alert-warning'>GUID değeri geçersiz veya boş.</div>", "text/html");
            }

            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Check if data exists for the given GUID
                    string checkQuery = "SELECT COUNT(*) FROM CARI_HESAP_HAREKETLERI WHERE cha_Guid = @guid";
                    var checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@guid", guid);
                    int recordCount = (int)checkCommand.ExecuteScalar();

                    if (recordCount == 0)
                    {
                        return Content("<div class='alert alert-info'>Bu evrak için detay kaydı bulunamadı.</div>", "text/html");
                    }

                    // Updated query to include sth_vergi
                    string query = @"SELECT 
                sth_fat_uid,
                sth_evrakno_seri,
                sth_evrakno_sira,
                sth_stok_kod,
                sto_isim,
                sth_miktar,
                sth_tutar,
                sth_vergi, -- Added tax field
                cha_tarihi 
            FROM CARI_HESAP_HAREKETLERI 
            LEFT JOIN STOK_HAREKETLERI ON cha_Guid = sth_fat_uid
            LEFT JOIN STOKLAR ON sth_stok_kod = sto_kod 
            WHERE cha_Guid = @guid";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@guid", guid);
                    command.CommandTimeout = 60;

                    var reader = command.ExecuteReader();

                    // Group by stock code and date
                    Dictionary<string, List<dynamic>> gruplar = new Dictionary<string, List<dynamic>>();
                    Dictionary<string, (double toplamMiktar, double toplamMeblag, double toplamVergi)> toplamlar =
                        new Dictionary<string, (double, double, double)>();

                    while (reader.Read())
                    {
                        var stokKod = reader.IsDBNull(reader.GetOrdinal("sth_stok_kod")) ? "-" : reader.GetString(reader.GetOrdinal("sth_stok_kod"));
                        var tarih = reader.IsDBNull(reader.GetOrdinal("cha_tarihi")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("cha_tarihi"));
                        var tarihStr = tarih != DateTime.MinValue ? tarih.ToString("dd.MM.yyyy") : "-";

                        string grupAnahtari = $"{stokKod}_{tarihStr}";

                        var miktar = reader.IsDBNull(reader.GetOrdinal("sth_miktar")) ? 0 : reader.GetDouble(reader.GetOrdinal("sth_miktar"));
                        var meblag = reader.IsDBNull(reader.GetOrdinal("sth_tutar")) ? 0 : reader.GetDouble(reader.GetOrdinal("sth_tutar"));
                        var vergi = reader.IsDBNull(reader.GetOrdinal("sth_vergi")) ? 0 : reader.GetDouble(reader.GetOrdinal("sth_vergi")); // Added tax
                        var birimFiyat = miktar > 0 ? meblag / miktar : 0;

                        var fatUid = reader.IsDBNull(reader.GetOrdinal("sth_fat_uid")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("sth_fat_uid"));
                        var evrakSeri = reader.IsDBNull(reader.GetOrdinal("sth_evrakno_seri")) ? "-" : reader.GetString(reader.GetOrdinal("sth_evrakno_seri"));
                        var evrakSira = reader.IsDBNull(reader.GetOrdinal("sth_evrakno_sira")) ? 0 : reader.GetInt32(reader.GetOrdinal("sth_evrakno_sira"));
                        var stokIsim = reader.IsDBNull(reader.GetOrdinal("sto_isim")) ? "-" : reader.GetString(reader.GetOrdinal("sto_isim"));

                        var hareket = new
                        {
                            FatUid = fatUid,
                            EvrakSeri = evrakSeri,
                            EvrakSira = evrakSira,
                            StokKod = stokKod,
                            StokIsim = stokIsim,
                            Miktar = miktar,
                            Meblag = meblag,
                            Vergi = vergi, // Added tax
                            BirimFiyat = birimFiyat,
                            Tarih = tarihStr
                        };

                        if (!gruplar.ContainsKey(grupAnahtari))
                        {
                            gruplar[grupAnahtari] = new List<dynamic>();
                            toplamlar[grupAnahtari] = (0, 0, 0); // Updated to include tax
                        }

                        gruplar[grupAnahtari].Add(hareket);

                        var mevcutToplam = toplamlar[grupAnahtari];
                        toplamlar[grupAnahtari] = (
                            mevcutToplam.toplamMiktar + miktar,
                            mevcutToplam.toplamMeblag + meblag,
                            mevcutToplam.toplamVergi + vergi // Added tax total
                        );
                    }

                    reader.Close();

                    double genelToplamMiktar = toplamlar.Values.Sum(t => t.toplamMiktar);
                    double genelToplamMeblag = toplamlar.Values.Sum(t => t.toplamMeblag);
                    double genelToplamVergi = toplamlar.Values.Sum(t => t.toplamVergi);
                    double genelToplamVergili = genelToplamMeblag + genelToplamVergi; // Grand total with tax

                    var detailHtml = new StringBuilder();
                    detailHtml.Append("<div class='detail-report-container'>");
                    detailHtml.Append("<h4 class='mb-3'>Evrak Detay Bilgileri</h4>");
                    detailHtml.Append("<div class='table-responsive'>");
                    detailHtml.Append("<table class='table table-striped table-bordered detail-table' id='detailTable'>");

                    detailHtml.Append("<thead class='table-dark'><tr>");
                    detailHtml.Append("<th>Tarih</th>");
                    detailHtml.Append("<th>Stok Adı</th>");
                
                    detailHtml.Append("<th class='text-right'>Toplam Miktar</th>");
                    detailHtml.Append("<th class='text-right'>Birim Fiyat</th>");
         
                    detailHtml.Append("<th class='text-right'>Toplam Meblağ (Vergili)</th>");
   
                    detailHtml.Append("</tr></thead><tbody>");

                    if (gruplar.Count > 0)
                    {
                        foreach (var grup in gruplar)
                        {
                            var ilkHareket = grup.Value.FirstOrDefault();
                            var toplamDegerler = toplamlar[grup.Key];
                            var toplamVergiliMeblag = toplamDegerler.toplamMeblag + toplamDegerler.toplamVergi;

                            if (ilkHareket != null)
                            {
                                detailHtml.Append("<tr>");
                                detailHtml.Append($"<td>{ilkHareket.Tarih}</td>");
                                detailHtml.Append($"<td>{ilkHareket.StokIsim}</td>");
                
                                detailHtml.Append($"<td class='text-right'>{toplamDegerler.toplamMiktar.ToString("N2")}</td>");
                                detailHtml.Append($"<td class='text-right'>{ilkHareket.BirimFiyat.ToString("N2")} ₺</td>");
                              
                                detailHtml.Append($"<td class='text-right'>{toplamVergiliMeblag.ToString("N2")} ₺</td>");
               
                                detailHtml.Append("</tr>");
                            }
                        }

                        // Subtotal (excluding tax)
                        detailHtml.Append("<tr class='table-info font-weight-bold'>");
                        detailHtml.Append("<td  class='text-right'>TOPLAM (Vergisiz)</td>");
                        detailHtml.Append("<td></td>"); // Empty cell for grand total column
                        detailHtml.Append("<td></td>"); // Empty cell for stock code
                        detailHtml.Append("<td></td>"); // Empty cell for stock code

                        detailHtml.Append($"<td class='text-right'>{genelToplamVergili.ToString("N2")} ₺</td>");
                     
                        detailHtml.Append("</tr>");

                     
                    }
                    else
                    {
                        detailHtml.Append("<tr><td colspan='9' class='text-center'>Bu evrak için detay bulunamadı.</td></tr>");
                    }

                    detailHtml.Append("</tbody></table></div></div>");
                    return Content(detailHtml.ToString(), "text/html");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cari hareket detayları getirilirken hata oluştu. GUID: {Guid}", guid);
                    return Content($"<div class='alert alert-danger'><i class='fa fa-exclamation-circle'></i> Veriler getirilirken hata oluştu: {ex.Message}</div>", "text/html");
                }
            }
        }

        private bool ShouldSkipColumn(string columnName)
        {
            // Atlanacak kolonların listesi
            var columnsToSkip = new List<string>
    {
        // Burada göstermek istemediğiniz kolonları ekleyin
        "cha_Guid", // GUID zaten biliniyor
        // Diğer gösterilmeyecek kolonlar...
    };

            return columnsToSkip.Contains(columnName);
        }
        // Cari Hareket Metodu
        [HttpGet]
        public JsonResult GetCariHesaplar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var cariList = new List<object>();

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Query to fetch all cari accounts from CARI_HESAPLAR
                    string query = @"SELECT 
                            cari_kod, 
                            cari_unvan1 
                            FROM CARI_HESAPLAR 
                            ORDER BY cari_kod";

                    var command = new SqlCommand(query, connection);
                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        cariList.Add(new
                        {
                            Kod = reader["cari_kod"].ToString(),
                            Unvan = reader["cari_unvan1"].ToString()
                        });
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cari hesaplar listelenirken hata oluştu.");
                }
            }

            return Json(cariList);
        }

        public IActionResult CariHareket(int cariCins, string cariKod, string selectedCariText, DateTime? ilkTar, DateTime? sonTar)
        {
            string firmalar = "0";
            int? grupNo = null;
            int odemeEmriDegerlemeDok = 0;

            // Default date range to current month if not provided
            if (!ilkTar.HasValue)
            {
                // First day of current month
                ilkTar = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (!sonTar.HasValue)
            {
                // Last day of current month
                sonTar = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                                       DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            // Get the cari kodlari for dropdown
            var cariKodlari = _sirketDurumuRepository.GetCariKodlari();

            // Seçilen cari koduna ait unvan bilgisini al
            string selectedCariUnvan = null;
            if (!string.IsNullOrEmpty(cariKod))
            {
                var selectedCari = cariKodlari.FirstOrDefault(c => c.CariKod == cariKod);
                if (selectedCari != null)
                {
                    selectedCariUnvan = selectedCari.CariUnvan1;
                    // If selectedCariText is not provided, construct it
                    if (string.IsNullOrEmpty(selectedCariText))
                    {
                        selectedCariText = $"{cariKod} - {selectedCariUnvan}";
                    }
                }
            }

            // Get customer transaction data
            var cariHareketFoyu = _sirketDurumuRepository.GetCariHareketFoyu(
                firmalar, cariCins, cariKod, grupNo, null, ilkTar, sonTar, odemeEmriDegerlemeDok, "");

            // ViewData'ya bilgileri aktar
            ViewData["CariKodlari"] = Newtonsoft.Json.JsonConvert.SerializeObject(cariKodlari);
            ViewData["SelectedCariKod"] = cariKod;
            ViewData["SelectedCariUnvan"] = selectedCariUnvan;
            ViewData["SelectedCariText"] = selectedCariText; // Kullanıcının gördüğü tam metin
            ViewData["SelectedCariCins"] = cariCins;
            ViewData["IlkTarih"] = ilkTar?.ToString("yyyy-MM-dd");
            ViewData["SonTarih"] = sonTar?.ToString("yyyy-MM-dd");

            return View(cariHareketFoyu);
        }        // Stok Hareket Metodu
        public IActionResult StokHareket(string stokKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi, int paraBirimi = 0, string depolar = null)
        {
            try
            {
                var depolarList = _sirketDurumuRepository.GetDepolar();
                var stoklarList = _sirketDurumuRepository.GetStoklar();

                ViewBag.Depolar = depolarList;
                ViewBag.Stoklar = stoklarList;

                // Varsayılan tarih aralığı
                if (!baslamaTarihi.HasValue)
                    baslamaTarihi = DateTime.Now.AddMonths(-1);

                if (!bitisTarihi.HasValue)
                    bitisTarihi = DateTime.Now;

                // Depo seçimi kontrolü ve loglaması
                _logger.LogInformation("Controller'da depolar parametresi: {depolar}", depolar ?? "NULL");

                // Stok kodu kontrolü
                if (string.IsNullOrEmpty(stokKodu))
                {
                    return View(Enumerable.Empty<StokHareketFoyu>());
                }

                // Stok hareketlerini al
                var stokHareketFoyu = _sirketDurumuRepository.GetStokHareketFoyu(
                    stokKodu,
                    baslamaTarihi.Value,
                    bitisTarihi.Value,
                    paraBirimi,
                    depolar
                );

                // ViewBag'e değerleri ekle
                ViewBag.SelectedStokKodu = stokKodu;
                ViewBag.BaslamaTarihi = baslamaTarihi.Value.ToString("yyyy-MM-dd");
                ViewBag.BitisTarihi = bitisTarihi.Value.ToString("yyyy-MM-dd");
                ViewBag.ParaBirimi = paraBirimi;
                ViewBag.SelectedDepolar = depolar;

                return View(stokHareketFoyu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok hareket verileri alınırken bir hata oluştu. StokKodu: {StokKodu}", stokKodu);

                TempData["ErrorMessage"] = "Stok hareket verileri alınırken bir hata oluştu: " + ex.Message;

                var depolarList = _sirketDurumuRepository.GetDepolar();
                var stoklarList = _sirketDurumuRepository.GetStoklar();

                ViewBag.Depolar = depolarList;
                ViewBag.Stoklar = stoklarList;

                return View(Enumerable.Empty<StokHareketFoyu>());
            }
        }
        [AllowAnonymous]
        public IActionResult EvrakAktarim()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult KasaRaporu(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            // Varsayılan tarih aralığı: cari ayın başlangıcından bugüne
            baslamaTarihi ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            bitisTarihi ??= DateTime.Now;

            try
            {
                // Ödemeleri çek
                var odemeler = _sirketDurumuRepository.GetKasaRaporuOdemeleri(
                    baslamaTarihi.Value,
                    bitisTarihi.Value
                ).ToList();

                // Tahsilatları çek
                var tahsilatlar = _sirketDurumuRepository.GetKasaRaporuTahsilatlari(
                    baslamaTarihi.Value,
                    bitisTarihi.Value
                ).ToList();

                // Toplam bakiyeleri tarih filtresinden bağımsız olarak çek
                var toplamBakiyeListesi = _sirketDurumuRepository.GetToplamBakiyeListesi();

                // Para birimlerine göre gruplanmış toplamlar
                var tlToplam = toplamBakiyeListesi.Where(b => b.DovizKodu == 0).Sum(b => b.Bakiye);
                var usdToplam = toplamBakiyeListesi.Where(b => b.DovizKodu == 1).Sum(b => b.Bakiye);
                var euroToplam = toplamBakiyeListesi.Where(b => b.DovizKodu == 2).Sum(b => b.Bakiye);

                // Satış faturalarının tekilleştirilmesi (tekrarlı olanların kaldırılması)
                var satisFaturalari = tahsilatlar.Where(t => t.EvrakTipi == "Satış Faturası").ToList();
                var tekillesmisF = satisFaturalari
                    .GroupBy(h => new { h.Açıklama, h.Tutar })
                    .Select(g => g.First())
                    .ToList();

                // Tekilleştirilmiş satış faturaları ile diğer tahsilatları birleştir
                var filtrelenmisT = tahsilatlar
                    .Where(h => h.EvrakTipi != "Satış Faturası")
                    .Concat(tekillesmisF)
                    .OrderBy(h => h.VadeTarihi)
                    .ToList();

                // Kredi kartlarını banka ve evrak tipine göre grupla
                var krediKartlariO = odemeler.Where(o => o.EvrakTipi.Contains("Kredi Kartı")).ToList();
                var krediKartlariT = filtrelenmisT.Where(t => t.EvrakTipi.Contains("Kredi Kartı")).ToList();

                var grupluKrediKartlariO = krediKartlariO
                    .GroupBy(h => new { h.Banka, h.EvrakTipi })
                    .Select(g => new KasaRaporuOdemeModel
                    {
                        VadeTarihi = g.First().VadeTarihi,
                        EvrakTipi = g.Key.EvrakTipi,
                        Açıklama = g.Key.EvrakTipi,
                        Banka = g.Key.Banka,
                        Tutar = g.Sum(x => x.Tutar),
                        PB = g.First().PB
                    })
                    .ToList();

                var grupluKrediKartlariT = krediKartlariT
                    .GroupBy(h => new { h.Banka, h.EvrakTipi })
                    .Select(g => new KasaRaporuOdemeModel
                    {
                        VadeTarihi = g.First().VadeTarihi,
                        EvrakTipi = g.Key.EvrakTipi,
                        Açıklama = g.Key.EvrakTipi,
                        Banka = g.Key.Banka,
                        Tutar = g.Sum(x => x.Tutar),
                        PB = g.First().PB
                    })
                    .ToList();

                // Kredi kartı dışındaki hareketleri al
                var filtrelenmisO = odemeler
                    .Where(h => !h.EvrakTipi.Contains("Kredi Kartı"))
                    .ToList();

                var filtrelenmisT2 = filtrelenmisT
                    .Where(h => !h.EvrakTipi.Contains("Kredi Kartı"))
                    .ToList();

                // Son ödemeler ve tahsilatlar listelerini oluştur
                var sonOdemeler = filtrelenmisO.Concat(grupluKrediKartlariO).OrderBy(o => o.VadeTarihi).ToList();
                var sonTahsilatlar = filtrelenmisT2.Concat(grupluKrediKartlariT).OrderBy(t => t.VadeTarihi).ToList();

                // Toplam tutar ve diğer özet bilgileri hesapla
                ViewBag.ToplamOdemeTutari = sonOdemeler.Sum(o => o.Tutar);
                ViewBag.ToplamTahsilatTutari = sonTahsilatlar.Sum(t => t.Tutar);
                ViewBag.NetBakiye = ViewBag.ToplamTahsilatTutari - ViewBag.ToplamOdemeTutari;
                ViewBag.ToplamOdemeSayisi = sonOdemeler.Count();
                ViewBag.ToplamTahsilatSayisi = sonTahsilatlar.Count();
                ViewBag.ToplamBakiyeListesi = toplamBakiyeListesi;
                ViewBag.KasadakiToplamPara = toplamBakiyeListesi.Sum(b => b.Bakiye);

                // Para birimlerine göre toplamları gönder
                ViewBag.TLToplam = tlToplam;
                ViewBag.USDToplam = usdToplam;
                ViewBag.EuroToplam = euroToplam;

                // Tarihleri viewdata'ya ekle
                ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
                ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");

                // ViewBag'leri güncelle
                ViewBag.Odemeler = sonOdemeler;
                ViewBag.Tahsilatlar = sonTahsilatlar;

                // Veya model olarak birleştirilmiş listeyi gönderebiliriz
                var tumHareketler = sonOdemeler.Concat(sonTahsilatlar).OrderBy(x => x.VadeTarihi).ToList();
                return View(tumHareketler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa raporu yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "Kasa raporu yüklenemedi. Lütfen daha sonra tekrar deneyin.";
                return View(Enumerable.Empty<KasaRaporuOdemeModel>());
            }
        }


            [AllowAnonymous]
        public IActionResult RiskRaporu()
        {
            try
            {
                var model = _sirketDurumuRepository.GetBankaLimitleri();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka limitleri yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "Banka limitleri yüklenemedi. Lütfen daha sonra tekrar deneyin.";
                return View(new List<BankaLimitViewModel>());
            }
        }
    }
}
