namespace Deneme_proje.Models
{
    public class AktarimEntities

    {
     
        public class StockMovement
        {
            public string Grup { get; set; }         // ✅ Yeni eklenen alan
            public string StokKodu { get; set; }
            public string Aciklama { get; set; }
            public string Birim { get; set; }
            public string Fiyat { get; set; }
            public string Net { get; set; }
            public string Brut { get; set; }
            public string Agirlik { get; set; }
            public string NetTutar { get; set; }
            public string BrutTutar { get; set; }
            public string Para { get; set; }
            public string Kur { get; set; }
        }

    }
}
