using Microsoft.AspNetCore.Mvc;

namespace Deneme_proje.Models
{
    public class CrmEntities : Controller
    {
        public class Firsat
        {
            public Guid Firsat_Guid { get; set; }
            public string Firsat_Adi { get; set; }
            public string Firma_Adi { get; set; }
            public string Email { get; set; }
            public string Telefon { get; set; }
            public decimal? Tutar { get; set; }
            public string Etiketler { get; set; }
            public string Atanan_Kisi { get; set; }
            public string Durum { get; set; }
            public string Kaynak { get; set; }
            public DateTime? Son_Iletisim_Tarihi { get; set; }
            public DateTime Olusturulma_Tarihi { get; set; }
            public string Adres { get; set; }
            public string Pozisyon { get; set; }
            public string Sehir { get; set; }
            public string Ilce { get; set; }
            public string Ulke { get; set; }
            public string Website { get; set; }
            public string Posta_Kodu { get; set; }
            public string Varsayilan_Dil { get; set; }
            public string Aciklama { get; set; }
        }
    }
}
