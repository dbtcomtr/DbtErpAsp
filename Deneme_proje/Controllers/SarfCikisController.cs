using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Repository;
using Deneme_proje.Models;
using System;
using System.Linq;

namespace Deneme_proje.Controllers
{
    [AllowAnonymous]
    public class SarfCikisController : Controller
    {
        private readonly SarfCikisRepository _repository;
        private readonly DiokiRepository _diokiRepository;

        public SarfCikisController(SarfCikisRepository repository, DiokiRepository diokiRepository)
        {
            _repository = repository;
            _diokiRepository = diokiRepository;
        }

        #region Talep Oluşturma

        public IActionResult Talep()
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");

                var model = new SarfCikisViewModel
                {
                    SarfCikis = new SarfCikisDepartmanBazli
                    {
                        sth_evrakno_seri = "SÇ",
                        sth_evrakno_sira = _repository.GetSonEvrakSiraNo("SÇ"),
                        talep_tarihi = DateTime.Now
                    },
                    Depolar = _repository.GetDepolar().ToList(),
                    AnaMasrafGruplari = _repository.GetAnaMasrafMerkezleri().ToList(),
                    SorumlulukMerkezleri = _repository.GetSorumlulukMerkezleri().ToList(),   // ← yeni
                    Partilotlar = _repository.GetPartilotlar().ToList()
                };

                ViewBag.UserName = userName;
                ViewBag.UserNo = userNo;
                ViewBag.StokAnaGruplari = _repository.GetStokAnaGruplari();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sayfa yüklenirken hata: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpGet]
        public JsonResult GetAltMasrafMerkezleri(string anaPrefix)
        {
            if (string.IsNullOrWhiteSpace(anaPrefix))
                return Json(new { success = false, data = Array.Empty<object>() });

            var altlar = _repository.GetAltMasrafMerkezleri(anaPrefix).ToList();
            return Json(new { success = true, data = altlar });
        }

        [HttpGet]
        public JsonResult StokAra(string arama)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(arama) || arama.Length < 2)
                    return Json(new { success = false, message = "En az 2 karakter giriniz" });

                arama = arama.ToLower().Trim();

                var stoklar = _repository.GetStoklar()
                    .Where(s =>
                        (s.sto_kod?.ToLower().Contains(arama) ?? false) ||
                        (s.sto_isim?.ToLower().Contains(arama) ?? false))
                    .Take(50)
                    .ToList();

                var result = stoklar.Select(s => new
                {
                    sto_kod = s.sto_kod ?? "",
                    sto_isim = s.sto_isim ?? "",
                    sto_birim1_ad = s.sto_birim1_ad ?? "",
                    sto_birim2_ad = s.sto_birim2_ad ?? "",
                    sto_birim3_ad = s.sto_birim3_ad ?? "",
                    sto_birim4_ad = s.sto_birim4_ad ?? ""
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult TalepKaydet([FromBody] SarfCikisKaydetModel model)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");

                if (string.IsNullOrEmpty(userNo))
                    return Json(new { success = false, message = "Oturum bulunamadı" });

                if (model?.SarfCikis == null || model.Stoklar == null || !model.Stoklar.Any())
                    return Json(new { success = false, message = "Geçersiz veri" });

                // Masraf merkezi kontrolü (artık alt seviyeden geliyor)
                if (string.IsNullOrWhiteSpace(model.SarfCikis.masraf_merkezi_kodu))
                    return Json(new { success = false, message = "Masraf merkezi seçilmedi" });

                // Stok miktarı kontrolü
                foreach (var stok in model.Stoklar)
                {
                    var mevcutMiktar = _repository.GetDepoStokMiktar(
                        stok.sth_stok_kod,
                        model.SarfCikis.sth_cikis_depo_no ?? 0);

                    if (mevcutMiktar < (decimal)stok.sth_miktar)
                        return Json(new { success = false, message = $"Yetersiz stok! {stok.sth_stok_kod}" });
                }

                var id = _repository.SarfCikisKaydet(model, userNo, userName);
                return Json(new { success = true, message = "Talep kaydedildi", id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSonEvrakNo(string seriNo)
        {
            try
            {
                var siraNo = _repository.GetSonEvrakSiraNo(seriNo ?? "SÇ");
                return Json(new { success = true, siraNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Tamamlama İşlemleri

        public IActionResult Tamamla()
        {
            var talepler = _repository.GetTamamlanabilirSarfCikislar();
            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(talepler);
        }

        [HttpPost]
        public JsonResult TamamlaIslem(int id)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");

                if (string.IsNullOrEmpty(userNo))
                    return Json(new { success = false, message = "Oturum bulunamadı" });

                var success = _repository.SarfCikisTamamla(id, userNo, userName);
                return Json(new { success, message = success ? "Tamamlandı" : "Hata oluştu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Yardımcı API'ler

        [HttpGet]
        public JsonResult GetStokMiktar(string stokKod, int depoNo)
        {
            try
            {
                var miktar = _repository.GetDepoStokMiktar(stokKod, depoNo);
                return Json(new { success = true, miktar, yeterli = miktar > 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetBarkodBilgisi(string barkod)
        {
            try
            {
                var barkodTanim = _diokiRepository.GetBarkodBilgileri(barkod);
                if (barkodTanim == null)
                    return Json(new { success = false, message = "Barkod bulunamadı" });

                var stok = _repository.GetStoklar().FirstOrDefault(s => s.sto_kod == barkodTanim.bar_stokkodu);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        stokKod = barkodTanim.bar_stokkodu,
                        stokAdi = stok?.sto_isim ?? "",
                        partiKodu = barkodTanim.bar_partikodu,
                        lotNo = barkodTanim.bar_lotno
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSarfCikisDetay(int id)
        {
            try
            {
                var sarfCikis = _repository.GetTumSarfCikislar().FirstOrDefault(s => s.Id == id);
                if (sarfCikis == null)
                    return Json(new { success = false, message = "Kayıt bulunamadı" });

                var stoklar = _repository.GetSarfCikisStoklar(
                    sarfCikis.sth_evrakno_seri,
                    sarfCikis.sth_evrakno_sira ?? 0
                ).ToList();

                return Json(new
                {
                    success = true,
                    sarfCikis = sarfCikis,
                    stoklar = stoklar
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Eski Onay Sayfaları → Yönlendirme

        public IActionResult Onay()
        {
            // Onay mekanizması kaldırıldığı için Tamamla sayfasına yönlendir
            return RedirectToAction(nameof(Tamamla));
        }

        public IActionResult TalepListesi()
        {
            // İstersen burada da Tamamla'ya yönlendirebilirsin veya tüm talepleri gösterebilirsin
            // Şimdilik tüm talepleri gösteriyorum (eski davranış)
            var talepler = _repository.GetTumSarfCikislar();
            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(talepler);
        }

        #endregion
    }
}