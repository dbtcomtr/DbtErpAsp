using System.Text.Json.Serialization;

namespace Deneme_proje.Models
{
    public class AktarimEntities

    {
        public class AktarimModel
        {
            public string UrunKodu { get; set; }
            public string GirisDepo { get; set; }
            public string CikisDepo { get; set; }
            public string Miktar { get; set; }
            public string SrmMerkezi { get; set; }
            public string Tarih { get; set; }
            public string Data { get; set; }
            public bool ApplyFilter { get; set; }
        }

        public class ReceteKayitModel
        {
            public int satirNo { get; set; }
            public string rec_anakod { get; set; }
            public string rec_tarih { get; set; }
            public string rec_anamiktar { get; set; }
            public string rec_tuketim_kod { get; set; }
            public string rec_tuketim_miktar { get; set; }
            public string rec_depono { get; set; }
            public string grupAdi { get; set; }
            public string aciklama { get; set; }
            public string birim { get; set; }
            public string fiyat { get; set; }
        }
        public class SorumluKod
        {
            public string SorumluKodu { get; set; }
            public string SorumluAdi { get; set; }
        }
        public class ParametreModel
        {
            public string ParametreAdi { get; set; }
            public string StokKodu { get; set; }
        }
        public class StokModel
        {
            public string StokKodu { get; set; }
            public string StokAdi { get; set; }
        }

        public class TransferRequest
        {
            public string UrunKodu { get; set; }
            public DateTime Tarih { get; set; }
            public decimal Miktar { get; set; }

            [JsonPropertyName("ObjectCikisDepoNo")]
            public string CikisDepoNo { get; set; }

            [JsonPropertyName("StockMovements")]
            public List<StockMovement> StockMovements { get; set; }
        }

        public class StockMovement
        {
            public string Grup { get; set; }
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
            public bool IsSelected { get; set; } // Checkbox durumunu tutar
        }

    }
}
