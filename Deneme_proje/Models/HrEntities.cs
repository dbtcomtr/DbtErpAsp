namespace Deneme_proje.Models
{
    public class HrEntities
    {
        public class LeaveEntitlementReportModel
        {
            public string PersonelKodu { get; set; }
            public string PersonelAdi { get; set; }
            public string PersonelSoyadi { get; set; }
            public DateTime IseGirisTarihi { get; set; }
            public int CalismaSuresiYil { get; set; }
            public int YillikIzinHakki { get; set; }
            public decimal GecenYilDevirGun { get; set; }
            public decimal GecenYilDevirSaat { get; set; }
            public decimal KullanilanIzinGun { get; set; }
            public decimal KalanIzinBakiyesi { get; set; }

            // Sayfada görünen ama sorgudan gelmeyen alanlar
            public string Departman { get; set; }
            public string Gorev { get; set; }
        }
        public class IzinTalepModel
        {
            public Guid Guid { get; set; }
            public string PersonelKodu { get; set; }
            public string PersonelAdSoyad { get; set; }
            public string IdariAmirKodu { get; set; }
            public string OnaylayanAdSoyad { get; set; }
            public string IdariAmirAdi { get; set; }
            public DateTime TalepTarihi { get; set; }
            public DateTime BitisTarihi { get; set; }
            public byte IzinTipi { get; set; }
            public byte GunSayisi { get; set; }
            public DateTime BaslangicTarihi { get; set; }
            public double BaslamaSaati { get; set; }
            public float BitisSaati { get; set; }
            public string Amac { get; set; }
            public byte IzinDurumu { get; set; }
            public DateTime OlusturmaTarihi { get; set; }
            public float IzinSaat { get; set; }
            public DateTime IseBaslamaTarihi { get; set; }
            public string OnaylayanKullanici { get; set; }
            public string OnaylayanKullaniciAdi { get; set; }
            public string ReddetmeNedeni { get; set; }  // Corresponds to pit_aciklama1
            public decimal KalanIzinHakki { get; set; }

            public string IzinTipiAdi =>
                IzinTipi switch
                {
                    0 => "Yıllık İzin",
                    3 => "Askerlik",
                    5 => "Devamsızlık",
                    7 => "Diğer",
                    8 => "Ücretsiz",
                    10 => "Evlilik",
                    11 => "Doğum",
                    12 => "Babalık",
                    13 => "Süt",
                    14 => "Ölüm",
                    15 => "İş Arama",
                    _ => "Diğer"
                };

            public string IzinDurumuAdi =>
                IzinDurumu switch
                {
                    0 => "Beklemede",
                    1 => "Onaylandı",
                    2 => "Reddedildi",
                    _ => "Bilinmeyen"
                };
        }
    }
}