using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data.SqlClient;
using static Deneme_proje.Models.AktarimEntities;
using System.Globalization;
using System.Text;


namespace Deneme_proje.Controllers
{
    public class AktarimController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AktarimController> _logger;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FaturaRepository _faturaRepository;

        public AktarimController(
            IConfiguration configuration,
            DatabaseSelectorService dbSelectorService,
            ILogger<AktarimController> logger,
            IHttpContextAccessor httpContextAccessor,
            FaturaRepository faturaRepository) // FaturaRepository'i doğrudan enjekte ediyoruz
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _faturaRepository = faturaRepository;
        }

        [HttpGet]
        public IActionResult Index(string selectedParametre = null)
        {
            try
            {
                List<StockMovement> model = new List<StockMovement>();
                ViewBag.Parametreler = GetParametreler();
                ViewBag.SelectedParametre = selectedParametre;

                // Repository metotlarını doğrudan kullanıyoruz
                ViewBag.Depolar = _faturaRepository.GetDepoList();
                ViewBag.SorumluKodlari = _faturaRepository.GetSorumluKodlari(DateTime.Now, DateTime.Now);

                if (!string.IsNullOrEmpty(selectedParametre))
                {
                    // Seçilen parametrenin stok kodlarını al
                    var parametreler = GetParametreler();
                    var secilenParametre = parametreler.FirstOrDefault(p => p.ParametreAdi == selectedParametre);

                    if (secilenParametre != null)
                    {
                        ViewBag.SecilenStokKodlari = secilenParametre.StokKodu.Split(',').Select(s => s.Trim()).ToList();
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index sayfası yüklenirken hata oluştu");
                ViewBag.ErrorMessage = "Veri yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(new List<StockMovement>());
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Index(IFormFile excelFile, string selectedParametre, string urunKodu, string girisDep, string cikisDep, string miktar, string srmMerkezi, string tarih)
        {
            try
            {
                var data = new List<StockMovement>();

                // Formdan gelen tüm değerleri TempData'ya kaydet
                TempData["UrunKodu"] = urunKodu;
                TempData["GirisDepo"] = girisDep;
                TempData["CikisDepo"] = cikisDep;
                TempData["Miktar"] = miktar;
                TempData["SrmMerkezi"] = srmMerkezi;
                TempData["Tarih"] = tarih;
                TempData["SelectedParametre"] = selectedParametre;

                if (excelFile != null && excelFile.Length > 0)
                {
                    using var stream = new MemoryStream();
                    excelFile.CopyTo(stream);
                    stream.Position = 0;

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        ViewBag.ErrorMessage = "Excel dosyasında geçerli bir sayfa bulunamadı.";
                        return View(data);
                    }

                    string currentGroup = string.Empty;
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var kod = worksheet.Cells[row, 1].Text;
                        var aciklama = worksheet.Cells[row, 2].Text;

                        if (string.IsNullOrWhiteSpace(kod) && !string.IsNullOrWhiteSpace(aciklama))
                        {
                            currentGroup = aciklama.Trim();
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(kod))
                        {
                            data.Add(new StockMovement
                            {
                                Grup = currentGroup,
                                StokKodu = kod.Replace("#", "").Trim(),
                                Aciklama = aciklama,
                                Birim = worksheet.Cells[row, 3].Text,
                                Fiyat = worksheet.Cells[row, 4].Text,
                                Net = worksheet.Cells[row, 5].Text,
                                Brut = worksheet.Cells[row, 6].Text,
                                Agirlik = worksheet.Cells[row, 7].Text,
                                NetTutar = worksheet.Cells[row, 8].Text,
                                BrutTutar = worksheet.Cells[row, 9].Text,
                                Para = worksheet.Cells[row, 10].Text,
                                Kur = worksheet.Cells[row, 11].Text
                            });
                        }
                    }
                }

                ViewBag.Parametreler = GetParametreler();
                ViewBag.SelectedParametre = selectedParametre;
                ViewBag.Depolar = _faturaRepository.GetDepoList();
                ViewBag.SorumluKodlari = _faturaRepository.GetSorumluKodlari(DateTime.Now, DateTime.Now);

                if (!string.IsNullOrEmpty(selectedParametre))
                {
                    var parametre = GetParametreler()
                                    .FirstOrDefault(p => p.ParametreAdi == selectedParametre);

                    if (parametre != null)
                    {
                        ViewBag.SecilenStokKodlari = parametre.StokKodu.Split(',')
                                                                        .Select(s => s.Trim())
                                                                        .ToList();
                    }
                }

                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel dosyası işlenirken hata oluştu.");
                ViewBag.ErrorMessage = "Excel dosyası okunurken bir hata oluştu. Lütfen dosya formatını ve içeriğini kontrol edin.";
                return View(new List<StockMovement>());
            }
        }
     
        public IActionResult ParametreAyarla()
        {
            try
            {
                var stoklar = GetStokList();
                ViewBag.Parametreler = GetParametreler();
                return View(stoklar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre ayarla sayfası yüklenirken hata oluştu");
                ViewBag.ErrorMessage = "Parametreler yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(new List<StokModel>());
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ParametreAyarla(string parametreAdi, List<string> secilenStoklar)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parametreAdi) || secilenStoklar == null || !secilenStoklar.Any())
                {
                    ViewBag.Mesaj = "Parametre adı ve en az bir stok seçilmelidir.";
                    ViewBag.Parametreler = GetParametreler();
                    return View(GetStokList());
                }

                var stoklarVirgullu = string.Join(",", secilenStoklar);

                using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
                conn.Open();
                using var cmd = new SqlCommand("INSERT INTO AktarimParametre (ParametreAdi, StokKodu) VALUES (@adi, @kodlar)", conn);
                cmd.Parameters.AddWithValue("@adi", parametreAdi);
                cmd.Parameters.AddWithValue("@kodlar", stoklarVirgullu);
                cmd.ExecuteNonQuery();

                ViewBag.Mesaj = "Parametre kaydedildi.";
                ViewBag.Parametreler = GetParametreler();
                return View(GetStokList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre kaydedilirken hata oluştu. ParametreAdi: {ParametreAdi}", parametreAdi);
                ViewBag.ErrorMessage = "Parametre kaydedilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                ViewBag.Parametreler = GetParametreler();
                return View(GetStokList());
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ParametreSil(string parametreAdi)
        {
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
                conn.Open();
                using var cmd = new SqlCommand("DELETE FROM AktarimParametre WHERE ParametreAdi = @adi", conn);
                cmd.Parameters.AddWithValue("@adi", parametreAdi);
                cmd.ExecuteNonQuery();

                ViewBag.Mesaj = "Parametre silindi.";
                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre silinirken hata oluştu. ParametreAdi: {ParametreAdi}", parametreAdi);
                ViewBag.ErrorMessage = "Parametre silinirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
        }

        [AllowAnonymous]
        private List<StokModel> GetStokList()
        {
            var result = new List<StokModel>();

            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DynamicDatabase"));
                conn.Open();
                using var cmd = new SqlCommand("SELECT sto_kod, sto_isim FROM STOKLAR", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new StokModel
                    {
                        StokKodu = reader.GetString(0),
                        StokAdi = reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok listesi alınırken hata oluştu");
                // Boş liste döndür
            }

            return result;
        }

        [AllowAnonymous]
        private List<ParametreModel> GetParametreler()
        {
            var result = new List<ParametreModel>();

            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
                conn.Open();
                using var cmd = new SqlCommand("SELECT ParametreAdi, StokKodu FROM AktarimParametre", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new ParametreModel
                    {
                        ParametreAdi = reader.GetString(0),
                        StokKodu = reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre listesi alınırken hata oluştu");
                // Boş liste döndür
            }

            return result;
        }
        public int GetSonSatirNo(string urunKodu)
        {
            int satirNo = 0;

            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DynamicDatabase"));
                conn.Open();
                using var cmd = new SqlCommand("SELECT MAX(rec_satirno) FROM URUN_RECETELERI WHERE rec_anakod = @urunKodu", conn);
                cmd.Parameters.AddWithValue("@urunKodu", urunKodu);

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    satirNo = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Son satır numarası alınırken hata oluştu. UrunKodu: {UrunKodu}", urunKodu);
            }

            return satirNo;
        }

        public void UrunReceteEkle(string urunKodu, DateTime tarih, decimal anaMiktar,
                                   string stokKodu, decimal tuketimMiktar, int depoNo, int satirNo)
        {
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DynamicDatabase"));
                conn.Open();

                string sql = @"INSERT INTO URUN_RECETELERI (
                        rec_anakod, rec_tarih, rec_anamiktar, 
                        rec_tuketim_kod, rec_tuketim_miktar, rec_depono, rec_satirno,rec_SpecRECno,rec_iptal,rec_fileid,rec_hidden,rec_kilitli,rec_degisti,rec_checkcum,rec_special,rec_special2,
rec_special3,rec_anatipi,rec_tanim_kod,rec_cinsi,rec_aciklama,rec_anabirim,rec_tuketim_tur,rec_tuketim_tanim_kodu,rec_tukrtim_recete_cinsi,rec_tuketim_birim,recuretim_tuketim,rec_satir_acik,
rec_fireyuzde,rec_baslama_tarihi,rec_bitis_tarihi,rec_alt_tukkod1,rec_alt_tukkod2,rec_alt_tukkod3,rec_alt_tukkod4,rec_alt_tukkod5,rec_safha_no,rec_safha_turu,rec_ana_renk_no,rec_ana_beden_no
      ,rec_tuketim_renk_no
      ,rec_tuketim_beden_no
      ,rec_PlanlamaTipi
      ,rec_eklenme_sarti
      ,rec_miktar_fonksiyon_adi
                      ) VALUES (
                        @urunKodu, @tarih, @anaMiktar, 
                        @stokKodu, @tuketimMiktar, @depoNo, @satirNo,0,0,18,0,0,0,0,,,,0,,0,,1,0,,0,1,0,,0,'1899-12-30 00:00:00.000','1899-12-30 00:00:00.000',,,,,,0,0,0,0,0,0,0,0,0
                      )";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@urunKodu", urunKodu);
                cmd.Parameters.AddWithValue("@tarih", tarih);
                cmd.Parameters.AddWithValue("@anaMiktar", anaMiktar);
                cmd.Parameters.AddWithValue("@stokKodu", stokKodu);
                cmd.Parameters.AddWithValue("@tuketimMiktar", tuketimMiktar);
                cmd.Parameters.AddWithValue("@depoNo", depoNo);
                cmd.Parameters.AddWithValue("@satirNo", satirNo);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün reçetesi eklenirken hata oluştu. UrunKodu: {UrunKodu}, StokKodu: {StokKodu}",
                                urunKodu, stokKodu);
                throw; // Hatayı yukarı fırlat
            }
        }
        // Bu metotları AktarimController sınıfına ekleyin

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ParametreGuncelle(string eskiParametreAdi, string yeniParametreAdi, List<string> modalSecilenStoklar)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eskiParametreAdi) || string.IsNullOrWhiteSpace(yeniParametreAdi))
                {
                    ViewBag.Mesaj = "Parametre adları boş olamaz.";
                    ViewBag.Parametreler = GetParametreler();
                    return View("ParametreAyarla", GetStokList());
                }

                if (modalSecilenStoklar == null || !modalSecilenStoklar.Any())
                {
                    ViewBag.Mesaj = "En az bir stok seçilmelidir.";
                    ViewBag.Parametreler = GetParametreler();
                    return View("ParametreAyarla", GetStokList());
                }

                var stoklarVirgullu = string.Join(",", modalSecilenStoklar);

                using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
                conn.Open();

                // Eğer parametre adı değiştiyse, aynı isimde başka parametre var mı kontrol et
                if (eskiParametreAdi != yeniParametreAdi)
                {
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM AktarimParametre WHERE ParametreAdi = @yeniAdi", conn);
                    checkCmd.Parameters.AddWithValue("@yeniAdi", yeniParametreAdi);
                    var existCount = (int)checkCmd.ExecuteScalar();

                    if (existCount > 0)
                    {
                        ViewBag.Mesaj = "Bu parametre adı zaten kullanılıyor. Lütfen farklı bir ad seçin.";
                        ViewBag.Parametreler = GetParametreler();
                        return View("ParametreAyarla", GetStokList());
                    }
                }

                // Parametreyi güncelle
                using var cmd = new SqlCommand("UPDATE AktarimParametre SET ParametreAdi = @yeniAdi, StokKodu = @kodlar WHERE ParametreAdi = @eskiAdi", conn);
                cmd.Parameters.AddWithValue("@eskiAdi", eskiParametreAdi);
                cmd.Parameters.AddWithValue("@yeniAdi", yeniParametreAdi);
                cmd.Parameters.AddWithValue("@kodlar", stoklarVirgullu);

                int affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows > 0)
                {
                    ViewBag.Mesaj = "Parametre başarıyla güncellendi.";
                }
                else
                {
                    ViewBag.Mesaj = "Parametre güncellenemedi. Parametre bulunamadı.";
                }

                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre güncellenirken hata oluştu. EskiParametreAdi: {EskiParametreAdi}, YeniParametreAdi: {YeniParametreAdi}",
                                eskiParametreAdi, yeniParametreAdi);
                ViewBag.ErrorMessage = "Parametre güncellenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
        }



        [AllowAnonymous]
        [HttpPost]
        public IActionResult ParametreKopyala(string yeniParametreAdi, string stokKodlari)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(yeniParametreAdi) || string.IsNullOrWhiteSpace(stokKodlari))
                {
                    ViewBag.Mesaj = "Parametre kopyalanırken hata oluştu: Gerekli bilgiler eksik.";
                    ViewBag.Parametreler = GetParametreler();
                    return View("ParametreAyarla", GetStokList());
                }

                using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
                conn.Open();

                // Aynı isimde parametre var mı kontrol et
                using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM AktarimParametre WHERE ParametreAdi = @adi", conn);
                checkCmd.Parameters.AddWithValue("@adi", yeniParametreAdi);
                var existCount = (int)checkCmd.ExecuteScalar();

                if (existCount > 0)
                {
                    ViewBag.Mesaj = "Bu parametre adı zaten kullanılıyor. Lütfen farklı bir ad seçin.";
                    ViewBag.Parametreler = GetParametreler();
                    return View("ParametreAyarla", GetStokList());
                }

                // Yeni parametreyi kaydet
                using var cmd = new SqlCommand("INSERT INTO AktarimParametre (ParametreAdi, StokKodu) VALUES (@adi, @kodlar)", conn);
                cmd.Parameters.AddWithValue("@adi", yeniParametreAdi);
                cmd.Parameters.AddWithValue("@kodlar", stokKodlari);
                cmd.ExecuteNonQuery();

                ViewBag.Mesaj = "Parametre başarıyla kopyalandı.";
                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parametre kopyalanırken hata oluştu. YeniParametreAdi: {YeniParametreAdi}", yeniParametreAdi);
                ViewBag.ErrorMessage = "Parametre kopyalanırken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                ViewBag.Parametreler = GetParametreler();
                return View("ParametreAyarla", GetStokList());
            }
        }
        // AktarimController.cs dosyasına bu metodu ekleyin

        [AllowAnonymous]
        [HttpPost]
        public IActionResult GetAjaxData(IFormFile excelFile, string selectedParametre)
        {
            try
            {
                var data = new List<StockMovement>();
                var sessionKey = "ExcelData_" + HttpContext.Session.Id;

                // Excel dosyası varsa veya session'da veri varsa veriyi al
                if (excelFile != null && excelFile.Length > 0)
                {
                    using var stream = new MemoryStream();
                    excelFile.CopyTo(stream);
                    stream.Position = 0;

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        return Json(new { success = false, message = "Excel dosyasında geçerli bir sayfa bulunamadı." });
                    }

                    string currentGroup = string.Empty;
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var kod = worksheet.Cells[row, 1].Text;
                        var aciklama = worksheet.Cells[row, 2].Text;

                        if (string.IsNullOrWhiteSpace(kod) && !string.IsNullOrWhiteSpace(aciklama))
                        {
                            currentGroup = aciklama.Trim();
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(kod))
                        {
                            data.Add(new StockMovement
                            {
                                Grup = currentGroup,
                                StokKodu = kod.Replace("#", "").Trim(),
                                Aciklama = aciklama,
                                Birim = worksheet.Cells[row, 3].Text,
                                Fiyat = worksheet.Cells[row, 4].Text,
                                Net = worksheet.Cells[row, 5].Text,
                                Brut = worksheet.Cells[row, 6].Text,
                                Agirlik = worksheet.Cells[row, 7].Text,
                                NetTutar = worksheet.Cells[row, 8].Text,
                                BrutTutar = worksheet.Cells[row, 9].Text,
                                Para = worksheet.Cells[row, 10].Text,
                                Kur = worksheet.Cells[row, 11].Text
                            });
                        }
                    }

                    // Verileri session'a kaydet
                    var dataBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data);
                    HttpContext.Session.Set(sessionKey, dataBytes);
                }
                else
                {
                    // Session'dan veriyi al
                    if (HttpContext.Session.TryGetValue(sessionKey, out var sessionData))
                    {
                        var jsonString = System.Text.Encoding.UTF8.GetString(sessionData);
                        data = System.Text.Json.JsonSerializer.Deserialize<List<StockMovement>>(jsonString) ?? new List<StockMovement>();
                    }
                    else
                    {
                        // Session'da veri yoksa ve Excel dosyası yüklenmemişse hata döndür
                        return Json(new { success = false, message = "Excel dosyası yüklenmedi. Lütfen bir dosya yükleyin." });
                    }
                }

                // Seçilen stok kodlarını al
                List<string> secilenStokKodlari = new List<string>();
                if (!string.IsNullOrEmpty(selectedParametre))
                {
                    _logger.LogInformation("Seçilen parametre: {SelectedParametre}", selectedParametre);
                    var parametreler = GetParametreler();
                    var secilenParametre = parametreler.FirstOrDefault(p => p.ParametreAdi == selectedParametre);
                    if (secilenParametre != null)
                    {
                        secilenStokKodlari = secilenParametre.StokKodu.Split(',').Select(s => s.Trim()).ToList();
                        _logger.LogInformation("Seçilen stok kodları: {StokKodlari}", string.Join(", ", secilenStokKodlari));
                    }
                    else
                    {
                        _logger.LogWarning("Parametre bulunamadı: {SelectedParametre}", selectedParametre);
                    }
                }
                else
                {
                    _logger.LogInformation("Parametre seçilmedi");
                }

                // HTML oluştur
                var html = GenerateTableHtml(data, secilenStokKodlari);

                return Json(new { success = true, html = html, secilenStokKodlari = secilenStokKodlari });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AJAX veri yüklenirken hata oluştu");
                return Json(new { success = false, message = "Veri yüklenirken bir hata oluştu: " + ex.Message });
            }
        }

        private string GenerateTableHtml(List<StockMovement> data, List<string> secilenStokKodlari)
        {
            var html = new StringBuilder();

            foreach (var group in data.GroupBy(x => x.Grup))
            {
                html.AppendLine("<div class='card'>");
                html.AppendLine($"<div class='card-header tablo'>{group.Key}</div>");
                html.AppendLine("<div class='grid-container'>");
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Aktarılsın</th><th>Stok Kodu</th><th>Açıklama</th><th>Birim</th>");
                html.AppendLine("<th>Fiyat</th><th>Net</th><th>Brüt</th><th>Ağırlık</th>");
                html.AppendLine("<th>Net Tutar</th><th>Brüt Tutar</th><th>Para</th><th>Kur</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var item in group)
                {
                    bool isChecked = true;
                    if (secilenStokKodlari != null && secilenStokKodlari.Contains(item.StokKodu))
                    {
                        isChecked = false;
                    }

                    html.AppendLine("<tr>");
                    html.AppendLine($"<td><input type='checkbox' class='stok-checkbox' data-stok-kodu='{item.StokKodu}' {(isChecked ? "checked" : "")} /></td>");
                    html.AppendLine($"<td>{item.StokKodu}</td>");
                    html.AppendLine($"<td>{item.Aciklama}</td>");
                    html.AppendLine($"<td>{item.Birim}</td>");
                    html.AppendLine($"<td>{item.Fiyat}</td>");
                    html.AppendLine($"<td>{item.Net}</td>");
                    html.AppendLine($"<td>{item.Brut}</td>");
                    html.AppendLine($"<td>{item.Agirlik}</td>");
                    html.AppendLine($"<td>{item.NetTutar}</td>");
                    html.AppendLine($"<td>{item.BrutTutar}</td>");
                    html.AppendLine($"<td>{item.Para}</td>");
                    html.AppendLine($"<td>{item.Kur}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }

            return html.ToString();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult TablodanAktar(string urunKodu, string girisDep, string cikisDep,
                             string miktar, string srmMerkezi, DateTime tarih,
                             List<string> stokKodlari, List<string> netMiktarlar)
        {
            try
            {
                // Gerekli alanları kontrol et
                if (string.IsNullOrEmpty(urunKodu))
                {
                    TempData["ErrorMessage"] = "Ürün kodu boş olamaz";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(cikisDep))
                {
                    TempData["ErrorMessage"] = "Hammadde Çıkış Depo seçilmelidir";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(miktar))
                {
                    TempData["ErrorMessage"] = "Miktar boş olamaz";
                    return RedirectToAction("Index");
                }

                if (stokKodlari == null || stokKodlari.Count == 0)
                {
                    TempData["ErrorMessage"] = "Aktarılacak stok bulunamadı";
                    return RedirectToAction("Index");
                }

                string userNo = HttpContext.Session.GetString("UserNo");
                if (string.IsNullOrEmpty(userNo))
                {
                    TempData["ErrorMessage"] = "Kullanıcı oturumu bulunamadı. Lütfen giriş yapın.";
                    return RedirectToAction("Index");
                }

                int sonSatirNo = 0;

                // Maksimum satır numarasını bul ve bir sonraki kayıt için uygun numarayı hazırla
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DynamicDatabase")))
                {
                    conn.Open();
                    using var cmd = new SqlCommand("SELECT ISNULL(MAX(rec_satirno), -1) FROM URUN_RECETELERI WHERE rec_anakod = @urunKodu", conn);
                    cmd.Parameters.AddWithValue("@urunKodu", urunKodu);
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        sonSatirNo = Convert.ToInt32(result) + 1;
                    }
                }

                // Verileri kaydet
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DynamicDatabase")))
                {
                    conn.Open();
                    using var transaction = conn.BeginTransaction();

                    try
                    {
                        for (int i = 0; i < stokKodlari.Count; i++)
                        {
                            string sql = @"INSERT INTO URUN_RECETELERI (
                        rec_anakod, rec_tarih, rec_anamiktar, 
                        rec_tuketim_kod, rec_tuketim_miktar, rec_depono, rec_satirno, rec_SpecRECno, rec_iptal, rec_fileid, rec_hidden, rec_kilitli, 
                        rec_degisti, rec_checksum, rec_special1, rec_special2, rec_special3, rec_anatipi, rec_tanimkod, rec_cinsi, rec_aciklama, 
                        rec_anabirim, rec_tuketim_tur, rec_tuketim_tanim_kodu, rec_tuketim_recete_cinsi, rec_tuketim_birim, rec_uretim_tuketim, 
                        rec_satir_acik, rec_fireyuzde, rec_baslama_tarihi, rec_bitis_tarihi, rec_alt_tukkod1, rec_alt_1_katsayi, rec_alt_tukkod2, 
                        rec_alt_2_katsayi, rec_alt_tukkod3, rec_alt_3_katsayi, rec_alt_tukkod4, rec_alt_4_katsayi, rec_alt_tukkod5, rec_alt_5_katsayi,
                        rec_safha_no, rec_safha_turu, rec_ana_renk_no, rec_ana_beden_no, rec_tuketim_renk_no, rec_tuketim_beden_no, 
                        rec_PlanlamaTipi, rec_eklenme_sarti, rec_miktar_fonksiyon_adi, rec_create_user, rec_lastup_user
                    ) VALUES (
                        @urunKodu, @tarih, @anaMiktar, 
                        @stokKodu, @tuketimMiktar, @depoNo, @satirNo, 0, 0, 18, 0, 0, 
                        0, 0, '', '', '', 0, '', 0, '', 
                        1, 0, '', 0, 1, 0, 
                        '', 0, '1899-12-30 00:00:00.000', '1899-12-30 00:00:00.000', '', '', '', 
                        '', '', '', '', '', '', '',
                        0, 0, 0, 0, 0, 0, 
                        0, 0, '', @userNo, @userNo
                    )";

                            using var cmd = new SqlCommand(sql, conn, transaction);
                            cmd.Parameters.AddWithValue("@urunKodu", urunKodu);
                            cmd.Parameters.AddWithValue("@tarih", tarih);

                            if (!decimal.TryParse(miktar.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal anaMiktar))
                                anaMiktar = 0;
                            cmd.Parameters.AddWithValue("@anaMiktar", anaMiktar);

                            cmd.Parameters.AddWithValue("@stokKodu", stokKodlari[i]);

                            if (!decimal.TryParse(netMiktarlar[i].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tuketimMiktar))
                                tuketimMiktar = 0;
                            cmd.Parameters.AddWithValue("@tuketimMiktar", tuketimMiktar);

                            if (!int.TryParse(cikisDep, out int depoNo))
                                depoNo = 0;
                            cmd.Parameters.AddWithValue("@depoNo", depoNo);

                            cmd.Parameters.AddWithValue("@satirNo", sonSatirNo++);
                            cmd.Parameters.AddWithValue("@userNo", userNo);

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Veriler başarıyla aktarıldı.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Veri aktarımı sırasında hata oluştu: {ExMessage}", ex.Message);
                        TempData["ErrorMessage"] = "Veri aktarımı sırasında hata oluştu: " + ex.Message;
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veri aktarımı sırasında beklenmeyen bir hata oluştu: {ExMessage}", ex.Message);
                TempData["ErrorMessage"] = "Veri aktarımı sırasında beklenmeyen bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }

        public class StokModel
    {
        public string StokKodu { get; set; }
        public string StokAdi { get; set; }
    }

    public class ParametreModel
    {
        public string ParametreAdi { get; set; }
        public string StokKodu { get; set; }
    }
}