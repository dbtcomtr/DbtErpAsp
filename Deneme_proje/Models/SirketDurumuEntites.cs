namespace Deneme_proje.Models
{
    public class SirketDurumuEntites
    {
        public class BankaLimitViewModel
        {
            public string BankaAdi { get; set; }
            public string BankaKodu { get; set; } // Eklenen BankaKodu alanı
            public decimal KrediKartiLimiti { get; set; }
            public decimal CekTeminatLimiti { get; set; }
            public decimal IpotekTeminatLimiti { get; set; }
            public decimal DBSLimiti { get; set; }
            public decimal GayriNakdiLimiti { get; set; }
            public decimal KefaletKarsiligiLimit { get; set; }
            public decimal KMHLimiti { get; set; }
            public decimal BCHLimiti { get; set; }

            // Yeni eklenen alanlar - Ödeme Emirleri Tablosundan gelecek
            public decimal TeminatMektubuToplam { get; set; } // sck_tip = 11 (Firma Teminat Mektubu)
            public decimal KendiKrediKartiToplam { get; set; } // sck_tip = 9 (Kendi Kredi Kartımız)
            public decimal KrediSozlesmesiTutari { get; set; }
            public decimal KendiCekToplam { get; set; } // sck_tip = 2 (Kendi Çekimiz)
        }
        public class BakiyeModel
        {
            public string Kod { get; set; }
            public string Isim { get; set; }
            public string DovizCinsi { get; set; }
            public int DovizKodu { get; set; }  // Döviz kodu için yeni property
            public decimal Bakiye { get; set; }
            public string HesapTipi { get; set; } // "Banka" veya "Kasa"
        }
        public class KasaRaporuOdemeModel
        {
            public DateTime VadeTarihi { get; set; }
            public string EvrakTipi { get; set; }
            public string Açıklama { get; set; }
            public string Banka { get; set; }
            public decimal Tutar { get; set; }
            public string PB { get; set; }
        }
        public class CekAnaliziViewModel
        {
            public IEnumerable<CekAnalizi> CekAnaliziList { get; set; }
            public IEnumerable<CekAnalizi> MusteriCekleriList { get; set; }
            public IEnumerable<string> SckSonpozList { get; set; } // Yeni eklenen özellik
        }

        public class CekAnalizi
        {
            public string SonPozisyon { get; set; }
            public string ÇekNo { get; set; }
            public string ProjeKodu { get; set; }
            public string SrmMerkeziKodu { get; set; }
            public DateTime VadeTarihi { get; set; }
            public DateTime İlkHareketTarihi { get; set; }
            public DateTime SonHareketTarihi { get; set; }
            public DateTime DüzenlemeTarihi { get; set; }
            public string Sahibi { get; set; }
            public string Borçlu { get; set; }
            public string Nerede { get; set; }
            public decimal Tutar { get; set; }
            public decimal ÖdenenTutar { get; set; }
            public decimal KalanTutar { get; set; }
            public string DövizCinsi { get; set; }
        }

        public class FirmaCekleri
        {
            public string SonPozisyon { get; set; }
            public string ÇekNo { get; set; }
            public string ProjeKodu { get; set; }
            public string SrmMerkeziKodu { get; set; }
            public DateTime VadeTarihi { get; set; }
            public DateTime İlkHareketTarihi { get; set; }
            public DateTime SonHareketTarihi { get; set; }
            public DateTime DüzenlemeTarihi { get; set; }
            public string Sahibi { get; set; }
            public string Borçlu { get; set; }
            public string Nerede { get; set; }
            public decimal Tutar { get; set; }
            public decimal ÖdenenTutar { get; set; }
            public decimal KalanTutar { get; set; }
            public string DövizCinsi { get; set; }
        }

        public class MusteriCekleri
        {
            public string SonPozisyon { get; set; }
            public string ÇekNo { get; set; }
            public string ProjeKodu { get; set; }
            public string SrmMerkeziKodu { get; set; }
            public DateTime VadeTarihi { get; set; }
            public DateTime İlkHareketTarihi { get; set; }
            public DateTime SonHareketTarihi { get; set; }
            public DateTime DüzenlemeTarihi { get; set; }
            public string Sahibi { get; set; }
            public string Borçlu { get; set; }
            public string Nerede { get; set; }
            public decimal Tutar { get; set; }
            public decimal ÖdenenTutar { get; set; }
            public decimal KalanTutar { get; set; }
            public string DövizCinsi { get; set; }
        }
        public class BankModel
        {
            public string Kodu { get; set; }
            public string Adı { get; set; }
            public string Şube { get; set; }
            public decimal KMHBakiye { get; set; }
            public decimal Bakiye { get; set; }
            public decimal KullanılabilirBakiye { get; set; }
            public decimal UPBBakiye { get; set; }
            public string UPB { get; set; }
            public string PB { get; set; }

            // Tarih aralığı burada kullanılmayacaksa kaldırılabilir
            // public DateTime? BaslamaTarihi { get; set; }
            // public DateTime? BitisTarihi { get; set; }
        }

        public class BankDetailModel
        {
            public DateTime Tarih { get; set; }
            public string EvrakTipi { get; set; }
            public string ProjeKodu { get; set; }
            public string SrmMerkeziKodu { get; set; }
            public string CariCinsi { get; set; }
            public string CariKodu { get; set; }
            public string CariAdi { get; set; }
            public string KarsiHesapCinsi { get; set; }
            public string KarsiHesapKodu { get; set; }
            public string KarsiHesapIsmi { get; set; }
            public string Cinsi { get; set; }
            public string Aciklama { get; set; }
            public DateTime VadeGun { get; set; }
            public string BorcAlacak { get; set; }
            public decimal Tutar { get; set; }
            public decimal GelenTutar { get; set; }
            public decimal GidenTutar { get; set; }
            public string PB { get; set; }
            public decimal UPBTutar { get; set; }
            public string UPB { get; set; }
        }

        public class BankDetailsViewModel
        {
            public IEnumerable<BankModel> Banks { get; set; }
            public BankModel SelectedBank { get; set; }
            public List<BankDetailModel> Transactions { get; set; }
            public DateTime? BaslamaTarihi { get; set; }
            public DateTime? BitisTarihi { get; set; }
        }
        public class CariHareketFoyu
        {
            // PropertyName attribute'ları ile SQL sütun adlarını eşleştirebiliriz
            public Guid Cari_GUID { get; set; }
            public string Kodu { get; set; }
            public string Adı { get; set; }
            public string FirmaUnvanı { get; set; }
            public DateTime Tarih { get; set; }
            public string Seri { get; set; }
            public string Sıra { get; set; }
            public DateTime? BelgeTarihi { get; set; }
            public string BelgeNo { get; set; }
            public string EvrakTipi { get; set; }
            public string Cinsi { get; set; }
            public string HareketCinsi { get; set; }
            public string Grubu { get; set; }
            public string SrmMrkKodu { get; set; }
            public string SrmMrkAdı { get; set; }
            public decimal AnaDövizBorç { get; set; }
            public decimal AnaDövizAlacak { get; set; }
            public decimal AnaDövizTutar { get; set; }
            public decimal AnaDövizBorçBakiye { get; set; }
            public decimal AnaDövizAlacakBakiye { get; set; }
            public decimal  AnaDövizBakiye  { get; set; }
            public string Açıklama { get; set; }
            public string SorumluKodu { get; set; }
            public string Sorumluİsmi { get; set; }
            public DateTime? VadeTarihi { get; set; }
            public int VadeGün { get; set; }
            public decimal AltDövizBorç { get; set; }
            public decimal AltDövizAlacak { get; set; }
            public decimal AltDövizTutar { get; set; }
            public decimal AltDövizBorçBakiye { get; set; }
            public decimal AltDövizAlacakBakiye { get; set; }
            public decimal AltDövizBakiye { get; set; }
            public string OrjDöviz { get; set; }
            public decimal OrjDövizBorç { get; set; }
            public decimal OrjDövizAlacak { get; set; }
            public decimal OrjDövizTutar { get; set; }
            public decimal OrjDövizBorçBakiye { get; set; }
            public decimal OrjDövizAlacakBakiye { get; set; }
            public decimal OrjDövizBakiye { get; set; }
            public string GibFaturaNo { get; set; }
            public string KarşıHesapCinsi { get; set; }
            public string KarşıHesapKodu { get; set; }
            public string KarşıHesapİsmi { get; set; }
            public string ProjeKodu { get; set; }
            public string ProjeAdı { get; set; }
            public string HareketGrupKodu1 { get; set; }
            public string HareketGrupAdı1 { get; set; }
            public string HareketGrupKodu2 { get; set; }
            public string HareketGrupAdı2 { get; set; }
            public string HareketGrupKodu3 { get; set; }
            public string HareketGrupAdı3 { get; set; }
            public int SatırNo { get; set; }
        }
        public class CariHesap
        {
            public string CariKod { get; set; }
            public string CariUnvan1 { get; set; }
        }

        public class StokHareketFoyu
        {
            public string StokKodu { get; set; }
            public string StokAdi { get; set; }
            public DateTime Tarih { get; set; }
            public string Seri { get; set; }
            public string Sira { get; set; }
            public string EvrakTipi { get; set; }
            public string HareketCinsi { get; set; }
            public string Tipi { get; set; }
            public string NormalIade { get; set; }
            public string BelgeNo { get; set; }
            public DateTime? BelgeTarihi { get; set; }
            public string Depo { get; set; }
            public string KarsiDepo { get; set; }
            public DateTime? TeslimTarihi { get; set; }
            public string Parti { get; set; }
            public string LotNo { get; set; }
            public string SrmMrkzKodu { get; set; }
            public string SrmMrkzAdi { get; set; }
            public string CariSrmMrkzKodu { get; set; }
            public string CariSrmMrkzAdi { get; set; }
            public string IsMerkezi { get; set; }
            public string ProjeKodu { get; set; }
            public string ProjeAdi { get; set; }
            public decimal GirenMiktar { get; set; }
            public decimal CikanMiktar { get; set; }
            public decimal Miktar { get; set; }
            public decimal KalanMiktar { get; set; }
            public string BirimAdi { get; set; }
        }

        public class Stok
        {
            public string StokKodu { get; set; }
            public string StokAdi { get; set; }
        }

        //public class Depo
        //{
        //    public string DepoNo { get; set; }
        //    public string DepoAdi { get; set; }
        //}




    }
}
