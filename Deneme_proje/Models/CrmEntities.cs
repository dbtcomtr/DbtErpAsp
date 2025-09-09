using System.ComponentModel.DataAnnotations;

namespace Deneme_proje.Models
{
    public class CrmEntities
    {
        #region Teklif Modelleri

        public class TeklifListeModel
        {
            public Guid tkl_Guid { get; set; }
            public string TeklifNo { get; set; }
            public string Konu { get; set; }
            public string Kime { get; set; }
            public decimal? Toplam { get; set; }
            public DateTime Tarih { get; set; }
            public DateTime? GecerlilikTarihi { get; set; }
            public string Etiketler { get; set; }
            public DateTime OlusturmaTarihi { get; set; }
            public string Durum { get; set; }
            public string TeklifKonusu { get; set; }
            public string Urunler { get; set; } // New field for concatenated product names

            // Formatted properties
            public string ToplamFormatted => Toplam?.ToString("N2") + " TL";
            public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
            public string GecerlilikTarihiFormatted => GecerlilikTarihi?.ToString("dd.MM.yyyy");
        }

        public class YeniTeklifModel
        {
            public string CariKod { get; set; }
            public DateTime Tarih { get; set; }
            public DateTime BaslangicTarihi { get; set; } // tkl_baslangic_tarihi
            public int GecerlilikSuresi { get; set; } = 7; // Gün sayısı
            public string FormNo { get; set; }
            public string SorumluKod { get; set; }
            public string Yetkili { get; set; }
            public string Aciklama { get; set; }
            public string Durum { get; set; } = "Taslak"; // Varsayılan: 0 (Taslak)
            public string CreateUser { get; set; }
            public List<TeklifUrunModel> Urunler { get; set; } = new List<TeklifUrunModel>();
        }

        public class TeklifDetayModel
        {
            public int tkl_evrakno_sira { get; set; }
            public string tkl_cari_kod { get; set; }
            public string tkl_evrak_tarihi { get; set; }
            public string tkl_baslangic_tarihi { get; set; }
            public DateTime tkl_Gecerlilik_Sures { get; set; } // Bitiş tarihi olarak kullanılıyor
            public string tkl_belge_no { get; set; }
            public string tkl_Sorumlu_Kod { get; set; }
            public string tkl_Aciklama { get; set; }
            public string tkl_durumu { get; set; }
            public decimal? tkl_Alisfiyati { get; set; }
            public string CariAdi { get; set; }
            public string HazirlayanAdi { get; set; }
            public List<TeklifUrunModel> Urunler { get; set; } = new List<TeklifUrunModel>();
        }
        public class TeklifUrunModel
        {
            public string StokKod { get; set; }
            public string StokAdi { get; set; }
            public string Aciklama { get; set; }
            public decimal Miktar { get; set; }
            public decimal BirimFiyat { get; set; }
            public decimal IndirimliFiyat { get; set; }
            public decimal Toplam => Miktar * (IndirimliFiyat > 0 ? IndirimliFiyat : BirimFiyat);
            public byte[]? ImageData { get; set; } // Ürün fotoğrafı için yeni alan
        }
        public class TeklifGuncelleModel : YeniTeklifModel
        {
            public Guid TeklifGuid { get; set; }
        }

        public class TeklifIstatistikleri
        {
            public int ToplamTeklif { get; set; }
            public int AcikTeklifler { get; set; }
            public int KazanilanTeklifler { get; set; }
            public int KaybedilenTeklifler { get; set; }
            public int ErtelelenTeklifler { get; set; }
            public int IptalEdilenTeklifler { get; set; }
            public decimal? ToplamTutar { get; set; }
        }

        public class AylikTeklifGrafik
        {
            public int Yil { get; set; }
            public int Ay { get; set; }
            public int TeklifSayisi { get; set; }
            public decimal? ToplamTutar { get; set; }

            public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMMM yyyy");
        }

        #endregion

        #region Cari Hesap Modelleri

        public class CariHesapModel
        {
            public string CariKod { get; set; }
            public string CariAdi { get; set; }
            public string TemsilciKodu { get; set; }
            public string SektorKodu { get; set; }
            public string GrupKodu { get; set; }
            public string Email { get; set; }
            public string Telefon { get; set; }
        }

        public class CariHesapDetayModel
        {
            // CARI_HESAPLAR tablosundan temel alanlar
            public string cari_kod { get; set; }
            public string cari_unvan1 { get; set; }
            public string cari_unvan2 { get; set; }
            public string cari_temsilci_kodu { get; set; }
            public string cari_sektor_kodu { get; set; }
            public string cari_grup_kodu { get; set; }
            public string cari_EMail { get; set; }
            public string cari_CepTel { get; set; }
            public string cari_wwwadresi { get; set; }
            public string cari_vdaire_adi { get; set; }
            public string cari_vdaire_no { get; set; }
            public string cari_VergiKimlikNo { get; set; }

            // Join'den gelen alanlar
            public string TemsilciAdi { get; set; }
        }

        public class PersonelModel
        {
            public string PersonelKod { get; set; }
            public string PersonelAdi { get; set; }
        }

        #endregion

        #region Stok Modelleri

        public class StokModel
        {
            public string StokKod { get; set; }
            public string StokAdi { get; set; }
            public string KisaIsim { get; set; }
            public decimal SatisFiyat { get; set; }
            public string Birim1 { get; set; }
            public string AnaGrupKod { get; set; }
            public decimal Birim1Katsayi { get; set; }
            public string FiyatDoviz { get; set; }
            public byte[] ImageData { get; set; } // Fotoğraf verisi
            public string SatisFiyatFormatted => SatisFiyat.ToString("N2") + " TL";
        }

        public class StokDetayModel
        {
            public string StokKod { get; set; }
            public string StokAdi { get; set; }
            public string KisaIsim { get; set; }
            public decimal SatisFiyat { get; set; }
            public string Birim1 { get; set; }
            public string Birim2 { get; set; }
            public string Birim3 { get; set; }
            public decimal Birim1Katsayi { get; set; }
            public decimal Birim2Katsayi { get; set; }
            public decimal Birim3Katsayi { get; set; }
            public string FiyatDoviz { get; set; }
            public decimal PerakendeVergi { get; set; }
            public decimal ToptanVergi { get; set; }
            public string AnaGrupKod { get; set; }
            public string AltGrupKod { get; set; }
            public string MarkaKodu { get; set; }
            public string KategoriKodu { get; set; }
            public decimal MinStok { get; set; }
            public decimal MaxStok { get; set; }
        }

        public class StokSelectModel
        {
            public string Value { get; set; } // StokKod
            public string Text { get; set; }  // StokKod - StokAdi
            public decimal Fiyat { get; set; }
            public string Birim { get; set; }
            public string AnaGrupAdi { get; set; }
        }

        public class StokFiyatGecmis
        {
            public decimal Fiyat { get; set; }
            public DateTime BaslangicTarihi { get; set; }
            public DateTime? BitisTarihi { get; set; }
            public int ListeNo { get; set; }
            public string ListeAdi { get; set; }
        }

       

        #endregion

        #region Dashboard ve Genel Modeller

        public class DashboardModel
        {
            public TeklifIstatistikleri TeklifIstatistikleri { get; set; }
            public List<AylikTeklifGrafik> AylikGrafik { get; set; }
            public List<TeklifListeModel> SonTeklifler { get; set; }
        }

        public class TeklifFormViewModel
        {
            public YeniTeklifModel Teklif { get; set; } = new YeniTeklifModel();
            public List<CariHesapModel> CariHesaplar { get; set; } = new List<CariHesapModel>();
            public List<PersonelModel> Personeller { get; set; } = new List<PersonelModel>();
            public List<StokModel> Stoklar { get; set; } = new List<StokModel>();
            public List<string> Durumlar { get; set; } = new List<string>();
        }

        public class TeklifEditViewModel : TeklifFormViewModel
        {
            public TeklifDetayModel MevcutTeklif { get; set; }
        }

        #endregion

        #region Mevcut Firsat Modeli (Değişiklik yok)

        public class Firsat
        {
            public Guid Firsat_Guid { get; set; }
            public string Firsat_Adi { get; set; } = string.Empty;
            public string Adi => Firsat_Adi;
            public string Firma_Adi { get; set; } = string.Empty;
            public string Firma => Firma_Adi;
            public string Email { get; set; } = string.Empty;
            public string Telefon { get; set; } = string.Empty;
            public decimal? Tutar { get; set; }
            public string Etiketler { get; set; } = string.Empty;
            public string Atanan_Kisi { get; set; } = string.Empty;
            public string Durum { get; set; } = string.Empty;
            public string Kaynak { get; set; } = string.Empty;
            public DateTime? Son_Iletisim_Tarihi { get; set; }
            public DateTime Olusturulma_Tarihi { get; set; } = DateTime.Now;
            public string Adres { get; set; } = string.Empty;
            public string Pozisyon { get; set; } = string.Empty;
            public string Sehir { get; set; } = string.Empty;
            public string Ilce { get; set; } = string.Empty;
            public string Ulke { get; set; } = string.Empty;
            public string Website { get; set; } = string.Empty;
            public string Posta_Kodu { get; set; } = string.Empty;
            public string Varsayilan_Dil { get; set; } = string.Empty;
            public string Aciklama { get; set; } = string.Empty;
        }

        #endregion

        #region API Response Modelleri

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }

        public class CariSelectModel
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }

      

        #endregion
    }
}