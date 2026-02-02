using System;
using System.Collections.Generic;

namespace Deneme_proje.Models
{
    // Ana ViewModel - Talep oluşturma ve listeleme için kullanılır
    public class SarfCikisViewModel
    {
        public SarfCikisDepartmanBazli SarfCikis { get; set; }
        public List<SarfCikisDepartmanBazliStoklar> StokDetaylari { get; set; }

        // Yardımcı listeler
        public List<DepoBilgisi> Depolar { get; set; }
        public IEnumerable<dynamic> AnaMasrafGruplari { get; set; }     // ← Yeni: Ana masraf grupları
        public List<dynamic> SorumlulukMerkezleri { get; set; } = new(); // ← DÜZELTİLDİ
        public List<PartilotBilgisi> Partilotlar { get; set; }

        // Eski MasrafMerkezleri kaldırıldı → artık hiyerarşik seçim yapılıyor

        public SarfCikisViewModel()
        {
            SarfCikis = new SarfCikisDepartmanBazli();
            StokDetaylari = new List<SarfCikisDepartmanBazliStoklar>();
            Depolar = new List<DepoBilgisi>();
            Partilotlar = new List<PartilotBilgisi>();
            AnaMasrafGruplari = new List<dynamic>();
            SorumlulukMerkezleri = new List<dynamic>();

        }
    }

    // Ana Talep Tablosu (SarfCikisDepartmanBazli)
    // Onay ile ilgili alanlar (bekleyen, onaylanan, red_edilen vb.) artık kullanılmıyor
    // ama veritabanında varsa tutmaya devam ediyoruz (migration yapmadan silmiyoruz)
    public class SarfCikisDepartmanBazli
    {
        public int Id { get; set; }
        public string sth_evrakno_seri { get; set; }
        public int? sth_evrakno_sira { get; set; }
        public string masraf_merkezi_kodu { get; set; }      // artık alt seviye kod
        public string masraf_merkezi_ismi { get; set; }
        public string birim_adi { get; set; }
        public int? sth_cikis_depo_no { get; set; }

        // Onay mekanizması kaldırıldığı için bu alanlar artık frontend/backend'de aktif kullanılmıyor
        public bool? bekleyen { get; set; }           // kullanılmıyor
        public bool? onaylanan { get; set; }          // kullanılmıyor
        public bool? tamamlanan { get; set; }
        public bool? kismi_tamamlanan { get; set; }
        public bool? red_edilen { get; set; }         // kullanılmıyor
        public bool? kapatilan { get; set; }

        public string talep_eden_isim { get; set; }
        public string talep_eden_kodu { get; set; }
        public DateTime? talep_tarihi { get; set; }

        // Onay ile ilgili eski alanlar (kaldırılabilir ama db'de varsa tutuyoruz)
        public string onaylayan_kisi_isim { get; set; }
        public string onaylayan_kisi_kodu { get; set; }
        public DateTime? onaylanma_tarihi { get; set; }

        public string tamamlayan_kisi_isim { get; set; }
        public string tamamlayan_kisi_kodu { get; set; }
        public DateTime? tamamlanma_tarihi { get; set; }
        public string sorumluluk_merkezi_kodu { get; set; }
        public string sorumluluk_merkezi_ismi { get; set; }
        public string kapatma_nedeni { get; set; }
        public int? gereken_onay_sayisi { get; set; }   // artık kullanılmıyor
    }

    // Stok Detay Tablosu
    public class SarfCikisDepartmanBazliStoklar
    {
        public int Id { get; set; }
        public string sth_evrakno_seri { get; set; }
        public int? sth_evrakno_sira { get; set; }
        public string sth_belge_no { get; set; }
        public DateTime? sth_belge_tarih { get; set; }
        public int? sth_birim_pntr { get; set; }
        public double? sth_miktar { get; set; }
        public double? sth_tutar { get; set; }
        public string sth_stok_kod { get; set; }
        public string sth_stok_adi { get; set; }
        public double? planlanan_adet { get; set; }
        public double? tamamlanan_adet { get; set; }
        public string birim_adi { get; set; }   // ekstra okunabilirlik için
    }

    // Talep Kaydetme için kullanılan DTO
    public class SarfCikisKaydetModel
    {
        public SarfCikisDepartmanBazli SarfCikis { get; set; }
        public List<SarfCikisDepartmanBazliStoklar> Stoklar { get; set; }
    }

    // Yardımcı Modeller (değişmedi)

    public class StokBilgisi
    {
        public string sto_kod { get; set; }
        public string sto_isim { get; set; }
        public string sto_kisa_ismi { get; set; }
        public string sto_birim1_ad { get; set; }
        public string sto_birim2_ad { get; set; }
        public string sto_birim3_ad { get; set; }
        public string sto_birim4_ad { get; set; }
        public string sto_anagrup_kod { get; set; }
        public string sto_altgrup_kod { get; set; }
    }

    public class DepoBilgisi
    {
        public int dep_no { get; set; }
        public string dep_adi { get; set; }
    }

    public class PartilotBilgisi
    {
        public string pl_partikodu { get; set; }
        public string pl_lotno { get; set; }
        public string pl_stokkodu { get; set; }
    }

    public class StokDepoMiktar
    {
        public int dep_no { get; set; }
        public string dep_adi { get; set; }
        public decimal miktar { get; set; }
    }

    public class BarkodTanimi
    {
        public string bar_kodu { get; set; }
        public string bar_stokkodu { get; set; }
        public string bar_partikodu { get; set; }
        public string bar_lotno { get; set; }
    }

    // Artık kullanılmayan eski model (referans için bırakılabilir)
    // public class MasrafMerkezi { ... } → ViewModel'den ve repository'den kaldırıldı
}