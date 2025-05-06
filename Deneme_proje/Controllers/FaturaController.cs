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
namespace Deneme_proje.Controllers
{
    [AuthFilter]

    public class FaturaController : BaseController
    {
        private readonly ILogger<FaturaController> _logger;
        private readonly FaturaRepository _faturaRepository;

        // Constructor
        public FaturaController(ILogger<FaturaController> logger, FaturaRepository faturaRepository)
        {
            _logger = logger;
            _faturaRepository = faturaRepository;
        }

        public ActionResult Index(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var faturaData = _faturaRepository.GetFaturaData(cariKodu, ticariFaiz);

            return View(faturaData);
        }

        public IActionResult TedarikciKapaliFatura(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var faturaData = _faturaRepository.GetTedarikciFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
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

            if (!data.Any())
            {
                ViewBag.Message = string.IsNullOrEmpty(stockCode)
                    ? "Veri bulunamadı. Stok kodu veya depo seçimi yapabilirsiniz."
                    : "Aramanıza uygun veri bulunamadı.";
            }

            ViewData["SelectedStockCode"] = stockCode;
            ViewData["SelectedDepoNo"] = depoNo;

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


        [AllowAnonymous]
        public IActionResult GetIsEmirleri()
        {
            try
            {
                var isEmirleri = _faturaRepository.GetIsEmirleri();
                var hasProductionPermission = _faturaRepository.HasProductionPermission();

                // Property isimlerini açıkça belirterek JSON'a dönüştür
                return Json(new
                {
                    success = true,
                    isEmirleri = isEmirleri.Select(e => new
                    {
                        e.is_Guid,
                        e.is_Kod,
                        e.is_Ismi,
                        e.is_EmriDurumu,
                        e.is_BaslangicTarihi,
                        UrunKodu = e.UrunKodu,  // Açıkça belirt
                        UrunAdi = e.UrunAdi,    // Açıkça belirt
                        Miktar = e.Miktar,      // Açıkça belirt
                        IsMerkezi = e.IsMerkezi, // Açıkça belirt

                    }),
                    hasProductionPermission
                }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Property isimlerini olduğu gibi koru
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                return Json(new { success = false, message = "Veriler getirilirken hata oluştu." });
            }
        }

        public IActionResult IsEmirleri()
        {
            try
            {
                var isEmirleri = _faturaRepository.GetIsEmirleri();

                // Üretim yetkisini ViewBag'e ekleyin
                ViewBag.HasProductionPermission = _faturaRepository.HasProductionPermission();

                return View(isEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emirleri listelenirken bir hata oluştu.";
                return View("Error");
            }
        }
        [HttpPost]

        public JsonResult UretIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            try
            {
                var sonuc = _faturaRepository.UretIsEmri(isEmriKodu, urunKodu, depoNo);
                return Json(new
                {
                    success = true,
                    message = $"Üretim başarıyla tamamlandı. {sonuc}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üretim işlemi sırasında hata oluştu");
                return Json(new
                {
                    success = false,
                    message = "Üretim işlemi sırasında bir hata oluştu."
                });
            }
        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateIsEmriDurumu(string isEmriKodu, int yeniDurum)
        {
            try
            {
                var success = _faturaRepository.UpdateIsEmriDurumu(isEmriKodu, yeniDurum);
                if (success)
                {
                    TempData["SuccessMessage"] = "İş emri durumu başarıyla güncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "İş emri durumu güncellenemedi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emri durumu güncellenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emri durumu güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("IsEmirleri");
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
                KrediDetaylari = krediDetaylari
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
        public IActionResult YazdirIsEmri(string isEmriKodu, string urunKodu, string barkod)
        {
            try
            {
                // İş emri bulma
                var isEmri = _faturaRepository.GetIsEmirleri()
                    .FirstOrDefault(ie => ie.is_Kod == isEmriKodu && ie.UrunKodu == urunKodu);
                if (isEmri == null)
                {
                    return NotFound("İş emri bulunamadı.");
                }

                // Yazdırma view modeli oluştur
                var model = new IsEmriYazdirViewModel
                {
                    IsEmriKodu = string.IsNullOrEmpty(barkod) ? isEmriKodu : barkod, // Barkod varsa onu kullan, yoksa iş emri kodu
                    UrunKodu = urunKodu,
                    UrunAdi = isEmri.UrunAdi,
                    KisaIsim = isEmri.KisaIsim,
                    YabanciIsim = isEmri.YabanciIsim,
                    Birim2Katsayi = isEmri.Birim2Katsayi,
                    Miktar = isEmri.Miktar,
                    BaslangicTarihi = isEmri.is_BaslangicTarihi,
                    IsMerkezi = isEmri.IsMerkezi,
                    // Standart firma bilgileri
                    FirmaAdi = "Şirket Adı",
                    FirmaAdresi = "Şirket Adresi",
                    FirmaTelefon = "Şirket Telefon"
                };

                // Yazdırma sayfasını görüntüle
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emri yazdırılırken hata oluştu. İş Emri Kodu: {IsEmriKodu}, Ürün Kodu: {UrunKodu}, Barkod: {Barkod}",
                    isEmriKodu, urunKodu, barkod);
                return View("Error");
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
                };

                // Yazdırma sayfasını görüntüle
                return View("YazdirUrunEtiketi", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün etiketi yazdırılırken hata oluştu. İş Emri Kodu: {IsEmriKodu}, Ürün Kodu: {UrunKodu}", isEmriKodu, urunKodu);
                return View("Error");
            }
        }
        public IActionResult IsEmriDurumu()
        {
            try
            {
                var isEmirleri = _faturaRepository.GetIsEmirleri();

                // Üretim yetkisini ViewBag'e ekleyin
                ViewBag.HasProductionPermission = _faturaRepository.HasProductionPermission();

                return View(isEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emirleri listelenirken bir hata oluştu.";
                return View("Error");
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











