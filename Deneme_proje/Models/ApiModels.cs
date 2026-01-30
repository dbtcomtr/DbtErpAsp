// Models/ApiModels.cs
using System.ComponentModel.DataAnnotations;

namespace Deneme_proje.Models
{
    // Sistem geneli API ayarları
    public class MikroApiAyarlari
    {
        public int Id { get; set; }
        public string MikroSifre { get; set; }
        public bool Aktif { get; set; }
        public DateTime GuncellemeTarihi { get; set; }
        public string GuncelleyenKullanici { get; set; }
    }

    public class ApiAyarlariViewModel
    {
        // Readonly alanlar - sadece gösterim için
        public string ApiKey { get; set; }
        public string KullaniciKodu { get; set; }
        public string ServerAddress { get; set; }
        public string BaseUrl { get; set; }

        // Kullanıcının gireceği alan (sadece SRV için)
        [Required(ErrorMessage = "Mikro Şifre zorunludur")]
        public string MikroSifre { get; set; }
        public bool Aktif { get; set; }
        public bool MevcutKayitVar { get; set; }
        public DateTime? SonGuncellemeTarihi { get; set; }
        public string GuncelleyenKullanici { get; set; }
    }

    // Mikro API için modeller
    public class MikroApiRequest
    {
        public MikroApiData Mikro { get; set; }
    }

    public class MikroApiData
    {
        public string FirmaKodu { get; set; }
        public string CalismaYili { get; set; }
        public string ApiKey { get; set; }
        public string KullaniciKodu { get; set; }
        public string Sifre { get; set; }

        // ⚠️ Bu küçük harfle kalmalı (Mikro API'nin beklediği format)
        public List<Evrak> evraklar { get; set; }
    }

    public class Evrak
    {
        public List<EvrakAciklama> evrak_aciklamalari { get; set; }
        public List<Satir> satirlar { get; set; }
    }

    public class EvrakAciklama
    {
        public string aciklama { get; set; }
    }

    public class Satir
    {
        // ✅ MEVCUT ALANLAR
        public string tkl_Guid { get; set; }  // ⬅️ YENİ EKLENEN
        public string tkl_evrak_tarihi { get; set; }
        public string tkl_evrakno_seri { get; set; }
        public string tkl_belge_no { get; set; }
        public string tkl_cari_kod { get; set; }
        public int tkl_harekettipi { get; set; }
        public string tkl_stok_kod { get; set; }
        public string tkl_Aciklama { get; set; }
        public decimal tkl_Alisfiyati { get; set; }
        public string tkl_baslangic_tarihi { get; set; }
        public decimal tkl_miktar { get; set; }
        public int tkl_birim_pntr { get; set; }
        public int tkl_vergi_pntr { get; set; }
        public string tkl_cari_tipi { get; set; }
        public decimal tkl_karorani { get; set; }
        public string tkl_ProjeKodu { get; set; }
        public string tkl_cari_sormerk { get; set; }
        public string tkl_stok_sormerk { get; set; }

        // ✅ YENİ EKLENENLER (KDV ve eksik alanlar için)
        public decimal tkl_Brut_fiyat { get; set; }           // Liste fiyatı
        public string tkl_Gecerlilik_Sures { get; set; }      // Bitiş tarihi
        public string tkl_special2 { get; set; }              // KDV oranı (0, 1, 10, 20, 30)
        public decimal tkl_vergi { get; set; }                // KDV tutarı
    }

    // ✅ YENİ EKLENEN: API Response modeli
    public class MikroApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public string Response { get; set; }
    }
}