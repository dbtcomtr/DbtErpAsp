using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using static Deneme_proje.Models.AktarimEntities;

namespace Deneme_proje.Controllers
{
    public class AktarimController : Controller
    {
        private readonly IConfiguration _configuration;

        public AktarimController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index(string selectedParametre = null)
        {
            List<StockMovement> model = new List<StockMovement>();
            ViewBag.Parametreler = GetParametreler();
            ViewBag.SelectedParametre = selectedParametre;

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

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Index(IFormFile excelFile, string selectedParametre = null)
        {
            List<StockMovement> data = new List<StockMovement>();

            if (excelFile != null && excelFile.Length > 0)
            {
                using var stream = new MemoryStream();
                excelFile.CopyTo(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                string currentGroup = string.Empty;

                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    var kod = worksheet.Cells[row, 1].Text;
                    var aciklama = worksheet.Cells[row, 2].Text;

                    // Grup satırı: Stok kodu yok ama açıklama varsa
                    if (string.IsNullOrWhiteSpace(kod) && !string.IsNullOrWhiteSpace(aciklama))
                    {
                        currentGroup = aciklama.Trim();
                        continue;
                    }

                    // Ürün satırı
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

            // Seçilen parametrenin stok kodlarını al
            if (!string.IsNullOrEmpty(selectedParametre))
            {
                var parametreler = GetParametreler();
                var secilenParametre = parametreler.FirstOrDefault(p => p.ParametreAdi == selectedParametre);

                if (secilenParametre != null)
                {
                    ViewBag.SecilenStokKodlari = secilenParametre.StokKodu.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            return View(data);
        }
        [AllowAnonymous]
        public IActionResult ParametreAyarla()
        {
            var stoklar = GetStokList();
            ViewBag.Parametreler = GetParametreler();
            return View(stoklar);
        }
        [AllowAnonymous]

        [HttpPost]
        public IActionResult ParametreAyarla(string parametreAdi, List<string> secilenStoklar)
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
            var cmd = new SqlCommand("INSERT INTO AktarimParametre (ParametreAdi, StokKodu) VALUES (@adi, @kodlar)", conn);
            cmd.Parameters.AddWithValue("@adi", parametreAdi);
            cmd.Parameters.AddWithValue("@kodlar", stoklarVirgullu);
            cmd.ExecuteNonQuery();

            ViewBag.Mesaj = "Parametre kaydedildi.";
            ViewBag.Parametreler = GetParametreler();
            return View(GetStokList());
        }
        [AllowAnonymous]

        [HttpPost]
        public IActionResult ParametreSil(string parametreAdi)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("ERPDatabase"));
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM AktarimParametre WHERE ParametreAdi = @adi", conn);
            cmd.Parameters.AddWithValue("@adi", parametreAdi);
            cmd.ExecuteNonQuery();

            ViewBag.Mesaj = "Parametre silindi.";
            ViewBag.Parametreler = GetParametreler();
            return View("ParametreAyarla", GetStokList());
        }
        [AllowAnonymous]

        private List<StokModel> GetStokList()
        {
            var result = new List<StokModel>();
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
            return result;
        }
        [AllowAnonymous]

        private List<ParametreModel> GetParametreler()
        {
            var result = new List<ParametreModel>();
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
            return result;
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
