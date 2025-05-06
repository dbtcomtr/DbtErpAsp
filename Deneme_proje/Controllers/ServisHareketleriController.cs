using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;
using static Deneme_proje.Models.ServisEntities;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class ServisHareketleriController : Controller
    {
        private readonly IConfiguration _configuration;

        public ServisHareketleriController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetNextEvrakNo(string servisMerkezi)
        {
            if (string.IsNullOrEmpty(servisMerkezi))
            {
                return BadRequest("Servis Merkezi belirtilmedi.");
            }

            string connectionString = _configuration.GetConnectionString("ERPDatabase");
            int nextNo;

            string query = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 
                     FROM ServisHareketleri 
                     WHERE Servis_Merkezi = @ServisMerkezi";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            return Json(new { nextEvrakNo = nextNo });
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetEvrak(int evrakNo)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            var evrak = new
            {
                EvrakNo = 0,
                Tarih = "",
                MusteriAdi = "",
                PlakaNo = "",
                UniteSeriNo = "",
                CihazMarkaModel = "",
                CalismaSaati = 0,
                ServiseGirisTarihi = "",
                IlkGozlem = "",
                YedekParcaNo = "",
                YedekParcaAdi = "",
                Adet = "",
                BirimFiyat = "",
                Tutar = ""
            };

            string query = @"SELECT Evrak_No, Tarih, Musteri_Adi, Plaka_No,UniteSeriNo, Cihaz_Marka_Model, 
                     Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem, 
                     Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar
                     FROM ServisHareketleri
                     WHERE Evrak_No = @EvrakNo";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (evrak.EvrakNo == 0)
                    {
                        evrak = new
                        {
                            EvrakNo = (int)reader["Evrak_No"],
                            Tarih = ((DateTime)reader["Tarih"]).ToString("yyyy-MM-dd"),
                            MusteriAdi = reader["Musteri_Adi"].ToString(),
                            PlakaNo = reader["Plaka_No"].ToString(),
                            UniteSeriNo = reader["UniteSeriNo"].ToString(),
                            CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                            CalismaSaati = (int)reader["Calisma_Saati"],
                            ServiseGirisTarihi = ((DateTime)reader["Servise_Giris_Tarihi"]).ToString("yyyy-MM-dd"),
                            IlkGozlem = reader["Ilk_Gozlem"].ToString(),
                            YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                            YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                            Adet = reader["Adet"].ToString(),
                            BirimFiyat = reader["Birim_Fiyat"].ToString(),
                            Tutar = reader["Tutar"].ToString()
                        };
                    }
                }
                con.Close();
            }

            return Json(evrak);
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetEvrakDetayFull(int evrakNo, string servisMerkezi)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Ana evrak bilgilerini ve detaylarını içerecek bir dictionary
                var evrakDetay = new Dictionary<string, object>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Ana evrak bilgilerini çeken detaylı SQL sorgusu - servisMerkezi parametresi eklendi
                    string queryEvrak = @"
        SELECT
            Evrak_No, 
            Tarih, 
            Servis_Merkezi, 
            No, 
            Musteri_Adi, 
            Sofor_Adi, 
            Sofor_Telefon, 
            Plaka_No,
            UniteSeriNo,
            Cihaz_Marka_Model, 
            Teknisyen_Adi, 
            Calisma_Saati, 
            Servise_Giris_Tarihi, 
            is_turu, 
            Ilk_Gozlem,
            A_Toplam,
            Vergi,
            Birim_Fiyat, 
            Tutar, 
            iscilik_tutari, 
            harici_iscilik_tutari,
IskontoYuzde,
            G_Toplam
        FROM ServisHareketleri 
        WHERE Evrak_No = @EvrakNo AND Servis_Merkezi = @ServisMerkezi AND Evrak_Sira_No = 1";

                    using (SqlCommand cmdEvrak = new SqlCommand(queryEvrak, con))
                    {
                        cmdEvrak.Parameters.AddWithValue("@EvrakNo", evrakNo);
                        cmdEvrak.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);

                        using (SqlDataReader reader = cmdEvrak.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Ana evrak bilgilerini dictionary'e ekle
                                evrakDetay["evrakNo"] = SafeGetString(reader, "Evrak_No");
                                evrakDetay["tarih"] = reader["Tarih"] != DBNull.Value
                                    ? ((DateTime)reader["Tarih"]).ToString("yyyy-MM-dd")
                                    : "";
                                evrakDetay["servisMerkezi"] = SafeGetString(reader, "Servis_Merkezi");
                                evrakDetay["no"] = SafeGetString(reader, "No");
                                evrakDetay["musteriAdi"] = SafeGetString(reader, "Musteri_Adi");
                                evrakDetay["soforAdi"] = SafeGetString(reader, "Sofor_Adi");
                                evrakDetay["soforTelefon"] = SafeGetString(reader, "Sofor_Telefon");
                                evrakDetay["plakaNo"] = SafeGetString(reader, "Plaka_No");
                                evrakDetay["UniteSeriNo"] = SafeGetString(reader, "UniteSeriNo");
                                evrakDetay["cihazMarkaModel"] = SafeGetString(reader, "Cihaz_Marka_Model");
                                evrakDetay["teknisyenAdi"] = SafeGetString(reader, "Teknisyen_Adi");
                                evrakDetay["calismaSaati"] = SafeGetString(reader, "Calisma_Saati");
                                evrakDetay["serviseGirisTarihi"] = reader["Servise_Giris_Tarihi"] != DBNull.Value
                                    ? ((DateTime)reader["Servise_Giris_Tarihi"]).ToString("yyyy-MM-dd")
                                    : "";
                                evrakDetay["isTuru"] = SafeGetString(reader, "is_turu");
                                evrakDetay["ilkGozlem"] = SafeGetString(reader, "Ilk_Gozlem");
                                evrakDetay["aToplam"] = SafeGetDecimal(reader, "A_Toplam");
                                evrakDetay["vergi"] = SafeGetDecimal(reader, "Vergi");
                                evrakDetay["gToplam"] = SafeGetDecimal(reader, "G_Toplam");
                                evrakDetay["birimFiyat"] = SafeGetString(reader, "Birim_Fiyat");
                                evrakDetay["tutar"] = SafeGetString(reader, "Tutar");
                                evrakDetay["iscilikTutari"] = SafeGetString(reader, "iscilik_tutari");
                                evrakDetay["hariciIscilikTutari"] = SafeGetString(reader, "harici_iscilik_tutari");
                                evrakDetay["IskontoYuzde"] = SafeGetString(reader, "IskontoYuzde");
                            }
                            else
                            {
                                // Ana kayıt bulunamadıysa hata döndür
                                return Json(new
                                {
                                    success = false,
                                    message = $"Evrak No {evrakNo}, Servis Merkezi {servisMerkezi} için kayıt bulunamadı."
                                });
                            }
                        }
                    }

                    // Detay kayıtlarını çeken SQL sorgusu - ServisMerkezi parametresi direkt evrakDetay'den alınıyor
                    string queryDetay = @"
        SELECT 
            Evrak_No,
            Evrak_Sira_No,
            Yedek_Parca_No, 
            Yedek_Parca_Adi,
            Servis_Merkezi, 
            Adet, 
            Birim_Fiyat, 
            Tutar, 
            iscilik_tutari, 
            harici_iscilik_tutari,
IskontoYuzde
        FROM ServisHareketleri 
        WHERE Evrak_No = @EvrakNo 
        AND Servis_Merkezi = @ServisMerkezi
        ORDER BY Evrak_Sira_No";

                    using (SqlCommand cmdDetay = new SqlCommand(queryDetay, con))
                    {
                        cmdDetay.Parameters.AddWithValue("@EvrakNo", evrakNo);
                        cmdDetay.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);

                        using (SqlDataReader reader = cmdDetay.ExecuteReader())
                        {
                            var stokHizmetListesi = new List<Dictionary<string, string>>();
                            while (reader.Read())
                            {
                                stokHizmetListesi.Add(new Dictionary<string, string>
                                {
                                    ["evrakNo"] = SafeGetString(reader, "Evrak_No"),
                                    ["evrakSiraNo"] = SafeGetString(reader, "Evrak_Sira_No"),
                                    ["yedekParcaNo"] = SafeGetString(reader, "Yedek_Parca_No"),
                                    ["yedekParcaAdi"] = SafeGetString(reader, "Yedek_Parca_Adi"),
                                    ["servisMerkezi"] = SafeGetString(reader, "Servis_Merkezi"),
                                    ["adet"] = SafeGetString(reader, "Adet"),
                                    ["birimFiyat"] = SafeGetString(reader, "Birim_Fiyat"),
                                    ["tutar"] = SafeGetString(reader, "Tutar"),
                                    ["iscilikTutari"] = SafeGetString(reader, "iscilik_tutari"),
                                    ["hariciIscilikTutari"] = SafeGetString(reader, "harici_iscilik_tutari"),
                                    ["IskontoYuzde"] = SafeGetString(reader, "IskontoYuzde")
                                });
                            }

                            // Detay kayıtları varsa ekle
                            if (stokHizmetListesi.Any())
                            {
                                evrakDetay["stokHizmetListesi"] = stokHizmetListesi;
                            }
                            else
                            {
                                // Detay kayıt bulunamadıysa boş liste gönder
                                evrakDetay["stokHizmetListesi"] = new List<Dictionary<string, string>>();
                            }
                        }
                    }
                }

                // Başarılı veri çekme durumunda JSON olarak dön
                return Json(evrakDetay);
            }
            catch (Exception ex)
            {
                // Hata durumunda detaylı hata mesajı dön
                return Json(new
                {
                    success = false,
                    message = "Evrak bilgileri alınamadı: " + ex.Message
                });
            }
        }

        [AllowAnonymous]
        // Güvenli string alma metodu
        private string SafeGetString(SqlDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(columnIndex) ? "" : reader.GetValue(columnIndex).ToString().Trim();
        }
        [AllowAnonymous]
        // Güvenli decimal alma metodu
        private string SafeGetDecimal(SqlDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(columnIndex) ? "0" : Math.Round(reader.GetDecimal(columnIndex), 2).ToString();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult UpdateEvrak([FromBody] EvrakUpdateDTO updateData)
        {
            if (updateData == null || updateData.model == null || updateData.stokHizmet == null)
            {
                return Json(new { success = false, message = "Geçersiz veri formatı." });
            }

            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Servis merkezi ve evrak numarasına göre kayıt kontrolü
                        string checkQuery = @"
                    SELECT COUNT(*) FROM ServisHareketleri 
                    WHERE Evrak_No = @EvrakNo 
                    AND Servis_Merkezi = @ServisMerkezi 
                    AND Evrak_Sira_No = 1";

                        int recordCount = 0;
                        using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con, transaction))
                        {
                            cmdCheck.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                            cmdCheck.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                            recordCount = (int)cmdCheck.ExecuteScalar();
                        }

                        if (recordCount == 0)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = "Güncellenecek kayıt bulunamadı." });
                        }

                        // Ana kayıt güncelleme
                        string queryUpdateEvrak = @"
                    UPDATE ServisHareketleri 
                    SET 
                        Tarih = @Tarih,
                        Musteri_Adi = @MusteriAdi, 
                        Sofor_Adi = @SoforAdi,
                        Sofor_Telefon = @SoforTelefon,
                        Plaka_No = @PlakaNo, 
                        UniteSeriNo=@UniteSeriNo, 
                        Cihaz_Marka_Model = @CihazMarkaModel, 
                        Teknisyen_Adi = @TeknisyenAdi,
                        Calisma_Saati = @CalismaSaati, 
                        Servise_Giris_Tarihi = @ServiseGirisTarihi, 
                        is_turu = @IsTuru,
                        Ilk_Gozlem = @IlkGozlem
                    WHERE Evrak_No = @EvrakNo 
                    AND Servis_Merkezi = @ServisMerkezi 
                    AND Evrak_Sira_No = 1";

                        using (SqlCommand cmdUpdate = new SqlCommand(queryUpdateEvrak, con, transaction))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Tarih", updateData.model.Tarih);
                            cmdUpdate.Parameters.AddWithValue("@MusteriAdi", updateData.model.MusteriAdi ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@SoforAdi", updateData.model.SoforAdi ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@SoforTelefon", updateData.model.SoforTelefon ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@PlakaNo", updateData.model.PlakaNo ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@UniteSeriNo", updateData.model.UniteSeriNo ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@CihazMarkaModel", updateData.model.CihazMarkaModel ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@TeknisyenAdi", updateData.model.TeknisyenAdi ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@CalismaSaati", updateData.model.CalismaSaati);
                            cmdUpdate.Parameters.AddWithValue("@ServiseGirisTarihi", updateData.model.ServiseGirisTarihi);
                            cmdUpdate.Parameters.AddWithValue("@IsTuru", updateData.model.IsTuru ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@IlkGozlem", updateData.model.IlkGozlem ?? (object)DBNull.Value);
                            cmdUpdate.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                            cmdUpdate.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        // Mevcut stok/hizmet kayıtlarını al
                        var existingDetails = new List<(string YedekParcaNo, int EvrakSiraNo)>();
                        string getExistingDetailsQuery = @"
                    SELECT Yedek_Parca_No, Evrak_Sira_No 
                    FROM ServisHareketleri 
                    WHERE Evrak_No = @EvrakNo 
                    AND Servis_Merkezi = @ServisMerkezi 
                    AND Evrak_Sira_No > 1";

                        using (SqlCommand cmdGetDetails = new SqlCommand(getExistingDetailsQuery, con, transaction))
                        {
                            cmdGetDetails.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                            cmdGetDetails.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                            using (SqlDataReader reader = cmdGetDetails.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    existingDetails.Add((
                                        reader["Yedek_Parca_No"].ToString(),
                                        (int)reader["Evrak_Sira_No"]
                                    ));
                                }
                            }
                        }

                        // Gelen stok/hizmet listesindeki YedekParcaNo'ları al
                        var incomingYedekParcaNos = updateData.stokHizmet
                            .Where(item => !string.IsNullOrWhiteSpace(item.YedekParcaNo))
                            .Select(item => item.YedekParcaNo)
                            .ToList();

                        // Silinen kayıtları bul
                        var deletedDetails = existingDetails
                            .Where(existing => !incomingYedekParcaNos.Contains(existing.YedekParcaNo))
                            .ToList();

                        // Silinen kayıtları veritabanından kaldır
                        if (deletedDetails.Any())
                        {
                            string deleteQuery = @"
                        DELETE FROM ServisHareketleri 
                        WHERE Evrak_No = @EvrakNo 
                        AND Servis_Merkezi = @ServisMerkezi 
                        AND Yedek_Parca_No = @YedekParcaNo";

                            foreach (var deleted in deletedDetails)
                            {
                                using (SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                                {
                                    cmdDelete.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                    cmdDelete.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                    cmdDelete.Parameters.AddWithValue("@YedekParcaNo", deleted.YedekParcaNo);
                                    cmdDelete.ExecuteNonQuery();
                                }
                            }
                        }

                        // Detay kayıtlarını güncelleme ve yeni kayıt ekleme
                        int updatedRows = 0;
                        int addedRows = 0;

                        foreach (var item in updateData.stokHizmet)
                        {
                            if (string.IsNullOrWhiteSpace(item.YedekParcaNo))
                            {
                                continue;
                            }

                            string checkDetailQuery = @"
SELECT COUNT(*) FROM ServisHareketleri 
WHERE Evrak_No = @EvrakNo 
AND Servis_Merkezi = @ServisMerkezi 
AND Yedek_Parca_No = @YedekParcaNo";

                            int detailCount = 0;
                            using (SqlCommand cmdCheckDetail = new SqlCommand(checkDetailQuery, con, transaction))
                            {
                                cmdCheckDetail.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                cmdCheckDetail.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                cmdCheckDetail.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo);
                                detailCount = (int)cmdCheckDetail.ExecuteScalar();
                            }

                            // Burada iskonto hesaplaması düzeltilmeli
                            decimal parcaTutari = item.Adet * item.BirimFiyat;
                            decimal iscilikTutari = item.IscilikTutari ?? 0;
                            decimal hariciIscilikTutari = item.HariciIscilikTutari ?? 0;

                            // Satır bazlı iskonto uygula
                            decimal iskontoTutarItem = 0;
                            if (item.IskontoYuzde.HasValue && item.IskontoYuzde.Value > 0)
                            {
                                iskontoTutarItem = parcaTutari * (item.IskontoYuzde.Value / 100M);
                            }

                            // Düzeltilmiş tutar hesaplama
                            decimal toplamSatirTutari = (parcaTutari - iskontoTutarItem) + iscilikTutari + hariciIscilikTutari;
                            if (detailCount > 0)
                            {
                                // Mevcut kaydı güncelle
                                string queryUpdateDetay = @"
                            UPDATE ServisHareketleri 
                            SET 
                                Yedek_Parca_Adi = @YedekParcaAdi, 
                                Adet = @Adet, 
                                Birim_Fiyat = @BirimFiyat, 
                                Tutar = @Tutar,
                                iscilik_tutari = @IscilikTutari,
                                harici_iscilik_tutari = @HariciIscilikTutari,
                                IskontoYuzde = @IskontoYuzde

                            WHERE Evrak_No = @EvrakNo 
                            AND Servis_Merkezi = @ServisMerkezi 
                            AND Yedek_Parca_No = @YedekParcaNo";

                                using (SqlCommand cmdUpdateDetay = new SqlCommand(queryUpdateDetay, con, transaction))
                                {
                                    cmdUpdateDetay.Parameters.AddWithValue("@YedekParcaAdi", item.YedekParcaAdi ?? (object)DBNull.Value);
                                    cmdUpdateDetay.Parameters.AddWithValue("@Adet", item.Adet);
                                    cmdUpdateDetay.Parameters.AddWithValue("@BirimFiyat", item.BirimFiyat);
                                    cmdUpdateDetay.Parameters.AddWithValue("@Tutar", toplamSatirTutari);
                                    cmdUpdateDetay.Parameters.AddWithValue("@IscilikTutari", item.IscilikTutari ?? (object)DBNull.Value);
                                    cmdUpdateDetay.Parameters.AddWithValue("@HariciIscilikTutari", item.HariciIscilikTutari ?? (object)DBNull.Value);
                                    cmdUpdateDetay.Parameters.AddWithValue("@IskontoYuzde", item.IskontoYuzde ?? (object)DBNull.Value);
                                    cmdUpdateDetay.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                    cmdUpdateDetay.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                    cmdUpdateDetay.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo);

                                    updatedRows += cmdUpdateDetay.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Yeni kayıt ekle
                                string queryEvrakSiraNo = @"
                            SELECT ISNULL(MAX(Evrak_Sira_No), 0) + 1 
                            FROM ServisHareketleri 
                            WHERE Evrak_No = @EvrakNo 
                            AND Servis_Merkezi = @ServisMerkezi";

                                int evrakSiraNo;
                                using (SqlCommand cmdSiraNo = new SqlCommand(queryEvrakSiraNo, con, transaction))
                                {
                                    cmdSiraNo.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                    cmdSiraNo.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                    evrakSiraNo = (int)cmdSiraNo.ExecuteScalar();
                                }

                                string queryInsertDetay = @"
                            INSERT INTO ServisHareketleri (
                                Evrak_No, Servis_Merkezi, Evrak_Sira_No, Tarih, Musteri_Adi, Plaka_No,UniteSeriNo, 
                                Cihaz_Marka_Model, Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem,
                                Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, A_Toplam, 
                                Vergi, G_Toplam, Sofor_Adi, Sofor_Telefon, Teknisyen_Adi, is_turu,
                                iscilik_tutari, harici_iscilik_tutari,IskontoYuzde
                            )
                            VALUES (
                                @EvrakNo, @ServisMerkezi, @EvrakSiraNo, @Tarih, @MusteriAdi, @PlakaNo, 
                                @CihazMarkaModel, @CalismaSaati, @ServiseGirisTarihi, @IlkGozlem,
                                @YedekParcaNo, @YedekParcaAdi, @Adet, @BirimFiyat, @Tutar, @AToplam, 
                                @Vergi, @GToplam, @SoforAdi, @SoforTelefon, @TeknisyenAdi, @IsTuru,
                                @IscilikTutari, @HariciIscilikTutari,@IskontoYuzde
                            )";

                                using (SqlCommand cmdInsertDetay = new SqlCommand(queryInsertDetay, con, transaction))
                                {
                                    cmdInsertDetay.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                    cmdInsertDetay.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                    cmdInsertDetay.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo);
                                    cmdInsertDetay.Parameters.AddWithValue("@Tarih", updateData.model.Tarih);
                                    cmdInsertDetay.Parameters.AddWithValue("@MusteriAdi", updateData.model.MusteriAdi);
                                    cmdInsertDetay.Parameters.AddWithValue("@PlakaNo", updateData.model.PlakaNo);
                                    cmdInsertDetay.Parameters.AddWithValue("@UniteSeriNo", updateData.model.UniteSeriNo);
                                    cmdInsertDetay.Parameters.AddWithValue("@CihazMarkaModel", updateData.model.CihazMarkaModel);
                                    cmdInsertDetay.Parameters.AddWithValue("@CalismaSaati", updateData.model.CalismaSaati);
                                    cmdInsertDetay.Parameters.AddWithValue("@ServiseGirisTarihi", updateData.model.ServiseGirisTarihi);
                                    cmdInsertDetay.Parameters.AddWithValue("@IlkGozlem", updateData.model.IlkGozlem ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo);
                                    cmdInsertDetay.Parameters.AddWithValue("@YedekParcaAdi", item.YedekParcaAdi ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@Adet", item.Adet);
                                    cmdInsertDetay.Parameters.AddWithValue("@BirimFiyat", item.BirimFiyat);
                                    cmdInsertDetay.Parameters.AddWithValue("@Tutar", toplamSatirTutari);
                                    cmdInsertDetay.Parameters.AddWithValue("@AToplam", updateData.model.AToplam);
                                    cmdInsertDetay.Parameters.AddWithValue("@Vergi", updateData.model.Vergi);
                                    cmdInsertDetay.Parameters.AddWithValue("@GToplam", updateData.model.GToplam);
                                    cmdInsertDetay.Parameters.AddWithValue("@SoforAdi", updateData.model.SoforAdi ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@SoforTelefon", updateData.model.SoforTelefon ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@TeknisyenAdi", updateData.model.TeknisyenAdi ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@IsTuru", updateData.model.IsTuru ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@IscilikTutari", item.IscilikTutari ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@HariciIscilikTutari", item.HariciIscilikTutari ?? (object)DBNull.Value);
                                    cmdInsertDetay.Parameters.AddWithValue("@IskontoYuzde", item.IskontoYuzde ?? (object)DBNull.Value);

                                    addedRows += cmdInsertDetay.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return Json(new
                        {
                            success = true,
                            message = "Güncelleme başarılı.",
                            updatedRecords = updatedRows,
                            addedRecords = addedRows,
                            deletedRecords = deletedDetails.Count
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                        return Json(new { success = false, message = "Güncelleme sırasında hata oluştu: " + ex.Message });
                    }
                }
            }
        }






        [HttpGet]
        public IActionResult AllIsEmri(string servisMerkezi, int? evrakNo)
        {
            string username = HttpContext.Session.GetString("Username");
            ViewBag.Username = username;
            int nextNo = 1;
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            // Evrak No için sorgu
            string evrakNoQuery = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 FROM ServisHareketleri";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(evrakNoQuery, con);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            List<IsEmirleri> servisHareketleri = new List<IsEmirleri>();

            // Dinamik sorgu
            string selectQuery = @"
     SELECT Evrak_No, Evrak_Sira_No, Musteri_Adi, Plaka_No,UniteSeriNo, Tarih, Cihaz_Marka_Model, 
           Calisma_Saati, Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, 
           A_Toplam, Vergi, G_Toplam, Durum, Servis_Merkezi
    FROM ServisHareketleri
    WHERE 1=1"; // Bu satır sorgu koşullarını kolayca ekleyebilmek için

            // Koşulları dinamik olarak ekle
            if (!string.IsNullOrEmpty(servisMerkezi))
            {
                selectQuery += " AND Servis_Merkezi = @ServisMerkezi";
            }
            if (evrakNo.HasValue)
            {
                selectQuery += " AND Evrak_No = @EvrakNo";
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(selectQuery, con);

                // Parametreleri ekle
                if (!string.IsNullOrEmpty(servisMerkezi))
                {
                    cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                }
                if (evrakNo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo.Value);
                }

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    servisHareketleri.Add(new IsEmirleri
                    {
                        EvrakNo = (int)reader["Evrak_No"],
                        EvrakSiraNo = (int)reader["Evrak_Sira_No"],
                        MusteriAdi = reader["Musteri_Adi"].ToString(),
                        PlakaNo = reader["Plaka_No"].ToString(),
                        UniteSeriNo = reader["UniteSeriNo"].ToString(),
                        Tarih = (DateTime)reader["Tarih"],
                        CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                        CalismaSaati = (int)reader["Calisma_Saati"],
                        YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                        YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                        Adet = (int)reader["Adet"],
                        BirimFiyat = (decimal)reader["Birim_Fiyat"],
                        Tutar = (decimal)reader["Tutar"],
                        AToplam = (decimal)reader["A_Toplam"],
                        Vergi = (decimal)reader["Vergi"],
                        GToplam = (decimal)reader["G_Toplam"],
                        Durum = (int)reader["Durum"],
                        Servis_Merkezi = reader["Servis_Merkezi"].ToString(),
                    });
                }
                con.Close();
            }

            var servisHareketGruplari = servisHareketleri
                .GroupBy(h => new { h.EvrakNo, h.Servis_Merkezi })
                .ToList();
            ViewBag.ServisHareketGruplari = servisHareketGruplari;

            var model = new IsEmirleri
            {
                No = nextNo,
                Tarih = DateTime.Now
            };

            return View(model);
        }



        [HttpGet]
        public IActionResult IsEmriGiris(string servisMerkezi, int? evrakNo)
        {
            string username = HttpContext.Session.GetString("Username");
            ViewBag.Username = username;
            int nextNo = 1;
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            // Evrak No için sorgu
            string evrakNoQuery = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 FROM ServisHareketleri";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(evrakNoQuery, con);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            List<IsEmirleri> servisHareketleri = new List<IsEmirleri>();

            // Dinamik sorgu
            string selectQuery = @"
     SELECT Evrak_No, Evrak_Sira_No, Musteri_Adi, Plaka_No,UniteSeriNo, Tarih, Cihaz_Marka_Model, 
           Calisma_Saati, Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, 
           A_Toplam, Vergi, G_Toplam, Durum, Servis_Merkezi
    FROM ServisHareketleri
    WHERE 1=1"; // Bu satır sorgu koşullarını kolayca ekleyebilmek için

            // Koşulları dinamik olarak ekle
            if (!string.IsNullOrEmpty(servisMerkezi))
            {
                selectQuery += " AND Servis_Merkezi = @ServisMerkezi";
            }
            if (evrakNo.HasValue)
            {
                selectQuery += " AND Evrak_No = @EvrakNo";
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(selectQuery, con);

                // Parametreleri ekle
                if (!string.IsNullOrEmpty(servisMerkezi))
                {
                    cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                }
                if (evrakNo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo.Value);
                }

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    servisHareketleri.Add(new IsEmirleri
                    {
                        EvrakNo = (int)reader["Evrak_No"],
                        EvrakSiraNo = (int)reader["Evrak_Sira_No"],
                        MusteriAdi = reader["Musteri_Adi"].ToString(),
                        PlakaNo = reader["Plaka_No"].ToString(),
                        UniteSeriNo = reader["UniteSeriNo"].ToString(),
                        Tarih = (DateTime)reader["Tarih"],
                        CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                        CalismaSaati = (int)reader["Calisma_Saati"],
                        YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                        YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                        Adet = (int)reader["Adet"],
                        BirimFiyat = (decimal)reader["Birim_Fiyat"],
                        Tutar = (decimal)reader["Tutar"],
                        AToplam = (decimal)reader["A_Toplam"],
                        Vergi = (decimal)reader["Vergi"],
                        GToplam = (decimal)reader["G_Toplam"],
                        Durum = (int)reader["Durum"],
                        Servis_Merkezi = reader["Servis_Merkezi"].ToString(),
                    });
                }
                con.Close();
            }


            var servisHareketGruplari = servisHareketleri
                .GroupBy(h => new { h.EvrakNo, h.Servis_Merkezi })
                .ToList();

            ViewBag.ServisHareketGruplari = servisHareketGruplari;

            // İş Merkezleri için Dropdown Listesi
            ViewBag.ServisMerkezleri = servisHareketleri
                .Select(h => h.Servis_Merkezi)
                .Distinct()
                .ToList();

            var model = new IsEmirleri
            {
                No = nextNo,
                Tarih = DateTime.Now
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult IsEmriGiris(IsEmirleri model, List<StokHizmet> stokHizmet)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Yeni Evrak No'yu bul
                        int evrakNo = 1;
                        string queryEvrakNo = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 FROM ServisHareketleri WHERE Servis_Merkezi = @ServisMerkezi";
                        using (SqlCommand cmd = new SqlCommand(queryEvrakNo, con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                            evrakNo = (int)cmd.ExecuteScalar();
                        }

                        // A Toplam hesapla
                        decimal aToplam = 0;

                        if (stokHizmet != null && stokHizmet.Any())
                        {
                            foreach (var item in stokHizmet)
                            {
                                if (item != null && (item.Adet > 0 || item.IscilikTutari > 0 || item.HariciIscilikTutari > 0))
                                {
                                    decimal parcaTutari = item.Adet * item.BirimFiyat;
                                    decimal iscilikTutari = item.IscilikTutari ?? 0;
                                    decimal hariciIscilikTutari = item.HariciIscilikTutari ?? 0;

                                    // Satır bazlı iskonto uygula
                                    decimal iskontoTutarItem = 0;
                                    if (item.IskontoYuzde.HasValue && item.IskontoYuzde.Value > 0)
                                    {
                                        iskontoTutarItem = parcaTutari * (item.IskontoYuzde.Value / 100M);
                                    }

                                    decimal toplamTutar = (parcaTutari - iskontoTutarItem) + iscilikTutari + hariciIscilikTutari;
                                    item.Tutar = toplamTutar;
                                    aToplam += toplamTutar;
                                }
                            }
                        }

                        decimal vergi = aToplam * 0.20M; // %20 vergi
                        decimal gToplam = aToplam + vergi;

                        // Ana kayıt ekle (Evrak_Sira_No = 1)
                        string queryInsertMain = @"
                    INSERT INTO ServisHareketleri (
                        Evrak_No, Servis_Merkezi, Evrak_Sira_No, Tarih, Musteri_Adi, Plaka_No, UniteSeriNo,
                        Cihaz_Marka_Model, Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem,
                        Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar,
                        A_Toplam, Vergi, G_Toplam, Sofor_Adi, Sofor_Telefon, Teknisyen_Adi,
                        is_turu, iscilik_tutari, harici_iscilik_tutari, IskontoYuzde
                    )
                    VALUES (
                        @EvrakNo, @ServisMerkezi, 1, @Tarih, @MusteriAdi, @PlakaNo, @UniteSeriNo,
                        @CihazMarkaModel, @CalismaSaati, @ServiseGirisTarihi, @IlkGozlem,
                        '', '', 0, 0, 0,
                        @AToplam, @Vergi, @GToplam, @SoforAdi, @SoforTelefon, @TeknisyenAdi,
                        @IsTuru, 0, 0, NULL
                    )";

                        using (SqlCommand cmdInsertMain = new SqlCommand(queryInsertMain, con, transaction))
                        {
                            cmdInsertMain.Parameters.AddWithValue("@EvrakNo", evrakNo);
                            cmdInsertMain.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                            cmdInsertMain.Parameters.AddWithValue("@Tarih", model.Tarih);
                            cmdInsertMain.Parameters.AddWithValue("@MusteriAdi", (object?)model.MusteriAdi ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@PlakaNo", (object?)model.PlakaNo ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@UniteSeriNo", (object?)model.UniteSeriNo ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@CihazMarkaModel", (object?)model.CihazMarkaModel ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@CalismaSaati", model.CalismaSaati);
                            cmdInsertMain.Parameters.AddWithValue("@ServiseGirisTarihi", model.ServiseGirisTarihi);
                            cmdInsertMain.Parameters.AddWithValue("@IlkGozlem", (object?)model.IlkGozlem ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@AToplam", aToplam);
                            cmdInsertMain.Parameters.AddWithValue("@Vergi", vergi);
                            cmdInsertMain.Parameters.AddWithValue("@GToplam", gToplam);
                            cmdInsertMain.Parameters.AddWithValue("@SoforAdi", (object?)model.SoforAdi ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@SoforTelefon", (object?)model.SoforTelefon ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@TeknisyenAdi", (object?)model.TeknisyenAdi ?? DBNull.Value);
                            cmdInsertMain.Parameters.AddWithValue("@IsTuru", (object?)model.IsTuru ?? DBNull.Value);

                            cmdInsertMain.ExecuteNonQuery();
                        }

                        // Stok hizmet detayları ekle
                        if (stokHizmet != null && stokHizmet.Any())
                        {
                            int evrakSiraNo = 2;

                            foreach (var item in stokHizmet)
                            {
                                if (item == null ||
                                    (string.IsNullOrWhiteSpace(item.YedekParcaNo) && string.IsNullOrWhiteSpace(item.YedekParcaAdi)) ||
                                    (item.Adet <= 0 && item.BirimFiyat <= 0 && (item.IscilikTutari ?? 0) <= 0 && (item.HariciIscilikTutari ?? 0) <= 0))
                                    continue;

                                if (string.IsNullOrWhiteSpace(item.YedekParcaNo)) item.YedekParcaNo = "-";
                                if (string.IsNullOrWhiteSpace(item.YedekParcaAdi)) item.YedekParcaAdi = "-";

                                string queryInsertItem = @"
                            INSERT INTO ServisHareketleri (
                                Evrak_No, Servis_Merkezi, Evrak_Sira_No, Tarih, Musteri_Adi, Plaka_No, UniteSeriNo,
                                Cihaz_Marka_Model, Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem,
                                Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar,
                                A_Toplam, Vergi, G_Toplam, Sofor_Adi, Sofor_Telefon, Teknisyen_Adi,
                                is_turu, iscilik_tutari, harici_iscilik_tutari, IskontoYuzde
                            )
                            VALUES (
                                @EvrakNo, @ServisMerkezi, @EvrakSiraNo, @Tarih, @MusteriAdi, @PlakaNo, @UniteSeriNo,
                                @CihazMarkaModel, @CalismaSaati, @ServiseGirisTarihi, @IlkGozlem,
                                @YedekParcaNo, @YedekParcaAdi, @Adet, @BirimFiyat, @Tutar,
                                @AToplam, @Vergi, @GToplam, @SoforAdi, @SoforTelefon, @TeknisyenAdi,
                                @IsTuru, @IscilikTutari, @HariciIscilikTutari, @IskontoYuzde
                            )";

                                using (SqlCommand cmdInsertItem = new SqlCommand(queryInsertItem, con, transaction))
                                {
                                    cmdInsertItem.Parameters.AddWithValue("@EvrakNo", evrakNo);
                                    cmdInsertItem.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                                    cmdInsertItem.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo++);
                                    cmdInsertItem.Parameters.AddWithValue("@Tarih", model.Tarih);
                                    cmdInsertItem.Parameters.AddWithValue("@MusteriAdi", (object?)model.MusteriAdi ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@PlakaNo", (object?)model.PlakaNo ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@UniteSeriNo", (object?)model.UniteSeriNo ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@CihazMarkaModel", (object?)model.CihazMarkaModel ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@CalismaSaati", model.CalismaSaati);
                                    cmdInsertItem.Parameters.AddWithValue("@ServiseGirisTarihi", model.ServiseGirisTarihi);
                                    cmdInsertItem.Parameters.AddWithValue("@IlkGozlem", (object?)model.IlkGozlem ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo);
                                    cmdInsertItem.Parameters.AddWithValue("@YedekParcaAdi", item.YedekParcaAdi);
                                    cmdInsertItem.Parameters.AddWithValue("@Adet", item.Adet);
                                    cmdInsertItem.Parameters.AddWithValue("@BirimFiyat", item.BirimFiyat);
                                    cmdInsertItem.Parameters.AddWithValue("@Tutar", item.Tutar);
                                    cmdInsertItem.Parameters.AddWithValue("@AToplam", aToplam);
                                    cmdInsertItem.Parameters.AddWithValue("@Vergi", vergi);
                                    cmdInsertItem.Parameters.AddWithValue("@GToplam", gToplam);
                                    cmdInsertItem.Parameters.AddWithValue("@SoforAdi", (object?)model.SoforAdi ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@SoforTelefon", (object?)model.SoforTelefon ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@TeknisyenAdi", (object?)model.TeknisyenAdi ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@IsTuru", (object?)model.IsTuru ?? DBNull.Value);
                                    cmdInsertItem.Parameters.AddWithValue("@IscilikTutari", item.IscilikTutari ?? 0);
                                    cmdInsertItem.Parameters.AddWithValue("@HariciIscilikTutari", item.HariciIscilikTutari ?? 0);
                                    cmdInsertItem.Parameters.AddWithValue("@IskontoYuzde", item.IskontoYuzde ?? (object)DBNull.Value);

                                    cmdInsertItem.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"IsEmriGiris hatası: {ex.Message}");
                        TempData["ErrorMessage"] = "İş emri kaydedilirken bir hata oluştu: " + ex.Message;
                        return RedirectToAction("Error", "Home");
                    }
                }
            }

            return RedirectToAction("IsEmriGiris");
        }




        [AllowAnonymous]
        [HttpPost]
        public JsonResult RedEvrak([FromBody] int evrakNo)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            try
            {
                string queryUpdate = "UPDATE ServisHareketleri SET Durum = 0 WHERE Evrak_No = @EvrakNo";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(queryUpdate, con);
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Evrak No bulunamadı veya güncellenmedi." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [AllowAnonymous]
        public class RedSatirRequest
        {
            public int EvrakNo { get; set; }
            public int EvrakSiraNo { get; set; }
        }
        [AllowAnonymous]
        [HttpPost]
        public JsonResult RedSatir([FromBody] RedSatirRequest request)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");
            int evrakNo = request.EvrakNo;
            int evrakSiraNo = request.EvrakSiraNo;

            try
            {
                string queryUpdate = "UPDATE ServisHareketleri SET Durum = 0 WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No = @EvrakSiraNo";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(queryUpdate, con);
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                    cmd.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Evrak ve Satır No bulunamadı veya güncellenmedi." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SilEvrakForm(int evrakNo, string servisMerkezi)
        {
            try
            {
                // Kullanıcı kontrolü
                string username = HttpContext.Session.GetString("Username") ?? "";
                if (username != "SRV")
                {
                    TempData["ErrorMessage"] = "Bu işlemi gerçekleştirme yetkiniz yok.";
                    return RedirectToAction("IsEmriGiris");
                }

                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string queryDelete = @"
                DELETE FROM ServisHareketleri 
                WHERE Evrak_No = @EvrakNo AND Servis_Merkezi = @ServisMerkezi";

                    SqlCommand cmd = new SqlCommand(queryDelete, con);
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                    cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Evrak başarıyla silindi.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Evrak bulunamadı veya silinemedi.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Silme işlemi sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction("IsEmriGiris");
        }


        // Yardımcı sınıf
        public class SilEvrakRequest
        {
            public int EvrakNo { get; set; }
            public string ServisMerkezi { get; set; }
        }
    }
}

