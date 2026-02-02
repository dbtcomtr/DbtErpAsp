using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Deneme_proje.Models;

namespace Deneme_proje.Repository
{
    public class SarfCikisRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<SarfCikisRepository> _logger;

        public SarfCikisRepository(DatabaseSelectorService dbSelectorService, ILogger<SarfCikisRepository> logger)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        #region MikroDB İşlemleri

        public IEnumerable<DepoBilgisi> GetDepolar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT dep_no, dep_adi
                FROM DEPOLAR
                WHERE dep_iptal = 0
                ORDER BY dep_adi";
            return connection.Query<DepoBilgisi>(query);
        }

        public IEnumerable<StokBilgisi> GetStoklar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT
                    sto_kod,
                    COALESCE(NULLIF(LTRIM(RTRIM(sto_isim)), ''), NULLIF(LTRIM(RTRIM(sto_kisa_ismi)), ''), sto_kod) as sto_isim,
                    sto_kisa_ismi,
                    sto_birim1_ad,
                    sto_birim2_ad,
                    sto_birim3_ad,
                    sto_birim4_ad,
                    sto_anagrup_kod,
                    sto_altgrup_kod
                FROM STOKLAR
                WHERE sto_pasif_fl = 0 AND sto_iptal = 0
                ORDER BY sto_isim";
            return connection.Query<StokBilgisi>(query);
        }

        public decimal GetDepoStokMiktar(string stokKod, int depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"SELECT dbo.fn_DepodakiMiktar(@StokKod, @DepoNo, NULL) as MevcutMiktar";
            try
            {
                return connection.QuerySingle<decimal>(query, new { StokKod = stokKod, DepoNo = depoNo });
            }
            catch
            {
                return 0;
            }
        }

        public IEnumerable<StokDepoMiktar> GetStokDepoMiktarlari(string stokKod)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT
                    d.dep_no,
                    d.dep_adi,
                    dbo.fn_DepodakiMiktar(@StokKod, d.dep_no, NULL) as miktar
                FROM DEPOLAR d
                WHERE d.dep_iptal = 0 
                  AND dbo.fn_DepodakiMiktar(@StokKod, d.dep_no, NULL) > 0
                ORDER BY d.dep_adi";
            return connection.Query<StokDepoMiktar>(query, new { StokKod = stokKod });
        }

        public IEnumerable<PartilotBilgisi> GetPartilotlar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT pl_partikodu, pl_lotno, pl_stokkodu
                FROM PARTILOT
                ORDER BY pl_partikodu";
            return connection.Query<PartilotBilgisi>(query);
        }

        public IEnumerable<dynamic> GetStokAnaGruplari()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT san_kod, san_isim
                FROM STOK_ANA_GRUPLARI
                WHERE san_iptal = 0
                ORDER BY san_isim";
            return connection.Query(query);
        }

        public IEnumerable<dynamic> GetStokAltGruplari(string anaGrupKod)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT sta_kod, sta_isim
                FROM STOK_ALT_GRUPLARI
                WHERE sta_iptal = 0 
                  AND sta_ana_grup_kod = @AnaGrupKod
                ORDER BY sta_isim";
            return connection.Query(query, new { AnaGrupKod = anaGrupKod });
        }

        #endregion

        #region DBT_ERP İşlemleri

        public int GetSonEvrakSiraNo(string seriNo)
        {
            var connectionString = _dbSelectorService.GetERPConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT ISNULL(MAX(sth_evrakno_sira), 0) + 1
                FROM SarfCikisDepartmanBazli
                WHERE sth_evrakno_seri = @SeriNo";
            try
            {
                return connection.QuerySingle<int>(query, new { SeriNo = seriNo });
            }
            catch
            {
                return 1;
            }
        }

        public int SarfCikisKaydet(SarfCikisKaydetModel model, string userNo, string userName)
        {
            var erpConnectionString = _dbSelectorService.GetERPConnectionString();
            using var connection = new SqlConnection(erpConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertQuery = @"
                    INSERT INTO SarfCikisDepartmanBazli (
                        uuid, sth_evrakno_seri, sth_evrakno_sira, masraf_merkezi_kodu, masraf_merkezi_ismi,
                        birim_adi, bekleyen, onaylanan, tamamlanan, kismi_tamamlanan, red_edilen, kapatilan,
                        talep_eden_isim, talep_eden_kodu, sth_cikis_depo_no, talep_tarihi, gereken_onay_sayisi
                    ) VALUES (
                        NEWID(), @seri, @sira, @mmKod, @mmIsim, @birim, 1, 0, 0, 0, 0, 0,
                        @talepEden, @talepKod, @depo, GETDATE(), 1
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                var sarfCikisId = connection.QuerySingle<int>(insertQuery, new
                {
                    seri = model.SarfCikis.sth_evrakno_seri,
                    sira = model.SarfCikis.sth_evrakno_sira,
                    mmKod = model.SarfCikis.masraf_merkezi_kodu ?? "",
                    mmIsim = model.SarfCikis.masraf_merkezi_ismi ?? "",
                    birim = model.SarfCikis.birim_adi ?? "",
                    talepEden = userName,
                    talepKod = userNo,
                    depo = model.SarfCikis.sth_cikis_depo_no ?? 0
                }, transaction);

                foreach (var stok in model.Stoklar)
                {
                    const string stokQuery = @"
                        INSERT INTO SarfCikisDepartmanBazliStoklar (
                            sth_evrakno_seri, sth_evrakno_sira, sth_belge_no, sth_belge_tarih,
                            sth_birim_pntr, sth_miktar, sth_tutar, sth_stok_kod, sth_stok_adi,
                            planlanan_adet, tamamlanan_adet
                        ) VALUES (
                            @seri, @sira, @belge, GETDATE(), @birim, @miktar, @tutar, @kod, @isim, @miktar, 0
                        )";

                    connection.Execute(stokQuery, new
                    {
                        seri = model.SarfCikis.sth_evrakno_seri,
                        sira = model.SarfCikis.sth_evrakno_sira,
                        belge = stok.sth_belge_no ?? "",
                        birim = stok.sth_birim_pntr,
                        miktar = stok.sth_miktar,
                        tutar = stok.sth_tutar,
                        kod = stok.sth_stok_kod,
                        isim = stok.sth_stok_adi
                    }, transaction);
                }

                transaction.Commit();
                return sarfCikisId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public IEnumerable<SarfCikisDepartmanBazli> GetTamamlanabilirSarfCikislar()
        {
            var connectionString = _dbSelectorService.GetERPConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT * FROM SarfCikisDepartmanBazli
                WHERE tamamlanan = 0
                  AND red_edilen = 0
                ORDER BY talep_tarihi DESC";
            return connection.Query<SarfCikisDepartmanBazli>(query);
        }

        public IEnumerable<SarfCikisDepartmanBazli> GetTumSarfCikislar()
        {
            var connectionString = _dbSelectorService.GetERPConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = "SELECT * FROM SarfCikisDepartmanBazli ORDER BY talep_tarihi DESC";
            return connection.Query<SarfCikisDepartmanBazli>(query);
        }

        public IEnumerable<SarfCikisDepartmanBazliStoklar> GetSarfCikisStoklar(string seriNo, int siraNo)
        {
            var connectionString = _dbSelectorService.GetERPConnectionString();
            using var connection = new SqlConnection(connectionString);
            const string query = @"
                SELECT * FROM SarfCikisDepartmanBazliStoklar
                WHERE sth_evrakno_seri = @Seri 
                  AND sth_evrakno_sira = @Sira
                ORDER BY Id";
            return connection.Query<SarfCikisDepartmanBazliStoklar>(query, new { Seri = seriNo, Sira = siraNo });
        }

        public bool SarfCikisTamamla(int id, string userNo, string userName)
        {
            var erpConnectionString = _dbSelectorService.GetERPConnectionString();
            var mikroConnectionString = _dbSelectorService.GetConnectionString();

            using var erpConn = new SqlConnection(erpConnectionString);
            using var mikroConn = new SqlConnection(mikroConnectionString);

            erpConn.Open();
            mikroConn.Open();

            using var erpTx = erpConn.BeginTransaction();
            using var mikroTx = mikroConn.BeginTransaction();

            try
            {
                var sarfCikis = erpConn.QuerySingle<SarfCikisDepartmanBazli>(
                    "SELECT * FROM SarfCikisDepartmanBazli WHERE Id = @Id",
                    new { Id = id }, erpTx);

                var stoklar = erpConn.Query<SarfCikisDepartmanBazliStoklar>(
                    @"SELECT * FROM SarfCikisDepartmanBazliStoklar
                      WHERE sth_evrakno_seri = @Seri AND sth_evrakno_sira = @Sira",
                    new { Seri = sarfCikis.sth_evrakno_seri, Sira = sarfCikis.sth_evrakno_sira },
                    erpTx).ToList();

                int satirNo = 0;
                DateTime islemTarihi = DateTime.Now;
                int userIdInt = int.Parse(userNo);

                foreach (var stok in stoklar)
                {
                    satirNo++;

                    const string stokHareketQuery = @"
                      INSERT INTO STOK_HAREKETLERI (
        sth_Guid,
        sth_DBCno,
        sth_SpecRECno,
        sth_iptal,
        sth_fileid,
        sth_hidden,
        sth_kilitli,
        sth_degisti,
        sth_checksum,
        sth_create_user,
        sth_create_date,
        sth_lastup_user,
        sth_lastup_date,
        sth_special1,
        sth_special2,
        sth_special3,
        sth_firmano,
        sth_subeno,
        sth_tarih,
        sth_tip,
        sth_cins,
        sth_normal_iade,
        sth_evraktip,
        sth_evrakno_seri,
        sth_evrakno_sira,
        sth_satirno,
        sth_belge_no,
        sth_belge_tarih,
        sth_stok_kod,
        sth_isk_mas1,
        sth_isk_mas2,
        sth_isk_mas3,
        sth_isk_mas4,
        sth_isk_mas5,
        sth_isk_mas6,
        sth_isk_mas7,
        sth_isk_mas8,
        sth_isk_mas9,
        sth_isk_mas10,
        sth_sat_iskmas1,
        sth_sat_iskmas2,
        sth_sat_iskmas3,
        sth_sat_iskmas4,
        sth_sat_iskmas5,
        sth_sat_iskmas6,
        sth_sat_iskmas7,
        sth_sat_iskmas8,
        sth_sat_iskmas9,
        sth_sat_iskmas10,
        sth_pos_satis,
        sth_promosyon_fl,
        sth_cari_cinsi,
        sth_cari_kodu,
        sth_cari_grup_no,
        sth_isemri_gider_kodu,
        sth_plasiyer_kodu,
        sth_har_doviz_cinsi,
        sth_har_doviz_kuru,
        sth_alt_doviz_kuru,
        sth_stok_doviz_cinsi,
        sth_stok_doviz_kuru,
        sth_miktar,
        sth_miktar2,
        sth_birim_pntr,
        sth_tutar,
        sth_iskonto1,
        sth_iskonto2,
        sth_iskonto3,
        sth_iskonto4,
        sth_iskonto5,
        sth_iskonto6,
        sth_masraf1,
        sth_masraf2,
        sth_masraf3,
        sth_masraf4,
        sth_vergi_pntr,
        sth_vergi,
        sth_masraf_vergi_pntr,
        sth_masraf_vergi,
        sth_netagirlik,
        sth_odeme_op,
        sth_aciklama,
        sth_sip_uid,
        sth_fat_uid,
        sth_giris_depo_no,
        sth_cikis_depo_no,
        sth_malkbl_sevk_tarihi,
        sth_cari_srm_merkezi,
        sth_stok_srm_merkezi,
        sth_fis_tarihi,
        sth_fis_sirano,
        sth_vergisiz_fl,
        sth_maliyet_ana,
        sth_maliyet_alternatif,
        sth_maliyet_orjinal,
        sth_adres_no,
        sth_parti_kodu,
        sth_lot_no,
        sth_kons_uid,
        sth_proje_kodu,
        sth_exim_kodu,
        sth_otv_pntr,
        sth_otv_vergi,
        sth_brutagirlik,
        sth_disticaret_turu,
        sth_otvtutari,
        sth_otvvergisiz_fl,
        sth_oiv_pntr,
        sth_oiv_vergi,
        sth_oivvergisiz_fl,
        sth_fiyat_liste_no,
        sth_oivtutari,
        sth_Tevkifat_turu,
        sth_nakliyedeposu,
        sth_nakliyedurumu,
        sth_yetkili_uid,
        sth_taxfree_fl,
        sth_ilave_edilecek_kdv,
        sth_ismerkezi_kodu,
        sth_HareketGrupKodu1,
        sth_HareketGrupKodu2,
        sth_HareketGrupKodu3,
        sth_Olcu1,
        sth_Olcu2,
        sth_Olcu3,
        sth_Olcu4,
        sth_Olcu5,
        sth_FormulMiktarNo,
        sth_FormulMiktar,
        sth_eirs_senaryo,
        sth_eirs_tipi,
        sth_teslim_tarihi,
        sth_matbu_fl,
        sth_satis_fiyat_doviz_cinsi,
        sth_satis_fiyat_doviz_kuru,
        sth_eticaret_kanal_kodu,
        sth_bagli_ithalat_kodu,
        sth_tevkifat_sifirlandi_fl
    ) VALUES (
        NEWID(),                        -- sth_Guid
        0,                              -- sth_DBCno
        0,                              -- sth_SpecRECno
        0,                              -- sth_iptal
        16,                             -- sth_fileid
        0,                              -- sth_hidden
        0,                              -- sth_kilitli
        0,                              -- sth_degisti
        0,                              -- sth_checksum
        @userId,                        -- sth_create_user
        @tarih,                         -- sth_create_date
        @userId,                        -- sth_lastup_user
        @tarih,                         -- sth_lastup_date
        '',                             -- sth_special1
        '',                             -- sth_special2
        '',                             -- sth_special3
        0,                              -- sth_firmano
        0,                              -- sth_subeno
        @tarih,                         -- sth_tarih
        1,                              -- sth_tip          → 1 = Çıkış
        5,                              -- sth_cins         → 5 = Sarf
        0,                              -- sth_normal_iade
        0,                              -- sth_evraktip
        @seri,                          -- sth_evrakno_seri
        @sira,                          -- sth_evrakno_sira
        @satirNo,                       -- sth_satirno
        @belgeno,                       -- sth_belge_no
        @tarih,                         -- sth_belge_tarih
        @stokKod,                       -- sth_stok_kod
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,   -- sth_isk_mas1 .. sth_isk_mas10
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,   -- sth_sat_iskmas1 .. sth_sat_iskmas10
        0,                              -- sth_pos_satis
        0,                              -- sth_promosyon_fl
        0,                              -- sth_cari_cinsi
        '',                             -- sth_cari_kodu
        0,                              -- sth_cari_grup_no
        @masrafKodu,                    -- sth_isemri_gider_kodu     ← MASRAF MERKEZİ
        '',                             -- sth_plasiyer_kodu
        0,                              -- sth_har_doviz_cinsi
        1,                              -- sth_har_doviz_kuru
        1,                              -- sth_alt_doviz_kuru
        0,                              -- sth_stok_doviz_cinsi
        1,                              -- sth_stok_doviz_kuru
        @miktar,                        -- sth_miktar
        0,                              -- sth_miktar2
        @birim,                         -- sth_birim_pntr
        @tutar,                         -- sth_tutar
        0, 0, 0, 0, 0, 0,               -- sth_iskonto1 .. sth_iskonto6
        0, 0, 0, 0,                     -- sth_masraf1 .. sth_masraf4
        0,                              -- sth_vergi_pntr
        0,                              -- sth_vergi
        0,                              -- sth_masraf_vergi_pntr
        0,                              -- sth_masraf_vergi
        0,                              -- sth_netagirlik
        0,                              -- sth_odeme_op
        @aciklama,                      -- sth_aciklama
        '00000000-0000-0000-0000-000000000000', -- sth_sip_uid
        '00000000-0000-0000-0000-000000000000', -- sth_fat_uid
        0,                              -- sth_giris_depo_no
        @depo,                          -- sth_cikis_depo_no
        @tarih,                         -- sth_malkbl_sevk_tarihi
        @sorumlulukKodu,                -- sth_cari_srm_merkezi      ← SORUMLULUK MERKEZİ
        @sorumlulukKodu,                -- sth_stok_srm_merkezi      ← SORUMLULUK MERKEZİ
        @tarih,                         -- sth_fis_tarihi
        0,                              -- sth_fis_sirano
        0,                              -- sth_vergisiz_fl
        @tutar,                         -- sth_maliyet_ana
        @tutar,                         -- sth_maliyet_alternatif
        @tutar,                         -- sth_maliyet_orjinal
        0,                              -- sth_adres_no
        '',                             -- sth_parti_kodu
        0,                              -- sth_lot_no
        '00000000-0000-0000-0000-000000000000', -- sth_kons_uid
        '',                             -- sth_proje_kodu
        '',                             -- sth_exim_kodu
        0,                              -- sth_otv_pntr
        0,                              -- sth_otv_vergi
        0,                              -- sth_brutagirlik
        0,                              -- sth_disticaret_turu
        0,                              -- sth_otvtutari
        0,                              -- sth_otvvergisiz_fl
        0,                              -- sth_oiv_pntr
        0,                              -- sth_oiv_vergi
        0,                              -- sth_oivvergisiz_fl
        0,                              -- sth_fiyat_liste_no
        0,                              -- sth_oivtutari
        0,                              -- sth_Tevkifat_turu
        0,                              -- sth_nakliyedeposu
        0,                              -- sth_nakliyedurumu
        '00000000-0000-0000-0000-000000000000', -- sth_yetkili_uid
        0,                              -- sth_taxfree_fl
        0,                              -- sth_ilave_edilecek_kdv
        @masrafKodu,                    -- sth_ismerkezi_kodu        ← MASRAF MERKEZİ
        '',                             -- sth_HareketGrupKodu1
        '',                             -- sth_HareketGrupKodu2
        '',                             -- sth_HareketGrupKodu3
        0, 0, 0, 0, 0,                  -- sth_Olcu1 .. sth_Olcu5
        0,                              -- sth_FormulMiktarNo
        0,                              -- sth_FormulMiktar
        0,                              -- sth_eirs_senaryo
        0,                              -- sth_eirs_tipi
        @tarih,                         -- sth_teslim_tarihi
        0,                              -- sth_matbu_fl
        0,                              -- sth_satis_fiyat_doviz_cinsi
        0,                              -- sth_satis_fiyat_doviz_kuru
        '',                             -- sth_eticaret_kanal_kodu
        '',                             -- sth_bagli_ithalat_kodu
        0                               -- sth_tevkifat_sifirlandi_fl
    )";

                    mikroConn.Execute(stokHareketQuery, new
                    {
                        userId = userIdInt,
                        tarih = islemTarihi,
                        seri = sarfCikis.sth_evrakno_seri,
                        sira = sarfCikis.sth_evrakno_sira,
                        satirNo,
                        belgeno = $"{sarfCikis.sth_evrakno_seri}-{sarfCikis.sth_evrakno_sira}",
                        stokKod = stok.sth_stok_kod,

                        masrafKodu = sarfCikis.masraf_merkezi_kodu ?? "",
                        sorumlulukKodu = sarfCikis.sorumluluk_merkezi_kodu ?? "",   // ← BU SATIRI EKLE

                        miktar = stok.sth_miktar ?? 0,
                        birim = stok.sth_birim_pntr ?? 1,
                        tutar = stok.sth_tutar ?? 0,
                        depo = sarfCikis.sth_cikis_depo_no ?? 0,
                        aciklama = $"Sarf Çıkış - {sarfCikis.birim_adi ?? ""}"
                    }, mikroTx);

                    // Stok detay güncelle
                    erpConn.Execute(
                        "UPDATE SarfCikisDepartmanBazliStoklar SET tamamlanan_adet = planlanan_adet WHERE Id = @Id",
                        new { stok.Id }, erpTx);
                }

                // Ana kayıt güncelle
                erpConn.Execute(@"
                    UPDATE SarfCikisDepartmanBazli
                    SET tamamlanan = 1,
                        onaylanan = 0,
                        tamamlayan_kisi_isim = @userName,
                        tamamlayan_kisi_kodu = @userNo,
                        tamamlanma_tarihi = @tarih
                    WHERE Id = @Id",
                    new { Id = id, userNo, userName, tarih = islemTarihi }, erpTx);

                erpTx.Commit();
                mikroTx.Commit();

                _logger.LogInformation($"Sarf çıkışı tamamlandı → ID: {id}, Evrak: {sarfCikis.sth_evrakno_seri}-{sarfCikis.sth_evrakno_sira}");
                return true;
            }
            catch (Exception ex)
            {
                erpTx?.Rollback();
                mikroTx?.Rollback();
                _logger.LogError(ex, "Sarf çıkışı tamamlanırken hata oluştu → ID: {id}", id);
                throw;
            }
        }

        #endregion

        #region Hiyerarşik Masraf Merkezi (Yeni Eklenen)

        // ANA GRUPLAR: xx-xx-00-000 şeklinde biten veya ana seviye olanlar
        public IEnumerable<dynamic> GetAnaMasrafMerkezleri()
        {
            var connStr = _dbSelectorService.GetConnectionString(); // MikroDB bağlantısı
            using var conn = new SqlConnection(connStr);

            const string query = @"
        SELECT DISTINCT 
            [msg_S_0078] AS AnaKod,
            [msg_S_0870] AS AnaIsim
        FROM [dbo].[HAREKET_GRUBU_2_CHOOSE_2]
        WHERE [msg_S_0078] LIKE '%-00-000'          -- ana gruplar genellikle böyle bitiyor
           OR [msg_S_0078] NOT LIKE '%-[0-9][0-9]-%' -- alt seviye olmayanlar
        ORDER BY [msg_S_0078]";

            return conn.Query(query);
        }

        // ALT GRUPLAR: Seçilen ana kodun prefix'ine uyanlar (ana kodun kendisi hariç)
        public IEnumerable<dynamic> GetAltMasrafMerkezleri(string anaKod)
        {
            var connStr = _dbSelectorService.GetConnectionString();
            using var conn = new SqlConnection(connStr);

            // Örnek: anaKod = '01-03-00-000' ise prefix '01-03-'
            string prefix = anaKod.Substring(0, anaKod.Length - 7); // son 7 karakteri (-00-000) kes

            const string query = @"
        SELECT 
            [msg_S_0078] AS Kod,
            [msg_S_0870] AS Isim
        FROM [dbo].[HAREKET_GRUBU_2_CHOOSE_2]
        WHERE [msg_S_0078] LIKE @Prefix + '%'
          AND [msg_S_0078] != @AnaKod               -- ana kodun kendisi olmasın
         
        ORDER BY [msg_S_0078]";

            return conn.Query(query, new { Prefix = prefix, AnaKod = anaKod });
        }
        public IEnumerable<dynamic> GetSorumlulukMerkezleri()
        {
            var connectionString = _dbSelectorService.GetConnectionString(); // MikroDB
            using var connection = new SqlConnection(connectionString);

            const string query = @"
        SELECT 
            msg_S_0078 AS Kod,
            msg_S_0870 AS Isim
        FROM SORUMLULUK_MERKEZLERI_CHOOSE_2
        WHERE msg_S_0078 IS NOT NULL
          AND LTRIM(RTRIM(msg_S_0078)) <> ''
        ORDER BY msg_S_0078";

            return connection.Query(query);
        }
        #endregion
    }
}