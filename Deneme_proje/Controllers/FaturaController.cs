using ClosedXML.Excel;
using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ILogger için gerekli
using System.Collections.Generic;
using System.Linq;
using static Deneme_proje.Models.Entities;

using System.IO;
using NuGet.Protocol.Core.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using Dapper;
namespace Deneme_proje.Controllers
{
    [AuthFilter]

    public class FaturaController : BaseController
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<FaturaController> _logger;
        private readonly FaturaRepository _faturaRepository;
        private readonly IConfiguration _configuration; // BUNU EKLEYİN

        // Constructor
        public FaturaController(
            ILogger<FaturaController> logger,
            FaturaRepository faturaRepository,
            DatabaseSelectorService dbSelectorService,
            IConfiguration configuration) // BUNU EKLEYİN
        {
            _logger = logger;
            _faturaRepository = faturaRepository;
            _dbSelectorService = dbSelectorService;
            _configuration = configuration; // BUNU EKLEYİN
        }

        public ActionResult Index(string cariKodu, DateTime? vadeBaslangic, DateTime? vadeBitis)
        {
            float ticariFaiz = 66.24f;

            // Eğer tarih girilmemişse, yılın başı ve sonu olarak ayarla
            DateTime defaultBaslangic = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime defaultBitis = new DateTime(DateTime.Now.Year, 12, 31);

            var baslangic = vadeBaslangic ?? defaultBaslangic;
            var bitis = vadeBitis ?? defaultBitis;

            var faturaData = _faturaRepository.GetFaturaData(cariKodu, ticariFaiz)
                              .Where(x => x.FaturaVadeTarihi >= baslangic && x.FaturaVadeTarihi <= bitis)
                              .ToList();

            // ViewBag'e aktar
            ViewBag.VadeBaslangic = baslangic.ToString("yyyy-MM-dd");
            ViewBag.VadeBitis = bitis.ToString("yyyy-MM-dd");

            return View(faturaData);
        }




        public IActionResult TedarikciKapaliFatura(string cariKodu, DateTime? vadeBaslangic, DateTime? vadeBitis)
        {
            float ticariFaiz = 66.24f;

            // Eğer tarih girilmemişse, yılın başı ve sonu olarak ayarla
            DateTime defaultBaslangic = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime defaultBitis = new DateTime(DateTime.Now.Year, 12, 31);

            var baslangic = vadeBaslangic ?? defaultBaslangic;
            var bitis = vadeBitis ?? defaultBitis;

            // Tedarikçi fatura verilerini al ve tarih filtresi uygula
            var faturaData = _faturaRepository.GetTedarikciFaturaData(cariKodu, ticariFaiz)
                              .Where(x => x.FaturaVadeTarihi >= baslangic && x.FaturaVadeTarihi <= bitis)
                              .ToList();

            // ViewBag'e filtreleri aktar
            ViewBag.CariKodu = cariKodu;
            ViewBag.VadeBaslangic = baslangic.ToString("yyyy-MM-dd");
            ViewBag.VadeBitis = bitis.ToString("yyyy-MM-dd");

            return View(faturaData);
        }

        public IActionResult CustomerAnalysis(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var customerAnalysisData = _faturaRepository.GetFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
            return View(customerAnalysisData);
        }

        public IActionResult CariBazliTedarikci(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var customerAnalysisData = _faturaRepository.GetTedarikciFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
            return View(customerAnalysisData);
        }

        public IActionResult MaliBorc()
        {
            var krediDetayData = _faturaRepository.GetKrediDetayData();
            return View(krediDetayData);
        }



        [AllowAnonymous]// Action to get detailed credit information by bank code
        public IActionResult GetKrediDetay(string bankCode)
        {
            try
            {
                var krediDetayListesi = _faturaRepository.GetKrediDetayListByBankCode(bankCode);

                if (krediDetayListesi == null || !krediDetayListesi.Any())
                {
                    ViewBag.ErrorMessage = "No data found for the provided bank code.";
                    return PartialView("_KrediDetayPartial", new Dictionary<string, Dictionary<string, List<KrediDetayi>>>());
                }

                var groupedData = krediDetayListesi
                    .GroupBy(d => d.krsoztaksit_sozkodu)
                    .ToDictionary(
                        g => g.Key, // Contract Code
                        g => g.GroupBy(d => d.AyAd).ToDictionary(
                            gg => gg.Key, // Month
                            gg => gg.ToList()
                        )
                    );

                return PartialView("_KrediDetayPartial", groupedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving kredi detay for bank code: {BankCode}", bankCode);
                return PartialView("_Error", new ErrorViewModel { ErrorMessage = "An error occurred while retrieving details." });
            }
        }

        public ActionResult aokf()
        {
            var krediDetayListesi = _faturaRepository.GetKrediDetayList();
            return View(krediDetayListesi);
        }

        public ActionResult MusteriYaslandirma(string cariIlkKod = "", string cariSonKod = "", string cariKodYapisi = "", DateTime? raporTarihi = null, byte hangiHesaplar = 0)
        {
            var data = _faturaRepository.GetCariMusteriYaslandirma(cariIlkKod, cariSonKod, cariKodYapisi, raporTarihi, hangiHesaplar);
            return View(data);
        }
        public ActionResult TedarikciYaslandirma(string cariIlkKod = "", string cariSonKod = "", string cariKodYapisi = "", DateTime? raporTarihi = null, byte hangiHesaplar = 0)
        {
            var data = _faturaRepository.GetCariTedarikciYaslandirma(cariIlkKod, cariSonKod, cariKodYapisi, raporTarihi, hangiHesaplar);
            return View(data);
        }
        public IActionResult StokYaslandirma(string stockCode = null, DateTime? reportDate = null, int? depoNo = null)
        {
            // Varsayılan olarak bugünün tarihini kullan
            reportDate ??= DateTime.Now;

            // Stok kodları ve isimlerini al
            var stockCodesAndNames = _faturaRepository.GetStockCodesAndNames();
            var stockSelectList = stockCodesAndNames
                .Select(x => new SelectListItem { Value = x.StockCode, Text = $"{x.StockCode} - {x.StockName}" })
                .ToList();
            ViewData["StockCodesAndNames"] = stockSelectList;

            // Depo numarası ve adlarını al
            var depoList = _faturaRepository.GetDepoList();
            var depoSelectList = depoList
                .Select(d => new SelectListItem { Value = d.DepoNo.ToString(), Text = d.DepoAdi })
                .ToList();
            ViewData["DepoList"] = depoSelectList;

            // Verileri al, depo numarası veya stok kodu filtreleri uygula (eğer varsa)
            var data = _faturaRepository.GetStokYaslandirma(stockCode, reportDate.Value, depoNo);

            // Debug için log ekleyin
            System.Diagnostics.Debug.WriteLine($"StokCode: {stockCode}");
            System.Diagnostics.Debug.WriteLine($"Data Count: {data.Count()}");
            foreach (var item in data)
            {
                System.Diagnostics.Debug.WriteLine($"Stok: {item.MsgS0078}, Seri: {item.StokEvraknoSeri}, IsNegative: {item.IsNegativeStock}");
            }

            if (!data.Any())
            {
                ViewBag.Message = string.IsNullOrEmpty(stockCode)
                    ? "Veri bulunamadı. Stok kodu veya depo seçimi yapabilirsiniz."
                    : $"'{stockCode}' stok kodu için veri bulunamadı.";
            }
            else
            {
                ViewBag.Message = $"{data.Count()} kayıt bulundu.";
            }

            ViewData["SelectedStockCode"] = stockCode;
            ViewData["SelectedDepoNo"] = depoNo;
            ViewData["HasData"] = data.Any(); // Veri olup olmadığını view'a gönder

            return View(data);
        }



        public IActionResult Stok()
        {
            // Fetch stock codes and names
            var stockCodesAndNames = _faturaRepository.GetStockCodesAndNames();

            // Prepare the view model
            var viewModel = new StokViewModel
            {
                StockCodes = stockCodesAndNames.Select(x => x.StockCode).ToList()
            };

            // Return the view with the view model
            return View(viewModel);
        }
        public ActionResult StockAging(string stokKod, DateTime? raporTarihi)
        {
            // Eğer stok kodu boş veya null ise formu tekrar göster
            if (string.IsNullOrEmpty(stokKod))
            {
                return View(); // Kullanıcıdan stok kodu girmesini bekliyoruz
            }

            // Eğer stok kodu girilmişse raporu getir
            var stockAgingList = _faturaRepository.GetStockAging(stokKod, raporTarihi);

            if (stockAgingList == null || !stockAgingList.Any())
            {
                // Eğer rapor boşsa, kullanıcıya stok kodu bulunamadığı mesajını ver
                ViewBag.ErrorMessage = "Girilen stok kodu için rapor bulunamadı.";
                return View(); // Formu tekrar göster
            }

            // Eğer rapor varsa, sonuçları kullanıcıya göster
            return View(stockAgingList); // Rapor sonuçlarını model olarak gönderiyoruz
        }


        [HttpGet]
        public IActionResult NakitAkisi()
        {
            return View();
        }

        // POST method to process the form and show data
        [HttpPost]
        [AllowAnonymous]
        public IActionResult NakitAkisi(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            // Verilerin List'e dönüştürülmesi
            var musteriCekleri = _faturaRepository.GetMusteriCekleri(baslamaTarihi, bitisTarihi).ToList();
            var firmaCekleri = _faturaRepository.GetFirmaCekleri(baslamaTarihi, bitisTarihi).ToList();
            var musteriKrediKartlari = _faturaRepository.GetMusteriKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var firmaKrediKartlari = _faturaRepository.GetFirmaKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var artiBakiyeFaturaMusteri = _faturaRepository.GetArtiBakiyeFaturaMusteri(baslamaTarihi, bitisTarihi).ToList();
            var artiBakiyeFaturaTedarikci = _faturaRepository.GetArtiBakiyeFaturaTedarikci(baslamaTarihi, bitisTarihi).ToList();

            // Yeni: Kredi Detayları alınması
            var krediDetaylari = _faturaRepository.GetKrediDetay(baslamaTarihi, bitisTarihi).ToList();

            var viewModel = new CekDurumuViewModel
            {
                BaslamaTarihi = baslamaTarihi,
                BitisTarihi = bitisTarihi,
                MusteriCekleri = musteriCekleri,
                FirmaCekleri = firmaCekleri,
                MusteriKrediKartlari = musteriKrediKartlari,
                FirmaKrediKartlari = firmaKrediKartlari,
                ArtiBakiyeFaturaMusteri = artiBakiyeFaturaMusteri,
                ArtiBakiyeFaturaTedarikci = artiBakiyeFaturaTedarikci,
                KrediDetaylari = krediDetaylari // Yeni eklenen özellik
            };

            return View(viewModel);
        }

        public IActionResult CiroRaporuDepoBazli(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1); // Varsayılan 1 ay önce
            bitisTarihi ??= DateTime.Now;

            var ciroRaporu = _faturaRepository.GetCiroRaporuDepoBazli(baslamaTarihi.Value, bitisTarihi.Value);

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");

            return View(ciroRaporu);
        }

        public IActionResult EnCokSatilan(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1); // Varsayılan 1 ay önce
            bitisTarihi ??= DateTime.Now;

            var urunRaporu = _faturaRepository.GetEnCokSatilanUrunler(baslamaTarihi.Value, bitisTarihi.Value);

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");

            return View(urunRaporu);
        }

        public IActionResult SatilanMalinKarlilikveMaliyet(DateTime? baslamaTarihi, DateTime? bitisTarihi, string depoNo = "")
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1);
            bitisTarihi ??= DateTime.Now;

            var rapor = _faturaRepository.GetSatilanMalinKarlilikveMaliyet(baslamaTarihi.Value, bitisTarihi.Value, depoNo);

            // Depo listesini al ve ViewData'ya ekle
            var depoList = _faturaRepository.GetDepoList();
            var depoSelectList = depoList
                .Select(d => new SelectListItem { Value = d.DepoNo.ToString(), Text = d.DepoAdi })
                .ToList();

            ViewData["DepoList"] = depoSelectList;

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["DepoNo"] = depoNo;

            return View(rapor);
        }

        public IActionResult StokRaporu(int? anaGrup = null, int? reyonKodu = null, int? depoNo = null)
        {
            if (!depoNo.HasValue)
            {
                // Kullanıcı depo seçmediyse sayfa formu göster
                ViewData["ErrorMessage"] = "Lütfen bir depo numarası seçiniz.";
                return View();
            }

            try
            {
                var stokRaporu = _faturaRepository.GetStokRaporu(anaGrup, reyonKodu, depoNo.Value);

                if (!stokRaporu.Any())
                {
                    ViewBag.Message = "Arama kriterlerinize uygun sonuç bulunamadı.";
                }

                return View(stokRaporu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok raporu oluşturulurken hata oluştu.");
                ViewData["ErrorMessage"] = "Stok raporu oluşturulurken bir hata meydana geldi.";
                return View();
            }
        }




        public IActionResult MusteriRiskAnalizi(DateTime? raporTarihi = null)
        {
            try
            {
                var data = _faturaRepository.GetMusteriRiskAnalizi(raporTarihi);
                ViewData["RaporTarihi"] = raporTarihi?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri risk analizi görüntülenirken hata oluştu");
                TempData["HataMesaji"] = "Risk analizi oluşturulurken bir hata meydana geldi.";
                return View("Hata");
            }
        }

        public IActionResult SiparisDurum(string filter = "all")
        {
            // Başlangıç tarihi bugünden 15 gün öncesi
            var startDate = DateTime.Now.AddDays(-100);
            // Bitiş tarihi bugün
            var endDate = DateTime.Now;

            var siparisler = _faturaRepository.GetSiparisDetay(startDate, endDate);

            // Filtreleme işlemi
            if (filter == "started")
            {
                siparisler = siparisler.Where(s => s.IslemDurumu == "Basladi");
            }

            ViewData["CurrentFilter"] = filter; // Aktif filtreyi view'a gönder
            return View(siparisler);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetSiparisDurumData(string filter = "all")
        {
            var startDate = DateTime.Now.AddDays(-100); // Son 100 gün
            var endDate = DateTime.Now;

            var siparisler = _faturaRepository.GetSiparisDetay(startDate, endDate);

            // Filtreleme
            if (filter == "started")
            {
                siparisler = siparisler.Where(s => s.IslemDurumu == "Basladi");
            }

            // JSON formatına dönüştür
            var jsonData = siparisler.Select(s => new
            {
                CariAdi = s.CariAdi,
                EvrakSira = s.EvrakSira,
                SiparisTarihi = s.SiparisTarihi.ToString("dd.MM.yyyy"),
                RampaBilgisi = s.RampaBilgisi,
                IslemDurumu = s.IslemDurumu,
                SiparisGuid = s.SiparisGuid
            });

            return Json(new { success = true, data = jsonData });
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateSiparisDurum(int evrakSira, Guid siparisGuid, string rampaBilgisi, string islemDurumu)
        {
            try
            {
                var result = _faturaRepository.UpdateSiparisDurum(evrakSira, siparisGuid, rampaBilgisi, islemDurumu);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu güncellenirken hata oluştu");
                return Json(new RampUpdateResult
                {
                    Success = false,
                    Message = "İşlem sırasında bir hata oluştu."
                });
            }
        }


        [HttpGet]
        [AllowAnonymous]

        public IActionResult StokHareketleriniGetir(string siparisGuid)
        {
            _logger.LogInformation($"Stok hareketleri istendi. SiparisGuid: {siparisGuid}");

            try
            {
                var stokHareketleri = _faturaRepository.GetStokHareketleri(siparisGuid);
                return Json(new
                {
                    success = true,
                    data = stokHareketleri,
                    count = stokHareketleri.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok hareketleri getirilirken hata oluştu");
                return Json(new
                {
                    success = false,
                    error = "Stok hareketleri yüklenirken bir hata oluştu.",
                    message = ex.Message
                });
            }
        }




        // CanliBilancoController.cs dosyasında yapılacak değişiklikler
        // Mevcut IActionResult CanliBilanco() metodu güncelleniyor
        public IActionResult CanliBilanco()
        {
            // Temel bilanço verilerini al
            var kasaBilgisi = _faturaRepository.GetKasaToplami();
            var gelecekGiderBilgisi = _faturaRepository.GetGelecekAylaraAitGider();
            var digerCesitliAlacaklarBilgisi = _faturaRepository.GetDigerCesitliAlacaklar();
            var isAvanslariBilgisi = _faturaRepository.GetIsAvanslari();
            var devredenKdvBilgisi = _faturaRepository.GetDevredenKdv();
            var finansalKiralamaBorcBilgisi = _faturaRepository.GetFinansalKiralamaBorclar();
            var ertelenmisFinansalKiralamaBilgisi = _faturaRepository.GetErtelenmisFinansalKiralama();
            var digerMaliBorclarBilgisi = _faturaRepository.GetDigerMaliBorclar();
            var digerMaliBorclarBilgisiDetay = _faturaRepository.GetDigerMaliBorclarDetay();
            var alinanDepozitoVeTeminatBilgisi = _faturaRepository.GetAlinanDepozitoVeTeminatlar();
            var verilenDepozitoVeTeminatBilgisi = _faturaRepository.GetVerilenDepozitoVeTeminatlar();
            var verilenDepozitoVeTeminatlarDetay = _faturaRepository.GetVerilenDepozitoVeTeminatlarDetay();
            var personelBorclariBilgisi = _faturaRepository.GetPersonelBorclari();
            var odenecekVergiVeFonBilgisi = _faturaRepository.GetOdenecekVergiVeFonlar();
            var odenecekSosyalGuvenlikKesintileriBilgisi = _faturaRepository.GetOdenecekSosyalGuvenlikKesintileri();
            var odenecekDigerYukumlulukler = _faturaRepository.GetOdenecekDigerYukumlulukler();
            var gelecekAylaraAitGelirGiderTahmini = _faturaRepository.GetGelecekAylaraAitGelirGiderTahmini();
            var ortaklaraBorclar = _faturaRepository.GetOrtaklaraBorclar();
            var personelAvanslari = _faturaRepository.GetPersonelAvanslari();
            var digerBorclar = _faturaRepository.GetDigerBorclar();
            var digerCesitliBorclar = _faturaRepository.GetDigerCesitliBorclar();
            var supheliTicariAlacaklar = _faturaRepository.GetSupheliTicariAlacaklar();

            // Diğer varlık verileri
            var verilenSiparisAvanslari = _faturaRepository.GetVerilenSiparisAvanslari();
            var verilenSiparisAvanslariDetay = _faturaRepository.GetVerilenSiparisAvanslariDetay();
            var ortaklardanAlacaklar = _faturaRepository.GetOrtaklardanAlacaklar();
            var ortaklardanAlacaklarDetay = _faturaRepository.GetOrtaklardanAlacaklarDetay();
            var personeldenAlacaklar = _faturaRepository.GetPersoneldenAlacaklar();
            var digerStoklar = _faturaRepository.GetDigerStoklar();
            var digerStoklarDetay = _faturaRepository.GetDigerStoklarDetay();
            var pesinOdenenVergiveFon = _faturaRepository.GetPesinOdenenVergiVeFon();
            var sayimTesellumNoksanlari = _faturaRepository.GetSayimVeTesellumNoksanlari();
            var stokDepoDagilimi = _faturaRepository.GetStokDepoDagilimi();
            _logger.LogInformation($"Stok Depo Dağılımı: {stokDepoDagilimi.Count} adet stok bulundu");

            // Bugünden itibaren 1 yıllık dönem için tarih aralığı belirle
            var baslamaTarihi = DateTime.Now;
            var bitisTarihi = DateTime.Now.AddYears(10); // 10 yıl sonrası

            // Müşteri kredi kartları ve diğer veriler
            var musteriKrediKartlari = _faturaRepository.GetMusteriKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var firmaKrediKartlari = _faturaRepository.GetFirmaKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var musteriCekleri = _faturaRepository.GetMusteriCekleri(baslamaTarihi, bitisTarihi).ToList();
            var firmaCekleri = _faturaRepository.GetFirmaCekleri(baslamaTarihi, bitisTarihi).ToList();
            var bankaHesaplari = _faturaRepository.GetBankaHesaplari();

            // Güncellenen metotları çağırıyoruz
            var artiBakiyeliMusteriler = _faturaRepository.GetArtiBakiyeliMusteriFaturaları();
            var artiBakiyeliTedarikciler = _faturaRepository.GetArtiBakiyeliTedarikciFaturalari();
            var eksiBakiyeliFaturalar = _faturaRepository.GetEksiBakiyeliFaturalar();
            var eksiBakiyeliTedarikciler = _faturaRepository.GetEksiBakiyeliTedarikciFaturalari();
            var krediDetaylari = _faturaRepository.GetKrediDetayList().ToList();
            var stokMaliyetBilgileri = _faturaRepository.GetStokMaliyetBilgileri();
            // View model oluştur ve verileri ata

            var viewModel = new CanliBilancoViewModel
            {
                // Temel bilanço verileri
                KasaBilgisi = kasaBilgisi,
                GelecekGiderBilgisi = gelecekGiderBilgisi,
                DigerCesitliAlacaklarBilgisi = digerCesitliAlacaklarBilgisi,
                IsAvanslariBilgisi = isAvanslariBilgisi,
                DevredenKdvBilgisi = devredenKdvBilgisi,
                FinansalKiralamaBorcBilgisi = finansalKiralamaBorcBilgisi,
                ErtelenmisFinansalKiralamaBilgisi = ertelenmisFinansalKiralamaBilgisi,
                DigerMaliBorclarBilgisi = digerMaliBorclarBilgisi,
                DigerMaliBorclarDetay = digerMaliBorclarBilgisiDetay,
                AlinanDepozitoVeTeminatBilgisi = alinanDepozitoVeTeminatBilgisi,
                VerilenDepozitoVeTeminatBilgisi = verilenDepozitoVeTeminatBilgisi,
                PersonelBorclariBilgisi = personelBorclariBilgisi,
                VerilenDepozitoVeTeminatlarDetay = verilenDepozitoVeTeminatlarDetay,
                OdenecekVergiVeFonBilgisi = odenecekVergiVeFonBilgisi,
                OdenecekSosyalGuvenlikKesintileriBilgisi = odenecekSosyalGuvenlikKesintileriBilgisi,
                OdenecekDigerYukumlulukler = odenecekDigerYukumlulukler,
                GelecekAylaraAitGelirGiderTahmini = gelecekAylaraAitGelirGiderTahmini,
                OrtaklaraBorclar = ortaklaraBorclar,
                PersonelAvanslari = personelAvanslari,
                DigerBorclar = digerBorclar,
                DigerCesitliBorclar = digerCesitliBorclar,

                // Diğer varlık verileri
                VerilenSiparisAvanslari = verilenSiparisAvanslari,
                VerilenSiparisAvanslariDetay = verilenSiparisAvanslariDetay,
                OrtaklardanAlacaklar = ortaklardanAlacaklar,
                OrtaklardanAlacaklarDetay = ortaklardanAlacaklarDetay,
                PersoneldenAlacaklar = personeldenAlacaklar,
                DigerStoklar = digerStoklar,
                DigerStoklarDetay = digerStoklarDetay,
                PesinOdenenVergiveFon = pesinOdenenVergiveFon,
                SayimveTesellumNoksanlari = sayimTesellumNoksanlari,
                OdenecekDigerYumunlulukler = odenecekDigerYukumlulukler,
                SupheliTicariAlacaklar = supheliTicariAlacaklar,

                // Çek ve finansal veriler
                ArtiBakiyeliMusteriler = artiBakiyeliMusteriler,
                ArtiBakiyeliTedarikciler = artiBakiyeliTedarikciler,
                EksiBakiyeliMusteriler = eksiBakiyeliFaturalar,
                EksiBakiyeliTedarikciler = eksiBakiyeliTedarikciler,
                StokDepoDagilimi = stokDepoDagilimi,
                MusteriKrediKartlari = musteriKrediKartlari,
                FirmaKrediKartlari = firmaKrediKartlari,
                BankaHesaplari = bankaHesaplari,
                MusteriCekleri = musteriCekleri,
                FirmaCekleri = firmaCekleri,
                BaslamaTarihi = baslamaTarihi,
                BitisTarihi = bitisTarihi,
                KrediDetaylari = krediDetaylari,
                StokMaliyeti= stokMaliyetBilgileri
            };

            return View(viewModel);
        }
        [AllowAnonymous]
        public IActionResult SiparisYuklemeRampa1()
        {
            try
            {
                var siparisData = _faturaRepository.GetSiparisYuklemeRampaDetaylari("Rampa1");

                // AJAX isteği olup olmadığını kontrol et
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // İrsaliye numarası 0 ise ve listede geçerli bir irsaliye numarası varsa onu kullan
                    int maxIrsaliyeNo = 0;

                    foreach (var item in siparisData)
                    {
                        int currentIrsaliye = 0;
                        if (item.IrsaliyeNo != null && int.TryParse(item.IrsaliyeNo.ToString(), out currentIrsaliye) && currentIrsaliye > 0)
                        {
                            maxIrsaliyeNo = Math.Max(maxIrsaliyeNo, currentIrsaliye);
                        }
                    }

                    // JSON dönüşümünde sipariş no ve irsaliye no bilgilerini dahil et
                    return Json(siparisData.Select(x => new
                    {
                        urunAdi = x.UrunAdi,
                        toplamSiparisMiktari = x.ToplamSiparisMiktari,
                        kalanMiktar = x.KalanMiktar,
                        yuklenenMiktar = x.YuklenenMiktar,
                        cariUnvan = x.CariUnvan,
                        siparisDurumu = x.SiparisDurumu,
                        // Sipariş no ve irsaliye no bilgilerini ekleyin
                        evrakSira = x.EvrakSira,
                        irsaliyeNo = IsValidIrsaliyeNo(x.IrsaliyeNo) ? x.IrsaliyeNo : maxIrsaliyeNo
                    }));
                }

                // Normal sayfa yüklemesi için view dön
                return View(siparisData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rampa1 sipariş yükleme sayfasında hata oluştu");
                return View("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult SiparisYuklemeRampa2()
        {
            try
            {
                // Rampa bilgisini kontrol et
                _logger.LogInformation("SiparisYuklemeRampa2 metoduna girildi");
                var siparisData = _faturaRepository.GetSiparisYuklemeRampaDetaylari("Rampa2");

                // Log kayıtları
                _logger.LogInformation($"Gelen sipariş verisi sayısı: {siparisData?.Count() ?? 0}");

                // AJAX isteği olup olmadığını kontrol et
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // İrsaliye numarası 0 ise ve listede geçerli bir irsaliye numarası varsa onu kullan
                    int maxIrsaliyeNo = 0;

                    foreach (var item in siparisData)
                    {
                        int currentIrsaliye = 0;
                        if (item.IrsaliyeNo != null && int.TryParse(item.IrsaliyeNo.ToString(), out currentIrsaliye) && currentIrsaliye > 0)
                        {
                            maxIrsaliyeNo = Math.Max(maxIrsaliyeNo, currentIrsaliye);
                        }
                    }

                    // JSON dönüşünde ek kontroller ve sipariş no, irsaliye no ekleyin
                    var jsonResult = siparisData.Select(x => new
                    {
                        urunAdi = x.UrunAdi,
                        toplamSiparisMiktari = x.ToplamSiparisMiktari,
                        kalanMiktar = x.KalanMiktar,
                        yuklenenMiktar = x.YuklenenMiktar,
                        cariUnvan = x.CariUnvan,
                        siparisDurumu = x.SiparisDurumu,
                        // Sipariş no ve irsaliye no bilgilerini ekleyin
                        evrakSira = x.EvrakSira,
                        irsaliyeNo = IsValidIrsaliyeNo(x.IrsaliyeNo) ? x.IrsaliyeNo : maxIrsaliyeNo
                    }).ToList();

                    _logger.LogInformation($"JSON'a dönüştürülen veri sayısı: {jsonResult.Count}");
                    return Json(jsonResult);
                }

                // Normal sayfa yüklemesi için view dön
                return View(siparisData);
            }
            catch (Exception ex)
            {
                // Detaylı hata günlüğü
                _logger.LogError(ex, "Rampa2 sipariş yükleme sayfasında kritik hata");
                _logger.LogError($"Hata Mesajı: {ex.Message}");
                _logger.LogError($"Hata Yığını: {ex.StackTrace}");
                return View("Error");
            }
        }

        // Geçerli irsaliye numarası kontrolü için yardımcı metot
        private bool IsValidIrsaliyeNo(object irsaliyeNo)
        {
            if (irsaliyeNo == null)
                return false;

            int value = 0;
            if (int.TryParse(irsaliyeNo.ToString(), out value))
                return value > 0;

            return false;
        }
        // İş emri yazdırma sayfasını gösteren action
        [AllowAnonymous]
        public IActionResult YazdirIsEmri(string isEmriKodu, string urunKodu, string barkod, int? paletNo = null)
        {
            try
            {
                var isEmri = _faturaRepository.GetIsEmirleri()
                    .FirstOrDefault(ie => ie.is_Kod == isEmriKodu && ie.UrunKodu == urunKodu);

                if (isEmri == null)
                {
                    return NotFound("İş emri bulunamadı.");
                }

                var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
                var connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Bu iş emrinden şu ana kadar kaç barkod basıldığını say
                int kacinciBarkod = GetBarkodSayisi(isEmriKodu, urunKodu, connectionString);

                var model = new IsEmriYazdirViewModel
                {
                    IsEmriKodu = string.IsNullOrEmpty(barkod) ? isEmriKodu : barkod,
                    UrunKodu = urunKodu,
                    UrunAdi = isEmri.UrunAdi,
                    KisaIsim = isEmri.KisaIsim,
                    YabanciIsim = isEmri.YabanciIsim,
                    Birim2Katsayi = isEmri.Birim2Katsayi,
                    Birim3Katsayi = isEmri.Birim3Katsayi,
                    Miktar = isEmri.Miktar,
                    BaslangicTarihi = isEmri.is_BaslangicTarihi,
                    IsMerkezi = isEmri.IsMerkezi,
                    Renk = isEmri.Renk,
                    Kalip = isEmri.Kalip,
                    Hammadde = isEmri.Hammadde,
                    Adet = isEmri.Adet,
                    OperatorAdi = username,
                    PaletNo = kacinciBarkod, // Kaçıncı barkod = kaçıncı palet
                    FirmaAdi = "Şirket Adı",
                    FirmaAdresi = "Şirket Adresi",
                    FirmaTelefon = "Şirket Telefon"
                };

                // Barkod basıldıktan SONRA kaydet
                SaveBarkodBasimi(isEmriKodu, urunKodu, barkod, username, kacinciBarkod, connectionString);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emri yazdırılırken hata oluştu.");
                return View("Error");
            }
        }

        // Bu iş emrinden şu ana kadar kaç barkod basıldığını say
        private int GetBarkodSayisi(string isEmriKodu, string urunKodu, string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Tablo yoksa oluştur
                    string createTableQuery = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BarkodBasimGecmisi')
            BEGIN
                CREATE TABLE BarkodBasimGecmisi (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    IsEmriKodu NVARCHAR(50) NOT NULL,
                    UrunKodu NVARCHAR(50) NOT NULL,
                    Barkod NVARCHAR(100),
                    SiraNo INT NOT NULL,
                    OperatorAdi NVARCHAR(100),
                    BasimTarihi DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_IsEmri_Urun ON BarkodBasimGecmisi(IsEmriKodu, UrunKodu);
            END";

                    using (SqlCommand cmd = new SqlCommand(createTableQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Bu iş emrinden şu ana kadar kaç barkod basıldı?
                    string countQuery = @"
            SELECT COUNT(*) + 1
            FROM BarkodBasimGecmisi 
            WHERE IsEmriKodu = @IsEmriKodu AND UrunKodu = @UrunKodu";

                    using (SqlCommand cmd = new SqlCommand(countQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@IsEmriKodu", isEmriKodu);
                        cmd.Parameters.AddWithValue("@UrunKodu", urunKodu);

                        int sayi = (int)cmd.ExecuteScalar();
                        return sayi; // İlk barkod = 1, ikinci = 2, üçüncü = 3...
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod sayısı alınırken hata oluştu");
                return 1; // Hata durumunda 1 döndür
            }
        }

        // Barkod basımını kaydet
        private void SaveBarkodBasimi(string isEmriKodu, string urunKodu, string barkod, string operatorAdi, int siraNo, string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string insertQuery = @"
            INSERT INTO BarkodBasimGecmisi (IsEmriKodu, UrunKodu, Barkod, SiraNo, OperatorAdi, BasimTarihi)
            VALUES (@IsEmriKodu, @UrunKodu, @Barkod, @SiraNo, @OperatorAdi, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@IsEmriKodu", isEmriKodu);
                        cmd.Parameters.AddWithValue("@UrunKodu", urunKodu);
                        cmd.Parameters.AddWithValue("@Barkod", barkod ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SiraNo", siraNo);
                        cmd.Parameters.AddWithValue("@OperatorAdi", operatorAdi);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod basımı kaydedilirken hata oluştu");
            }
        }
        [AllowAnonymous]
        // Bu metodu BarcodeLib olmadan kullanabilirsiniz
        public IActionResult GenerateBarcode(string data, int width = 200, int height = 80)
        {
            try
            {
                // Dış bir servis yerine basit bir SVG çıktısı oluşturabiliriz
                // veya görüntü kaynağı yerine sadece iş emri kodunu yazdırabiliriz
                string svgContent = $@"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>
            <rect width='100%' height='100%' fill='white'/>
            <text x='50%' y='50%' font-family='Arial' font-size='12' text-anchor='middle' dominant-baseline='middle'>{data}</text>
        </svg>";

                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod oluşturulurken hata oluştu. Data: {Data}", data);
                return Content("Barkod oluşturulamadı.");
            }
        }
        // FaturaController.cs içine aşağıdaki metodu ekleyin
        [AllowAnonymous]
        public IActionResult YazdirUrunEtiketi(string isEmriKodu, string urunKodu)
        {
            try
            {
                // İş emrini ve ürün bilgilerini bul
                var isEmri = _faturaRepository.GetIsEmirleri()
                    .FirstOrDefault(ie => ie.is_Kod == isEmriKodu && ie.UrunKodu == urunKodu);

                if (isEmri == null)
                {
                    return NotFound("İş emri bulunamadı.");
                }

                // Stok bilgilerini veritabanından al
                var stokDetay = _faturaRepository.GetStokDetay(urunKodu);

                var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
                var connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Bu iş emrinden şu ana kadar kaç barkod basıldığını say
                int kacinciBarkod = GetBarkodSayisi(isEmriKodu, urunKodu, connectionString);

                // Yazdırma view modeli oluştur
                var model = new IsEmriYazdirViewModel
                {
                    IsEmriKodu = isEmriKodu,
                    UrunKodu = urunKodu,
                    UrunAdi = stokDetay?.YabanciIsim ?? isEmri.UrunAdi, // sto_yabanci_isim
                    UrunKisaIsim = stokDetay?.KisaIsim ?? "",  // sto_kisa_ismi (fiyat kısmında gösterilecek)
                    UrunAciklamasi = stokDetay?.Isim ?? isEmri.UrunAdi, // sto_isim
                    Miktar = stokDetay?.Birim2Katsayi ?? isEmri.Miktar, // sto_birim2_katsayi
                    BaslangicTarihi = isEmri.is_BaslangicTarihi,
                    IsMerkezi = isEmri.IsMerkezi,
                    PaletNo = kacinciBarkod, // Kaçıncı barkod = kaçıncı palet
                    OperatorAdi = username,
                    Renk = isEmri.Renk,
                    Kalip = isEmri.Kalip,
                    Hammadde = isEmri.Hammadde,
                    Adet = isEmri.Adet,
                    Birim2Katsayi = isEmri.Birim2Katsayi
                };

                // Barkod basıldıktan SONRA kaydet
                SaveBarkodBasimi(isEmriKodu, urunKodu, isEmriKodu, username, kacinciBarkod, connectionString);

                // Yazdırma sayfasını görüntüle
                return View("YazdirUrunEtiketi", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün etiketi yazdırılırken hata oluştu. İş Emri Kodu: {IsEmriKodu}, Ürün Kodu: {UrunKodu}", isEmriKodu, urunKodu);
                return View("Error");
            }
        }
        [AllowAnonymous]
        public IActionResult MusteriAcikFaturalar(string aramaMetni = "")
        {
            try
            {
                var model = _faturaRepository.GetMusteriAcikFaturalar();

                // Arama filtresi uygula
                if (!string.IsNullOrWhiteSpace(aramaMetni))
                {
                    aramaMetni = aramaMetni.ToLower();
                    model = model.Where(m =>
                        m.MusteriKodu.ToLower().Contains(aramaMetni) ||
                        m.Unvan.ToLower().Contains(aramaMetni)
                    ).ToList();
                }

                // Toplam değerleri hesapla
                ViewBag.ToplamVadesiGecmis = model.Sum(m => m.VadesiGecmisBakiye);
                ViewBag.ToplamBugunOdenmesiGereken = model.Sum(m => m.BugunOdenmesiGereken);
                ViewBag.ToplamGelecekVadeli = model.Sum(m => m.GelecekVadeliFaturalar);
                ViewBag.ToplamBorc = model.Sum(m => m.ToplamBorc);
                ViewBag.AramaMetni = aramaMetni;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri açık faturaları listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Müşteri açık faturaları alınırken bir hata oluştu.";
                return View("Error");
            }
        }

        [HttpPost]
        public IActionResult Ara(string aramaMetni)
        {
            return RedirectToAction("Index", new { aramaMetni });
        }

        public IActionResult ExcelExport()
        {
            try
            {
                var model = _faturaRepository.GetMusteriAcikFaturalar();

                // Excel oluşturma işlemleri burada yapılacak
                // ClosedXML kütüphanesi ile Excel dosyası oluşturulabilir

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel raporu oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Excel raporu oluşturulurken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }
      
   // FaturaController.cs içindeki HataliUretimler metodunu güncelleyin
public IActionResult HataliUretimler(DateTime? baslangicTarihi = null, DateTime? bitisTarihi = null, 
    string baslangicSaati = "00:00", string bitisSaati = "23:59", string stokArama = "")
{
    // Varsayılan değerler
    baslangicTarihi ??= DateTime.Now.AddMonths(-1);
    bitisTarihi ??= DateTime.Now;

    // Saat bilgilerini tarih ile birleştir
    DateTime baslangicDateTime = baslangicTarihi.Value.Date.Add(TimeSpan.Parse(baslangicSaati + ":00"));
    DateTime bitisDateTime = bitisTarihi.Value.Date.Add(TimeSpan.Parse(bitisSaati + ":59"));

    var model = _faturaRepository.GetHataliUretimler(baslangicDateTime, bitisDateTime, stokArama);

    ViewData["BaslangicTarihi"] = baslangicTarihi.Value.ToString("yyyy-MM-dd");
    ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");
    ViewData["BaslangicSaati"] = baslangicSaati;
    ViewData["BitisSaati"] = bitisSaati;
    ViewData["StokArama"] = stokArama;

    return View(model);
}
        [AllowAnonymous]
        [HttpPost]
 
        public IActionResult HataliUretimleriSil(List<string> seciliUretimler)
        {
            if (seciliUretimler == null || seciliUretimler.Count == 0)
            {
                return Json(new { success = false, message = "Hiçbir üretim seçilmedi." });
            }

            try
            {
                int silinenKayitSayisi = _faturaRepository.HataliUretimleriSil(seciliUretimler);
                return Json(new { success = true, message = $"{silinenKayitSayisi} adet hatalı üretim kaydı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatalı üretimler silinirken hata oluştu");
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu: " + ex.Message });
            }
        }
     
        public IActionResult SilinenBarkodlar(DateTime? baslangicTarihi = null, DateTime? bitisTarihi = null, string stokKodu = null)
        {
            // Varsayılan değerler
            baslangicTarihi ??= DateTime.Now.AddMonths(-1);
            bitisTarihi ??= DateTime.Now;

            try
            {
                var model = _faturaRepository.GetSilinenBarkodlar(baslangicTarihi.Value, bitisTarihi.Value, stokKodu);

                ViewData["BaslangicTarihi"] = baslangicTarihi.Value.ToString("yyyy-MM-dd");
                ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");
                ViewData["StokKodu"] = stokKodu;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Silinen barkodlar listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Silinen barkodlar görüntülenirken bir hata oluştu.";
                return View("Error");
            }
        }

        // FaturaController.cs içine eklenecek metotlar

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetMalzemePlanlama(string isEmriKodu)
        {
            try
            {
                var malzemePlanlama = _faturaRepository.GetMalzemePlanlama(isEmriKodu);
                return Json(new { success = true, data = malzemePlanlama });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Malzeme planlaması alınırken hata oluştu. İş Emri: {IsEmriKodu}", isEmriKodu);
                return Json(new { success = false, message = "Malzeme planlaması alınırken hata oluştu." });
            }
        }




        [HttpPost]
        public JsonResult UretIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            try
            {
                _logger.LogInformation($"Üretim başlatıldı - İş Emri: {isEmriKodu}, Ürün: {urunKodu}, Depo: {depoNo}");

                var sonuc = _faturaRepository.UretIsEmri(isEmriKodu, urunKodu, depoNo);

                _logger.LogInformation($"Üretim tamamlandı - Sonuç: {sonuc}");

                return Json(new
                {
                    success = true,
                    message = $"Üretim başarıyla tamamlandı. {sonuc}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Üretim işlemi sırasında hata oluştu - İş Emri: {isEmriKodu}");
                return Json(new
                {
                    success = false,
                    message = "Üretim işlemi sırasında bir hata oluştu: " + ex.Message
                });
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public JsonResult BarkodBasimIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            try
            {
                _logger.LogInformation($"Üretim başlatıldı - İş Emri: {isEmriKodu}, Ürün: {urunKodu}, Depo: {depoNo}");

                var sonuc = _faturaRepository.UretIsEmri(isEmriKodu, urunKodu, depoNo);

                _logger.LogInformation($"Üretim tamamlandı - Sonuç: {sonuc}");

                return Json(new
                {
                    success = true,
                    message = $"Üretim başarıyla tamamlandı. {sonuc}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Üretim işlemi sırasında hata oluştu - İş Emri: {isEmriKodu}");
                return Json(new
                {
                    success = false,
                    message = "Üretim işlemi sırasında bir hata oluştu: " + ex.Message
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateIsEmriDurumu(string isEmriKodu, int yeniDurum)
        {
            try
            {
                if (string.IsNullOrEmpty(isEmriKodu))
                {
                    TempData["ErrorMessage"] = "İş emri kodu gerekli";
                    return RedirectToAction("IsEmriDurumu");
                }

                if (yeniDurum != 0 && yeniDurum != 1)
                {
                    TempData["ErrorMessage"] = "Geçersiz durum değeri";
                    return RedirectToAction("IsEmriDurumu");
                }

                _logger.LogInformation($"İş emri durumu güncelleniyor - Kod: {isEmriKodu}, Yeni Durum: {yeniDurum}");

                var success = _faturaRepository.UpdateIsEmriDurumu(isEmriKodu, yeniDurum);

                if (success)
                {
                    TempData["SuccessMessage"] = $"İş emri {isEmriKodu} durumu başarıyla güncellendi";
                    _logger.LogInformation($"İş emri durumu güncellendi - Kod: {isEmriKodu}");
                }
                else
                {
                    TempData["ErrorMessage"] = "İş emri durumu güncellenemedi";
                    _logger.LogWarning($"İş emri durumu güncellenemedi - Kod: {isEmriKodu}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"İş emri durumu güncellenirken hata oluştu - Kod: {isEmriKodu}");
                TempData["ErrorMessage"] = "İş emri durumu güncellenirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("IsEmriDurumu");
        }


        [AllowAnonymous]
        public IActionResult GetIsMerkezleri()
        {
            try
            {
                _logger.LogInformation("GetIsMerkezleri API çağrıldı");

                // Tüm iş merkezlerini al
                var tumIsMerkezleri = _faturaRepository.GetTumIsMerkezleri();

                _logger.LogInformation($"Tüm iş merkezi sayısı: {tumIsMerkezleri.Count}");

                var result = new
                {
                    success = true,
                    isMerkezleri = tumIsMerkezleri.Select(im => new
                    {
                        kod = im.IsM_Kodu,
                        aciklama = im.IsM_Aciklama
                    }),
                    debug = new
                    {
                        totalCount = tumIsMerkezleri.Count
                    }
                };

                _logger.LogInformation($"GetIsMerkezleri: {tumIsMerkezleri.Count} iş merkezi döndürüldü");

                return Json(result, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş merkezleri listelenirken hata oluştu: {Message}", ex.Message);
                return Json(new
                {
                    success = false,
                    message = "İş merkezleri alınırken hata oluştu.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        // Kullanıcı numarasını alma metodu - Controller'a ekleyin
        private string GetCurrentUserNoFromSession()
        {
            string userNo = null;

            try
            {
                // 1. Session'dan string olarak almayı dene
                if (HttpContext.Session != null)
                {
                    userNo = HttpContext.Session.GetString("UserNo");
                    if (!string.IsNullOrEmpty(userNo))
                    {
                        _logger.LogInformation($"Session string'den UserNo alındı: {userNo}");
                        return userNo;
                    }

                    // 2. Session'dan int olarak almayı dene
                    var userNoInt = HttpContext.Session.GetInt32("UserNo");
                    if (userNoInt.HasValue)
                    {
                        userNo = userNoInt.Value.ToString();
                        _logger.LogInformation($"Session int'den UserNo alındı: {userNo}");
                        return userNo;
                    }

                    // 3. Session'dan "User_no" anahtarı ile dene
                    userNo = HttpContext.Session.GetString("User_no");
                    if (!string.IsNullOrEmpty(userNo))
                    {
                        _logger.LogInformation($"Session 'User_no' anahtarından UserNo alındı: {userNo}");
                        return userNo;
                    }

                    // Session içeriğini logla
                    _logger.LogWarning("Session'da UserNo bulunamadı. Session içerikleri:");
                    foreach (var key in HttpContext.Session.Keys)
                    {
                        var value = HttpContext.Session.GetString(key);
                        _logger.LogWarning($"- {key}: {value}");
                    }
                }

                // 4. Claims'den almayı dene
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var userNoClaim = User.Claims.FirstOrDefault(c =>
                        c.Type == "UserNo" ||
                        c.Type == "User_no" ||
                        c.Type == "user_no");

                    if (userNoClaim != null)
                    {
                        userNo = userNoClaim.Value;
                        _logger.LogInformation($"Claims'den UserNo alındı: {userNo}");
                        return userNo;
                    }
                }

                // 5. Identity Name'i kullan
                if (User?.Identity?.Name != null)
                {
                    userNo = User.Identity.Name;
                    _logger.LogInformation($"Identity.Name'den UserNo alındı: {userNo}");
                    return userNo;
                }

                // 6. Test için sabit değer (geliştirme aşamasında)
                userNo = "1"; // Veya sisteminizde geçerli bir test kullanıcı numarası
                _logger.LogWarning($"UserNo hiçbir yöntemle bulunamadı, test değeri kullanılıyor: {userNo}");

                return userNo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserNo alınırken hata oluştu: {Message}", ex.Message);
                return "1"; // Hata durumunda varsayılan değer
            }
        }

        // İş emirlerini belirli iş merkezlerine göre filtreleyip getiren metot


        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetStokListesi(string arama = "")
        {
            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query;
                    object parameters;

                    if (string.IsNullOrWhiteSpace(arama))
                    {
                        // Arama metni yoksa tüm stokları getir
                        query = @"
                    SELECT 
                        sto_kod AS StokKodu,
                        sto_isim AS StokAdi,
                        sto_birim1_ad AS BirimAdi
                    FROM STOKLAR 
                    WHERE sto_kod IS NOT NULL AND sto_isim IS NOT NULL
                    ORDER BY sto_isim";
                        parameters = new { };
                    }
                    else
                    {
                        // Arama metni varsa stok ismi ve kodu ile ara
                        query = @"
                   SELECT 
     sto_kod AS StokKodu,
     sto_isim AS StokAdi,
     sto_birim1_ad AS BirimAdi
FROM STOKLAR 
WHERE (sto_isim LIKE @Arama OR sto_kod LIKE @Arama)
  AND sto_kod IS NOT NULL 
  AND sto_isim IS NOT NULL
  AND sto_cins IN (1,2,3,4,5,6,7,11)
ORDER BY 
     CASE 
         WHEN sto_isim LIKE @AramaBaslangic THEN 1
         WHEN sto_kod LIKE @AramaBaslangic THEN 2
         WHEN sto_isim LIKE @Arama THEN 3
         ELSE 4
     END,
     sto_isim";
                        parameters = new
                        {
                            Arama = "%" + arama + "%",
                            AramaBaslangic = arama + "%"
                        };
                    }

                    var stokListesi = connection.Query<dynamic>(query, parameters).ToList();

                    _logger.LogInformation($"Stok arama: '{arama}' - Bulunan: {stokListesi.Count} stok");

                    return Json(new { success = true, data = stokListesi });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok listesi alınırken hata oluştu. Arama: {Arama}", arama);
                return Json(new { success = false, message = "Stok listesi alınamadı: " + ex.Message });
            }
        }

        // Stok kodu ile detay getiren metod ekle
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetStokDetayByKod(string stokKodu)
        {
            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"
                SELECT 
                    sto_kod AS StokKodu,
                    sto_isim AS StokAdi,
                    sto_birim1_ad AS BirimAdi
                FROM STOKLAR 
                WHERE sto_kod = @StokKodu";

                    var stokDetay = connection.QueryFirstOrDefault<dynamic>(query, new { StokKodu = stokKodu });

                    if (stokDetay != null)
                    {
                        return Json(new { success = true, data = stokDetay });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Stok bulunamadı." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok detayı alınırken hata oluştu. Stok Kodu: {StokKodu}", stokKodu);
                return Json(new { success = false, message = "Stok detayı alınamadı: " + ex.Message });
            }
        }

        // ESKİ METODU SİLİN veya yorum satırı yapın
        /*
        [HttpPost]
        [AllowAnonymous]
        public JsonResult MalzemeTuketimi(string isEmriKodu, List<TuketimItem> tuketimListesi, List<TuketimItem> eklenenMalzemeler = null)
        {
            // Bu eski metod - silin
        }
        */

        // SADECE BU METODU KULLANIN
        // FaturaController.cs içindeki MalzemeTuketimi metodunu değiştirin

        public IActionResult Tuketim()
        {
            try
            {
                // Aktif durumu 1 veya 0 olan iş emirlerini getir
                var aktifIsEmirleri = _faturaRepository.GetIsEmirleri()
                    .Where(ie => ie.is_EmriDurumu == 1 )  // Aktif durum
                    .OrderBy(ie => ie.UrunAdi)
                    .ToList();

                // ViewBag.HasProductionPermission kaldırıldı
                _logger.LogInformation($"Tuketim sayfası: {aktifIsEmirleri.Count} iş emri gösteriliyor");

                return View(aktifIsEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüketim sayfası yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "Tüketim sayfası yüklenirken bir hata oluştu.";
                return View("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult GetIsEmirleri()
        {
            try
            {
                _logger.LogInformation("GetIsEmirleri API çağrıldı");

                var isEmirleri = _faturaRepository.GetIsEmirleri();

                var result = new
                {
                    success = true,
                    isEmirleri = isEmirleri.Select(e => new
                    {
                        e.is_Guid,
                        e.is_Kod,
                        e.is_Ismi,
                        e.is_EmriDurumu,
                        e.is_BaslangicTarihi,
                        UrunKodu = e.UrunKodu,
                        UrunAdi = e.UrunAdi,
                        Miktar = e.Miktar,
                        IsMerkezi = e.IsMerkezi,
                    })
                };

                _logger.LogInformation($"GetIsEmirleri: {isEmirleri.Count()} iş emri döndürüldü");

                return Json(result, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                return Json(new
                {
                    success = false,
                    message = "Veriler getirilirken hata oluştu.",
                    error = ex.Message
                });
            }
        }
        public IActionResult IsEmirleri()
        {
            try
            {
                _logger.LogInformation("IsEmirleri sayfası açıldı");

                var isEmirleri = _faturaRepository.GetIsEmirleri();

                _logger.LogInformation($"IsEmirleri sayfası: {isEmirleri.Count()} iş emri gösteriliyor");

                return View(isEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emirleri listelenirken bir hata oluştu: " + ex.Message;
                return View("Error");
            }
        }
        public IActionResult IsEmriDurumu()
        {
            try
            {
                _logger.LogInformation("IsEmriDurumu sayfası açıldı");

                var isEmirleri = _faturaRepository.GetIsEmirleri();

                // Üretim yetkisini true olarak set et
                ViewBag.HasProductionPermission = true;

                _logger.LogInformation($"IsEmriDurumu sayfası: {isEmirleri.Count()} iş emri gösteriliyor");

                return View(isEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emirleri listelenirken bir hata oluştu: " + ex.Message;
                return View("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult GetIsEmirleriByMerkezler(string isMerkezleri)
        {
            try
            {
                _logger.LogInformation($"GetIsEmirleriByMerkezler API çağrıldı - İş Merkezleri: {isMerkezleri}");

                var isEmirleri = _faturaRepository.GetIsEmirleri();

                // İş merkezi filtresi uygulanacaksa
                if (!string.IsNullOrEmpty(isMerkezleri))
                {
                    var seciliMerkezler = isMerkezleri.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(x => x.Trim())
                                                     .Where(x => !string.IsNullOrEmpty(x))
                                                     .ToList();

                    if (seciliMerkezler.Any())
                    {
                        isEmirleri = isEmirleri.Where(ie => seciliMerkezler.Contains(ie.IsMerkezi));
                    }
                }

                var result = new
                {
                    success = true,
                    isEmirleri = isEmirleri.Select(e => new
                    {
                        e.is_Guid,
                        e.is_Kod,
                        e.is_Ismi,
                        e.is_EmriDurumu,
                        e.is_BaslangicTarihi,
                        UrunKodu = e.UrunKodu,
                        UrunAdi = e.UrunAdi,
                        Miktar = e.Miktar,
                        IsMerkezi = e.IsMerkezi,
                    })
                };

                _logger.LogInformation($"GetIsEmirleriByMerkezler: {isEmirleri.Count()} iş emri döndürüldü");

                return Json(result, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Filtrelenmiş iş emirleri listelenirken hata oluştu");
                return Json(new
                {
                    success = false,
                    message = "Veriler getirilirken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult MalzemeTuketimi([FromBody] MalzemeTuketimRequest request)
        {
            try
            {
                _logger.LogInformation($"MalzemeTuketimi çağrıldı - İş Emri: {request.IsEmriKodu}, Üretim Miktarı: {request.UretimMiktari}");

                if (request == null)
                {
                    return Json(new { success = false, message = "Geçersiz istek verisi." });
                }

                // Null kontrolü
                var tuketimListesi = request.TuketimListesi ?? new List<TuketimItem>();
                var eklenenMalzemeler = request.EklenenMalzemeler ?? new List<TuketimItem>();

                _logger.LogInformation($"Planlanan malzeme sayısı: {tuketimListesi.Count}, Eklenen malzeme sayısı: {eklenenMalzemeler.Count}");

                // Boş liste kontrolü
                var planliTuketim = tuketimListesi.Where(t => t != null && t.Miktar > 0).ToList();
                var eklenenTuketim = eklenenMalzemeler.Where(t => t != null && t.Miktar > 0).ToList();

                if (!planliTuketim.Any() && !eklenenTuketim.Any())
                {
                    return Json(new { success = false, message = "Tüketilecek malzeme seçilmedi." });
                }

                // Üretim miktarı kontrolü
                if (request.UretimMiktari.HasValue && request.UretimMiktari.Value <= 0)
                {
                    return Json(new { success = false, message = "Üretim miktarı 0'dan büyük olmalıdır." });
                }

                // Repository metodunu çağır
                var sonuc = _faturaRepository.MalzemeTuketimi(
                    request.IsEmriKodu,
                    planliTuketim,
                    eklenenTuketim,
                    request.UretimMiktari
                );

                _logger.LogInformation($"Tüketim ve üretim başarılı: {sonuc}");
                return Json(new { success = true, message = sonuc });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Malzeme tüketimi ve üretim sırasında hata oluştu. İş Emri: {IsEmriKodu}", request?.IsEmriKodu);
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetStokDepodakiMiktar(string stokKodu, int depoNo = 1)
        {
            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = "SELECT dbo.fn_DepodakiMiktar(@StokKodu, @DepoNo, GETDATE()) AS DepodakiMiktar";

                    var miktar = connection.QueryFirstOrDefault<decimal?>(query, new { StokKodu = stokKodu, DepoNo = depoNo }) ?? 0;

                    return Json(new { success = true, miktar = miktar });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depodaki miktar alınırken hata oluştu. Stok: {StokKodu}, Depo: {DepoNo}", stokKodu, depoNo);
                return Json(new { success = false, message = "Depodaki miktar alınamadı.", miktar = 0 });
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetPartiKodlari(string stokKodu)
        {
            try
            {
                _logger.LogInformation($"GetPartiKodlari API çağrıldı - Stok Kodu: {stokKodu}");

                if (string.IsNullOrEmpty(stokKodu))
                {
                    return Json(new { success = false, message = "Stok kodu belirtilmedi." });
                }

                var partiKodlari = _faturaRepository.GetPartiKodlari(stokKodu);

                // DEBUG: Repository'den dönen veri logunu ekle
                _logger.LogInformation($"Repository'den dönen parti sayısı: {partiKodlari?.Count() ?? 0}");

                var result = new
                {
                    success = true,
                    partiKodlari = partiKodlari?.Select(p => new
                    {
                        p.StokKodu,
                        p.PartiKodu,
                        p.LotNo,
                        p.Miktar
                    }) ?? Enumerable.Empty<object>()
                };

                _logger.LogInformation($"GetPartiKodlari: {result.partiKodlari.Count()} parti kodu döndürüldü");
                return Json(result, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parti kodları listelenirken hata oluştu. Stok Kodu: {StokKodu}", stokKodu);
                return Json(new
                {
                    success = false,
                    message = "Parti kodları getirilirken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        public IActionResult EksikPaletPartiler(string stokArama = "")
        {
            try
            {
                // Repository'den eksik paletli partileri çek (tarih olmadan)
                var eksikPartiler = _faturaRepository.GetEksikPaletPartiler(stokArama);

                // ViewData'ya parametreleri gönder
                ViewData["StokArama"] = stokArama;

                return View(eksikPartiler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eksik paletli partiler listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Eksik paletli partiler listelenirken bir hata oluştu: " + ex.Message;
                return View(Enumerable.Empty<EksikPaletPartiViewModel>());
            }
        }
    }
}




//[HttpPost]
//public IActionResult Stok(string stokKod, DateTime? raporTarihi)
//{
//    // Fetch data based on selected stock code and report date
//    var data = _faturaRepository.GetStokYaslandirmaData(stokKod, raporTarihi);

//    // Prepare the view model with the fetched data
//    var viewModel = new StokViewModel
//    {
//        StockCodes = _faturaRepository.GetStockCodesAndNames().Select(x => x.StockCode).ToList(),
//        StokYaslandirmaData = data
//    };

//    return View(viewModel);
//}











