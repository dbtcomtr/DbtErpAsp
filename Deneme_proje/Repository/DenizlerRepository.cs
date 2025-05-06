using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // SqlConnection buradan
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using static Deneme_proje.Models.DenizlerEntities;
using NuGet.Protocol.Plugins;
using static Deneme_proje.Models.Entities;

namespace Deneme_proje.Repository
{
    public interface IDenizlerRepository
    {
        IEnumerable<RaportModel.Rapor1Model> GetRaportDenizler1(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu);
        IEnumerable<RaportModel.Rapor2Model> GetRaportDenizler2(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu);
        IEnumerable<RaportModel.Rapor3Model> GetRaportDenizler3(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu);
        IEnumerable<RaportModel.Rapor4Model> GetRaportDenizler4(string srmKodu, string projeKodu);
    }
    public class DenizlerRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<DenizlerRepository> _logger;

        public DenizlerRepository(DatabaseSelectorService dbSelectorService, ILogger<DenizlerRepository> logger)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        public IEnumerable<Deneme_proje.Models.DenizlerEntities.FirmaCekleri> GetDenizlerFirmaCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_LOJISTIK_FirmaCekleri_TariheGore]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<Deneme_proje.Models.DenizlerEntities.FirmaCekleri>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<Deneme_proje.Models.DenizlerEntities.MusteriCekleri> GetMusteriCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_LOJISTIK_MusteriCekleri_TariheGore]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<Deneme_proje.Models.DenizlerEntities.MusteriCekleri>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<Deneme_proje.Models.DenizlerEntities.AracKmYakit> GetAracKmYakitBilgileri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_LOJISTIK_ARACLAR_KMLITRE]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<Deneme_proje.Models.DenizlerEntities.AracKmYakit>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<Alici> GetAlıcılar(DateTime baslamaTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_LOJISTIK_AlıcıSatıcı]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, MüşteriSatıcı = 0 };

                return connection.Query<Alici>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<Satici> GetSatıcılar(DateTime baslamaTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_LOJISTIK_AlıcıSatıcı]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, MüşteriSatıcı = 1 };

                return connection.Query<Satici>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<KrediSozlesmesi> GetKrediSozlesmeleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_KrediSozlesmesiTaksitOperasyonu_TariheGore]";

                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<KrediSozlesmesi>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<Deneme_proje.Models.DenizlerEntities.CariAnaliz> GetCariAnaliz(string cariKod, string chaDCins)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_BakiyeYaslandirma_Toplam_CariKodaPByeGore]";

                var parameters = new { cari_kod = cariKod, cha_d_cins = chaDCins };

                return connection.Query<Deneme_proje.Models.DenizlerEntities.CariAnaliz>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<FaturaAlis> GetFaturaAlis(DateTime baslangicTarihi, DateTime bitisTarihi, string projeKodu, string srmKodu)
{
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_FATURA_ALIS]";

        var parameters = new
        {
            BaslangicTarihi = baslangicTarihi,
            BitisTarihi = bitisTarihi,
            ProjeKodu = projeKodu,
            SrmKodu = srmKodu
        };

        return connection.Query<FaturaAlis>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
    }
}

public IEnumerable<FaturaSatis> GetFaturaSatis(DateTime baslangicTarihi, DateTime bitisTarihi, string projeKodu, string srmKodu)
{
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_FATURA_SATIS]";

        var parameters = new
        {
            BaslangicTarihi = baslangicTarihi,
            BitisTarihi = bitisTarihi,
            ProjeKodu = projeKodu,
            SrmKodu = srmKodu
        };

        return connection.Query<FaturaSatis>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
    }
}

        public IEnumerable<Proje> GetProjeKodlari(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT DISTINCT
            pro_kodu AS ProjeKodu,
            pro_adi AS ProjeAdi
        FROM  PROJELER
  ";

                var parameters = new { BaslangicTarihi = baslangicTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<Proje>(query, parameters);
            }
        }
        public IEnumerable<Sorumlu> GetSorumluKodlari(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
  SELECT DISTINCT
      som_kod AS SorumluKodu,
      som_isim AS SorumluAdi
  FROM SORUMLULUK_MERKEZLERI
      ";

                var parameters = new { BaslangicTarihi = baslangicTarihi, BitisTarihi = bitisTarihi };

                return connection.Query<Sorumlu>(query, parameters);
            }
        }
		public IEnumerable<RaportModel.Rapor1Model> GetRaportDenizler1(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu)
		{
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_RAPORBANKASI_DENIZLER_ARACMASRAF]"; // Prosedür adı doğru mu kontrol et

				var parameters = new
				{
					BaslamaTarihi = baslamaTarihi,
					BitisTarihi = bitisTarihi,
					SorumlulukMerkezi = string.IsNullOrEmpty(sorumlulukMerkezi) ? null : sorumlulukMerkezi,
					ProjeKodu = string.IsNullOrEmpty(projeKodu) ? null : projeKodu
				};

				return connection.Query<RaportModel.Rapor1Model>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
			}
		}

		public IEnumerable<RaportModel.Rapor2Model> GetRaportDenizler2(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_RAPORBANKASI_DENIZLER_2]";
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi, SorumlulukMerkezi = sorumlulukMerkezi, ProjeKodu = projeKodu };
                return connection.Query<RaportModel.Rapor2Model>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<RaportModel.Rapor3Model> GetRaportDenizler3(DateTime baslamaTarihi, DateTime bitisTarihi, string sorumlulukMerkezi, string projeKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_RAPORBANKASI_DENIZLER_3]";
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi, SorumlulukMerkezi = sorumlulukMerkezi, ProjeKodu = projeKodu };
                return connection.Query<RaportModel.Rapor3Model>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<RaportModel.Rapor4Model> GetRaportDenizler4(string srmKodu, string projeKodu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string storedProcedure = "[dbo].[DBT_RAPORBANKASI_DENIZLER_4]";
                var parameters = new { SrmKodu = srmKodu, ProjeKodu = projeKodu };
                return connection.Query<RaportModel.Rapor4Model>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }



		public Guid? GetSomGuidBySrmKodu(string srmKodu)
		{
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT som_guid 
        FROM SORUMLULUK_MERKEZLERI
        WHERE som_kod = @SrmKodu";

				var parameters = new { SrmKodu = srmKodu };
				return connection.QueryFirstOrDefault<Guid?>(query, parameters);
			}
		}

		public IEnumerable<SorumlulukMerkezleriUser> GetSorumlulukMerkeziUserBySomGuid(Guid somGuid)
		{
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
      SELECT 
    Record_uid, 
    Firma_Adi, 
    Arac_Marka, 
    Arac_Modeli, 
    Arac_Model_Yili, 
    Ruhsat_Seri_No, 
    Sase_No, 
    CONVERT(varchar, Muayene_Bitis_Tarihi, 104) AS Muayene_Bitis_Tarihi, 
    Kullanici_Adi, 
    Kullanici_Adi__2__
FROM 
    SORUMLULUK_MERKEZLERI_USER
WHERE 
    record_uid = @SomGuid";

				var parameters = new { SomGuid = somGuid };
				return connection.Query<SorumlulukMerkezleriUser>(query, parameters);
			}
		}


	}
}
