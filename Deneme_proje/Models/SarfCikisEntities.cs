using System;
using System.Collections.Generic;

namespace Deneme_proje.Models
{
    // Ana ViewModel
    public class SarfCikisViewModel
    {
        public SarfCikisDepartmanBazli SarfCikis { get; set; }
        public List<SarfCikisDepartmanBazliStoklar> StokDetaylari { get; set; }
        public List<StokBilgisi> TumStoklar { get; set; }
        public List<DepoBilgisi> Depolar { get; set; }
        public List<MasrafMerkezi> MasrafMerkezleri { get; set; }
        public List<PartilotBilgisi> Partilotlar { get; set; }

        public SarfCikisViewModel()
        {
            SarfCikis = new SarfCikisDepartmanBazli();
            StokDetaylari = new List<SarfCikisDepartmanBazliStoklar>();
            TumStoklar = new List<StokBilgisi>();
            Depolar = new List<DepoBilgisi>();
            MasrafMerkezleri = new List<MasrafMerkezi>();
            Partilotlar = new List<PartilotBilgisi>();
        }
    }

    // Ana Tablo
    public class SarfCikisDepartmanBazli
    {
        public int Id { get; set; }
        public string sth_evrakno_seri { get; set; }
        public int? sth_evrakno_sira { get; set; }
        public string masraf_merkezi_kodu { get; set; }
        public string masraf_merkezi_ismi { get; set; }
        public string masraf_merkezi_alt_baslik_kodu { get; set; }
        public string masraf_merkezi_alt_baslik_ismi { get; set; }
        public string birim_adi { get; set; }
        public int? sth_cikis_depo_no { get; set; }
        public bool? bekleyen { get; set; }
        public bool? onaylanan { get; set; }
        public bool? tamamlanan { get; set; }
        public bool? kismi_tamamlanan { get; set; }
        public bool? red_edilen { get; set; }
        public bool? kapatilan { get; set; }
        public string talep_eden_isim { get; set; }
        public string talep_eden_kodu { get; set; }
        public DateTime? talep_tarihi { get; set; }
        public string onaylayan_kisi_isim { get; set; }
        public string onaylayan_kisi_kodu { get; set; }
        public DateTime? onaylanma_tarihi { get; set; }
        public string tamamlayan_kisi_isim { get; set; }
        public string tamamlayan_kisi_kodu { get; set; }
        public DateTime? tamamlanma_tarihi { get; set; }
        public string kapatma_nedeni { get; set; }
        public int? gereken_onay_sayisi { get; set; }
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
        public string birim_adi { get; set; }
    }

    // Kaydetme Modeli
    public class SarfCikisKaydetModel
    {
        public SarfCikisDepartmanBazli SarfCikis { get; set; }
        public List<SarfCikisDepartmanBazliStoklar> Stoklar { get; set; }
    }

    // Yardımcı Modeller
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

    public class MasrafMerkezi
    {
        public string his_kod { get; set; }
        public string his_isim { get; set; }
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
}