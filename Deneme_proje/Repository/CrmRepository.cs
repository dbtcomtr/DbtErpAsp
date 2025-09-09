using System.Data.SqlClient;
using Dapper;
using static Deneme_proje.Models.CrmEntities;

namespace Deneme_proje.Repository
{
    public class CrmRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;

        public CrmRepository(DatabaseSelectorService dbSelectorService)
        {
            _dbSelectorService = dbSelectorService;
        }

        private string ConnectionString => _dbSelectorService.GetConnectionString();

        #region Teklifler

        // Teklif listesi getir
        public IEnumerable<TeklifListeModel> GetTeklifler()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
        SELECT 
            MIN(vt.tkl_Guid) as tkl_Guid,
            CAST(vt.tkl_evrakno_sira AS VARCHAR) as TeklifNo,
            MIN(vt.tkl_Aciklama) as Konu,
            MIN(ch.cari_unvan1) as Kime,
            SUM(vt.tkl_Alisfiyati) as Toplam,
            MIN(CASE 
                WHEN ISDATE(vt.tkl_evrak_tarihi) = 1 
                THEN CAST(vt.tkl_evrak_tarihi AS DATETIME)
                ELSE GETDATE()
            END) as Tarih,
            MIN(CASE 
                WHEN ISDATE(vt.tkl_evrak_tarihi) = 1 
                THEN DATEADD(DAY, ISNULL(CAST(vt.tkl_Gecerlilik_Sures AS INT), 7), CAST(vt.tkl_evrak_tarihi AS DATETIME))
                ELSE DATEADD(DAY, ISNULL(CAST(vt.tkl_Gecerlilik_Sures AS INT), 7), GETDATE())
            END) as GecerlilikTarihi,
            '' as Etiketler,
            MIN(ISNULL(vt.tkl_create_date, GETDATE())) as OlusturmaTarihi,
            MIN(CASE 
                WHEN ISNULL(vt.tkl_durumu, '0') = '0' THEN 'Taslak'
                WHEN vt.tkl_durumu = '1' THEN 'Gönderildi'
                WHEN vt.tkl_durumu = '2' THEN 'Kazanıldı'
                WHEN vt.tkl_durumu = '3' THEN 'Kaybedildi'
                WHEN vt.tkl_durumu = '4' THEN 'Ertelendi'
                WHEN vt.tkl_durumu = '5' THEN 'İptal Edildi'
                ELSE 'Taslak'
            END) as Durum,
            MIN(vt.tkl_belge_no) as TeklifKonusu,
            STRING_AGG(s.sto_isim, ', ') as Urunler
        FROM VERILEN_TEKLIFLER vt
        LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
        LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
        GROUP BY vt.tkl_evrakno_sira
        ORDER BY MIN(ISNULL(vt.tkl_create_date, GETDATE())) DESC";

            return connection.Query<TeklifListeModel>(query);
        }

        // Dashboard istatistikleri için de aynı mapping
        public TeklifIstatistikleri GetTeklifIstatistikleri()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
        SELECT 
            COUNT(DISTINCT tkl_evrakno_sira) as ToplamTeklif,
            COUNT(DISTINCT CASE WHEN ISNULL(tkl_durumu, '0') IN ('0', '1') THEN tkl_evrakno_sira END) as AcikTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '2' THEN tkl_evrakno_sira END) as KazanilanTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '3' THEN tkl_evrakno_sira END) as KaybedilenTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '4' THEN tkl_evrakno_sira END) as ErtelelenTeklifler,
            COUNT(DISTINCT CASE WHEN tkl_durumu = '5' THEN tkl_evrakno_sira END) as IptalEdilenTeklifler,
            SUM(tkl_Alisfiyati) as ToplamTutar
        FROM VERILEN_TEKLIFLER
        WHERE (
            CASE 
                WHEN ISDATE(tkl_evrak_tarihi) = 1 
                THEN CAST(tkl_evrak_tarihi AS DATETIME)
                ELSE tkl_create_date
            END >= DATEADD(MONTH, -12, GETDATE())
        )";

            return connection.QuerySingle<TeklifIstatistikleri>(query);
        }

        // Teklif detay getir
        //public TeklifDetayModel GetTeklifDetay(int evrakSiraNo)
        //{
        //    using var connection = new SqlConnection(ConnectionString);

        //    var teklifQuery = @"
        //SELECT 
        //    vt.tkl_evrakno_sira,
        //    MIN(vt.tkl_cari_kod) as tkl_cari_kod,
        //    MIN(vt.tkl_evrak_tarihi) as tkl_evrak_tarihi,
        //    MIN(vt.tkl_belge_no) as tkl_belge_no,
        //    MIN(vt.tkl_Gecerlilik_Sures) as tkl_Gecerlilik_Sures,
        //    MIN(vt.tkl_Sorumlu_Kod) as tkl_Sorumlu_Kod,
        //    MIN(vt.tkl_Aciklama) as tkl_Aciklama,
        //    MIN(vt.tkl_durumu) as tkl_durumu,
        //    SUM(vt.tkl_Alisfiyati) as tkl_Alisfiyati,
        //    MIN(ch.cari_unvan1) as CariAdi,
        //    MIN(cp.cari_per_adi + ' ' + cp.cari_per_soyadi) as HazirlayanAdi
        //FROM VERILEN_TEKLIFLER vt
        //LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
        //LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON vt.tkl_Sorumlu_Kod = cp.cari_per_kod
        //WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
        //GROUP BY vt.tkl_evrakno_sira;

        //SELECT 
        //    vt.tkl_stok_kod as StokKod,
        //    s.sto_isim as StokAdi,
        //    vt.tkl_miktar as Miktar,
        //    vt.tkl_Alisfiyati as BirimFiyat,
        //    vt.tkl_Alisfiyati as IndirimliFiyat -- Adjust if you have a separate discount field
        //FROM VERILEN_TEKLIFLER vt
        //LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
        //WHERE vt.tkl_evrakno_sira = @EvrakSiraNo";

        //    using var multi = connection.QueryMultiple(teklifQuery, new { EvrakSiraNo = evrakSiraNo });
        //    var teklif = multi.ReadSingleOrDefault<TeklifDetayModel>();
        //    if (teklif != null)
        //    {
        //        teklif.Urunler = multi.Read<TeklifUrunModel>().ToList();
        //    }

        //    return teklif;
        //}
        public IEnumerable<TeklifUrunModel> GetTeklifSatirlari(int evrakSiraNo)
        {
            using var connection = new SqlConnection(ConnectionString);

            var query = @"
        SELECT 
            vt.tkl_stok_kod as StokKod,
            ISNULL(s.sto_isim, '') as StokAdi,
            vt.tkl_miktar as Miktar,
            vt.tkl_Alisfiyati as BirimFiyat,
            vt.tkl_Alisfiyati as IndirimliFiyat,
            (vt.tkl_miktar * vt.tkl_Alisfiyati) as Toplam,
            vt.tkl_satirno as SatirNo,
            ISNULL(vt.tkl_Aciklama, '') as Aciklama,
            id.Data as ImageData
        FROM VERILEN_TEKLIFLER vt
        LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
        ORDER BY vt.tkl_satirno";

            return connection.Query<TeklifUrunModel>(query, new { EvrakSiraNo = evrakSiraNo });
        }
        public bool YeniTeklifKaydet(YeniTeklifModel teklif)
        {
            using var connection = new SqlConnection(ConnectionString);
            try
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                var evrakSiraQuery = "SELECT ISNULL(MAX(tkl_evrakno_sira), 0) + 1 FROM VERILEN_TEKLIFLER";
                var yeniEvrakSira = connection.QuerySingle<int>(evrakSiraQuery, transaction: transaction);

                var durumKodu = "0"; // Taslak
                if (!int.TryParse(teklif.CreateUser, out int createUserId))
                {
                    createUserId = 1;
                }

                // Tarih alanlarını net olarak tanımla
                var evrakTarihi = teklif.Tarih.ToString("yyyy-MM-dd");
                var baslangicTarihi = teklif.BaslangicTarihi.ToString("yyyy-MM-dd");
                var bitisTarihi = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("yyyy-MM-dd");

                Console.WriteLine($"Tarih bilgileri - Evrak: {evrakTarihi}, Başlangıç: {baslangicTarihi}, Bitiş: {bitisTarihi}");

                var teklifQuery = @"
            INSERT INTO VERILEN_TEKLIFLER (
                tkl_Guid, tkl_DBCno, tkl_SpecRECno, tkl_iptal, tkl_fileid, tkl_hidden, 
                tkl_kilitli, tkl_degisti, tkl_checksum, tkl_create_user, tkl_create_date, 
                tkl_lastup_user, tkl_lastup_date, tkl_durumu, tkl_special2, tkl_special3,
                tkl_firmano, tkl_subeno, tkl_stok_kod, tkl_cari_kod, tkl_evrakno_seri,
                tkl_evrakno_sira, tkl_evrak_tarihi, tkl_satirno, tkl_belge_no, tkl_belge_tarih,
                tkl_asgari_miktar, tkl_teslimat_suresi, tkl_baslangic_tarihi, tkl_Gecerlilik_Sures,
                tkl_Brut_fiyat, tkl_Odeme_Plani, tkl_Alisfiyati, tkl_karorani, tkl_miktar,
                tkl_Aciklama, tkl_doviz_cins, tkl_doviz_kur, tkl_alt_doviz_kur, tkl_iskonto1,
                tkl_iskonto2, tkl_iskonto3, tkl_iskonto4, tkl_iskonto5, tkl_iskonto6,
                tkl_masraf1, tkl_masraf2, tkl_masraf3, tkl_masraf4, tkl_vergi_pntr,
                tkl_vergi, tkl_masraf_vergi_pnt, tkl_masraf_vergi, tkl_isk_mas1, TKL_ISK_MAS2,
                TKL_ISK_MAS3, TKL_ISK_MAS4, TKL_ISK_MAS5, TKL_ISK_MAS6, TKL_ISK_MAS7,
                TKL_ISK_MAS8, TKL_ISK_MAS9, TKL_ISK_MAS10, TKL_SAT_ISKMAS1, TKL_SAT_ISKMAS2,
                TKL_SAT_ISKMAS3, TKL_SAT_ISKMAS4, TKL_SAT_ISKMAS5, TKL_SAT_ISKMAS6, TKL_SAT_ISKMAS7,
                TKL_SAT_ISKMAS8, TKL_SAT_ISKMAS9, TKL_SAT_ISKMAS10, TKL_VERGISIZ_FL, TKL_KAPAT_FL,
                TKL_TESLIMTURU, tkl_ProjeKodu, tkl_Sorumlu_Kod, tkl_adres_no, tkl_yetkili_uid,
                tkl_special1, tkl_TedarikEdilecekCari, tkl_fiyat_liste_no, tkl_Birimfiyati,
                tkl_paket_kod, tkl_teslim_miktar, tkl_OnaylayanKulNo, tkl_cagrilabilir_fl,
                tkl_harekettipi, tkl_cari_sormerk, tkl_stok_sormerk, tkl_kapatmanedenkod,
                tkl_servisisemrikodu, tkl_birim_pntr, tkl_cari_tipi, tkl_HareketGrupKodu1,
                tkl_HareketGrupKodu2, tkl_HareketGrupKodu3, tkl_Olcu1, tkl_Olcu2,
                tkl_Olcu3, tkl_Olcu4, tkl_Olcu5, tkl_FormulMiktarNo, tkl_FormulMiktar,
                tkl_Tevkifat_turu, tkl_tevkifat_sifirlandi_fl
            ) VALUES (
                @TeklifGuid, 0, 0, 0, 100, 0,
                0, 0, 0, @CreateUserId, GETDATE(),
                @CreateUserId, GETDATE(), @DurumKodu, '', '',
                0, 0, @StokKod, @CariKod, '',
                @EvrakSira, @EvrakTarihi, @SatirNo, @BelgeNo, NULL,
                0, 0, @BaslangicTarihi, @BitisTarihi,
                @BrutFiyat, 0, @BirimFiyat, 0, @Miktar,
                @Aciklama, '', 1, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 4,
                0, 0, 0, 0, 1,
                1, 1, 1, 1, 1,
                1, 1, 1, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, '', @SorumluKod, 1, '00000000-0000-0000-0000-000000000000',
                0, '', 0, 0,
                '', 0, 0, @BirimPntr,
                0, '', '', '', '',
                @BirimPntr, 0, '', '', '',
                0, 0, 0, 0, 0,
                0, 0, 0, 0
            )";

                for (int i = 0; i < teklif.Urunler.Count; i++)
                {
                    var urun = teklif.Urunler[i];
                    var teklifGuid = Guid.NewGuid();

                    connection.Execute(teklifQuery, new
                    {
                        TeklifGuid = teklifGuid,
                        EvrakSira = yeniEvrakSira,
                        EvrakTarihi = evrakTarihi,         // tkl_evrak_tarihi
                        BaslangicTarihi = baslangicTarihi, // tkl_baslangic_tarihi
                        BitisTarihi = bitisTarihi,         // tkl_Gecerlilik_Sures (bitiş tarihi olarak)
                        SatirNo = i,
                        BelgeNo = teklif.FormNo,
                        StokKod = urun.StokKod,
                        CariKod = teklif.CariKod,
                        BrutFiyat = urun.BirimFiyat,
                        BirimFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat,
                        Miktar = urun.Miktar,
                        Aciklama = teklif.Aciklama ?? "",
                        DurumKodu = durumKodu,
                        CreateUserId = createUserId,
                        SorumluKod = teklif.SorumluKod ?? "",
                        BirimPntr = 1
                    }, transaction: transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return false;
            }
        }
        public bool TeklifGuncelle(int evrakSiraNo, YeniTeklifModel teklif)
        {
            using var connection = new SqlConnection(ConnectionString);

            try
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                // CreateUser'ı sayısala çevir
                if (!int.TryParse(teklif.CreateUser, out int updateUserId))
                {
                    updateUserId = 1;
                }

                // Tarih hesaplamaları
                var evrakTarihi = teklif.Tarih.ToString("yyyy-MM-dd");
                var baslangicTarihi = teklif.BaslangicTarihi.ToString("yyyy-MM-dd");
                var bitisTarihi = teklif.BaslangicTarihi.AddDays(teklif.GecerlilikSuresi).ToString("yyyy-MM-dd");

                // 1. Mevcut satırları güncelle (ortak bilgiler)
                var updateCommonQuery = @"
            UPDATE VERILEN_TEKLIFLER 
            SET 
                tkl_evrak_tarihi = @EvrakTarihi,
                tkl_baslangic_tarihi = @BaslangicTarihi,
                tkl_Gecerlilik_Sures = @BitisTarihi,
                tkl_cari_kod = @CariKod,
                tkl_belge_no = @FormNo,
                tkl_Sorumlu_Kod = @SorumluKod,
                tkl_Aciklama = @Aciklama,
                tkl_lastup_user = @UpdateUserId,
                tkl_lastup_date = GETDATE()
            WHERE tkl_evrakno_sira = @EvrakSiraNo";

                connection.Execute(updateCommonQuery, new
                {
                    EvrakSiraNo = evrakSiraNo,
                    EvrakTarihi = evrakTarihi,
                    BaslangicTarihi = baslangicTarihi,
                    BitisTarihi = bitisTarihi,
                    CariKod = teklif.CariKod,
                    FormNo = teklif.FormNo,
                    SorumluKod = teklif.SorumluKod ?? "",
                    Aciklama = teklif.Aciklama ?? "",
                    UpdateUserId = updateUserId
                }, transaction: transaction);

                // 2. Ürün satırlarını güncelle - sadece bunları sil ve yeniden ekle
                var deleteProductsQuery = "DELETE FROM VERILEN_TEKLIFLER WHERE tkl_evrakno_sira = @EvrakSiraNo";
                connection.Execute(deleteProductsQuery, new { EvrakSiraNo = evrakSiraNo }, transaction: transaction);

                // 3. Mevcut kayıttan bir tanesini template olarak al
                var templateQuery = @"
            SELECT TOP 1 * FROM VERILEN_TEKLIFLER 
            WHERE tkl_evrakno_sira = (
                SELECT TOP 1 tkl_evrakno_sira FROM VERILEN_TEKLIFLER 
                WHERE tkl_evrakno_sira != @EvrakSiraNo 
                ORDER BY tkl_create_date DESC
            )";

                var template = connection.QueryFirstOrDefault(templateQuery, new { EvrakSiraNo = evrakSiraNo }, transaction: transaction);

                // 4. Yeni ürün satırlarını ekle - template'teki diğer alanları koruyarak
                var insertQuery = @"
            INSERT INTO VERILEN_TEKLIFLER (
                tkl_Guid, tkl_DBCno, tkl_SpecRECno, tkl_iptal, tkl_fileid, tkl_hidden, 
                tkl_kilitli, tkl_degisti, tkl_checksum, tkl_create_user, tkl_create_date, 
                tkl_lastup_user, tkl_lastup_date, tkl_durumu, tkl_special2, tkl_special3,
                tkl_firmano, tkl_subeno, tkl_stok_kod, tkl_cari_kod, tkl_evrakno_seri,
                tkl_evrakno_sira, tkl_evrak_tarihi, tkl_satirno, tkl_belge_no, tkl_belge_tarih,
                tkl_asgari_miktar, tkl_teslimat_suresi, tkl_baslangic_tarihi, tkl_Gecerlilik_Sures,
                tkl_Brut_fiyat, tkl_Odeme_Plani, tkl_Alisfiyati, tkl_karorani, tkl_miktar,
                tkl_Aciklama, tkl_doviz_cins, tkl_doviz_kur, tkl_alt_doviz_kur, tkl_iskonto1,
                tkl_iskonto2, tkl_iskonto3, tkl_iskonto4, tkl_iskonto5, tkl_iskonto6,
                tkl_masraf1, tkl_masraf2, tkl_masraf3, tkl_masraf4, tkl_vergi_pntr,
                tkl_vergi, tkl_masraf_vergi_pnt, tkl_masraf_vergi, tkl_isk_mas1, TKL_ISK_MAS2,
                TKL_ISK_MAS3, TKL_ISK_MAS4, TKL_ISK_MAS5, TKL_ISK_MAS6, TKL_ISK_MAS7,
                TKL_ISK_MAS8, TKL_ISK_MAS9, TKL_ISK_MAS10, TKL_SAT_ISKMAS1, TKL_SAT_ISKMAS2,
                TKL_SAT_ISKMAS3, TKL_SAT_ISKMAS4, TKL_SAT_ISKMAS5, TKL_SAT_ISKMAS6, TKL_SAT_ISKMAS7,
                TKL_SAT_ISKMAS8, TKL_SAT_ISKMAS9, TKL_SAT_ISKMAS10, TKL_VERGISIZ_FL, TKL_KAPAT_FL,
                TKL_TESLIMTURU, tkl_ProjeKodu, tkl_Sorumlu_Kod, tkl_adres_no, tkl_yetkili_uid,
                tkl_special1, tkl_TedarikEdilecekCari, tkl_fiyat_liste_no, tkl_Birimfiyati,
                tkl_paket_kod, tkl_teslim_miktar, tkl_OnaylayanKulNo, tkl_cagrilabilir_fl,
                tkl_harekettipi, tkl_cari_sormerk, tkl_stok_sormerk, tkl_kapatmanedenkod,
                tkl_servisisemrikodu, tkl_birim_pntr, tkl_cari_tipi, tkl_HareketGrupKodu1,
                tkl_HareketGrupKodu2, tkl_HareketGrupKodu3, tkl_Olcu1, tkl_Olcu2,
                tkl_Olcu3, tkl_Olcu4, tkl_Olcu5, tkl_FormulMiktarNo, tkl_FormulMiktar,
                tkl_Tevkifat_turu, tkl_tevkifat_sifirlandi_fl
            ) VALUES (
                @TeklifGuid, ISNULL(@DBCno, 0), ISNULL(@SpecRECno, 0), ISNULL(@iptal, 0), 
                ISNULL(@fileid, 100), ISNULL(@hidden, 0), ISNULL(@kilitli, 0), ISNULL(@degisti, 0), 
                ISNULL(@checksum, 0), @CreateUserId, GETDATE(), @UpdateUserId, GETDATE(), 
                ISNULL(@durumu, '0'), ISNULL(@special2, ''), ISNULL(@special3, ''),
                ISNULL(@firmano, 0), ISNULL(@subeno, 0), @StokKod, @CariKod, ISNULL(@evrakno_seri, ''),
                @EvrakSira, @EvrakTarihi, @SatirNo, @BelgeNo, @belge_tarih,
                ISNULL(@asgari_miktar, 0), ISNULL(@teslimat_suresi, 0), @BaslangicTarihi, @BitisTarihi,
                @BrutFiyat, ISNULL(@Odeme_Plani, 0), @BirimFiyat, ISNULL(@karorani, 0), @Miktar,
                @Aciklama, ISNULL(@doviz_cins, ''), ISNULL(@doviz_kur, 1), ISNULL(@alt_doviz_kur, 0), 
                ISNULL(@iskonto1, 0), ISNULL(@iskonto2, 0), ISNULL(@iskonto3, 0), ISNULL(@iskonto4, 0), 
                ISNULL(@iskonto5, 0), ISNULL(@iskonto6, 0), ISNULL(@masraf1, 0), ISNULL(@masraf2, 0), 
                ISNULL(@masraf3, 0), ISNULL(@masraf4, 0), ISNULL(@vergi_pntr, 4), ISNULL(@vergi, 0), 
                ISNULL(@masraf_vergi_pnt, 0), ISNULL(@masraf_vergi, 0), ISNULL(@isk_mas1, 0), 
                ISNULL(@ISK_MAS2, 1), ISNULL(@ISK_MAS3, 1), ISNULL(@ISK_MAS4, 1), ISNULL(@ISK_MAS5, 1), 
                ISNULL(@ISK_MAS6, 1), ISNULL(@ISK_MAS7, 1), ISNULL(@ISK_MAS8, 1), ISNULL(@ISK_MAS9, 1), 
                ISNULL(@ISK_MAS10, 1), ISNULL(@SAT_ISKMAS1, 0), ISNULL(@SAT_ISKMAS2, 0), 
                ISNULL(@SAT_ISKMAS3, 0), ISNULL(@SAT_ISKMAS4, 0), ISNULL(@SAT_ISKMAS5, 0), 
                ISNULL(@SAT_ISKMAS6, 0), ISNULL(@SAT_ISKMAS7, 0), ISNULL(@SAT_ISKMAS8, 0), 
                ISNULL(@SAT_ISKMAS9, 0), ISNULL(@SAT_ISKMAS10, 0), ISNULL(@VERGISIZ_FL, 0), 
                ISNULL(@KAPAT_FL, 0), ISNULL(@TESLIMTURU, ''), ISNULL(@ProjeKodu, ''), @SorumluKod, 
                ISNULL(@adres_no, 1), ISNULL(@yetkili_uid, '00000000-0000-0000-0000-000000000000'),
                ISNULL(@special1, 0), ISNULL(@TedarikEdilecekCari, ''), ISNULL(@fiyat_liste_no, 0), 
                ISNULL(@Birimfiyati, 0), ISNULL(@paket_kod, ''), ISNULL(@teslim_miktar, 0), 
                ISNULL(@OnaylayanKulNo, 0), ISNULL(@cagrilabilir_fl, @BirimPntr), ISNULL(@harekettipi, 0), 
                ISNULL(@cari_sormerk, ''), ISNULL(@stok_sormerk, ''), ISNULL(@kapatmanedenkod, ''), 
                ISNULL(@servisisemrikodu, ''), @BirimPntr, ISNULL(@cari_tipi, 0), ISNULL(@HareketGrupKodu1, ''), 
                ISNULL(@HareketGrupKodu2, ''), ISNULL(@HareketGrupKodu3, ''), ISNULL(@Olcu1, 0), 
                ISNULL(@Olcu2, 0), ISNULL(@Olcu3, 0), ISNULL(@Olcu4, 0), ISNULL(@Olcu5, 0), 
                ISNULL(@FormulMiktarNo, 0), ISNULL(@FormulMiktar, 0), ISNULL(@Tevkifat_turu, 0), 
                ISNULL(@tevkifat_sifirlandi_fl, 0)
            )";

                // Ürün satırları için döngü
                for (int i = 0; i < teklif.Urunler.Count; i++)
                {
                    var urun = teklif.Urunler[i];
                    var teklifGuid = Guid.NewGuid();

                    // Template'ten tüm değerleri al, sadece gerekenleri override et
                    connection.Execute(insertQuery, new
                    {
                        TeklifGuid = teklifGuid,
                        EvrakSira = evrakSiraNo,
                        EvrakTarihi = evrakTarihi,
                        BaslangicTarihi = baslangicTarihi,
                        BitisTarihi = bitisTarihi,
                        SatirNo = i,
                        BelgeNo = teklif.FormNo,
                        StokKod = urun.StokKod,
                        CariKod = teklif.CariKod,
                        BrutFiyat = urun.BirimFiyat,
                        BirimFiyat = urun.IndirimliFiyat > 0 ? urun.IndirimliFiyat : urun.BirimFiyat,
                        Miktar = urun.Miktar,
                        Aciklama = teklif.Aciklama ?? "",
                        SorumluKod = teklif.SorumluKod ?? "",
                        CreateUserId = updateUserId,
                        UpdateUserId = updateUserId,
                        BirimPntr = 1,

                        // Template'ten gelen değerler (null değilse kullan, yoksa varsayılan)
                        DBCno = template?.tkl_DBCno ?? 0,
                        SpecRECno = template?.tkl_SpecRECno ?? 0,
                        iptal = template?.tkl_iptal ?? 0,
                        fileid = template?.tkl_fileid ?? 100,
                        hidden = template?.tkl_hidden ?? 0,
                        kilitli = template?.tkl_kilitli ?? 0,
                        degisti = template?.tkl_degisti ?? 0,
                        checksum = template?.tkl_checksum ?? 0,
                        durumu = "0", // Daima taslak
                        special2 = template?.tkl_special2 ?? "",
                        special3 = template?.tkl_special3 ?? "",
                        firmano = template?.tkl_firmano ?? 0,
                        subeno = template?.tkl_subeno ?? 0,
                        evrakno_seri = template?.tkl_evrakno_seri ?? "",
                        belge_tarih = template?.tkl_belge_tarih,
                        asgari_miktar = template?.tkl_asgari_miktar ?? 0,
                        teslimat_suresi = template?.tkl_teslimat_suresi ?? 0,
                        Odeme_Plani = template?.tkl_Odeme_Plani ?? 0,
                        karorani = template?.tkl_karorani ?? 0,
                        doviz_cins = template?.tkl_doviz_cins ?? "",
                        doviz_kur = template?.tkl_doviz_kur ?? 1,
                        alt_doviz_kur = template?.tkl_alt_doviz_kur ?? 0,
                        iskonto1 = template?.tkl_iskonto1 ?? 0,
                        iskonto2 = template?.tkl_iskonto2 ?? 0,
                        iskonto3 = template?.tkl_iskonto3 ?? 0,
                        iskonto4 = template?.tkl_iskonto4 ?? 0,
                        iskonto5 = template?.tkl_iskonto5 ?? 0,
                        iskonto6 = template?.tkl_iskonto6 ?? 0,
                        masraf1 = template?.tkl_masraf1 ?? 0,
                        masraf2 = template?.tkl_masraf2 ?? 0,
                        masraf3 = template?.tkl_masraf3 ?? 0,
                        masraf4 = template?.tkl_masraf4 ?? 0,
                        vergi_pntr = template?.tkl_vergi_pntr ?? 4,
                        vergi = template?.tkl_vergi ?? 0,
                        masraf_vergi_pnt = template?.tkl_masraf_vergi_pnt ?? 0,
                        masraf_vergi = template?.tkl_masraf_vergi ?? 0,
                        isk_mas1 = template?.tkl_isk_mas1 ?? 0,
                        ISK_MAS2 = template?.TKL_ISK_MAS2 ?? 1,
                        ISK_MAS3 = template?.TKL_ISK_MAS3 ?? 1,
                        ISK_MAS4 = template?.TKL_ISK_MAS4 ?? 1,
                        ISK_MAS5 = template?.TKL_ISK_MAS5 ?? 1,
                        ISK_MAS6 = template?.TKL_ISK_MAS6 ?? 1,
                        ISK_MAS7 = template?.TKL_ISK_MAS7 ?? 1,
                        ISK_MAS8 = template?.TKL_ISK_MAS8 ?? 1,
                        ISK_MAS9 = template?.TKL_ISK_MAS9 ?? 1,
                        ISK_MAS10 = template?.TKL_ISK_MAS10 ?? 1,
                        SAT_ISKMAS1 = template?.TKL_SAT_ISKMAS1 ?? 0,
                        SAT_ISKMAS2 = template?.TKL_SAT_ISKMAS2 ?? 0,
                        SAT_ISKMAS3 = template?.TKL_SAT_ISKMAS3 ?? 0,
                        SAT_ISKMAS4 = template?.TKL_SAT_ISKMAS4 ?? 0,
                        SAT_ISKMAS5 = template?.TKL_SAT_ISKMAS5 ?? 0,
                        SAT_ISKMAS6 = template?.TKL_SAT_ISKMAS6 ?? 0,
                        SAT_ISKMAS7 = template?.TKL_SAT_ISKMAS7 ?? 0,
                        SAT_ISKMAS8 = template?.TKL_SAT_ISKMAS8 ?? 0,
                        SAT_ISKMAS9 = template?.TKL_SAT_ISKMAS9 ?? 0,
                        SAT_ISKMAS10 = template?.TKL_SAT_ISKMAS10 ?? 0,
                        VERGISIZ_FL = template?.TKL_VERGISIZ_FL ?? 0,
                        KAPAT_FL = template?.TKL_KAPAT_FL ?? 0,
                        TESLIMTURU = template?.TKL_TESLIMTURU ?? "",
                        ProjeKodu = template?.tkl_ProjeKodu ?? "",
                        adres_no = template?.tkl_adres_no ?? 1,
                        yetkili_uid = template?.tkl_yetkili_uid ?? Guid.Empty,
                        special1 = template?.tkl_special1 ?? 0,
                        TedarikEdilecekCari = template?.tkl_TedarikEdilecekCari ?? "",
                        fiyat_liste_no = template?.tkl_fiyat_liste_no ?? 0,
                        Birimfiyati = template?.tkl_Birimfiyati ?? 0,
                        paket_kod = template?.tkl_paket_kod ?? "",
                        teslim_miktar = template?.tkl_teslim_miktar ?? 0,
                        OnaylayanKulNo = template?.tkl_OnaylayanKulNo ?? 0,
                        cagrilabilir_fl = template?.tkl_cagrilabilir_fl ?? 1,
                        harekettipi = template?.tkl_harekettipi ?? 0,
                        cari_sormerk = template?.tkl_cari_sormerk ?? "",
                        stok_sormerk = template?.tkl_stok_sormerk ?? "",
                        kapatmanedenkod = template?.tkl_kapatmanedenkod ?? "",
                        servisisemrikodu = template?.tkl_servisisemrikodu ?? "",
                        cari_tipi = template?.tkl_cari_tipi ?? 0,
                        HareketGrupKodu1 = template?.tkl_HareketGrupKodu1 ?? "",
                        HareketGrupKodu2 = template?.tkl_HareketGrupKodu2 ?? "",
                        HareketGrupKodu3 = template?.tkl_HareketGrupKodu3 ?? "",
                        Olcu1 = template?.tkl_Olcu1 ?? 0,
                        Olcu2 = template?.tkl_Olcu2 ?? 0,
                        Olcu3 = template?.tkl_Olcu3 ?? 0,
                        Olcu4 = template?.tkl_Olcu4 ?? 0,
                        Olcu5 = template?.tkl_Olcu5 ?? 0,
                        FormulMiktarNo = template?.tkl_FormulMiktarNo ?? 0,
                        FormulMiktar = template?.tkl_FormulMiktar ?? 0,
                        Tevkifat_turu = template?.tkl_Tevkifat_turu ?? 0,
                        tevkifat_sifirlandi_fl = template?.tkl_tevkifat_sifirlandi_fl ?? 0
                    }, transaction: transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return false;
            }
        }
        #endregion
        // CrmRepository.cs dosyasındaki TeklifGuncelle metodunu güncelleyin ve GetTeklifDetay metodunu geliştirin

        #region Teklif Güncelleme Metodları

        // Mevcut GetTeklifDetay metodunu güncelleyin
        public TeklifDetayModel GetTeklifDetay(int evrakSiraNo)
        {
            using var connection = new SqlConnection(ConnectionString);

            var teklifQuery = @"
    SELECT 
        MIN(vt.tkl_evrakno_sira) as tkl_evrakno_sira,
        MIN(vt.tkl_cari_kod) as tkl_cari_kod,
        -- ✅ Tarihleri düzgün formatla
        CONVERT(VARCHAR(10), MIN(vt.tkl_evrak_tarihi), 23) as tkl_evrak_tarihi,
        MIN(vt.tkl_belge_no) as tkl_belge_no,
        CONVERT(VARCHAR(10), MIN(vt.tkl_baslangic_tarihi), 23) as tkl_baslangic_tarihi,
        CONVERT(VARCHAR(10), MIN(vt.tkl_Gecerlilik_Sures), 23) as tkl_Gecerlilik_Sures,
        MIN(vt.tkl_Sorumlu_Kod) as tkl_Sorumlu_Kod,
        MIN(vt.tkl_Aciklama) as tkl_Aciklama,
        MIN(vt.tkl_durumu) as tkl_durumu,
        SUM(vt.tkl_miktar * vt.tkl_Alisfiyati) as tkl_Alisfiyati,
        MIN(ch.cari_unvan1) as CariAdi,
        MIN(ISNULL(cp.cari_per_adi, '') + ' ' + ISNULL(cp.cari_per_soyadi, '')) as HazirlayanAdi
    FROM VERILEN_TEKLIFLER vt
    LEFT JOIN CARI_HESAPLAR ch ON vt.tkl_cari_kod = ch.cari_kod
    LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON vt.tkl_Sorumlu_Kod = cp.cari_per_kod
    WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
    GROUP BY vt.tkl_evrakno_sira";

            var teklifSatirQuery = @"
        SELECT 
            vt.tkl_stok_kod as StokKod,
            ISNULL(s.sto_isim, '') as StokAdi,
            vt.tkl_miktar as Miktar,
            vt.tkl_Alisfiyati as BirimFiyat,
            vt.tkl_Alisfiyati as IndirimliFiyat,
            (vt.tkl_miktar * vt.tkl_Alisfiyati) as Toplam,
            vt.tkl_satirno as SatirNo,
            ISNULL(vt.tkl_Aciklama, '') as Aciklama,
            id.Data as ImageData
        FROM VERILEN_TEKLIFLER vt
        LEFT JOIN STOKLAR s ON vt.tkl_stok_kod = s.sto_kod
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE vt.tkl_evrakno_sira = @EvrakSiraNo
        ORDER BY vt.tkl_satirno";

            using var multi = connection.QueryMultiple($"{teklifQuery}; {teklifSatirQuery}", new { EvrakSiraNo = evrakSiraNo });

            var teklif = multi.ReadSingleOrDefault<TeklifDetayModel>();
            if (teklif != null)
            {
                teklif.Urunler = multi.Read<TeklifUrunModel>().ToList();
            }

            return teklif;
        }

        // TeklifGuncelle metodunu tamamen yeniden yazın


        #endregion
        #region Cari Hesaplar

        // Cari hesap listesi
        public IEnumerable<CariHesapModel> GetCariHesaplar()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    ch.cari_kod as CariKod,
                    ch.cari_unvan1 as CariAdi,
                    ch.cari_temsilci_kodu as TemsilciKodu,
                    ch.cari_sektor_kodu as SektorKodu,
                    ch.cari_grup_kodu as GrupKodu,
                    ch.cari_EMail as Email,
                    ch.cari_CepTel as Telefon
                FROM CARI_HESAPLAR ch
              ";

            return connection.Query<CariHesapModel>(query);
        }

        // Cari hesap detay
        public CariHesapDetayModel GetCariHesapDetay(string cariKod)
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    ch.*,
                    cp.cari_per_adi + ' ' + cp.cari_per_soyadi as TemsilciAdi
                FROM CARI_HESAPLAR ch
                LEFT JOIN CARI_PERSONEL_TANIMLARI cp ON ch.cari_temsilci_kodu = cp.cari_per_kod
                WHERE ch.cari_kod = @CariKod";

            return connection.QueryFirstOrDefault<CariHesapDetayModel>(query, new { CariKod = cariKod });
        }

        // Cari temsilci koduna göre personel getir
        public IEnumerable<PersonelModel> GetCariPersonelleri(string cariKod)
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT DISTINCT
                    cp.cari_per_kod as PersonelKod,
                    cp.cari_per_adi + ' ' + cp.cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI cp
                INNER JOIN CARI_HESAPLAR ch ON cp.cari_per_kod = ch.cari_temsilci_kodu
                WHERE ch.cari_kod = @CariKod
                
                UNION
                
                SELECT 
                    cpt.cari_per_kod as PersonelKod,
                    cpt.cari_per_adi + ' ' + cpt.cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI cpt";

            return connection.Query<PersonelModel>(query, new { CariKod = cariKod });
        }

        #endregion

        #region Stoklar

        // Stok listesi - STOKLAR tablosundan
        // Updated GetStoklar method - only return basic stock info
        public IEnumerable<StokModel> GetStoklar()
        {
            using var connection = new SqlConnection(ConnectionString);

            // Önce hangi JOIN yönteminin çalıştığını test edelim
            var testQuery = @"
        SELECT COUNT(*) as TestCount
        FROM STOKLAR s
        INNER JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE s.sto_pasif_fl = 0 AND s.sto_iptal = 0";

            try
            {
                var testCount = connection.QuerySingle<int>(testQuery);
                Console.WriteLine($"GUID eşleştirme test sonucu: {testCount} kayıt bulundu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GUID test hatası: {ex.Message}");
            }

            // Ana sorgu - GUID formatını temizleyerek eşleştir
            var query = @"
        SELECT 
            s.sto_Guid As StukGuid,
            s.sto_kod AS StokKod,
            s.sto_isim AS StokAdi,
            s.sto_kisa_ismi AS KisaIsim,
            s.sto_birim1_ad AS Birim1,
            s.sto_birim1_katsayi AS Birim1Katsayi,
            s.sto_anagrup_kod AS AnaGrupKod,
            id.Data AS ImageData
        FROM STOKLAR s
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON UPPER(REPLACE(CAST(s.sto_guid AS VARCHAR(50)), '-', '')) = 
               UPPER(REPLACE(CAST(id.Record_uid AS VARCHAR(50)), '-', ''))
        WHERE s.sto_pasif_fl = 0 
            AND s.sto_iptal = 0
        ORDER BY s.sto_isim";

            var result = connection.Query<StokModel>(query).ToList();

            // Log için fotoğraflı stok sayısını say
            var fotografliStokSayisi = result.Count(s => s.ImageData != null && s.ImageData.Length > 0);
            Console.WriteLine($"Repository GetStoklar: {result.Count} stok, {fotografliStokSayisi} tanesi fotoğraflı");

            return result;
        }

        // Alternatif method - eğer yukarıdaki çalışmazsa bu deneyin
        public IEnumerable<StokModel> GetStoklar_Alternative()
        {
            using var connection = new SqlConnection(ConnectionString);

            // Farklı JOIN yöntemleri dene
            var query = @"
        SELECT 
            s.sto_Guid As StukGuid,
            s.sto_kod AS StokKod,
            s.sto_isim AS StokAdi,
            s.sto_kisa_ismi AS KisaIsim,
            s.sto_birim1_ad AS Birim1,
            s.sto_birim1_katsayi AS Birim1Katsayi,
            s.sto_anagrup_kod AS AnaGrupKod,
            id.Data AS ImageData
        FROM STOKLAR s
        LEFT JOIN [dbo].[mye_ImageData] id 
            ON (
                s.sto_guid = id.Record_uid OR
                CAST(s.sto_guid AS VARCHAR(50)) = CAST(id.Record_uid AS VARCHAR(50)) OR
                UPPER(LTRIM(RTRIM(CAST(s.sto_guid AS VARCHAR(50))))) = UPPER(LTRIM(RTRIM(CAST(id.Record_uid AS VARCHAR(50)))))
            )
        WHERE s.sto_pasif_fl = 0 
            AND s.sto_iptal = 0
        ORDER BY s.sto_isim";

            return connection.Query<StokModel>(query);
        }

        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        // Updated method to get price from STOK_SATIS_FIYAT_LISTELERI table
        public decimal GetStokSatisFiyati(string StokKod, int listeSiraNo = 1, int dovizCinsi = 0)
        {
            using var connection = new SqlConnection(ConnectionString);

            var fiyatQuery = @"
        SELECT TOP 1 sfiyat_fiyati 
        FROM STOK_SATIS_FIYAT_LISTELERI 
        WHERE sfiyat_stokkod = @StokKod 
            AND sfiyat_listesirano = @ListeSiraNo
            AND sfiyat_doviz = @DovizCinsi
            AND sfiyat_iptal = 0
        ORDER BY sfiyat_create_date DESC";

            var fiyat = connection.QueryFirstOrDefault<decimal?>(fiyatQuery, new
            {
                StokKod = StokKod,
                ListeSiraNo = listeSiraNo,
                DovizCinsi = dovizCinsi
            });

            return fiyat ?? 0;
        }

        #endregion

        #region Lookup Metodları

        // Form numarası üret
        public string GetYeniFormNumarasi()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT RIGHT('000000' + CAST(ISNULL(MAX(tkl_evrakno_sira), 0) + 1 AS VARCHAR), 6)
                FROM VERILEN_TEKLIFLER";

            return connection.QuerySingle<string>(query);
        }

        // Personel listesi
        public IEnumerable<PersonelModel> GetPersoneller()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    cari_per_kod as PersonelKod,
                    cari_per_adi + ' ' + cari_per_soyadi as PersonelAdi
                FROM CARI_PERSONEL_TANIMLARI
                ORDER BY cari_per_adi, cari_per_soyadi";

            return connection.Query<PersonelModel>(query);
        }

        // Durumlar
        public IEnumerable<string> GetTeklifDurumlari()
        {
            return new List<string>
            {
                "Taslak",
                "Gönderildi",
                "Kazanıldı",
                "Kaybedildi",
                "Ertelendi",
                "İptal Edildi"
            };
        }



        #endregion

        #region Dashboard & İstatistikler

        // Dashboard için teklif istatistikleri

        // Aylık teklif grafiği için veri
        public IEnumerable<AylikTeklifGrafik> GetAylikTeklifGrafigi()
        {
            using var connection = new SqlConnection(ConnectionString);
            var query = @"
                SELECT 
                    YEAR(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ) as Yil,
                    MONTH(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ) as Ay,
                    COUNT(*) as TeklifSayisi,
                    SUM(ISNULL(tkl_Alisfiyati, 0)) as ToplamTutar
                FROM VERILEN_TEKLIFLER
                WHERE (
                    CASE 
                        WHEN ISDATE(tkl_evrak_tarihi) = 1 
                        THEN CAST(tkl_evrak_tarihi AS DATETIME)
                        ELSE tkl_create_date
                    END >= DATEADD(MONTH, -12, GETDATE())
                )
                GROUP BY 
                    YEAR(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    ), 
                    MONTH(
                        CASE 
                            WHEN ISDATE(tkl_evrak_tarihi) = 1 
                            THEN CAST(tkl_evrak_tarihi AS DATETIME)
                            ELSE tkl_create_date
                        END
                    )
                ORDER BY Yil, Ay";

            return connection.Query<AylikTeklifGrafik>(query);
        }

        #endregion
    }
}