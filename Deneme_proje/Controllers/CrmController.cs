using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Repository;
using static Deneme_proje.Models.CrmEntities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Dapper;
using System.Security.Claims;
using System.Data.SqlClient;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.IO.Image;
using iText.Kernel.Geom;
using System.IO;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
namespace Deneme_proje.Controllers
{
    [AllowAnonymous]
    public class CrmController : BaseController
    {
        private readonly CrmRepository _crmRepository;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<CrmController> _logger;

        public CrmController(CrmRepository crmRepository, DatabaseSelectorService dbSelectorService, ILogger<CrmController> logger)
        {
            _crmRepository = crmRepository;
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        #region Dashboard

        public IActionResult Dashboard()
        {
            try
            {
                var istatistikler = _crmRepository.GetTeklifIstatistikleri();
                var aylikGrafik = _crmRepository.GetAylikTeklifGrafigi().ToList();

                var model = new DashboardModel
                {
                    TeklifIstatistikleri = istatistikler,
                    AylikGrafik = aylikGrafik
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
                return View(new DashboardModel());
            }
        }

        #endregion

        #region Teklifler

        public IActionResult Teklifler()
        {
            try
            {
                var teklifler = _crmRepository.GetTeklifler().ToList();
                return View(teklifler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teklif listesi yüklenirken hata oluştu");
                ViewBag.ErrorMessage = "Teklif listesi yüklenirken bir hata oluştu.";
                return View(new List<TeklifListeModel>());
            }
        }
        public IActionResult FiyatTeklif()
        {
            try
            {
                var model = new TeklifFormViewModel
                {
                    CariHesaplar = _crmRepository.GetCariHesaplar().ToList(),
                    Personeller = _crmRepository.GetPersoneller().ToList(),
                    Stoklar = _crmRepository.GetStoklar().ToList(),
                    Durumlar = _crmRepository.GetTeklifDurumlari().ToList(),
                    Teklif = new YeniTeklifModel
                    {
                        // Her iki tarih alanını da aynı değerle başlat
                        Tarih = DateTime.Today,
                        BaslangicTarihi = DateTime.Today,
                   
                        FormNo = _crmRepository.GetYeniFormNumarasi(),
                        Durum = "Taslak"
                    }
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni teklif formu yüklenirken hata oluştu");
                return View(new TeklifFormViewModel());
            }
        }

        [HttpPost]
        public IActionResult Fiyatteklif(TeklifFormViewModel model)
        {
            _logger.LogInformation("Fiyatteklif POST metoduna girildi");

            // ValidationState'den problematik alanları temizle
            ModelState.Remove("Teklif.Yetkili");
            ModelState.Remove("Teklif.CreateUser");
            ModelState.Remove("Teklif.Aciklama");
            ModelState.Remove("Teklif.SorumluKod");
            ModelState.Remove("Teklif.GecerlilikSuresi"); // ✅ Bu satırı ekleyin

            // Gelen model verilerini logla
            if (model?.Teklif != null)
            {
                _logger.LogInformation($"CariKod: {model.Teklif.CariKod}");
                _logger.LogInformation($"Tarih: {model.Teklif.Tarih}");
                _logger.LogInformation($"FormNo: {model.Teklif.FormNo}");
            }

            if (model?.Teklif?.Urunler != null)
            {
                foreach (var urun in model.Teklif.Urunler)
                {
                    _logger.LogInformation($"Ürün: StokKod={urun.StokKod}, Miktar={urun.Miktar}, BirimFiyat={urun.BirimFiyat}");
                }
            }

            try
            {
                // Model validation kontrolü
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState geçersiz - Validation hataları:");
                    foreach (var modelError in ModelState)
                    {
                        var key = modelError.Key;
                        var errors = modelError.Value.Errors;
                        foreach (var error in errors)
                        {
                            _logger.LogWarning($"Hata - {key}: {error.ErrorMessage}");
                        }
                    }

                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Form verilerinde hata bulundu. Lütfen kontrol edin.";
                    return View(model);
                }

                _logger.LogInformation("ModelState geçerli - kaydetme işlemine başlanıyor");

                // Model null kontrolü
                if (model?.Teklif == null)
                {
                    _logger.LogError("Model.Teklif null geldi");
                    TempData["ErrorMessage"] = "Teklif verileri bulunamadı.";
                    return View(new TeklifFormViewModel());
                }

                // Ürün kontrolü
                if (model.Teklif.Urunler == null || !model.Teklif.Urunler.Any())
                {
                    _logger.LogWarning("Hiç ürün eklenmemiş");
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Lütfen en az bir ürün ekleyiniz.";
                    return View(model);
                }

                // Geçerli ürünleri filtrele
                var gecerliUrunler = model.Teklif.Urunler
                    .Where(u => !string.IsNullOrEmpty(u.StokKod) && u.Miktar > 0)
                    .ToList();

                if (!gecerliUrunler.Any())
                {
                    _logger.LogWarning("Hiç geçerli ürün bulunamadı");
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Lütfen geçerli ürünler ekleyiniz.";
                    return View(model);
                }

                // Sadece geçerli ürünleri kaydet
                model.Teklif.Urunler = gecerliUrunler;

                // Kullanıcı bilgisini UserNo olarak al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;
                if (!int.TryParse(userNo, out int createUserId))
                {
                    createUserId = 0; // Varsayılan değer, örneğin 'SYSTEM' yerine 0
                    _logger.LogWarning("UserNo alınamadı veya geçersiz, varsayılan değer 0 kullanıldı.");
                }
                model.Teklif.CreateUser = createUserId.ToString(); // Repository string bekliyor

                _logger.LogInformation($"Kullanıcı UserNo: {createUserId}, Repository'ye gönderilecek ürün sayısı: {model.Teklif.Urunler.Count}");

                // Repository metodunu çağır
                var result = _crmRepository.YeniTeklifKaydet(model.Teklif);

                if (result)
                {
                    _logger.LogInformation("Teklif başarıyla kaydedildi");
                    TempData["SuccessMessage"] = "Teklif başarıyla kaydedildi.";
                    return RedirectToAction("Teklifler");
                }
                else
                {
                    _logger.LogError("Repository false döndü - kaydetme başarısız");
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Teklif kaydedilirken bir hata oluştu.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiyatteklif POST metodunda beklenmeyen hata oluştu");
                try
                {
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Dropdown verileri yüklenirken hata oluştu");
                    model = new TeklifFormViewModel();
                }

                TempData["ErrorMessage"] = "Teklif kaydedilirken bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult TeklifDetay(int id)
        {
            try
            {
                var satirlar = _crmRepository.GetTeklifSatirlari(id).ToList();
                if (satirlar == null || !satirlar.Any())
                {
                    return Json(new ApiResponse<List<TeklifUrunModel>>
                    {
                        Success = false,
                        Message = "Teklif satırları bulunamadı."
                    });
                }

                // Fotoğraf verisini Base64'e çevir
                var responseData = satirlar.Select(s => new
                {
                    s.StokKod,
                    s.StokAdi,
                    s.Aciklama,
                    s.Miktar,
                    s.BirimFiyat,
                    s.IndirimliFiyat,
                    s.Toplam,
                    ImageData = s.ImageData != null ? Convert.ToBase64String(s.ImageData) : null
                }).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Data = responseData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teklif satırları yüklenirken hata oluştu");
                return Json(new ApiResponse<List<TeklifUrunModel>>
                {
                    Success = false,
                    Message = "Teklif satırları yüklenirken bir hata oluştu."
                });
            }
        }

        //[HttpGet("teklifyazdir/{teklifNo}")]
        //public IActionResult TeklifYazdir(string teklifNo, string format = "pdf")
        //{
        //    try
        //    {
        //        // Fetch offer details from repository
        //        var teklifDetay = _crmRepository.GetTeklifDetay(int.Parse(teklifNo));
        //        if (teklifDetay == null) return NotFound("Teklif bulunamadı.");

        //        // Memory stream for PDF
        //        var stream = new MemoryStream();
        //        var writer = new PdfWriter(stream);
        //        var pdf = new PdfDocument(writer);
        //        var document = new Document(pdf, PageSize.A4);

        //        // Set margins
        //        document.SetMargins(36, 36, 36, 36);

        //        // Header table
        //        Table headerTable = new Table(2).SetWidth(UnitValue.CreatePercentValue(100)).SetBorder(Border.NO_BORDER);
        //        _ = headerTable.AddCell(new Cell().Add(new Paragraph("TEKLİF FORMU").SetFontSize(14)).SetBorder(Border.NO_BORDER));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("FİRMA / KURUM: " + teklifDetay.CariAdi).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.RIGHT));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("TARİH: " + teklifDetay.tkl_evrak_tarihi).SetFontSize(10)).SetBorder(Border.NO_BORDER));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("YETKİLİ: " + teklifDetay.HazirlayanAdi).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.RIGHT));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("FORM NO: " + teklifDetay.tkl_belge_no).SetFontSize(10)).SetBorder(Border.NO_BORDER));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("KONU: " + teklifDetay.tkl_Aciklama).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.RIGHT));
        //        headerTable.AddCell(new Cell().Add(new Paragraph("HAZIRLAYAN: " + teklifDetay.HazirlayanAdi).SetFontSize(10)).SetBorder(Border.NO_BORDER));
        //        document.Add(headerTable);

        //        // Product table
        //        Table productTable = new Table(UnitValue.CreatePercentArray(new float[] { 30, 20, 10, 15, 15, 10 })).UseAllAvailableWidth().SetMarginTop(20);
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("ÜRÜN ADI")));
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("ÜRÜN GÖRSELİ")));
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("ADET")));
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("BİRİM FİYATI")));
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("İNDİRİMLİ BİRİM FİYATI")));
        //        productTable.AddHeaderCell(new Cell().Add(new Paragraph("TUTAR")));

        //        decimal total = 0;
        //        foreach (var urun in teklifDetay.Urunler)
        //        {
        //            productTable.AddCell(new Cell().Add(new Paragraph(urun.StokAdi + "\n" + urun.Aciklama)));
        //            Cell imageCell = new Cell();
        //            if (urun.ImageData != null && urun.ImageData.Length > 0)
        //            {
        //                Image image = new Image(ImageDataFactory.Create(urun.ImageData)).ScaleToFit(50, 50);
        //                imageCell.Add(image);
        //            }
        //            else
        //            {
        //                imageCell.Add(new Paragraph("Görsel Yok").SetFontSize(8));
        //            }
        //            productTable.AddCell(imageCell);
        //            productTable.AddCell(new Cell().Add(new Paragraph(urun.Miktar.ToString())));
        //            productTable.AddCell(new Cell().Add(new Paragraph(urun.BirimFiyat.ToString("N2") + " TL")));
        //            productTable.AddCell(new Cell().Add(new Paragraph(urun.IndirimliFiyat.ToString("N2") + " TL")));
        //            productTable.AddCell(new Cell().Add(new Paragraph(urun.Toplam.ToString("N2") + " TL")));
        //            total += urun.Toplam;
        //        }

        //        document.Add(productTable);

        //        // Totals
        //        document.Add(new Paragraph("TOPLAM TUTAR: " + total.ToString("N2") + " TL").SetMarginTop(20));
        //        document.Add(new Paragraph("KDV DAHİL DEĞİLDİR.").SetFontSize(10));
        //        document.Add(new Paragraph("NAKLİYE VE MONTAJ FİRMAMIZA AİTTİR.").SetFontSize(10));
        //        document.Add(new Paragraph("FİYATLARIMIZIN GEÇERLİLİK SÜRESİ 7 İŞGÜNÜDÜR.").SetFontSize(10));

        //        // Footer
        //        document.Add(new Paragraph("SAYGILARIMLA").SetMarginTop(20));
        //        document.Add(new Paragraph("GÜRKAN BERBER - 0 533 764 78 99").SetFontSize(10));
        //        document.Add(new Paragraph("BERBEROĞLU ÇELİK VE AHŞAP BÜRO MALZEMELERİ SAN. VE TİC. LTD. ŞTİ.").SetFontSize(10).SetFixedPosition(36, 36, 500));
        //        document.Add(new Paragraph("Sanayi Sitesi C-14 Blok No:23 AKDENİZ/MERSİN Tel:0 324 235 52 80 www.berberoglucelik.com - info@berberoglucelik.com").SetFontSize(10).SetFixedPosition(36, 20, 500));

        //        document.Close();

        //        var pdfBytes = stream.ToArray();
        //        return File(pdfBytes, "application/pdf", $"TEKLİF_{teklifNo}.pdf");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "PDF 생성irken hata oluştu");
        //        return StatusCode(500, "PDF oluşturulurken hata oluştu.");
        //    }
        //}

        [HttpPost]
        public IActionResult TeklifDurumGuncelle([FromBody] TeklifDurumGuncelleModel model)
        {
            try
            {
                using var connection = new SqlConnection(_dbSelectorService.GetConnectionString());

                var durumKodu = model.yeniDurum switch
                {
                    "Taslak" => "0",
                    "Gönderildi" => "1",
                    "Kazanıldı" => "2",
                    "Kaybedildi" => "3",
                    "Ertelendi" => "4",
                    "İptal Edildi" => "5",
                    _ => "0"
                };

                var query = @"
            UPDATE VERILEN_TEKLIFLER 
            SET tkl_durumu = @DurumKodu
            WHERE tkl_evrakno_sira = @TeklifId";

                // TeklifNo'yu int'e çevir
                if (int.TryParse(model.teklifId, out int teklifIdInt))
                {
                    connection.Execute(query, new { DurumKodu = durumKodu, TeklifId = teklifIdInt });
                    return Json(new { success = true, message = "Durum başarıyla güncellendi." });
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz teklif ID." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teklif durumu güncellenirken hata oluştu");
                return Json(new { success = false, message = "Durum güncellenirken bir hata oluştu." });
            }
        }

        public class TeklifDurumGuncelleModel
        {
            public string teklifId { get; set; }
            public string yeniDurum { get; set; }
        }

        #endregion

        #region Ajax Metodları

        [HttpGet]
        public JsonResult GetCariHesaplar()
        {
            try
            {
                var cariler = _crmRepository.GetCariHesaplar()
                    .Select(c => new CariSelectModel
                    {
                        Value = c.CariKod,
                        Text = $"{c.CariKod} - {c.CariAdi}"
                    }).ToList();

                return Json(new ApiResponse<List<CariSelectModel>>
                {
                    Success = true,
                    Data = cariler
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari hesaplar getirilirken hata oluştu");
                return Json(new ApiResponse<List<CariSelectModel>>
                {
                    Success = false,
                    Message = "Cari hesaplar yüklenirken hata oluştu."
                });
            }
        }

        [HttpGet]
        public JsonResult GetCariPersonelleri(string cariKod)
        {
            try
            {
                var personeller = _crmRepository.GetCariPersonelleri(cariKod)
                    .Select(p => new
                    {
                        Value = p.PersonelKod,
                        Text = p.PersonelAdi
                    }).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Data = personeller
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari personelleri getirilirken hata oluştu");
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Personeller yüklenirken hata oluştu."
                });
            }
        }

        [HttpGet]
        public JsonResult GetStokFiyati(string StokKod, int listeSiraNo = 1, int dovizCinsi = 0)
        {
            try
            {
                _logger.LogInformation($"GetStokFiyati çağrıldı - StokKod: {StokKod}");

                if (string.IsNullOrEmpty(StokKod))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Stok kodu boş olamaz",
                        fiyat = 0
                    });
                }

                var fiyat = _crmRepository.GetStokSatisFiyati(StokKod, listeSiraNo, dovizCinsi);

                _logger.LogInformation($"Repository'den dönen fiyat: {fiyat}");

                return Json(new
                {
                    success = true,
                    message = "Fiyat başarıyla alındı",
                    fiyat = fiyat,
                    formattedFiyat = fiyat.ToString("N2") + " TL" // TL sabit olsun
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStokFiyati'da hata oluştu. StokKod: {StokKod}", StokKod);
                return Json(new
                {
                    success = false,
                    message = "Fiyat bilgisi alınamadı: " + ex.Message,
                    fiyat = 0
                });
            }
        }
        // Update the existing GetStoklar method to only return basic info
        public JsonResult GetStoklar()
        {
            try
            {
                var stoklar = _crmRepository.GetStoklar();
                if (stoklar == null)
                {
                    return Json(new { Success = false, Message = "Stok verisi null döndü" });
                }

                var stokListesi = stoklar.Select(s => {
                    var imageData = s.ImageData != null && s.ImageData.Length > 0
                        ? Convert.ToBase64String(s.ImageData)
                        : null;

                    // Debug için log
                    _logger.LogInformation($"Stok {s.StokKod}: ImageData null mu? {s.ImageData == null}, " +
                                         $"Length: {s.ImageData?.Length ?? 0}, " +
                                         $"Base64 Length: {imageData?.Length ?? 0}");

                    return new
                    {
                        StokKod = s.StokKod ?? "",
                        StokAdi = s.StokAdi ?? "",
                        KisaIsim = s.KisaIsim ?? "",
                        Birim1 = s.Birim1 ?? "Adet",
                        imageData = imageData,  // Küçük harf ile gönderyoruz
                        HasImage = imageData != null
                    };
                }).ToList();

                var fotografliStokSayisi = stokListesi.Count(s => s.HasImage);
                _logger.LogInformation($"GetStoklar: {stokListesi.Count} stok döndürüldü, {fotografliStokSayisi} tanesi fotoğraflı");

                // İlk birkaç fotoğraflı stok için detaylı log
                var fotografliStoklar = stokListesi.Where(s => s.HasImage).Take(3);
                foreach (var stok in fotografliStoklar)
                {
                    _logger.LogInformation($"Fotoğraflı stok örneği: {stok.StokKod} - Base64 uzunluk: {stok.imageData?.Length}");
                }

                return Json(new { success = true, data = stokListesi });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStoklar metodunda hata oluştu");
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region Mevcut Metodlar (Fırsatlar, Müşteriler vb.)

        public IActionResult Firsatlar()
        {
            // Mevcut kod korunuyor
            var connectionString = _dbSelectorService.GetConnectionString();

            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
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

        [HttpPost]
        public IActionResult FirsatEkle(Firsat firsat)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Firsatlar");
            }

            var connectionString = _dbSelectorService.GetConnectionString();

            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
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

        public IActionResult Musteriler()
        {
            // Mevcut kod CrmRepository kullanacak şekilde güncellenebilir
            try
            {
                var musteriler = _crmRepository.GetCariHesaplar().ToList();
                return View(musteriler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri listesi yüklenirken hata oluştu");
                ViewBag.ErrorMessage = "Müşteri listesi yüklenirken bir hata oluştu.";
                return View(new List<CariHesapModel>());
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
            try
            {
                var Stoklar = _crmRepository.GetStoklar().ToList();
                return View(Stoklar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok listesi yüklenirken hata oluştu");
                ViewBag.ErrorMessage = "Stok listesi yüklenirken bir hata oluştu.";
                return View(new List<StokModel>());
            }
        }

        public IActionResult StokEkle()
        {
            return View();
        }

        public IActionResult Siparisler()
        {
            // Mevcut kod korunuyor veya repository'ye taşınabilir
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
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

        #endregion
        // CrmController.cs dosyasına eklenecek metodlar
        // CrmController.cs dosyasındaki TeklifDuzenle metodlarını şununla değiştirin:

        #region Teklif Düzenleme - Düzeltilmiş

        [HttpGet]
        [Route("crm/teklifduzenle/{teklifNo}")]
        public IActionResult TeklifDuzenle(string teklifNo)
        {
            try
            {
                _logger.LogInformation($"TeklifDuzenle GET - Gelen teklifNo: '{teklifNo}'");

                if (!int.TryParse(teklifNo, out int evrakSiraNo))
                {
                    TempData["ErrorMessage"] = "Teklif numarası geçersiz format.";
                    return RedirectToAction("Teklifler");
                }

                var mevcutTeklif = _crmRepository.GetTeklifDetay(evrakSiraNo);
                if (mevcutTeklif == null)
                {
                    TempData["ErrorMessage"] = "Teklif bulunamadı.";
                    return RedirectToAction("Teklifler");
                }

                // ✅ Debug için tarihleri logla
                _logger.LogInformation($"Veritabanından gelen tarihler:");
                _logger.LogInformation($"tkl_evrak_tarihi: '{mevcutTeklif.tkl_evrak_tarihi}'");
                _logger.LogInformation($"tkl_baslangic_tarihi: '{mevcutTeklif.tkl_baslangic_tarihi}'");
                _logger.LogInformation($"tkl_Gecerlilik_Sures: '{mevcutTeklif.tkl_Gecerlilik_Sures}'");

                var model = new TeklifEditViewModel
                {
                    MevcutTeklif = mevcutTeklif,
                    CariHesaplar = _crmRepository.GetCariHesaplar().ToList(),
                    Personeller = _crmRepository.GetPersoneller().ToList(),
                    Stoklar = _crmRepository.GetStoklar().ToList(),
                    Durumlar = _crmRepository.GetTeklifDurumlari().ToList(),
                    Teklif = new YeniTeklifModel
                    {
                        CariKod = mevcutTeklif.tkl_cari_kod,

                        // ✅ Tarih parse'ını güvenli hale getir
                        Tarih = DateTime.TryParse(mevcutTeklif.tkl_evrak_tarihi, out DateTime evrakTarih)
                            ? evrakTarih : DateTime.Today,

                        BaslangicTarihi = DateTime.TryParse(mevcutTeklif.tkl_baslangic_tarihi, out DateTime basTarih)
                            ? basTarih : DateTime.Today,

                        // ✅ Geçerlilik süresini gün sayısı olarak hesapla
                        GecerlilikSuresi = DateTime.TryParse(mevcutTeklif.tkl_evrak_tarihi, out DateTime evrak) &&
                                  DateTime.TryParse(mevcutTeklif.tkl_Gecerlilik_Sures.ToString(), out DateTime bitis)
                    ? (int)(bitis - evrak).TotalDays
                    : 7, // Varsayılan 7 gün

                        FormNo = mevcutTeklif.tkl_belge_no,
                        SorumluKod = mevcutTeklif.tkl_Sorumlu_Kod,
                        Aciklama = mevcutTeklif.tkl_Aciklama,
                        Durum = mevcutTeklif.tkl_durumu switch
                        {
                            "0" => "Taslak",
                            "1" => "Gönderildi",
                            "2" => "Kazanıldı",
                            "3" => "Kaybedildi",
                            "4" => "Ertelendi",
                            "5" => "İptal Edildi",
                            _ => "Taslak"
                        },
                        Urunler = mevcutTeklif.Urunler ?? new List<TeklifUrunModel>()
                    }
                };

                // ✅ Debug için hesaplanan değerleri logla
                _logger.LogInformation($"Hesaplanan değerler:");
                _logger.LogInformation($"Tarih: {model.Teklif.Tarih:yyyy-MM-dd}");
                _logger.LogInformation($"BaslangicTarihi: {model.Teklif.BaslangicTarihi:yyyy-MM-dd}");
                _logger.LogInformation($"GecerlilikSuresi: {model.Teklif.GecerlilikSuresi} gün");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeklifDuzenle GET - Hata oluştu. TeklifNo: '{teklifNo}'");
                TempData["ErrorMessage"] = "Teklif düzenleme sayfası yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Teklifler");
            }
        }

        // ✅ Yardımcı metod ekleyin
        private int CalculateValidityDays(string startDateStr, string endDateStr)
        {
            try
            {
                if (DateTime.TryParse(startDateStr, out DateTime startDate) &&
                    DateTime.TryParse(endDateStr, out DateTime endDate))
                {
                    int days = (int)(endDate - startDate).TotalDays;
                    _logger.LogInformation($"Geçerlilik süresi hesaplandı: {startDate:yyyy-MM-dd} -> {endDate:yyyy-MM-dd} = {days} gün");

                    // Geçerli gün sayıları (7, 14, 30)
                    if (days == 7 || days == 14 || days == 30)
                        return days;

                    // Eğer tam olarak eşleşmiyorsa en yakın değeri döndür
                    if (days <= 10) return 7;
                    if (days <= 22) return 14;
                    return 30;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Geçerlilik süresi hesaplanamadı: {ex.Message}");
            }

            return 7; // Varsayılan
        }

        // Yardımcı metod: Geçerlilik süresini hesapla

        [HttpPost]
        [Route("crm/teklifduzenle/{teklifNo}")]
        public IActionResult TeklifDuzenle(string teklifNo, TeklifEditViewModel model)
        {
            _logger.LogInformation($"TeklifDuzenle POST - TeklifNo: '{teklifNo}'");

            int evrakSiraNo = 0;

            // TÜM MevcutTeklif validation'larını kaldır
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("MevcutTeklif.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            // Diğer problematik alanları da çıkar
            ModelState.Remove("Teklif.Yetkili");
            ModelState.Remove("Teklif.CreateUser");
            ModelState.Remove("Teklif.Aciklama");
            ModelState.Remove("Teklif.SorumluKod");

            // Ürün validation'larını da temizle
            var urunKeysToRemove = ModelState.Keys.Where(k => k.Contains(".ImageData") || k.Contains(".Aciklama")).ToList();
            foreach (var key in urunKeysToRemove)
            {
                ModelState.Remove(key);
            }

            try
            {
                if (string.IsNullOrEmpty(teklifNo) || !int.TryParse(teklifNo, out evrakSiraNo))
                {
                    TempData["ErrorMessage"] = "Geçersiz teklif numarası.";
                    return RedirectToAction("Teklifler");
                }

                // Debug için ModelState hatalarını logla
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("TeklifDuzenle POST - ModelState geçersiz");
                    foreach (var modelError in ModelState)
                    {
                        var key = modelError.Key;
                        var errors = modelError.Value.Errors;
                        foreach (var error in errors)
                        {
                            _logger.LogWarning($"Validation Hata - {key}: {error.ErrorMessage}");
                        }
                    }

                    // MevcutTeklif'i yeniden yükle
                    model.MevcutTeklif = _crmRepository.GetTeklifDetay(evrakSiraNo);

                    // Dropdown verilerini yeniden yükle
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Form verilerinde hata bulundu. Lütfen kontrol edin.";
                    return View(model);
                }

                // Model null kontrolü
                if (model?.Teklif == null)
                {
                    TempData["ErrorMessage"] = "Teklif verileri bulunamadı.";
                    return RedirectToAction("Teklifler");
                }

                // Geçerli ürünleri filtrele
                var gecerliUrunler = model.Teklif.Urunler
                    ?.Where(u => !string.IsNullOrEmpty(u.StokKod) && u.Miktar > 0)
                    .ToList();

                if (gecerliUrunler == null || !gecerliUrunler.Any())
                {
                    // MevcutTeklif'i yeniden yükle
                    model.MevcutTeklif = _crmRepository.GetTeklifDetay(evrakSiraNo);
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Lütfen geçerli ürünler ekleyiniz.";
                    return View(model);
                }

                model.Teklif.Urunler = gecerliUrunler;

                // Kullanıcı bilgisini al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;
                if (!int.TryParse(userNo, out int updateUserId))
                {
                    updateUserId = 1;
                }
                model.Teklif.CreateUser = updateUserId.ToString();

                // Repository metodunu çağır
                var result = _crmRepository.TeklifGuncelle(evrakSiraNo, model.Teklif);

                if (result)
                {
                    TempData["SuccessMessage"] = "Teklif başarıyla güncellendi.";
                    return RedirectToAction("Teklifler");
                }
                else
                {
                    model.MevcutTeklif = _crmRepository.GetTeklifDetay(evrakSiraNo);
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Teklif güncellenirken bir hata oluştu.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeklifDuzenle POST - Exception: TeklifNo: '{teklifNo}'");

                try
                {
                    // evrakSiraNo artık burada da kullanılabilir
                    if (evrakSiraNo > 0)
                    {
                        model.MevcutTeklif = _crmRepository.GetTeklifDetay(evrakSiraNo);
                    }
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Dropdown verileri yüklenemedi");
                    model = new TeklifEditViewModel();
                }

                TempData["ErrorMessage"] = "Teklif güncellenirken bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }

        #endregion
        // CrmController.cs - Ürün fotoğrafları dahil PDF çözümü

        [HttpGet("crm/teklifyazdir/{teklifNo}")]
        public IActionResult TeklifYazdir(string teklifNo, string format = "pdf")
        {
            try
            {
                _logger.LogInformation($"TeklifYazdir çağrıldı - TeklifNo: {teklifNo}");

                if (string.IsNullOrEmpty(teklifNo) || !int.TryParse(teklifNo, out int evrakSiraNo))
                {
                    return BadRequest("Geçersiz teklif numarası.");
                }

                var teklifDetay = _crmRepository.GetTeklifDetay(evrakSiraNo);
                if (teklifDetay == null)
                {
                    return NotFound("Teklif bulunamadı.");
                }

                // HTML oluştur (fotoğraflar Base64 olarak embed edilecek)
                var htmlContent = GeneratePrintableHtmlWithImages(teklifDetay);

                // Format parametresine göre HTML veya PDF döndür
                if (format == "html")
                {
                    return Content(htmlContent, "text/html", System.Text.Encoding.UTF8);
                }

                // PDF olarak döndür (browser'ın print to PDF özelliğini kullanacak)
                return Content(htmlContent, "text/html", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Teklif oluşturma hatası - TeklifNo: {teklifNo}");
                return StatusCode(500, $"Teklif oluşturulurken hata: {ex.Message}");
            }
        }
        private string GeneratePrintableHtmlWithImages(dynamic teklifDetay)
        {
            decimal grandTotal = 0;
            string urunlerHtml = "";
            int satirSayisi = 0;

            // ✅ Geçerlilik süresini hesapla
            int gecerlilikGunSayisi = 7; // Varsayılan değer
            try
            {
                if (DateTime.TryParse(teklifDetay.tkl_baslangic_tarihi?.ToString(), out DateTime baslangic))
                {
                    // tkl_Gecerlilik_Sures bir tarih mi, yoksa doğrudan gün sayısı mı?
                    if (DateTime.TryParse(teklifDetay.tkl_Gecerlilik_Sures?.ToString(), out DateTime bitisTarihi))
                    {
                        // tkl_Gecerlilik_Sures bir tarihse, gün farkını hesapla
                        gecerlilikGunSayisi = (int)(bitisTarihi - baslangic).TotalDays;
                        _logger.LogInformation($"PDF için geçerlilik süresi hesaplandı: {gecerlilikGunSayisi} gün");
                    }
                    else if (int.TryParse(teklifDetay.tkl_Gecerlilik_Sures?.ToString(), out int gunSayisi))
                    {
                        // tkl_Gecerlilik_Sures bir gün sayısı ise direkt kullan
                        gecerlilikGunSayisi = gunSayisi;
                        _logger.LogInformation($"PDF için geçerlilik süresi alındı: {gecerlilikGunSayisi} gün");
                    }
                    else
                    {
                        _logger.LogWarning("tkl_Gecerlilik_Sures geçersiz formatta, varsayılan 7 gün kullanıldı.");
                    }
                }
                else
                {
                    _logger.LogWarning("tkl_baslangic_tarihi geçersiz formatta, varsayılan 7 gün kullanıldı.");
                }

                // Geçerlilik süresini makul bir aralığa sınırlayalım
                if (gecerlilikGunSayisi <= 0 || gecerlilikGunSayisi > 365)
                {
                    gecerlilikGunSayisi = 7;
                    _logger.LogWarning("Geçerlilik süresi geçersiz ({0} gün), varsayılan 7 gün kullanıldı.", gecerlilikGunSayisi);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Geçerlilik süresi hesaplanırken hata: {ex.Message}. Varsayılan 7 gün kullanıldı.");
                gecerlilikGunSayisi = 7; // Hata durumunda varsayılan değer
            }

            if (teklifDetay.Urunler != null)
            {
                foreach (var urun in teklifDetay.Urunler)
                {
                    satirSayisi++;
                    var satirToplami = urun.Miktar * urun.IndirimliFiyat;
                    grandTotal += satirToplami;

                    // Ürün fotoğrafını Base64'e çevir
                    string imageHtml = "";
                    if (urun.ImageData != null && urun.ImageData.Length > 0)
                    {
                        try
                        {
                            string base64Image = Convert.ToBase64String(urun.ImageData);
                            imageHtml = $"<img src='data:image/jpeg;base64,{base64Image}' style='width: 80px; height: 80px; object-fit: cover; border-radius: 4px; border: 1px solid #ddd;' alt='Ürün Fotoğrafı'>";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Ürün fotoğrafı işlenemedi: {ex.Message}");
                            imageHtml = "<div style='width: 80px; height: 80px; background: #f0f0f0; display: flex; align-items: center; justify-content: center; border: 1px dashed #ccc; border-radius: 4px; font-size: 10px; color: #666;'>Fotoğraf<br>Yok</div>";
                        }
                    }
                    else
                    {
                        imageHtml = "<div style='width: 80px; height: 80px; background: #f0f0f0; display: flex; align-items: center; justify-content: center; border: 1px dashed #ccc; border-radius: 4px; font-size: 10px; color: #666;'>Fotoğraf<br>Yok</div>";
                    }

                    urunlerHtml += $@"
                <tr style='page-break-inside: avoid;'>
                    <td style='border: 1px solid #ddd; padding: 10px; vertical-align: top; width: 25%;'>
                        <div style='font-weight: bold; font-size: 13px; margin-bottom: 5px;'>{urun.StokAdi ?? ""}</div>
                        <div style='font-size: 11px; color: #666;'>{urun.StokKod ?? ""}</div>
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; text-align: center; vertical-align: middle; width: 15%;'>
                        {imageHtml}
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; text-align: center; vertical-align: middle; width: 10%; font-size: 14px; font-weight: bold;'>
                        {urun.Miktar:N0}
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; text-align: right; vertical-align: middle; width: 12%; font-size: 13px;'>
                        {urun.BirimFiyat:N2} TL
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; text-align: right; vertical-align: middle; width: 12%; font-size: 13px; color: #e74c3c; font-weight: bold;'>
                        {urun.IndirimliFiyat:N2} TL
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; text-align: right; vertical-align: middle; width: 13%; font-size: 14px; font-weight: bold; background: #f8f9fa;'>
                        {satirToplami:N2} TL
                    </td>
                    <td style='border: 1px solid #ddd; padding: 10px; vertical-align: top; width: 13%; font-size: 11px; color: #555;'>
                        {urun.Aciklama ?? ""}
                    </td>
                </tr>";
                }
            }

            var kdv = grandTotal * 0.20m;
            var toplamTutar = grandTotal; // KDV hariç toplam
            var tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            // HTML çıktısını oluştur
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Teklif - {teklifDetay.tkl_belge_no}</title>
        <style>
            @page {{
                size: A4;
                margin: 15mm;
            }}
            
            body {{
                font-family: 'Segoe UI', Arial, sans-serif;
                font-size: 12px;
                line-height: 1.4;
                margin: 0;
                padding: 0;
                color: #333;
                background: white;
            }}
            
            .header {{
                text-align: center;
                border-bottom: 4px solid #2c5aa0;
                padding-bottom: 20px;
                margin-bottom: 25px;
                page-break-after: avoid;
            }}
            
            .company-logo {{
                font-size: 20px;
                font-weight: bold;
                color: #2c5aa0;
                margin-bottom: 3px;
                text-transform: uppercase;
                letter-spacing: 1px;
            }}
            
            .company-subtitle {{
                font-size: 14px;
                font-weight: 600;
                color: #1e3d72;
                margin-bottom: 8px;
                text-transform: uppercase;
            }}
            
            .company-address {{
                font-size: 11px;
                color: #666;
                margin-bottom: 15px;
                line-height: 1.3;
            }}
            
            .form-title {{
                font-size: 28px;
                font-weight: bold;
                color: #2c5aa0;
                margin: 15px 0;
                text-shadow: 1px 1px 2px rgba(0,0,0,0.1);
            }}
            
            .info-grid {{
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 20px;
                margin: 25px 0;
                page-break-after: avoid;
            }}
            
            .info-box {{
                background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
                padding: 15px;
                border-radius: 8px;
                border-left: 5px solid #2c5aa0;
                box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            }}
            
            .info-title {{
                font-weight: bold;
                font-size: 14px;
                color: #2c5aa0;
                margin-bottom: 10px;
                text-transform: uppercase;
                letter-spacing: 0.5px;
            }}
            
            .info-row {{
                margin: 6px 0;
                font-size: 12px;
                display: flex;
                justify-content: space-between;
            }}
            
            .info-label {{
                font-weight: 600;
                color: #495057;
                min-width: 80px;
            }}
            
            .info-value {{
                color: #2c3e50;
                font-weight: 500;
                text-align: right;
                flex: 1;
            }}
            
            .products-section {{
                margin: 30px 0;
                page-break-after: avoid;
            }}
            
            .products-title {{
                font-size: 18px;
                font-weight: bold;
                color: #2c5aa0;
                margin-bottom: 15px;
                padding-bottom: 8px;
                border-bottom: 3px solid #2c5aa0;
                text-transform: uppercase;
                letter-spacing: 1px;
            }}
            
            .products-table {{
                width: 100%;
                border-collapse: collapse;
                margin: 15px 0;
                font-size: 11px;
                box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            }}
            
            .products-table th {{
                background: linear-gradient(135deg, #2c5aa0 0%, #1e3d72 100%);
                color: white;
                padding: 12px 8px;
                text-align: center;
                font-weight: bold;
                border: 1px solid #1e3d72;
                text-transform: uppercase;
                letter-spacing: 0.3px;
                font-size: 10px;
            }}
            
            .products-table td {{
                border: 1px solid #dee2e6;
                padding: 8px;
                vertical-align: middle;
            }}
            
            .products-table tr:nth-child(even) {{
                background: #f8f9fa;
            }}
            
            .products-table tr:hover {{
                background: #e3f2fd;
            }}
            
            .total-section {{
                float: right;
                width: 350px;
                margin: 30px 0;
                background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
                padding: 20px;
                border-radius: 10px;
                border: 2px solid #2c5aa0;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                page-break-inside: avoid;
            }}
            
            .total-title {{
                text-align: center;
                font-weight: bold;
                color: #2c5aa0;
                margin-bottom: 15px;
                font-size: 16px;
                text-transform: uppercase;
                letter-spacing: 1px;
            }}
            
            .total-row {{
                display: flex;
                justify-content: space-between;
                margin: 10px 0;
                font-size: 13px;
                padding: 5px 0;
            }}
            
            .total-label {{
                font-weight: 600;
                color: #495057;
            }}
            
            .total-value {{
                font-weight: bold;
                color: #2c3e50;
            }}
            
            .total-final {{
                border-top: 3px solid #2c5aa0;
                padding-top: 15px;
                margin-top: 15px;
                font-size: 16px;
                font-weight: bold;
                background: rgba(44, 90, 160, 0.1);
                padding: 12px;
                border-radius: 5px;
            }}
            
            .total-final .total-label,
            .total-final .total-value {{
                color: #2c5aa0;
                font-size: 18px;
            }}
            
            .notes {{
                clear: both;
                margin: 40px 0 30px 0;
                background: linear-gradient(135deg, #fff3cd 0%, #ffeaa7 100%);
                padding: 20px;
                border-radius: 10px;
                border-left: 6px solid #ffc107;
                box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                page-break-inside: avoid;
            }}
            
            .notes h4 {{
                color: #856404;
                margin: 0 0 15px 0;
                font-size: 16px;
                text-transform: uppercase;
                letter-spacing: 0.5px;
            }}
            
            .notes ul {{
                margin: 0;
                padding-left: 20px;
                list-style-type: none;
            }}
            
            .notes li {{
                margin: 8px 0;
                font-size: 12px;
                color: #856404;
                font-weight: 500;
                position: relative;
                padding-left: 15px;
            }}
            
            .notes li:before {{
                content: '→';
                position: absolute;
                left: 0;
                color: #ffc107;
                font-weight: bold;
            }}
            
            .signature {{
                text-align: center;
                margin: 50px 0;
                page-break-inside: avoid;
            }}
            
            .signature-title {{
                font-size: 16px;
                font-weight: bold;
                margin-bottom: 20px;
                color: #2c3e50;
            }}
            
            .signature-line {{
                width: 200px;
                height: 60px;
                border-bottom: 2px solid #333;
                margin: 0 auto 10px auto;
                display: flex;
                align-items: flex-end;
                justify-content: center;
                padding-bottom: 5px;
            }}
            
            .signature-name {{
                font-size: 18px;
                font-weight: bold;
                color: #2c5aa0;
                margin: 15px 0 5px 0;
                text-transform: uppercase;
                letter-spacing: 1px;
            }}
            
            .signature-title-person {{
                font-size: 12px;
                color: #6c757d;
                font-weight: 600;
                margin-bottom: 8px;
            }}
            
            .signature-contact {{
                font-size: 12px;
                color: #495057;
                margin: 3px 0;
                font-weight: 500;
            }}
            
            .print-controls {{
                position: fixed;
                top: 20px;
                right: 20px;
                background: white;
                padding: 15px;
                border-radius: 10px;
                box-shadow: 0 8px 24px rgba(0,0,0,0.2);
                z-index: 1000;
                border: 2px solid #2c5aa0;
            }}
            
            .print-button {{
                background: linear-gradient(135deg, #2c5aa0 0%, #1e3d72 100%);
                color: white;
                border: none;
                padding: 12px 20px;
                border-radius: 6px;
                cursor: pointer;
                margin: 5px;
                font-size: 13px;
                font-weight: bold;
                transition: all 0.3s ease;
                box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            }}
            
            .print-button:hover {{
                background: linear-gradient(135deg, #1e3d72 0%, #2c5aa0 100%);
                transform: translateY(-2px);
                box-shadow: 0 4px 8px rgba(0,0,0,0.3);
            }}
            
            .close-button {{
                background: linear-gradient(135deg, #6c757d 0%, #495057 100%);
                color: white;
                border: none;
                padding: 12px 20px;
                border-radius: 6px;
                cursor: pointer;
                margin: 5px;
                font-size: 13px;
                font-weight: bold;
                transition: all 0.3s ease;
            }}
            
            .close-button:hover {{
                background: linear-gradient(135deg, #495057 0%, #6c757d 100%);
                transform: translateY(-2px);
            }}
            
            .summary-stats {{
                background: #e3f2fd;
                padding: 15px;
                border-radius: 8px;
                margin: 20px 0;
                border-left: 5px solid #2196f3;
                display: flex;
                justify-content: space-between;
                font-size: 12px;
                page-break-inside: avoid;
            }}
            
            .stat-item {{
                text-align: center;
                flex: 1;
            }}
            
            .stat-value {{
                font-size: 16px;
                font-weight: bold;
                color: #1976d2;
                display: block;
            }}
            
            .stat-label {{
                color: #455a64;
                font-size: 10px;
                text-transform: uppercase;
                margin-top: 3px;
            }}
            /* Mevcut CSS'inizdeki bu kısımları değiştirin */

/* Print media query'yi daha agresif hale getirin */
@media print {{
    .print-controls {{
        display: none !important;
    }}
    
    body {{
        font-size: 9px !important; /* 11px'den 9px'e düşür */
        line-height: 1.2 !important; /* 1.4'ten 1.2'ye düşür */
        margin: 0;
        padding: 0;
    }}
    
    /* Header kısmını küçült */
    .header {{
        padding-bottom: 10px !important; /* 20px'den 10px'e */
        margin-bottom: 15px !important; /* 25px'den 15px'e */
    }}
    
    .company-logo {{
        font-size: 14px !important; /* 20px'den 14px'e */
        margin-bottom: 2px !important;
    }}
    
    .company-subtitle {{
        font-size: 10px !important; /* 14px'den 10px'e */
        margin-bottom: 4px !important;
    }}
    
    .company-address {{
        font-size: 8px !important; /* 11px'den 8px'e */
        margin-bottom: 8px !important;
    }}
    
    .form-title {{
        font-size: 18px !important; /* 28px'den 18px'e */
        margin: 8px 0 !important;
    }}
    
    /* Info grid'i tek sütun yap ve küçült */
    .info-grid {{
        display: block !important;
        margin: 10px 0 !important; /* 25px'den 10px'e */
    }}
    
    .info-box {{
        margin: 5px 0 !important; /* 8px'den 5px'e */
        padding: 8px !important; /* 15px'den 8px'e */
        page-break-inside: avoid;
    }}
    
    .info-title {{
        font-size: 10px !important; /* 14px'den 10px'e */
        margin-bottom: 5px !important;
    }}
    
    .info-row {{
        margin: 3px 0 !important; /* 6px'den 3px'e */
        font-size: 8px !important; /* 12px'den 8px'e */
    }}
    
    /* Summary stats'ı küçült */
    .summary-stats {{
        padding: 8px !important; /* 15px'den 8px'e */
        margin: 10px 0 !important; /* 20px'den 10px'e */
        font-size: 8px !important;
    }}
    
    .stat-value {{
        font-size: 12px !important; /* 16px'den 12px'e */
    }}
    
    .stat-label {{
        font-size: 7px !important; /* 10px'den 7px'e */
    }}
    
    /* Products section'ı küçült */
    .products-section {{
        margin: 15px 0 !important; /* 30px'den 15px'e */
    }}
    
    .products-title {{
        font-size: 12px !important; /* 18px'den 12px'e */
        margin-bottom: 8px !important;
        padding-bottom: 4px !important;
    }}
    
    .products-table {{
        font-size: 7px !important; /* 10px'den 7px'e */
        margin: 8px 0 !important;
    }}
    
    .products-table th {{
        font-size: 6px !important; /* 9px'den 6px'e */
        padding: 4px 2px !important; /* 8px 4px'den 4px 2px'e */
        line-height: 1.1 !important;
    }}
    
    .products-table td {{
        padding: 3px 2px !important; /* 6px 4px'den 3px 2px'ye */
        font-size: 7px !important;
        line-height: 1.1 !important;
    }}
    
    /* Ürün fotoğraflarını küçült */
    .products-table img {{
        width: 40px !important; /* 80px'den 40px'e */
        height: 40px !important; /* 80px'den 40px'e */
    }}
    
    /* Fotoğraf yoksa placeholder'ı küçült */
    .products-table div[style*=""width: 80px""] {{
        width: 40px !important;
        height: 40px !important;
        font-size: 6px !important;
    }}
    
    /* Total section'ı küçült */
    .total-section {{
        float: none !important;
        width: auto !important;
        margin: 10px 0 !important; /* 15px'den 10px'e */
        padding: 10px !important; /* 20px'den 10px'e */
    }}
    
    .total-title {{
        font-size: 10px !important; /* 16px'den 10px'e */
        margin-bottom: 8px !important;
    }}
    
    .total-row {{
        margin: 4px 0 !important; /* 10px'den 4px'e */
        font-size: 8px !important; /* 13px'den 8px'e */
        padding: 2px 0 !important;
    }}
    
    .total-final {{
        padding: 6px !important; /* 12px'den 6px'e */
        margin-top: 8px !important;
        font-size: 9px !important;
    }}
    
    .total-final .total-label,
    .total-final .total-value {{
        font-size: 10px !important; /* 18px'den 10px'e */
    }}
    
    /* Notes kısmını küçült */
    .notes {{
        margin: 15px 0 10px 0 !important; /* 40px 0 30px 0'dan düşür */
        padding: 10px !important; /* 20px'den 10px'e */
    }}
    
    .notes h4 {{
        font-size: 9px !important; /* 16px'den 9px'e */
        margin: 0 0 8px 0 !important;
    }}
    
    .notes li {{
        margin: 3px 0 !important; /* 8px'den 3px'e */
        font-size: 7px !important; /* 12px'den 7px'e */
    }}
    
    /* Signature kısmını küçült */
    .signature {{
        margin: 20px 0 !important; /* 50px'den 20px'e */
    }}
    
    .signature-title {{
        font-size: 9px !important; /* 16px'den 9px'e */
        margin-bottom: 10px !important;
    }}
    
    .signature-line {{
        width: 120px !important; /* 200px'den 120px'e */
        height: 30px !important; /* 60px'den 30px'e */
    }}
    
    .signature-name {{
        font-size: 10px !important; /* 18px'den 10px'e */
        margin: 8px 0 3px 0 !important;
    }}
    
    .signature-title-person {{
        font-size: 8px !important; /* 12px'den 8px'e */
        margin-bottom: 4px !important;
    }}
    
    .signature-contact {{
        font-size: 7px !important; /* 12px'den 7px'e */
        margin: 1px 0 !important;
    }}
    
    /* Sayfa kenar boşluklarını azalt */
    @page {{
        size: A4;
        margin: 10mm !important; /* 15mm'den 10mm'e */
    }}
    
    /* Gereksiz boşlukları kaldır */
    * {{
        margin: 0 !important;
        padding: 0 !important;
    }}
    
    /* Sadece gerekli elementlere margin/padding ver */
    .header, .info-grid, .products-section, 
    .total-section, .notes, .signature {{
        margin-top: 8px !important;
        margin-bottom: 8px !important;
    }}
}}

/* Ekran görünümü için de biraz küçültmek isterseniz */
@media screen {{
    .company-address {{
        font-size: 10px; /* 11px'den 10px'e */
    }}
    
    .info-row {{
        font-size: 11px; /* 12px'den 11px'e */
    }}
    
    .products-table {{
        font-size: 10px; /* 11px'den 10px'e */
    }}
}}
        </style>
    </head>
    <body>
        <div class='print-controls'>
            <button class='print-button' onclick='window.print()'>🖨️ PDF Olarak Yazdır</button>
            <button class='close-button' onclick='window.close()'>❌ Kapat</button>
        </div>
        
        <div class='header'>
            <div class='company-logo'>BERBEROĞLU ÇELİK VE AHŞAP BÜRO MALZEMELERİ</div>
            <div class='company-subtitle'>SANAYİ VE TİCARET LİMİTED ŞİRKETİ</div>
            <div class='company-address'>
                Sanayi Sitesi C-14 Blok No:23 AKDENİZ/MERSİN<br>
                Tel: 0 324 235 52 80 • www.berberoglucelik.com • info@berberoglucelik.com
            </div>
            <div class='form-title'>FİYAT TEKLİF FORMU</div>
        </div>
        
        <div class='info-grid'>
            <div class='info-box'>
                <div class='info-title'>📋 Teklif Detayları</div>
                <div class='info-row'>
                    <span class='info-label'>Teklif No:</span>
                    <span class='info-value'>{teklifDetay.tkl_belge_no}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Tarih:</span>
                    <span class='info-value'>{teklifDetay.tkl_evrak_tarihi}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Hazırlayan:</span>
                    <span class='info-value'>{teklifDetay.HazirlayanAdi}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Yazdırma:</span>
                    <span class='info-value'>{tarih}</span>
                </div>
            </div>
            
            <div class='info-box'>
                <div class='info-title'>🏢 Müşteri Bilgileri</div>
                <div class='info-row'>
                    <span class='info-label'>Firma:</span>
                    <span class='info-value'>{teklifDetay.CariAdi}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Konu:</span>
                    <span class='info-value'>{teklifDetay.tkl_Aciklama}</span>
                </div>
            </div>
        </div>
        
        <div class='summary-stats'>
            <div class='stat-item'>
                <span class='stat-value'>{satirSayisi}</span>
                <span class='stat-label'>Toplam Ürün</span>
            </div>
            <div class='stat-item'>
                <span class='stat-value'>{grandTotal:N0}</span>
                <span class='stat-label'>Ara Toplam (TL)</span>
            </div>
            <div class='stat-item'>
                <span class='stat-value'>{gecerlilikGunSayisi}</span>
                <span class='stat-label'>Geçerlilik (Gün)</span>
            </div>
        </div>
        
        <div class='products-section'>
            <div class='products-title'>📦 ÜRÜN LİSTESİ VE FOTOĞRAFLAR</div>
            
            <table class='products-table'>
                <thead>
                    <tr>
                        <th>Ürün Bilgileri</th>
                        <th>Ürün Fotoğrafı</th>
                        <th>Miktar</th>
                        <th>Liste Fiyatı</th>
                        <th>Teklif Fiyatı</th>
                        <th>Toplam Tutar</th>
                        <th>Açıklama</th>
                    </tr>
                </thead>
                <tbody>
                    {urunlerHtml}
                </tbody>
            </table>
        </div>
        
        <div class='total-section'>
            <div class='total-title'>💰 TOPLAM HESAPLAMA</div>
            
            <div class='total-row'>
                <span class='total-label'>Ara Toplam:</span>
                <span class='total-value'>{grandTotal:N2} TL</span>
            </div>
            
            <div class='total-row total-final'>
                <span class='total-label'>GENEL TOPLAM:</span>
                <span class='total-value'>{toplamTutar:N2} TL</span>
            </div>
        </div>
        
        <div class='notes'>
            <h4>⚠️ ÖNEMLİ ŞARTLAR VE KOŞULLAR</h4>
            <ul>
                <li>Yukarıda belirtilen tüm fiyatlarda KDV (%20) dahil değildir.</li>
                <li>Nakliye, kurulum ve montaj hizmetleri firmamız tarafından ücretsiz yapılacaktır.</li>
                <li>Bu teklifin geçerlilik süresi düzenleme tarihinden itibaren {gecerlilikGunSayisi} iş günüdür.</li>
            </ul>
        </div>
        
        <div class='signature'>
            <div class='signature-title'>Teklifimizi değerlendirmenizi ümit ediyor, saygılarımızı sunuyoruz.</div>
            
            <div class='signature-line'>
                <span style='font-size: 14px; color: #2c5aa0; font-weight: bold;'>Yetkili İmza</span>
            </div>
            
            <div class='signature-name'>GÜRKAN BERBER</div>
            <div class='signature-title-person'>Satış Temsilcisi</div>
            <div class='signature-contact'>Cep: 0 533 764 78 99</div>
            <div class='signature-contact'>E-posta: info@berberoglucelik.com</div>
            <div class='signature-contact'>WhatsApp: 0 533 764 78 99</div>
        </div>
        
        <script>
            console.log('Teklif PDF sayfası yüklendi');
            console.log('Toplam ürün sayısı: {satirSayisi}');
            console.log('Toplam tutar: {toplamTutar:N2} TL');
            
            document.addEventListener('keydown', function(e) {{
                if (e.ctrlKey && e.key === 'p') {{
                    e.preventDefault();
                    window.print();
                }}
                if (e.key === 'Escape') {{
                    window.close();
                }}
            }});
            
            const images = document.querySelectorAll('img[src^=""data:image""]');
            console.log(`${{images.length}} ürün fotoğrafı yüklendi`);
        </script>
    </body>
    </html>";
        }

        [HttpPost]
        public IActionResult TeklifGuncelle(TeklifEditViewModel model)  // Method adını değiştirin
        {
            _logger.LogInformation("TeklifGuncelle POST metoduna girildi");

            // Validation state'den problematik alanları temizle
            ModelState.Remove("Teklif.Yetkili");
            ModelState.Remove("Teklif.CreateUser");
            ModelState.Remove("Teklif.Aciklama");
            ModelState.Remove("Teklif.SorumluKod");

            // ViewData'dan UpdateUser değerini al
            string updateUser = ViewData["UpdateUser"]?.ToString() ?? "1";

            try
            {
                // Model validation kontrolü
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("TeklifGuncelle POST - ModelState geçersiz");
                    foreach (var modelError in ModelState)
                    {
                        var key = modelError.Key;
                        var errors = modelError.Value.Errors;
                        foreach (var error in errors)
                        {
                            _logger.LogWarning($"Validation Hata - {key}: {error.ErrorMessage}");
                        }
                    }

                    // Dropdown verilerini yeniden yükle
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Form verilerinde hata bulundu. Lütfen kontrol edin.";
                    return View("TeklifDuzenle", model);
                }

                // Model null kontrolü
                if (model?.Teklif == null || model?.MevcutTeklif == null)
                {
                    _logger.LogError("TeklifGuncelle POST - Model.Teklif veya MevcutTeklif null");
                    TempData["ErrorMessage"] = "Teklif verileri bulunamadı.";
                    return RedirectToAction("Teklifler");
                }

                int evrakSiraNo = model.MevcutTeklif.tkl_evrakno_sira;

                // Geçerli ürünleri filtrele
                var gecerliUrunler = model.Teklif.Urunler
                    ?.Where(u => !string.IsNullOrEmpty(u.StokKod) && u.Miktar > 0)
                    .ToList();

                if (gecerliUrunler == null || !gecerliUrunler.Any())
                {
                    _logger.LogWarning("TeklifGuncelle POST - Geçerli ürün bulunamadı");
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Lütfen geçerli ürünler ekleyiniz.";
                    return View("TeklifDuzenle", model);
                }

                model.Teklif.Urunler = gecerliUrunler;

                // Kullanıcı bilgisini al
                string userNo = User.Claims.FirstOrDefault(c => c.Type == "UserNo")?.Value;
                if (!int.TryParse(userNo, out int updateUserId))
                {
                    updateUserId = 1;
                    _logger.LogWarning("TeklifGuncelle POST - UserNo parse edilemedi, 1 kullanıldı");
                }
                model.Teklif.CreateUser = updateUserId.ToString();

                _logger.LogInformation($"TeklifGuncelle POST - Repository çağrılıyor. EvrakSira: {evrakSiraNo}, Ürün sayısı: {model.Teklif.Urunler.Count}");

                // Repository metodunu çağır
                var result = _crmRepository.TeklifGuncelle(evrakSiraNo, model.Teklif);

                if (result)
                {
                    _logger.LogInformation($"TeklifGuncelle POST - Başarılı: {evrakSiraNo}");
                    TempData["SuccessMessage"] = "Teklif başarıyla güncellendi.";
                    return RedirectToAction("Teklifler");
                }
                else
                {
                    _logger.LogError($"TeklifGuncelle POST - Repository false döndü: {evrakSiraNo}");
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();

                    TempData["ErrorMessage"] = "Teklif güncellenirken bir hata oluştu.";
                    return View("TeklifDuzenle", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeklifGuncelle POST - Exception");
                try
                {
                    model.CariHesaplar = _crmRepository.GetCariHesaplar().ToList();
                    model.Personeller = _crmRepository.GetPersoneller().ToList();
                    model.Stoklar = _crmRepository.GetStoklar().ToList();
                    model.Durumlar = _crmRepository.GetTeklifDurumlari().ToList();
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "TeklifGuncelle POST - Dropdown verileri yüklenemedi");
                    model = new TeklifEditViewModel();
                }

                TempData["ErrorMessage"] = "Teklif güncellenirken bir hata oluştu: " + ex.Message;
                return View("TeklifDuzenle", model);
            }
        }
    }
}

// Alternatif Route tanımlaması - eğer yukarıdaki çalışmazsa bunu deneyin:
// Startup.cs veya Program.cs'de route tanımlaması:

/*
app.MapControllerRoute(
    name: "TeklifDuzenle",
    pattern: "crm/teklifduzenle/{teklifNo}",
    defaults: new { controller = "Crm", action = "TeklifDuzenle" }
);
*/

// Ayrıca HTML'deki action button'ları da kontrol edin
// Şu şekilde olmalı:

/*
<button class="action-btn"
        onclick="event.stopPropagation(); editTeklif('@teklif.TeklifNo')"
        title="Düzenle">
    <i class="fas fa-edit"></i>
</button>
*/
