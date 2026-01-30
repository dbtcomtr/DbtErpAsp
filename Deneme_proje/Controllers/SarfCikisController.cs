using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        #region Talep İşlemleri

        [AllowAnonymous]
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
                    MasrafMerkezleri = _repository.GetMasrafMerkezleri().ToList(),
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
        [AllowAnonymous]
        public JsonResult StokAra(string arama)
        {
            try
            {
                Console.WriteLine($"[STOK ARA] Arama kelimesi: {arama}");

                if (string.IsNullOrWhiteSpace(arama) || arama.Length < 2)
                {
                    return Json(new { success = false, message = "En az 2 karakter giriniz" });
                }

                arama = arama.ToLower().Trim();

                var stoklar = _repository.GetStoklar()
                    .Where(s =>
                        (s.sto_kod != null && s.sto_kod.ToLower().Contains(arama)) ||
                        (s.sto_isim != null && s.sto_isim.ToLower().Contains(arama)))
                    .Take(50)
                    .ToList();

                Console.WriteLine($"[STOK ARA] {stoklar.Count} stok bulundu");

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
                Console.WriteLine($"[STOK ARA HATA] {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, AllowAnonymous]
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

                // Miktar kontrolü
                foreach (var stok in model.Stoklar)
                {
                    var mevcutMiktar = _repository.GetDepoStokMiktar(stok.sth_stok_kod, model.SarfCikis.sth_cikis_depo_no.Value);
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

        [AllowAnonymous]
        public IActionResult TalepListesi()
        {
            var talepler = _repository.GetTumSarfCikislar();
            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(talepler);
        }

        #endregion

        #region Onay İşlemleri

        public IActionResult Onay()
        {
            var bekleyenler = _repository.GetBekleyenSarfCikislar();
            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(bekleyenler);
        }

        [AllowAnonymous]
        public IActionResult OnayDetay(int id)
        {
            var sarfCikis = _repository.GetTumSarfCikislar().FirstOrDefault(s => s.Id == id);
            if (sarfCikis == null)
            {
                TempData["Error"] = "Kayıt bulunamadı";
                return RedirectToAction("Onay");
            }

            var model = new SarfCikisViewModel
            {
                SarfCikis = sarfCikis,
                StokDetaylari = _repository.GetSarfCikisStoklar(sarfCikis.sth_evrakno_seri, sarfCikis.sth_evrakno_sira ?? 0).ToList()
            };

            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(model);
        }

        [HttpPost, AllowAnonymous]
        public JsonResult Onayla(int id)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");
                var success = _repository.SarfCikisOnayla(id, userNo, userName);
                return Json(new { success, message = success ? "Onaylandı" : "Hata" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, AllowAnonymous]
        public JsonResult Reddet(int id, string kapatmaNedeni)
        {
            try
            {
                if (string.IsNullOrEmpty(kapatmaNedeni))
                    return Json(new { success = false, message = "Neden girmelisiniz" });

                var success = _repository.SarfCikisReddet(id, kapatmaNedeni);
                return Json(new { success, message = success ? "Reddedildi" : "Hata" });
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
            var onaylananlar = _repository.GetOnaylananSarfCikislar();
            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(onaylananlar);
        }

        [AllowAnonymous]
        public IActionResult TamamlaDetay(int id)
        {
            var sarfCikis = _repository.GetTumSarfCikislar().FirstOrDefault(s => s.Id == id);
            if (sarfCikis == null)
            {
                TempData["Error"] = "Kayıt bulunamadı";
                return RedirectToAction("Tamamla");
            }

            var model = new SarfCikisViewModel
            {
                SarfCikis = sarfCikis,
                StokDetaylari = _repository.GetSarfCikisStoklar(sarfCikis.sth_evrakno_seri, sarfCikis.sth_evrakno_sira ?? 0).ToList()
            };

            ViewBag.UserName = HttpContext.Session.GetString("Username");
            return View(model);
        }

        [HttpPost, AllowAnonymous]
        public JsonResult TamamlaIslem(int id)
        {
            try
            {
                var userNo = HttpContext.Session.GetString("UserNo");
                var userName = HttpContext.Session.GetString("Username");
                var success = _repository.SarfCikisTamamla(id, userNo, userName);
                return Json(new { success, message = success ? "Tamamlandı" : "Hata" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region API

        [HttpGet, AllowAnonymous]
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

        [HttpGet, AllowAnonymous]
        public JsonResult GetSonEvrakNo(string seriNo)
        {
            try
            {
                var siraNo = _repository.GetSonEvrakSiraNo(seriNo);
                return Json(new { success = true, siraNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet, AllowAnonymous]
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
        [HttpGet, AllowAnonymous]
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
    }
}