using Deneme_proje.Models;
using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class CariController : Controller
    {
        private readonly CariRepository _cariRepository;

        public CariController(CariRepository cariRepository)
        {
            _cariRepository = cariRepository;
        }

        public IActionResult Index(int upbPb, string sektorKodu, string bolgeKodu, string grupKodu, string temsilciKodu)
        {
            var dataTable = _cariRepository.GetCarilerAlacak(upbPb, sektorKodu, bolgeKodu, grupKodu, temsilciKodu);
            var alacakModelList = new List<Entities.CariAlacakModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                alacakModelList.Add(new Entities.CariAlacakModel
                {
                    CariKodu = row["Cari Kodu"].ToString(),
                    CariUnvani = row["Cari Unvanı"].ToString(),
                    TL = Convert.ToDecimal(row["TL"]),
                    USD = Convert.ToDecimal(row["USD"]),
                    EUR = Convert.ToDecimal(row["EUR"]),
                    GBP = Convert.ToDecimal(row["GBP"]),
                    UPB = Convert.ToDecimal(row["UPB"]),
                    PB = row["PB"].ToString(),
                    TL_UPB = Convert.ToDecimal(row["TL_UPB"]),
                    USD_UPB = Convert.ToDecimal(row["USD_UPB"]),
                    EUR_UPB = Convert.ToDecimal(row["EUR_UPB"]),
                    GBP_UPB = Convert.ToDecimal(row["GBP_UPB"])
                });
            }

            var dataTable2 = _cariRepository.GetCarilerVerecek(upbPb, sektorKodu, bolgeKodu, grupKodu, temsilciKodu);
            var verecekModelList = new List<Entities.CariVerecekModel>();

            foreach (DataRow row in dataTable2.Rows)
            {
                verecekModelList.Add(new Entities.CariVerecekModel
                {
                    CariKodu = row["Cari Kodu"].ToString(),
                    CariUnvani = row["Cari Unvanı"].ToString(),
                    TL = Convert.ToDecimal(row["TL"]),
                    USD = Convert.ToDecimal(row["USD"]),
                    EUR = Convert.ToDecimal(row["EUR"]),
                    GBP = Convert.ToDecimal(row["GBP"]),
                    UPB = Convert.ToDecimal(row["UPB"]),
                    PB = row["PB"].ToString(),
                    TL_UPB = Convert.ToDecimal(row["TL_UPB"]),
                    USD_UPB = Convert.ToDecimal(row["USD_UPB"]),
                    EUR_UPB = Convert.ToDecimal(row["EUR_UPB"]),
                    GBP_UPB = Convert.ToDecimal(row["GBP_UPB"])
                });
            }

            var model = new Entities.CariViewModel
            {
                CariAlacaklar = alacakModelList,
                CariVerecekler = verecekModelList,
                SektorKodu = sektorKodu,
                BolgeKodu = bolgeKodu,
                GrupKodu = grupKodu,
                TemsilciKodu = temsilciKodu,
                UPB_PB = upbPb,
                UPBOptions = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Value = "0", Text = "TL" },
            new SelectListItem { Value = "1", Text = "USD" },
            new SelectListItem { Value = "2", Text = "EUR" },
            new SelectListItem { Value = "12", Text = "GBP" }
        }, "Value", "Text", upbPb)
            };

            return View(model);
        }



    }

    // ViewModel sınıfı ekleniyor
}
