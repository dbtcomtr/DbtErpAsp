using Microsoft.AspNetCore.Mvc;

namespace Deneme_proje.Controllers
{
    [AllowAnonymous]
    public class MenuController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Siparisler()
        {
            var siparisler = GetSampleSiparisler();
            return View(siparisler);
        }

        // Yeni Sipariş Formu
        public IActionResult YeniSiparis()
        {
            return View();
        }

        // Sipariş Kaydet
        [HttpPost]
        public IActionResult YeniSiparis(SiparisModel siparis)
        {
            // Burada sipariş kaydedilecek
            // Şimdilik başarılı mesajı gösterelim
            TempData["SuccessMessage"] = "Sipariş başarıyla kaydedildi!";
            return RedirectToAction("Siparisler");
        }

        // Örnek veri üretimi
        private List<SiparisModel> GetSampleSiparisler()
        {
            return new List<SiparisModel>
            {
                new SiparisModel
                {
                    Id = 1,
                    SiparisNo = "SP-2025-001",
                    Tarih = DateTime.Now.AddDays(-5),
                    MusteriAdi = "ABC Ticaret Ltd.",
                    Tutar = 15750.50m,
                    Durum = "Onaylandı",
                    Aciklama = "Acil sipariş"
                },
                new SiparisModel
                {
                    Id = 2,
                    SiparisNo = "SP-2025-002",
                    Tarih = DateTime.Now.AddDays(-3),
                    MusteriAdi = "XYZ İnşaat A.Ş.",
                    Tutar = 28900.75m,
                    Durum = "Beklemede",
                    Aciklama = "Proje malzemesi"
                },
                new SiparisModel
                {
                    Id = 3,
                    SiparisNo = "SP-2025-003",
                    Tarih = DateTime.Now.AddDays(-1),
                    MusteriAdi = "Mega Market",
                    Tutar = 8500.00m,
                    Durum = "Hazırlanıyor",
                    Aciklama = "Düzenli sipariş"
                },
                new SiparisModel
                {
                    Id = 4,
                    SiparisNo = "SP-2025-004",
                    Tarih = DateTime.Now,
                    MusteriAdi = "Tech Solutions Ltd.",
                    Tutar = 45200.25m,
                    Durum = "Yeni",
                    Aciklama = "Teknoloji ekipmanları"
                },
                new SiparisModel
                {
                    Id = 5,
                    SiparisNo = "SP-2025-005",
                    Tarih = DateTime.Now.AddDays(-7),
                    MusteriAdi = "Global Trade Inc.",
                    Tutar = 12800.00m,
                    Durum = "Tamamlandı",
                    Aciklama = "İhracat siparişi"
                }
            };
        }
    }

    // Sipariş Model Sınıfı
    public class SiparisModel
    {
        public int Id { get; set; }
        public string SiparisNo { get; set; }
        public DateTime Tarih { get; set; }
        public string MusteriAdi { get; set; }
        public decimal Tutar { get; set; }
        public string Durum { get; set; }
        public string Aciklama { get; set; }
        public string Yetkili { get; set; }
        public string TeslimTarihi { get; set; }
        public string Proje { get; set; }
        public string OdemeYontemi { get; set; }
        public int Vade { get; set; }
        public string Kampanya { get; set; }
    }
}

