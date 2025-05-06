using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Repository;
using Deneme_proje.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using NuGet.Protocol.Core.Types;
using static Deneme_proje.Models.DenizlerEntities;
using DocumentFormat.OpenXml.InkML;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class DenizlerController : BaseController
    {
        private readonly DenizlerRepository _denizlerRepository;
        private readonly ILogger<DenizlerController> _logger;

        public DenizlerController(DenizlerRepository denizlerRepository, ILogger<DenizlerController> logger)
        {
            _denizlerRepository = denizlerRepository ?? throw new ArgumentNullException(nameof(denizlerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


		public ActionResult AracMasraf(DateTime? baslamaTarihi, DateTime? bitisTarihi, string projeKodu, string srmKodu)
		{
			// Varsayılan tarihler
			baslamaTarihi ??= DateTime.Now.AddMonths(-1);
			bitisTarihi ??= DateTime.Now;

			// Verilerin getirilmesi
			var denizler1Data = _denizlerRepository.GetRaportDenizler1(baslamaTarihi.Value, bitisTarihi.Value, srmKodu, projeKodu).ToList();
			var denizler2Data = _denizlerRepository.GetRaportDenizler2(baslamaTarihi.Value, bitisTarihi.Value, srmKodu, projeKodu).ToList();
			var denizler3Data = _denizlerRepository.GetRaportDenizler3(baslamaTarihi.Value, bitisTarihi.Value, srmKodu, projeKodu).ToList();
			var denizler4Data = _denizlerRepository.GetRaportDenizler4(srmKodu, projeKodu).ToList();

			// Gelir hesaplama
			double gelir = (double)denizler1Data.Sum(x => x.NakliyeCiroBedeli);

			// Gider hesaplama
			double gider = (double)denizler2Data.Sum(x => x.Tutar) + (double)denizler3Data.Sum(x => x.Tutar);

			// Sorumluluk kodu varsa som_guid'i al
			var somGuid = !string.IsNullOrEmpty(srmKodu) ? _denizlerRepository.GetSomGuidBySrmKodu(srmKodu) : null;

			// som_guid ile SORUMLULUK_MERKEZLERI_USER tablosundaki verileri getir, somGuid yoksa boş bir liste döner
			var sorumlulukMerkeziUserData = somGuid != null
				? _denizlerRepository.GetSorumlulukMerkeziUserBySomGuid(somGuid.Value).ToList()
				: new List<SorumlulukMerkezleriUser>();

			// ViewModel'in oluşturulması
			var model = new RaportViewModel
			{
				BaslamaTarihi = baslamaTarihi.Value,
				BitisTarihi = bitisTarihi.Value,
				Denizler1Data = denizler1Data,
				Denizler2Data = denizler2Data,
				Denizler3Data = denizler3Data,
				Denizler4Data = denizler4Data,
				GelirToplami = gelir,
				GiderToplami = gider,
				Sonuc = gelir - gider,

				// Sorumluluk merkezi verilerinin eklenmesi
				SorumlulukUserData = sorumlulukMerkeziUserData,

				ProjeKodlari = _denizlerRepository.GetProjeKodlari(baslamaTarihi.Value, bitisTarihi.Value)
					.Select(pk => new SelectListItem { Value = pk.ProjeKodu, Text = $"{pk.ProjeKodu} - {pk.ProjeAdi}" }).ToList(),

				SorumluKodlari = _denizlerRepository.GetSorumluKodlari(baslamaTarihi.Value, bitisTarihi.Value)
					.Select(sk => new SelectListItem { Value = sk.SorumluKodu, Text = $"{sk.SorumluKodu} - {sk.SorumluAdi}" }).ToList()
			};

			// Modelin View'e gönderilmesi
			return View(model);
		}





		public IActionResult GelirGider(DateTime? baslangicTarihi, DateTime? bitisTarihi, string projeKodu, string srmKodu)
        {
            baslangicTarihi ??= DateTime.Today.AddMonths(-1);  // Default to 1 month ago
            bitisTarihi ??= DateTime.Today;

            var faturaAlis = _denizlerRepository.GetFaturaAlis(baslangicTarihi.Value, bitisTarihi.Value, projeKodu, srmKodu);
            var faturaSatis = _denizlerRepository.GetFaturaSatis(baslangicTarihi.Value, bitisTarihi.Value, projeKodu, srmKodu);

            var projeKodlari = _denizlerRepository.GetProjeKodlari(baslangicTarihi.Value, bitisTarihi.Value);
            var sorumluKodlari = _denizlerRepository.GetSorumluKodlari(baslangicTarihi.Value, bitisTarihi.Value);

            var viewModel = new GelirGiderViewModel
            {
                FaturaAlis = faturaAlis,
                FaturaSatis = faturaSatis,
                BaslangicTarihi = baslangicTarihi.Value,
                BitisTarihi = bitisTarihi.Value,
                ProjeKodu = projeKodu,
                SrmKodu = srmKodu,
                ProjeKodlari = projeKodlari.Select(pk => new SelectListItem
                {
                    Value = pk.ProjeKodu,
                    Text = $"{pk.ProjeKodu} - {pk.ProjeAdi}" // Kod ve adı birleştiriyoruz
                }).ToList(),
                SorumluKodlari = sorumluKodlari.Select(sk => new SelectListItem
                {
                    Value = sk.SorumluKodu,
                    Text = $"{sk.SorumluKodu} - {sk.SorumluAdi}" // Kod ve adı birleştiriyoruz
                }).ToList()
            };

            return View(viewModel);
        }

		public IActionResult FirmaCekleri(DateTime? baslamaTarihi, DateTime? bitisTarihi)
		{
			// Tarihler null ise bugünün tarihini atayın
			var currentBaslamaTarihi = baslamaTarihi ?? DateTime.Now.AddMonths(-1); // Son 1 ay varsayılan tarih
			var currentBitisTarihi = bitisTarihi ?? DateTime.Now;

			var cekler = _denizlerRepository.GetDenizlerFirmaCekleri(currentBaslamaTarihi, currentBitisTarihi);

			// View'e tarihleri gönder
			ViewBag.BaslamaTarihi = currentBaslamaTarihi.ToString("yyyy-MM-dd");
			ViewBag.BitisTarihi = currentBitisTarihi.ToString("yyyy-MM-dd");

			return View(cekler);
		}

		public IActionResult MusteriCekleri(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            // Tarihler null ise bugünün tarihini atayın
            var currentBaslamaTarihi = baslamaTarihi ?? DateTime.Now.AddMonths(-1); // Son 1 ay varsayılan tarih
            var currentBitisTarihi = bitisTarihi ?? DateTime.Now;

            var cekler = _denizlerRepository.GetMusteriCekleri(currentBaslamaTarihi, currentBitisTarihi);

            // View'e tarihleri gönder
            ViewBag.BaslamaTarihi = currentBaslamaTarihi.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = currentBitisTarihi.ToString("yyyy-MM-dd");

            return View(cekler);
        }


        public IActionResult AracKmYakitBilgileri(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var currentBaslamaTarihi = baslamaTarihi ?? DateTime.Now;
            var currentBitisTarihi = bitisTarihi ?? DateTime.Now;

            var aracBilgileri = _denizlerRepository.GetAracKmYakitBilgileri(currentBaslamaTarihi, currentBitisTarihi);

            ViewBag.BaslamaTarihi = currentBaslamaTarihi.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = currentBitisTarihi.ToString("yyyy-MM-dd");

            return View(aracBilgileri);
        }
        public IActionResult AliciSatici(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var currentBaslamaTarihi = baslamaTarihi ?? DateTime.Now;
            var currentBitisTarihi = bitisTarihi ?? DateTime.Now;

            var alicilar = _denizlerRepository.GetAlıcılar(currentBaslamaTarihi);
            var saticilar = _denizlerRepository.GetSatıcılar(currentBaslamaTarihi);

            var model = new DenizlerEntities.AliciSaticiViewModel
            {
                Alicilar = alicilar,
                Saticilar = saticilar
            };

            ViewBag.BaslamaTarihi = currentBaslamaTarihi.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = currentBitisTarihi.ToString("yyyy-MM-dd");

            return View(model);
        }

        public IActionResult KrediSozlesmeleri(DateTime? baslamaTarihi, DateTime? bitisTarihi, string durum)
        {
            if (baslamaTarihi == null || bitisTarihi == null)
            {
                ModelState.AddModelError("", "Lütfen geçerli bir tarih aralığı giriniz.");
                return View();
            }

            ViewBag.BaslamaTarihi = baslamaTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            ViewBag.Durum = durum;

            var result = _denizlerRepository.GetKrediSozlesmeleri(baslamaTarihi.Value, bitisTarihi.Value)
                        .Where(x => x.Durum == durum).ToList();

            return View(result);
        }
        public IActionResult CariAnaliz(string cariKod, string chaDCins)
        {
            if (string.IsNullOrEmpty(cariKod) || string.IsNullOrEmpty(chaDCins))
            {
                ModelState.AddModelError("", "Cari kodu ve döviz cinsini giriniz.");
                return View();
            }

            var cariAnaliz = _denizlerRepository.GetCariAnaliz(cariKod, chaDCins);

            ViewBag.CariKod = cariKod;
            ViewBag.ChaDCins = chaDCins;

            return View(cariAnaliz);
        }

      
    }
}


