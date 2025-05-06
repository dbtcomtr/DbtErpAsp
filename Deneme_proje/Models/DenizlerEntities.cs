using Microsoft.AspNetCore.Mvc.Rendering;
using static Deneme_proje.Models.DenizlerEntities.RaportModel;

namespace Deneme_proje.Models
{
    public class DenizlerEntities
    {
        // Mevcut FirmaCekleri sınıfınız
        public class FirmaCekleri
        {
            public string SahipCariKodu { get; set; }
            public string SahipCariAdi { get; set; }
            public string NeredeCariKodu { get; set; }
            public string NeredeCariAdi { get; set; }
            public string RefNo { get; set; }
            public string Pozisyonu { get; set; }
            public DateTime Tarih { get; set; }
            public DateTime Vade { get; set; }
            public decimal Tutar { get; set; }
            public decimal Odenen { get; set; }
            public decimal Kalan { get; set; }
            public string PB { get; set; }
        }


        public class MusteriCekleri
        {
            public string SahipCariKodu { get; set; }
            public string SahipCariAdi { get; set; }
            public string NeredeCariKodu { get; set; }
            public string NeredeCariAdi { get; set; }
            public string RefNo { get; set; }
            public string Pozisyonu { get; set; }
            public DateTime Tarih { get; set; }
            public DateTime Vade { get; set; }
            public decimal Tutar { get; set; }
            public decimal Odenen { get; set; }
            public decimal Kalan { get; set; }
            public string PB { get; set; }
        }


        public class AracKmYakit
        {
            public string CariKodu { get; set; }
            public string CariAdı { get; set; }
            public decimal SaatteYakılanLitreMiktar { get; set; }
            public decimal SaatteYakılanLitreTutar { get; set; }
            public decimal YapılanKmSaat { get; set; }
            public decimal SeferSayısı { get; set; }
            public decimal SeferBaşıOrtalamaKM { get; set; }
            public decimal SeferBaşıOrtalamaTonaj { get; set; }
            public decimal ToplamTonaj { get; set; }
            public decimal KullanılanYakıtLitre { get; set; }
            public decimal KullanılanYakıtTutar { get; set; }
            public decimal BakımToplamı { get; set; }
        }

        public class AliciSaticiViewModel
        {
            public IEnumerable<Alici> Alicilar { get; set; }
            public IEnumerable<Satici> Saticilar { get; set; }
        }

        public class Alici
        {
            public string CariKodu { get; set; }
            public string CariAdı { get; set; }
            public decimal Bakiye { get; set; }
        }

        public class Satici
        {
            public string CariKodu { get; set; }
            public string CariAdı { get; set; }
            public decimal Bakiye { get; set; }
        }

        public class KrediSozlesmesi
        {
            public string SozlesmeKodu { get; set; }
            public string Aciklama { get; set; }
            public string BankaKodu { get; set; }
            public string BankaAdi { get; set; }
            public string KrediTipi { get; set; }
            public string DovizCinsi { get; set; }
            public DateTime KapanisTarihi { get; set; }
            public string Durum { get; set; }
            public decimal KrediTutari { get; set; }
            public int TaksitSayisi { get; set; }
            public int TaksitNo { get; set; }
            public DateTime VadeTarihi { get; set; }
            public decimal TaksitTutari { get; set; }
            public decimal TaksitAnapara { get; set; }
            public decimal TaksitFaiz { get; set; }
            public decimal TaksitBSMV { get; set; }
            public decimal TaksitKKDF { get; set; }
            public decimal KalanAnapara { get; set; }
            public DateTime OdemeFisTarihi { get; set; }
            public decimal OdenenTaksitTutari { get; set; }
            public decimal KalanTaksitTutari { get; set; }
        }


        public class FaturaAlis
        {
            public string Cari { get; set; }
            public decimal Meblag { get; set; }
        }

        public class FaturaSatis
        {
            public string Cari { get; set; }
            public decimal Meblag { get; set; }
        }
        public class GelirGiderViewModel
        {
            public IEnumerable<FaturaAlis> FaturaAlis { get; set; }
            public IEnumerable<FaturaSatis> FaturaSatis { get; set; }
            public DateTime BaslangicTarihi { get; set; }
            public DateTime BitisTarihi { get; set; }
            public string ProjeKodu { get; set; }
            public string SrmKodu { get; set; }
            public IEnumerable<SelectListItem> ProjeKodlari { get; set; }
            public IEnumerable<SelectListItem> SorumluKodlari { get; set; }
        }

        public class Proje
        {
            public string ProjeKodu { get; set; }
            public string ProjeAdi { get; set; }
        }

        public class Sorumlu
        {
            public string SorumluKodu { get; set; }
            public string SorumluAdi { get; set; }
        }

        public class CariAnaliz
        {
            public decimal? Bakiye { get; set; }
            public decimal? GecikenBakiye { get; set; }
            public DateTime? AgirlikliOrtalamaVade { get; set; }
            public decimal? VadeFark { get; set; }
            public decimal? KapanmayanBakiye { get; set; }
            public decimal? ToplamBakiye { get; set; }
            public string CariKod { get; set; }
            public string DovizCinsi { get; set; }
            public string DovizKod { get; set; }
            // Saklı yordamdan dönen diğer alanlar...
        }



        // Yeni prosedürlerin sonuçlarını temsil edecek sınıflar
        public class CariAlacakModel
        {
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public decimal TL { get; set; }
            public decimal USD { get; set; }
            public decimal EUR { get; set; }
            public decimal GBP { get; set; }
            public decimal UPB { get; set; }
            public string PB { get; set; }
            public decimal TL_UPB { get; set; }
            public decimal USD_UPB { get; set; }
            public decimal EUR_UPB { get; set; }
            public decimal GBP_UPB { get; set; }
        }

        // CariVerecekModel: Cari hesaplarda verecekleri temsil eder
        public class CariVerecekModel
        {
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public decimal TL { get; set; }
            public decimal USD { get; set; }
            public decimal EUR { get; set; }
            public decimal GBP { get; set; }
            public decimal UPB { get; set; }
            public string PB { get; set; }
            public decimal TL_UPB { get; set; }
            public decimal USD_UPB { get; set; }
            public decimal EUR_UPB { get; set; }
            public decimal GBP_UPB { get; set; }
        }

		public class SorumlulukVerisiModel
		{
			public Guid Record_uid { get; set; }
			public string Firma_Adi { get; set; }
			public string Arac_Marka { get; set; }
			public string Arac_Modeli { get; set; }
			public int Arac_Model_Yili { get; set; }
			public string Ruhsat_Seri_No { get; set; }
			public string Sase_No { get; set; }
			public DateTime Muayene_Bitis_Tarihi { get; set; }
			public string Kullanici_Adi { get; set; }
			public string Kullanici_Adi__2__ { get; set; }
		}


		public class CariViewModel
        {
            public IEnumerable<CariAlacakModel> CariAlacaklar { get; set; }
            public IEnumerable<CariVerecekModel> CariVerecekler { get; set; }
            public string SektorKodu { get; set; }
            public string BolgeKodu { get; set; }
            public string GrupKodu { get; set; }
            public string TemsilciKodu { get; set; }
            public int UPB_PB { get; set; }
            public SelectList UPBOptions { get; set; }
            public SelectList SektorOptions { get; set; }
            public SelectList BolgeOptions { get; set; }
            public SelectList GrupOptions { get; set; }
            public SelectList TemsilciOptions { get; set; }
        }
		public class RaportModel
		{
			// DBT_RAPORBANKASI_DENIZLER_1
			public class Rapor1Model
			{
				public decimal GidilenSeferSayısı { get; set; }        // Sefer Sayısı
				public decimal TaşınanTonaj { get; set; }              // Taşınan Tonaj
				public decimal KullanılanYakıtLitre { get; set; }      // Kullanılan Yakıt Litre Miktarı
				public decimal KullanılanYakıtTutarı { get; set; }     // Kullanılan Yakıt Tutarı
				public decimal StoktaKullanılan { get; set; }          // Stokta Kullanılan Miktar
				public decimal SanayideYapilan { get; set; }           // Sanayide Yapılan Harcamalar
				public decimal MazotHariçStoktaKullanılan { get; set; } // Mazot Hariç Stokta Kullanılan
				public decimal TümGiderler { get; set; }               // Toplam Giderler
				public decimal NakliyeCiroBedeli { get; set; }         // Nakliye Ciro Bedeli
				public decimal BirSaatKmYapılanYakıtLitresiTutar { get; set; } // Bir Saat KM Başına Yakıt Litresi Tutarı
				public decimal ÇalışmaSaatiYapılanKM { get; set; }     // Çalışma Saati Olarak Yapılan KM
				public decimal BirSaatKMYapılanYakıtLitresi { get; set; } // Bir Saatte Yapılan KM Başına Yakıt Litresi
				public decimal BirSaatKMYakılanYakıtLitreOran { get; set; } // Bir Saatte Yakılan Yakıt Litre Oranı
			}

			// DBT_RAPORBANKASI_DENIZLER_2
			public class Rapor2Model
			{
				public DateTime Tarih { get; set; }
				public string Açıklama { get; set; }
				public string MasrafAdı { get; set; }
				public double Miktar { get; set; }
				public double Tutar { get; set; }
			}

			// DBT_RAPORBANKASI_DENIZLER_3
			public class Rapor3Model
			{
				public DateTime Tarih { get; set; }
				public string Açıklama { get; set; }
				public string StokAdı { get; set; }
				public double Miktar { get; set; }
				public double Tutar { get; set; }
			}

			// DBT_RAPORBANKASI_DENIZLER_4
			public class Rapor4Model
			{
				public string Ad { get; set; }
			}
		}

		public class SorumlulukUserModel
		{
			public Guid Record_uid { get; set; }
			public string Firma_Adi { get; set; }
			public string Arac_Marka { get; set; }
			public string Arac_Modeli { get; set; }
			public int Arac_Model_Yili { get; set; }
			public string Ruhsat_Seri_No { get; set; }
			public string Sase_No { get; set; }
			public DateTime Muayene_Bitis_Tarihi { get; set; }
			public string Kullanici_Adi { get; set; }
			public string Kullanici_Adi__2__ { get; set; }
		}

		public class SorumlulukMerkezleriUser
		{
			public string Firma_Adi { get; set; }
			public string Arac_Marka { get; set; }
			public string Arac_Modeli { get; set; }
			public int Arac_Model_Yili { get; set; }
			public string Ruhsat_Seri_No { get; set; }
			public string Sase_No { get; set; }
			public string Muayene_Bitis_Tarihi { get; set; }
			public string Kullanici_Adi { get; set; }
			public string Kullanici_Adi__2__ { get; set; }
		}

		public class RaportViewModel
		{
			public DateTime BaslamaTarihi { get; set; }
			public DateTime BitisTarihi { get; set; }

			public IEnumerable<RaportModel.Rapor1Model> Denizler1Data { get; set; }
			public IEnumerable<RaportModel.Rapor2Model> Denizler2Data { get; set; }
			public IEnumerable<RaportModel.Rapor3Model> Denizler3Data { get; set; }
			public IEnumerable<RaportModel.Rapor4Model> Denizler4Data { get; set; }

			// Bu kısmı düzeltiyoruz
			public IEnumerable<SorumlulukMerkezleriUser> SorumlulukUserData { get; set; } // Düzeltilmiş alan

			public double GelirToplami { get; set; }
			public double GiderToplami { get; set; }
			public double Sonuc { get; set; }

			public IEnumerable<SelectListItem> ProjeKodlari { get; set; }
			public IEnumerable<SelectListItem> SorumluKodlari { get; set; }

			public string ProjeKodu { get; set; }
			public string SorumluKodu { get; set; }
		}


	}
}
