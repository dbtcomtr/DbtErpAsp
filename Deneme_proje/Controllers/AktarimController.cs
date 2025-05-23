using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data.SqlClient;
using static Deneme_proje.Models.AktarimEntities;
using System.Globalization;


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