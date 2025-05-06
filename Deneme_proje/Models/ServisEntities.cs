namespace Deneme_proje.Models
{
    public class ServisEntities
    {
        public class IsEmirleri
        {
            public DateTime Tarih { get; set; }
            public string MusteriAdi { get; set; }
            public string SoforAdi { get; set; }
            public string TeknisyenAdi { get; set; }
            public string IsTuru { get; set; }
            public string SoforTelefon { get; set; }
            public string Servis_Merkezi { get; set; }
            public int EvrakNo { get; set; }
            public int EvrakSiraNo { get; set; }
            public int No { get; set; }
            public string PlakaNo { get; set; }
            public string CihazMarkaModel { get; set; }
            public int CalismaSaati { get; set; }
            public DateTime ServiseGirisTarihi { get; set; }
            public string IlkGozlem { get; set; }
            public string YedekParcaNo { get; set; }
            public string YedekParcaAdi { get; set; }
            public int Adet { get; set; }
            public decimal BirimFiyat { get; set; }
            public decimal Tutar { get; set; }
            public decimal Vergi { get; set; }
            public decimal AToplam { get; set; }
            public decimal GToplam { get; set; }
            public int Durum { get; set; } // Yeni Durum Kolonu
            public decimal? IscilikTutari { get; set; }
            public decimal? HariciIscilikTutari { get; set; }

            // Yeni eklenen alanlar
            public string UniteSeriNo { get; set; }
            public decimal IskontoOrani { get; set; }
            public decimal? IskontoYuzde { get; set; }
            public decimal IskontoTutar { get; set; }
        }

        public class StokHizmet
        {
            public int EvrakSiraNo { get; set; }
            public string YedekParcaNo { get; set; }
            public string YedekParcaAdi { get; set; }
            public int Adet { get; set; }
            public decimal BirimFiyat { get; set; }
            public decimal Tutar { get; set; }
            public decimal? IscilikTutari { get; set; }
            public decimal? HariciIscilikTutari { get; set; }
            public decimal? IskontoYuzde { get; set; }
        }

        public class EvrakUpdateDTO
        {
            public IsEmirleri model { get; set; }
            public List<StokHizmet> stokHizmet { get; set; }
        }
    }
}