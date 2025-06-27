using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using static Deneme_proje.Models.HrEntities;
using System.Net.Mail;
using System.Net;
using System.Data;

namespace Deneme_proje.Controllers
{
    public class HrController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly EmailNotificationService _emailService;  // Yeni eklenen
        public HrController(
         IConfiguration configuration,
         DatabaseSelectorService dbSelectorService,
         EmailNotificationService emailService)  // Yeni parametre
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            _emailService = emailService;  // Atama
        }

        // HrController'da IzinTalepFormu action'ında:
        public async Task<IActionResult> IzinTalepFormu()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                // MikroDB_V16'dan kullanıcı bilgilerini al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo = null;

                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(userNo))
                {
                    string erpConnectionString = _dbSelectorService.GetConnectionString();

                    using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                    {
                        await erpConnection.OpenAsync();
                        // Personel ve departman bilgilerini alacak sorguyu düzelttik
                        string personnelQuery = @"
                    SELECT 
                        p.per_kod, 
                        p.per_adi, 
                        p.per_soyadi,
                        p.Per_TcKimlikNo,
                        d.pdp_adi as DepartmanAdi
                    FROM PERSONELLER p
                    LEFT JOIN DEPARTMANLAR d ON p.per_dept_kod = d.pdp_kodu
                    WHERE p.per_UserNo = @userNo AND CONVERT(DATE, p.per_cikis_tar) = '1899-12-31' ";

                        using (SqlCommand command = new SqlCommand(personnelQuery, erpConnection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    ViewBag.PersonnelCode = reader["per_kod"]?.ToString();
                                    ViewBag.PersonnelName = reader["per_adi"]?.ToString();
                                    ViewBag.PersonnelSurname = reader["per_soyadi"]?.ToString();
                                    ViewBag.TcNo = reader["Per_TcKimlikNo"]?.ToString();
                                    ViewBag.Department = reader["DepartmanAdi"]?.ToString();
                                }
                            }
                        }
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hata: {ex.Message}");
                return View();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> IzinTalepKaydet(string talepTarihi, string izinTipi, string eksikNedeni,
     int izinGun, int izinSaat, string baslangicTarihi, string bitisTarihi, string iseBaslamaTarihi, string baslamaSaat,
     string izinAmaci, string personnelCode)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                // Personel kodunu doğrudan kullan
                string persKod = personnelCode;

                // Kullanıcı bilgilerini alalım
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo = null;

                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(persKod) || string.IsNullOrEmpty(userNo))
                {
                    return Json(new { success = false, message = "Personel veya kullanıcı bilgisi bulunamadı." });
                }

                Guid izinGuid = Guid.NewGuid(); // Yeni GUID oluştur
                string sessionKey = "IseBaslamaTarihi_" + izinGuid.ToString();
                HttpContext.Session.SetString(sessionKey, iseBaslamaTarihi);
                int izinSatirNo = 0;

                string connectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Önce satır numarası al
                    string getMaxSatirNo = "SELECT ISNULL(MAX(pit_satir_no), 0) + 1 FROM PERSONEL_IZIN_TALEPLERI WHERE pit_pers_kod = @persKod";
                    using (SqlCommand command = new SqlCommand(getMaxSatirNo, connection))
                    {
                        command.Parameters.AddWithValue("@persKod", persKod);
                        var result = await command.ExecuteScalarAsync();
                        izinSatirNo = Convert.ToInt32(result);
                    }

                    string insertQuery = @"INSERT INTO PERSONEL_IZIN_TALEPLERI (
            pit_guid,
            pit_pers_kod,
            pit_talep_tarihi,
            pit_izin_tipi,
            pit_eksikcalismanedeni,
            pit_gun_sayisi,
            pit_yol_izni,
            pit_baslangictarih,
            pit_BaslamaSaati,
            pit_saat,
            pit_amac,
            pit_izin_durum,
            pit_onaylayan_kullanici,
            pit_create_user,
            pit_create_date,
            pit_lastup_user,
            pit_lastup_date,
            pit_special1,
            pit_special2,
            pit_special3,
            pit_iptal,
            pit_hidden,
            pit_kilitli,
            pit_degisti,
            pit_mali_yil,
            pit_satir_no,
            pit_SpecRECno,
            pit_fileid,
            pit_checksum,
            pit_cadde,
            pit_mahalle,
            pit_sokak,
            pit_il,
            pit_ulke,
            pit_Semt,
            pit_Apt_No,
            pit_Daire_No,
            pit_posta_kodu,
            pit_ilce,
            pit_adres_kodu,
            pit_tel1,
            pit_tel2,
            pit_email,
            pit_aciklama1,
            pit_aciklama2
        ) VALUES (
            @izinGuid,
            @persKod,
            @talepTarihi,
            @izinTipi,
            @eksikNedeni,
            @izinGun,
            0, -- Yol izni varsayılan olarak 0
            @baslangicTarihi,
            @bitisTarihi,
            @izinSaat,
            @izinAmaci,
            0, -- izin_durum varsayılan olarak 0 (beklemede)
            0, -- onaylayan kullanıcı varsayılan olarak 0
            @createUser,
            GETDATE(),
            @lastupUser,
            GETDATE(),
            ' ',
            ' ',
            ' ',
            0,
            0,
            0,
            0,
            YEAR(GETDATE()),
            @izinSatirNo,
            0,
            229,
            0,
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' ',
            ' '
        )";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@izinGuid", izinGuid);
                        command.Parameters.AddWithValue("@persKod", persKod);
                        command.Parameters.AddWithValue("@talepTarihi", Convert.ToDateTime(talepTarihi));
                        command.Parameters.AddWithValue("@izinTipi", Convert.ToByte(izinTipi));
                        command.Parameters.AddWithValue("@eksikNedeni", !string.IsNullOrEmpty(eksikNedeni) ? Convert.ToByte(eksikNedeni) : (byte)0);
                        command.Parameters.AddWithValue("@izinGun", izinGun);
                        command.Parameters.AddWithValue("@baslangicTarihi", Convert.ToDateTime(baslangicTarihi));
                        command.Parameters.AddWithValue("@bitisTarihi", Convert.ToDateTime(bitisTarihi));
                        command.Parameters.AddWithValue("@izinSatirNo", izinSatirNo);

                        command.Parameters.AddWithValue("@izinSaat", izinSaat); // izinSaat değeri pit_saat alanına kaydedilecek
                        command.Parameters.AddWithValue("@izinAmaci", !string.IsNullOrEmpty(izinAmaci) ? izinAmaci : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@createUser", userNo);
                        command.Parameters.AddWithValue("@lastupUser", userNo);

                        await command.ExecuteNonQueryAsync();
                    }

                    // İşe başlama tarihini PERSONEL_IZIN_TALEPLERI_user tablosuna kaydet
                    string insertUserQuery = @"
            IF EXISTS (SELECT 1 FROM PERSONEL_IZIN_TALEPLERI_user WHERE Record_uid = @izinGuid)
                UPDATE PERSONEL_IZIN_TALEPLERI_user 
                SET IsbaslamaTarihi = @iseBaslamaTarihi 
                WHERE Record_uid = @izinGuid
            ELSE
                INSERT INTO PERSONEL_IZIN_TALEPLERI_user (Record_uid, IsbaslamaTarihi) 
                VALUES (@izinGuid, @iseBaslamaTarihi)";

                    using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@izinGuid", izinGuid);
                        command.Parameters.AddWithValue("@iseBaslamaTarihi", Convert.ToDateTime(iseBaslamaTarihi));
                        await command.ExecuteNonQueryAsync();
                    }

                    // İzin kaydını gerçekleştirdikten sonra idari amire e-posta gönderme
                    await SendNotificationToManagersAsync(persKod, izinGuid, connection);

                    // İzin kaydını gerçekleştirdikten sonra izinId değerini döndürme
                    HttpContext.Session.SetString("LastIzinGuid", izinGuid.ToString());
                    HttpContext.Session.SetString("LastIzinTarihi", baslangicTarihi);
                    HttpContext.Session.SetString("LastIseBaslamaTarihi", iseBaslamaTarihi);
                    HttpContext.Session.SetString("LastIzinTipi", izinTipi);
                    HttpContext.Session.SetString("LastIzinAmaci", izinAmaci ?? "");
                }

                return Json(new { success = true, message = "İzin talebi başarıyla kaydedildi.", izinGuid = izinGuid });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }




        // İdari amire e-posta gönderme metodu
        private async Task SendNotificationToManagersAsync(string personelKodu, Guid izinGuid, SqlConnection connection)
        {
            try
            {
                // 1. İzin talebi detaylarını çek
                var izinTalebi = await GetIzinTalebiDetaylariAsync(izinGuid, connection);
                if (izinTalebi == null)
                {
                    System.Diagnostics.Debug.WriteLine($"İzin talebi detayları bulunamadı: {izinGuid}");
                    return;
                }

                // 2. Tüm yönetici kodlarını al
                var managerCodes = new HashSet<string>(); // Benzersiz kodlar için HashSet kullan

                string managerQuery = @"
            SELECT 
                per_IdariAmirKodu, 
                per_raporlama_yapacagi_per_kod, 
                per_TeknikAmirKodu 
            FROM PERSONELLER 
            WHERE per_kod = @personelKodu";

                using (SqlCommand managerCommand = new SqlCommand(managerQuery, connection))
                {
                    managerCommand.Parameters.AddWithValue("@personelKodu", personelKodu);

                    using (var reader = await managerCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // İdari amir ekle
                            if (!reader.IsDBNull(0))
                            {
                                string idariAmirKodu = reader.GetString(0);
                                if (!string.IsNullOrEmpty(idariAmirKodu))
                                {
                                    managerCodes.Add(idariAmirKodu);
                                }
                            }

                            // Raporlama yapacağı personel ekle
                            if (!reader.IsDBNull(1))
                            {
                                string raporlamaKodu = reader.GetString(1);
                                if (!string.IsNullOrEmpty(raporlamaKodu))
                                {
                                    managerCodes.Add(raporlamaKodu);
                                }
                            }

                            // Teknik amir ekle
                            if (!reader.IsDBNull(2))
                            {
                                string teknikAmirKodu = reader.GetString(2);
                                if (!string.IsNullOrEmpty(teknikAmirKodu))
                                {
                                    managerCodes.Add(teknikAmirKodu);
                                }
                            }
                        }
                    }
                }

                if (managerCodes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Personel {personelKodu} için bildirim gönderilecek yönetici bulunamadı.");
                    return;
                }

                // 3. Her yönetici için e-posta adreslerini bul ve bildirim gönder
                foreach (var managerCode in managerCodes)
                {
                    // Yöneticinin e-posta adresini al
                    string managerEmail = null;
                    string emailQuery = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE per_kod = @managerCode";

                    using (SqlCommand emailCommand = new SqlCommand(emailQuery, connection))
                    {
                        emailCommand.Parameters.AddWithValue("@managerCode", managerCode);
                        var result = await emailCommand.ExecuteScalarAsync();
                        managerEmail = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(managerEmail))
                    {
                        System.Diagnostics.Debug.WriteLine($"Yönetici {managerCode} için e-posta adresi bulunamadı.");
                        continue;
                    }

                    // E-posta gönder
                    await SendManagerNotificationEmailAsync(managerEmail, izinTalebi);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Yöneticilere bildirim gönderilirken hata oluştu: {ex.Message}");
                // Burada hatayı sadece logluyoruz, ana işlemi bozmamak için tekrar fırlatmıyoruz
            }
        }

        // İdari amire özel e-posta gönderme metodu
        private async Task SendManagerNotificationEmailAsync(string managerEmail, IzinTalepModel izinTalebi)
        {
            try
            {
                // SMTP ayarlarını konfigürasyondan al
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderDisplayName = _configuration["EmailSettings:SenderDisplayName"];

                // SSL güvenlik protokolünü ayarla
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                // Sertifika doğrulamasını geçici olarak atla
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    delegate { return true; };

                // E-posta başlığı
                string subject = $"Yeni İzin Talebi: {izinTalebi.PersonelAdSoyad}";

                // Türkçe tarih formatını ayarla
                System.Globalization.CultureInfo trCulture = new System.Globalization.CultureInfo("tr-TR");

                // Tarihleri doğru formatla
                string talepTarihi = izinTalebi.TalepTarihi.ToString("dd.MM.yyyy", trCulture);
                string baslangicTarihi = izinTalebi.BaslangicTarihi.ToString("dd.MM.yyyy", trCulture);
                string bitisTarihi = izinTalebi.BitisTarihi.ToString("dd.MM.yyyy", trCulture);

                // İzin saati bilgisini formatla
                string izinSaatText = "Belirtilmemiş";
                if (izinTalebi.IzinSaat > 0)
                {
                    // Ondalık sayı ise virgüllü, tam sayı ise virgülsüz göster
                    if (Math.Abs(izinTalebi.IzinSaat % 1) < 0.001)
                        izinSaatText = ((int)izinTalebi.IzinSaat).ToString();
                    else
                        izinSaatText = izinTalebi.IzinSaat.ToString("N2", trCulture).Replace(".00", ""); // 2 ondalık basamaklı
                }

                // E-posta içeriği
                string body = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 650px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 15px; border-bottom: 3px solid #007bff; }}
        .header h1 {{ color: #007bff; margin: 0; }}
        .content {{ padding: 20px 0; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-left: 3px solid #007bff; }}
        .footer {{ font-size: 12px; color: #6c757d; margin-top: 30px; border-top: 1px solid #e9ecef; padding-top: 10px; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; text-decoration: none; padding: 10px 15px; 
                   border-radius: 4px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Yeni İzin Talebi Bildirimi</h1>
        </div>
        <div class=""content"">
            <p>Sayın Yönetici,</p>
            
            <p><strong>{izinTalebi.PersonelAdSoyad}</strong> adlı personelden yeni bir izin talebi oluşturulmuştur. 
               Lütfen bu talebi inceleyip onaylama veya reddetme işlemini gerçekleştiriniz.</p>
            
            <div class=""details"">
                <h3>İzin Talebi Detayları:</h3>
                <p><strong>Personel:</strong> {izinTalebi.PersonelAdSoyad}</p>
                <p><strong>Talep Tarihi:</strong> {talepTarihi}</p>
                <p><strong>İzin Başlangıç Tarihi:</strong> {baslangicTarihi}</p>
                <p><strong>İzin Bitiş Tarihi:</strong> {bitisTarihi}</p>
                <p><strong>İzin Günü Sayısı:</strong> {izinTalebi.GunSayisi}</p>
                <p><strong>İzin Saati:</strong> {izinSaatText}</p>
                <p><strong>İzin Amacı:</strong> {izinTalebi.Amac ?? "Belirtilmemiş"}</p>
            </div>
            
            <p>Bu izin talebini incelemek ve işlem yapmak için lütfen İnsan Kaynakları portalına giriş yapınız. https://hr.dioki.com.tr/Login/LoginKullanici </p>
            
            <p>Bilgilerinize sunarız.</p>
        </div>
        <div class=""footer"">
            <p>Bu e-posta otomatik olarak oluşturulmuştur. Lütfen yanıtlamayınız.</p>
        </div>
    </div>
</body>
</html>";

                // SMTP istemcisini oluştur
                using (var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000 // 20 saniye timeout
                })
                {
                    // E-posta mesajını oluştur
                    using (var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderDisplayName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        // Alıcı e-posta adresini ekle
                        mailMessage.To.Add(managerEmail);

                        // E-postayı gönder
                        await client.SendMailAsync(mailMessage);

                        // Başarılı gönderim log'u
                        System.Diagnostics.Debug.WriteLine($"Yöneticiye izin talebi bildirimi gönderildi: {managerEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Genel hata yakalama
                System.Diagnostics.Debug.WriteLine($"Yöneticiye e-posta gönderme hatası: {ex.Message}");

                // İç içe hata varsa onu da logla
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç Hata: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // Güvenlik için sertifika doğrulamasını geri yükle
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }
        // IzinTalepPdfIndir metodundaki güncellemeler - işe giriş tarihi ve kalan izin hakkı eklendi

        [HttpGet]
        [Route("Hr/IzinTalepPdfIndir")]
        [AllowAnonymous]
        public async Task<IActionResult> IzinTalepPdfIndir(Guid izinGuid)
        {
            try
            {
                if (izinGuid == Guid.Empty)
                {
                    return RedirectToAction("Izinlerim", new { error = "Geçersiz izin talebi." });
                }

                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Izinlerim");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                // Personel bilgilerini al
                string personelKodu = "";
                string personelAdi = "";
                string personelSoyadi = "";
                string personelGorev = "";
                string personelBirim = "";
                string personelTcNo = "";
                DateTime personelIseGirisTarihi = DateTime.Now;
                decimal kalanIzinHakki = 0;
                string idariAmirKodu = "";
                string idariAmirAdi = "";
                string idariAmirSoyadi = "";
                string ikKodu = "";
                string ikAdi = "";
                string ikSoyadi = "";
                DateTime izinBaslangic = DateTime.Now;
                DateTime izinBitis = DateTime.Now;
                DateTime iseBaslamaTarih = DateTime.Now;
                byte izinTipi = 0;
                string izinAmaci = "";
                byte gunSayisi = 0;
                float izinSaati = 0;

                string connectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // İzin talebini al
                    if (izinGuid != Guid.Empty)
                    {
                        string izinQuery = @"
                SELECT 
                    pit_pers_kod, 
                    pit_baslangictarih, 
                    pit_BaslamaSaati,
                    pit_izin_tipi, 
                    pit_amac,
                    pit_gun_sayisi,
                    pit_saat
                FROM PERSONEL_IZIN_TALEPLERI 
                WHERE pit_guid = @izinGuid";

                        using (SqlCommand command = new SqlCommand(izinQuery, connection))
                        {
                            command.Parameters.AddWithValue("@izinGuid", izinGuid);
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    personelKodu = reader["pit_pers_kod"].ToString();
                                    izinBaslangic = Convert.ToDateTime(reader["pit_baslangictarih"]);
                                    izinBitis = Convert.ToDateTime(reader["pit_BaslamaSaati"]);
                                    izinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi"));
                                    izinAmaci = reader["pit_amac"]?.ToString() ?? "";
                                    gunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi"));
                                    izinSaati = reader["pit_saat"] != DBNull.Value
                                        ? Convert.ToSingle(reader["pit_saat"])
                                        : 0;
                                }
                            }
                        }
                    }

                    // İşe başlama tarihini PERSONEL_IZIN_TALEPLERI_user tablosundan al
                    string iseBaslamaSorgusu = @"
                SELECT IsbaslamaTarihi 
                FROM PERSONEL_IZIN_TALEPLERI_user 
                WHERE Record_uid = @izinGuid";

                    using (SqlCommand iseBaslamaCommand = new SqlCommand(iseBaslamaSorgusu, connection))
                    {
                        iseBaslamaCommand.Parameters.AddWithValue("@izinGuid", izinGuid);
                        var result = await iseBaslamaCommand.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            iseBaslamaTarih = Convert.ToDateTime(result);
                        }
                        else
                        {
                            iseBaslamaTarih = izinBitis.AddDays(1);
                        }
                    }

                    // Personel bilgilerini ve kalan izin hakkını al
                    string personelQuery = @"
DECLARE @Yil INT = YEAR(GETDATE());

WITH PersonelBilgileri AS (
    SELECT 
        p.per_kod,
        p.per_adi, 
        p.per_soyadi, 
        p.per_kim_gorev, 
        d.pdp_adi as DepartmanAdi, 
        p.Per_TcKimlikNo,
        p.per_IdariAmirKodu,
        p.per_raporlama_yapacagi_per_kod,
        p.per_giris_tar,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE())
            ELSE 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE()) - 1
        END AS CalismaSuresiYil,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 1
            ELSE 0
        END AS YildonumuGectiMi
    FROM PERSONELLER p
    LEFT JOIN DEPARTMANLAR d ON p.per_dept_kod = d.pdp_kodu
    WHERE p.per_kod = @personelKodu
),
IzinHaklari AS (
    SELECT
        per_kod,
        per_adi,
        per_soyadi,
        per_kim_gorev,
        DepartmanAdi,
        Per_TcKimlikNo,
        per_IdariAmirKodu,
        per_raporlama_yapacagi_per_kod,
        per_giris_tar,
        CalismaSuresiYil,
        YildonumuGectiMi,
        CASE 
            WHEN YildonumuGectiMi = 0 THEN 0
            WHEN CalismaSuresiYil >= 15 THEN 26
            WHEN CalismaSuresiYil > 5 THEN 20
            WHEN CalismaSuresiYil >= 1 THEN 14
            ELSE 0
        END AS HakEdilenYillikIzin
    FROM PersonelBilgileri
),
GecenYilDevredilen AS (
    SELECT
        IH.*,
        ISNULL((SELECT SUM(pro_gecyil_devir_izin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.per_kod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinGun,
        ISNULL((SELECT SUM(pro_gecyil_devir_saatlikizin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.per_kod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinSaat
    FROM IzinHaklari IH
),
KullanilanIzinler AS (
    SELECT
        GYD.*,
        ISNULL((
            SELECT SUM(
                CASE
                    WHEN pz_saat IS NOT NULL AND pz_saat > 0 THEN
                        CASE 
                            WHEN pz_saat <= 4 THEN 0.5 + pz_gun_sayisi
                            ELSE 1.0 + pz_gun_sayisi
                        END
                    ELSE pz_gun_sayisi
                END
            )
            FROM PERSONEL_IZINLERI WITH (NOLOCK) 
            WHERE pz_pers_kod = GYD.per_kod 
            AND pz_izin_yil = @Yil
            AND pz_izin_tipi = 0
        ), 0) AS KullanilanIzinGun
    FROM GecenYilDevredilen GYD
)
SELECT
    per_kod,
    per_adi,
    per_soyadi,
    per_kim_gorev,
    DepartmanAdi,
    Per_TcKimlikNo,
    per_IdariAmirKodu,
    per_raporlama_yapacagi_per_kod,
    per_giris_tar,
    CASE
        WHEN GecenYilDevirIzinSaat IS NOT NULL AND GecenYilDevirIzinSaat > 0 THEN
            CASE 
                WHEN GecenYilDevirIzinSaat <= 4 THEN 0.5 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)
                ELSE 1.0 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)
            END
        ELSE (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)
    END AS KalanIzinBakiyesi
FROM KullanilanIzinler";

                    using (SqlCommand command = new SqlCommand(personelQuery, connection))
                    {
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                personelAdi = reader["per_adi"].ToString();
                                personelSoyadi = reader["per_soyadi"].ToString();
                                personelGorev = reader["per_kim_gorev"]?.ToString() ?? "";
                                personelBirim = reader["DepartmanAdi"]?.ToString() ?? "";
                                personelTcNo = reader["Per_TcKimlikNo"]?.ToString() ?? "";
                                personelIseGirisTarihi = Convert.ToDateTime(reader["per_giris_tar"]);
                                kalanIzinHakki = Convert.ToDecimal(reader["KalanIzinBakiyesi"]);

                                idariAmirKodu = reader["per_IdariAmirKodu"]?.ToString();
                                ikKodu = reader["per_raporlama_yapacagi_per_kod"]?.ToString();
                            }
                        }
                    }

                    // İdari amir bilgilerini ayrı bir sorguda al
                    if (!string.IsNullOrEmpty(idariAmirKodu))
                    {
                        string amirSorgusu = @"
                    SELECT per_adi, per_soyadi
                    FROM PERSONELLER
                    WHERE per_kod = @amirKodu";

                        using (SqlCommand amirCommand = new SqlCommand(amirSorgusu, connection))
                        {
                            amirCommand.Parameters.AddWithValue("@amirKodu", idariAmirKodu);

                            using (SqlDataReader amirReader = await amirCommand.ExecuteReaderAsync())
                            {
                                if (await amirReader.ReadAsync())
                                {
                                    idariAmirAdi = amirReader["per_adi"].ToString();
                                    idariAmirSoyadi = amirReader["per_soyadi"].ToString();
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(ikKodu))
                    {
                        string ikSorgusu = @"
                    SELECT per_adi, per_soyadi
                    FROM PERSONELLER
                    WHERE per_kod = @ikKodu";

                        using (SqlCommand amirCommand = new SqlCommand(ikSorgusu, connection))
                        {
                            amirCommand.Parameters.AddWithValue("@ikKodu", ikKodu);

                            using (SqlDataReader amirReader = await amirCommand.ExecuteReaderAsync())
                            {
                                if (await amirReader.ReadAsync())
                                {
                                    ikAdi = amirReader["per_adi"].ToString();
                                    ikSoyadi = amirReader["per_soyadi"].ToString();
                                }
                            }
                        }
                    }
                }

                // PDF oluştur
                using (MemoryStream ms = new MemoryStream())
                {
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, "Cp1254", BaseFont.NOT_EMBEDDED);
                    Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                    Font boldFont = new Font(baseFont, 10, Font.BOLD);
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                    Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();

                    // Logo ekle
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logo.png");
                    if (System.IO.File.Exists(imagePath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(imagePath);
                        logo.ScaleToFit(150f, 100f);
                        logo.Alignment = iTextSharp.text.Image.ALIGN_LEFT;
                        document.Add(logo);
                    }

                    // Başlık
                    PdfPTable headerTable = new PdfPTable(1);
                    headerTable.WidthPercentage = 100;
                    headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

                    PdfPCell titleCell = new PdfPCell(new Phrase("PERSONEL İZİN\nİSTEK FORMU", titleFont));
                    titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    titleCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingBottom = 10;
                    headerTable.AddCell(titleCell);
                    document.Add(headerTable);

                    // Şirket bilgileri
                    PdfPTable companyInfoTable = new PdfPTable(1);
                    companyInfoTable.WidthPercentage = 100;
                    companyInfoTable.DefaultCell.Border = Rectangle.NO_BORDER;
                    companyInfoTable.DefaultCell.HorizontalAlignment = Element.ALIGN_RIGHT;

                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_RIGHT;

                    Paragraph infoPara = new Paragraph();
                    infoPara.Add(new Chunk("Dioki Petrokimya", boldFont));
                    infoPara.Add(new Chunk(" Serbest Bölgesi, Adana-Yumurtalık, 01920 Adana\n", smallFont));
                    infoPara.Add(new Chunk("T : +(0322) 634 20 15\n", smallFont));

                    infoCell.AddElement(infoPara);
                    companyInfoTable.AddCell(infoCell);
                    document.Add(companyInfoTable);

                    document.Add(new Paragraph(" ")); // Boşluk

                    // Personel Bilgileri Tablosu - Genişletilmiş
                    PdfPTable personelTable = new PdfPTable(6); // 5'ten 6'ya çıkarıldı
                    personelTable.WidthPercentage = 100;
                    personelTable.SetWidths(new float[] { 16f, 16f, 16f, 16f, 18f, 18f }); // Yeni sütun genişlikleri

                    // Başlık satırı
                    PdfPCell personelHeaderCell = new PdfPCell(new Phrase("PERSONELİN", boldFont));
                    personelHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    personelHeaderCell.Rowspan = 2;
                    personelHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    personelHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(personelHeaderCell);

                    // Başlık hücreleri
                    AddHeaderCell(personelTable, "ADI SOYADI", boldFont);
                    AddHeaderCell(personelTable, "GÖREVİ", boldFont);
                    AddHeaderCell(personelTable, "BİRİMİ", boldFont);
                    AddHeaderCell(personelTable, "İŞE GİRİŞ TARİHİ", boldFont);
                    AddHeaderCell(personelTable, "KALAN İZİN HAKKI", boldFont);

                    // İçerik satırı
                    PdfPCell adSoyadCell = new PdfPCell(new Phrase(personelAdi + " " + personelSoyadi, normalFont));
                    adSoyadCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    adSoyadCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(adSoyadCell);

                    PdfPCell gorevCell = new PdfPCell(new Phrase(personelGorev, normalFont));
                    gorevCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    gorevCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(gorevCell);

                    PdfPCell birimCell = new PdfPCell(new Phrase(personelBirim, normalFont));
                    birimCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    birimCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(birimCell);

                    PdfPCell iseGirisCell = new PdfPCell(new Phrase(personelIseGirisTarihi.ToString("dd/MM/yyyy"), normalFont));
                    iseGirisCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    iseGirisCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(iseGirisCell);

                    PdfPCell kalanIzinCell = new PdfPCell(new Phrase(kalanIzinHakki.ToString("F1") + " gün", normalFont));
                    kalanIzinCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    kalanIzinCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(kalanIzinCell);

                    document.Add(personelTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // İzin Türü Tablosu (aynı kalıyor)
                    PdfPTable izinTuruTable = new PdfPTable(8);
                    izinTuruTable.WidthPercentage = 100;
                    izinTuruTable.SetWidths(new float[] { 20f, 10f, 10f, 10f, 10f, 10f, 10f, 10f });

                    PdfPCell izinTuruHeaderCell = new PdfPCell(new Phrase("İSTENEN İZNİN NİTELİĞİ", boldFont));
                    izinTuruHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    izinTuruHeaderCell.Rowspan = 2;
                    izinTuruHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    izinTuruHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    izinTuruTable.AddCell(izinTuruHeaderCell);

                    AddHeaderCell(izinTuruTable, "YILLIK", boldFont);
                    AddHeaderCell(izinTuruTable, "DOĞUM", boldFont);
                    AddHeaderCell(izinTuruTable, "ÖLÜM", boldFont);
                    AddHeaderCell(izinTuruTable, "MAZERET", boldFont);
                    AddHeaderCell(izinTuruTable, "EVLİLİK", boldFont);
                    AddHeaderCell(izinTuruTable, "ÜCRETSİZ", boldFont);
                    AddHeaderCell(izinTuruTable, "DİĞER", boldFont);

                    // İzin türü checkbox'ları
                    bool isYillikIzin = izinTipi == 0;
                    bool isDogumIzin = izinTipi == 11 || izinTipi == 12 || izinTipi == 13;
                    bool isOlumIzin = izinTipi == 14;
                    bool isMazeretIzin = izinTipi == 4;
                    bool isevlilikIzin = izinTipi == 10;
                    bool isUcretsizIzin = izinTipi == 8;
                    bool isDigerIzin = izinTipi == 2 || izinTipi == 3 || izinTipi == 5 || izinTipi == 6 ||
                                       izinTipi == 7 || izinTipi == 9 || izinTipi == 15;

                    Font izinBilgiFont = new Font(baseFont, 12, Font.BOLD);

                    // Checkbox hücreleri ekle
                    AddCheckboxCellWithInfo(izinTuruTable, isYillikIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isDogumIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isOlumIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isMazeretIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isevlilikIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isUcretsizIzin, gunSayisi, izinSaati, izinBilgiFont);
                    AddCheckboxCellWithInfo(izinTuruTable, isDigerIzin, gunSayisi, izinSaati, izinBilgiFont);

                    document.Add(izinTuruTable);
                    document.Add(new Paragraph(" "));

                    // İzin Tarihleri Tablosu
                    PdfPTable tarihTable = new PdfPTable(3);
                    tarihTable.WidthPercentage = 100;
                    tarihTable.SetWidths(new float[] { 20f, 40f, 40f });

                    PdfPCell tarihHeaderCell = new PdfPCell(new Phrase("KULLANILACAK İZİNİN", boldFont));
                    tarihHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    tarihHeaderCell.Rowspan = 2;
                    tarihHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    tarihHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(tarihHeaderCell);

                    AddHeaderCell(tarihTable, "İZİN BAŞLANGIÇ TARİHİ", boldFont);
                    AddHeaderCell(tarihTable, "İZİN DÖNÜŞÜ GÖREVE BAŞLAMA TARİHİ", boldFont);

                    PdfPCell baslangicCell = new PdfPCell(new Phrase(izinBaslangic.ToString("dd/MM/yyyy"), normalFont));
                    baslangicCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    baslangicCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(baslangicCell);

                    PdfPCell iseBaslamaCell = new PdfPCell(new Phrase(iseBaslamaTarih.ToString("dd/MM/yyyy"), normalFont));
                    iseBaslamaCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    iseBaslamaCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(iseBaslamaCell);

                    document.Add(tarihTable);
                    document.Add(new Paragraph(" "));

                    // Not bölümü
                    PdfPTable noteTable = new PdfPTable(1);
                    noteTable.WidthPercentage = 100;

                    PdfPCell noteCell = new PdfPCell(new Phrase("NOT : 1) İdari, Teknik, Destek Personeli ve işçilerin yıllık iznini 4857 Sayılı İş Kanunu ile Şirketimiz \"İzin Kullanma Esasları\"na göre, ait olduğu yıl içinde bir seferde kullanır.", normalFont));
                    noteCell.BackgroundColor = new BaseColor(240, 240, 240);
                    noteCell.Border = Rectangle.BOX;
                    noteCell.PaddingTop = 5;
                    noteCell.PaddingBottom = 5;
                    noteCell.PaddingLeft = 5;
                    noteCell.PaddingRight = 5;
                    noteTable.AddCell(noteCell);

                    document.Add(noteTable);
                    document.Add(new Paragraph(" "));

                    // İzin açıklaması
                    document.Add(new Paragraph("İzin Açıklaması:", boldFont));
                    PdfPTable adresTable = new PdfPTable(1);
                    adresTable.WidthPercentage = 100;

                    PdfPCell adresCell = new PdfPCell(new Phrase(izinAmaci, normalFont));
                    adresCell.MinimumHeight = 70;
                    adresCell.VerticalAlignment = Element.ALIGN_TOP;
                    adresTable.AddCell(adresCell);

                    document.Add(adresTable);
                    document.Add(new Paragraph(" "));

                    // İmza alanları
                    PdfPTable signatureTable = new PdfPTable(3);
                    signatureTable.WidthPercentage = 100;

                    // Personel imza alanı
                    PdfPCell personelSignatureCell = new PdfPCell();
                    personelSignatureCell.BorderWidthTop = 1f;
                    personelSignatureCell.BorderWidthBottom = 0f;
                    personelSignatureCell.BorderWidthLeft = 0f;
                    personelSignatureCell.BorderWidthRight = 0f;
                    personelSignatureCell.PaddingTop = 10;
                    personelSignatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    Paragraph personelSignatureText = new Paragraph();
                    personelSignatureText.Alignment = Element.ALIGN_CENTER;
                    personelSignatureText.Add(new Chunk("PERSONELİN\n", boldFont));
                    personelSignatureText.Add(new Chunk("ADI SOYADI: " + personelAdi + " " + personelSoyadi + "\n\n", normalFont));
                    personelSignatureText.Add(new Chunk("İMZASI:\n\n", normalFont));
                    personelSignatureText.Add(new Chunk("......./......./20......", normalFont));

                    personelSignatureCell.AddElement(personelSignatureText);
                    signatureTable.AddCell(personelSignatureCell);

                    // Birim amiri imza alanı
                    PdfPCell amirSignatureCell = new PdfPCell();
                    amirSignatureCell.BorderWidthTop = 1f;
                    amirSignatureCell.BorderWidthBottom = 0f;
                    amirSignatureCell.BorderWidthLeft = 0f;
                    amirSignatureCell.BorderWidthRight = 0f;
                    amirSignatureCell.PaddingTop = 10;
                    amirSignatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    Paragraph amirSignatureText = new Paragraph();
                    amirSignatureText.Alignment = Element.ALIGN_CENTER;
                    amirSignatureText.Add(new Chunk("BİRİM AMİRİNİN\n", boldFont));

                    if (!string.IsNullOrEmpty(idariAmirAdi) && !string.IsNullOrEmpty(idariAmirSoyadi))
                    {
                        amirSignatureText.Add(new Chunk("ADI SOYADI: " + idariAmirAdi + " " + idariAmirSoyadi + "\n\n", normalFont));
                    }
                    else
                    {
                        amirSignatureText.Add(new Chunk("ADI SOYADI:\n\n", normalFont));
                    }

                    amirSignatureText.Add(new Chunk("İMZASI:\n\n", normalFont));
                    amirSignatureText.Add(new Chunk("......./......./20......", normalFont));

                    amirSignatureCell.AddElement(amirSignatureText);
                    signatureTable.AddCell(amirSignatureCell);

                    // İK Personeli imza alanı
                    PdfPCell ikSignatureCell = new PdfPCell();
                    ikSignatureCell.BorderWidthTop = 1f;
                    ikSignatureCell.BorderWidthBottom = 0f;
                    ikSignatureCell.BorderWidthLeft = 0f;
                    ikSignatureCell.BorderWidthRight = 0f;
                    ikSignatureCell.PaddingTop = 10;
                    ikSignatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    Paragraph ikSignatureText = new Paragraph();
                    ikSignatureText.Alignment = Element.ALIGN_CENTER;
                    ikSignatureText.Add(new Chunk("İNSAN KAYNAKLARI\n", boldFont));

                    if (!string.IsNullOrEmpty(ikAdi) && !string.IsNullOrEmpty(ikSoyadi))
                    {
                        ikSignatureText.Add(new Chunk("ADI SOYADI: " + ikAdi + " " + ikSoyadi + "\n\n", normalFont));
                    }
                    else
                    {
                        ikSignatureText.Add(new Chunk("ADI SOYADI:\n\n", normalFont));
                    }

                    ikSignatureText.Add(new Chunk("İMZASI:\n\n", normalFont));
                    ikSignatureText.Add(new Chunk("......./......./20......", normalFont));

                    ikSignatureCell.AddElement(ikSignatureText);
                    signatureTable.AddCell(ikSignatureCell);

                    document.Add(signatureTable);

                    // Form referans numarası
                    Paragraph formRef = new Paragraph("F/M/TARD/EV001", new Font(baseFont, 6));
                    formRef.Alignment = Element.ALIGN_RIGHT;
                    document.Add(formRef);

                    document.Close();

                    // PDF'i döndür
                    byte[] pdfBytes = ms.ToArray();
                    string personelAdSoyad = personelAdi + "_" + personelSoyadi;
                    return File(pdfBytes, "application/pdf", $"IzinTalep_{personelAdSoyad}.pdf");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Izinlerim", new { error = "PDF oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }

        // Yardımcı metot - checkbox ile bilgi gösteren hücre
        private void AddCheckboxCellWithInfo(PdfPTable table, bool isChecked, byte gunSayisi, float izinSaati, Font izinBilgiFont)
        {
            PdfPCell cell = new PdfPCell();
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.MinimumHeight = 30f;

            Paragraph para = new Paragraph();
            if (isChecked)
            {
                para.Add(new Chunk("☑", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
                para.Add(new Chunk("\n" + gunSayisi + " gün " + izinSaati + " saat", izinBilgiFont));
            }
            else
            {
                para.Add(new Chunk("☐", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
            }
            para.Alignment = Element.ALIGN_CENTER;
            cell.AddElement(para);
            table.AddCell(cell);
        }

        // Yardımcı metotlar - bunları değiştirmeye gerek yok
        private void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.BackgroundColor = new BaseColor(230, 230, 230);
            table.AddCell(cell);
        }

        // Yardımcı metotlar


        private void AddCheckboxCell(PdfPTable table, bool isChecked)
        {
            PdfPCell cell = new PdfPCell();
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.MinimumHeight = 20f;

            Paragraph p = new Paragraph();
            if (isChecked)
            {
                p.Add(new Chunk("☑", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
            }
            else
            {
                p.Add(new Chunk("☐", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
            }

            cell.AddElement(p);
            table.AddCell(cell);
        }

        // BeklemetIzinTalebi metodu - düzeltilmiş versiyon
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> BeklemetIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // MikroDB'den kullanıcı numarasını al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                short kullaniciNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
                    }

                    kullaniciNo = Convert.ToInt16(result);
                }

                // Ana veritabanı bağlantı dizesini al
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    // İzin talebini beklemeye al (durum 1'den 0'a çevir) ve PERSONEL_IZINLERI'nden sil
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Önce izin talebinin durumunu kontrol et
                            string checkQuery = "SELECT pit_izin_durum FROM PERSONEL_IZIN_TALEPLERI WHERE pit_Guid = @guid";
                            using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@guid", guid);
                                var currentStatus = await checkCommand.ExecuteScalarAsync();

                                if (currentStatus == null)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "İzin talebi bulunamadı." });
                                }

                                int status = Convert.ToInt32(currentStatus);
                                if (status != 1)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "Bu izin talebi zaten işlenmiş veya onaylanmamış durumda." });
                                }
                            }

                            // 2. PERSONEL_IZIN_TALEPLERI tablosunu güncelle
                            string updateTalepQuery = @"
                        UPDATE PERSONEL_IZIN_TALEPLERI 
                        SET pit_izin_durum = 0, 
                            pit_onaylayan_kullanici = 0,
                            pit_lastup_user = @kullaniciNo,
                            pit_lastup_date = GETDATE(),
                            pit_degisti = 1,
                            pit_aciklama1 = 'Onaylanmış izin iptal edildi ve beklemeye alındı.'
                        WHERE pit_Guid = @guid 
                        AND pit_izin_durum = 1";

                            using (SqlCommand updateCommand = new SqlCommand(updateTalepQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@guid", guid);
                                updateCommand.Parameters.AddWithValue("@kullaniciNo", kullaniciNo);

                                int affectedRows = await updateCommand.ExecuteNonQueryAsync();

                                if (affectedRows == 0)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "İzin talebi güncellenemedi." });
                                }
                            }

                            // 3. PERSONEL_IZINLERI tablosundan ilgili kaydı sil
                            string deleteIzinQuery = @"
                        DELETE FROM PERSONEL_IZINLERI 
                        WHERE pz_bagli_talep_uid = @guid";

                            using (SqlCommand deleteCommand = new SqlCommand(deleteIzinQuery, connection, transaction))
                            {
                                deleteCommand.Parameters.AddWithValue("@guid", guid);
                                await deleteCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();

                            // Personel e-posta adresini al ve bildirim gönder
                            string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

                            if (!string.IsNullOrEmpty(personelEmail))
                            {
                                // İzin talebi detaylarını çek
                                var izinTalebi = await GetIzinTalebiDetaylariAsync(guid, connection);

                                if (izinTalebi != null)
                                {
                                    izinTalebi.ReddetmeNedeni = "Onaylanmış izin iptal edildi ve beklemeye alındı.";

                                    // Beklemeye alma bildirimi gönder
                                    await SendLeaveStatusChangeNotificationAsync(
                                        personelEmail,
                                        "BEKLEMEYE_ALINDI",
                                        izinTalebi
                                    );
                                }
                            }

                            return Json(new { success = true, message = "İzin talebi başarıyla beklemeye alındı." });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"HATA (BeklemetIzinTalebi - Transaction): {ex.Message}");
                            return Json(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA (BeklemetIzinTalebi): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // ReddetIzinTalebi metodu - düzeltilmiş versiyon
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ReddetIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi, string reddetmeNedeni)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // MikroDB'den kullanıcı numarasını al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                short onaylayanKullaniciNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
                    }

                    onaylayanKullaniciNo = Convert.ToInt16(result);
                }

                // Ana veritabanı bağlantı dizesini al
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Önce izin talebinin durumunu kontrol et
                            string checkQuery = "SELECT pit_izin_durum FROM PERSONEL_IZIN_TALEPLERI WHERE pit_Guid = @guid";
                            using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@guid", guid);
                                var currentStatus = await checkCommand.ExecuteScalarAsync();

                                if (currentStatus == null)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "İzin talebi bulunamadı." });
                                }

                                int status = Convert.ToInt32(currentStatus);
                                if (status != 1)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "Bu izin talebi zaten işlenmiş veya onaylanmamış durumda." });
                                }
                            }

                            // 2. Güncelleme sorgusu - izin durumunu 2 (Reddedildi) olarak ayarla ve reddetme nedenini ekle
                            string updateQuery = @"
                        UPDATE PERSONEL_IZIN_TALEPLERI 
                        SET pit_izin_durum = 2, 
                            pit_onaylayan_kullanici = @onaylayanKullaniciNo,
                            pit_lastup_user = @onaylayanKullaniciNo,
                            pit_lastup_date = GETDATE(),
                            pit_degisti = 1,
                            pit_aciklama1 = @reddetmeNedeni
                        WHERE pit_Guid = @guid 
                        AND pit_izin_durum = 1";

                            using (SqlCommand command = new SqlCommand(updateQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@guid", guid);
                                command.Parameters.AddWithValue("@onaylayanKullaniciNo", onaylayanKullaniciNo);
                                command.Parameters.AddWithValue("@reddetmeNedeni", !string.IsNullOrEmpty(reddetmeNedeni) ? reddetmeNedeni : "");

                                int affectedRows = await command.ExecuteNonQueryAsync();

                                if (affectedRows == 0)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "İzin talebi güncellenemedi." });
                                }
                            }

                            // 3. PERSONEL_IZINLERI tablosundan ilgili kaydı sil (eğer varsa)
                            string deleteIzinQuery = @"
                        DELETE FROM PERSONEL_IZINLERI 
                        WHERE pz_bagli_talep_uid = @guid";

                            using (SqlCommand deleteCommand = new SqlCommand(deleteIzinQuery, connection, transaction))
                            {
                                deleteCommand.Parameters.AddWithValue("@guid", guid);
                                await deleteCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();

                            // Personel e-posta adresini al ve bildirim gönder
                            string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

                            if (!string.IsNullOrEmpty(personelEmail))
                            {
                                // İzin talebi detaylarını çek
                                var izinTalebi = await GetIzinTalebiDetaylariAsync(guid, connection);

                                if (izinTalebi != null)
                                {
                                    izinTalebi.ReddetmeNedeni = reddetmeNedeni;

                                    // E-posta gönder
                                    await _emailService.SendLeaveRequestNotificationAsync(
                                        personelEmail,
                                        false,  // Reddedildi
                                        izinTalebi
                                    );
                                }
                            }

                            return Json(new { success = true, message = "İzin talebi başarıyla reddedildi." });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"HATA (ReddetIzinTalebi - Transaction): {ex.Message}");
                            return Json(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA (ReddetIzinTalebi): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Güncellenmiş IzinliPersonel metodu - kalan izin hakkı ve işe giriş tarihi ile
        public async Task<IActionResult> IzinliPersonel()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    System.Diagnostics.Debug.WriteLine("Username is null or empty");
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                System.Diagnostics.Debug.WriteLine($"mikroDbConnectionString: {mikroDbConnectionString}");

                int userNo = 0;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    try
                    {
                        await mikroConnection.OpenAsync();
                        using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                        userCommand.Parameters.AddWithValue("@username", username);
                        var result = await userCommand.ExecuteScalarAsync();

                        if (result == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"User '{username}' not found in KULLANICILAR table");
                            return View(new List<IzinTalepModel>());
                        }

                        userNo = Convert.ToInt32(result);
                        System.Diagnostics.Debug.WriteLine($"Found user with User_no: {userNo}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting User_no: {ex.Message}");
                        return View(new List<IzinTalepModel>());
                    }
                }

                List<IzinTalepModel> onayliIzinler = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();
                System.Diagnostics.Debug.WriteLine($"erpConnectionString: {erpConnectionString}");

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    try
                    {
                        await erpConnection.OpenAsync();

                        // Improved query for personelKodu - also check if there's a record but per_kod is NULL
                        string personelQuery = @"
                    SELECT per_kod, per_adi, per_soyadi 
                    FROM PERSONELLER 
                    WHERE per_UserNo = @userNo AND per_cikis_tar = '1899-12-31 00:00:00.000'";

                        string personelKodu = null;
                        string personelName = null;

                        using (SqlCommand personelCommand = new SqlCommand(personelQuery, erpConnection))
                        {
                            personelCommand.Parameters.AddWithValue("@userNo", userNo);

                            using (SqlDataReader personelReader = await personelCommand.ExecuteReaderAsync())
                            {
                                if (await personelReader.ReadAsync())
                                {
                                    if (!personelReader.IsDBNull(personelReader.GetOrdinal("per_kod")))
                                    {
                                        personelKodu = personelReader.GetString(personelReader.GetOrdinal("per_kod"));
                                        personelName = $"{personelReader.GetString(personelReader.GetOrdinal("per_adi"))} {personelReader.GetString(personelReader.GetOrdinal("per_soyadi"))}";
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Found personnel record for User_no {userNo}, but per_kod is NULL");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"No personnel record found for User_no {userNo}");
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(personelKodu))
                        {
                            System.Diagnostics.Debug.WriteLine($"personelKodu is null or empty for User_no {userNo}");

                            // Check if the user exists in the PERSONELLER table regardless of per_UserNo
                            // This might help debug mapping issues between users and personnel
                            using (SqlCommand checkCommand = new SqlCommand("SELECT COUNT(*) FROM PERSONELLER", erpConnection))
                            {
                                var totalRecords = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                                System.Diagnostics.Debug.WriteLine($"Total personnel records: {totalRecords}");
                            }

                            return View(new List<IzinTalepModel>());
                        }

                        System.Diagnostics.Debug.WriteLine($"Found personnel with code: {personelKodu}, name: {personelName}");

                        // Modified query to include both IdariAmirKodu and per_raporlama_yapacagi_per_kod relationships
                        using SqlCommand command = new SqlCommand(@"
WITH PersonelHiyerarsi AS (
    -- Direct administrative subordinates
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    WHERE p2.per_kod = @personelKodu

    UNION ALL

    -- Recursive hierarchy lookup for deeper levels
    SELECT 
        p1.per_kod,
        p2.per_kod,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
)
SELECT DISTINCT 
    t.pit_guid,
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date,
    t.pit_onaylayan_kullanici
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_izin_durum = 1 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)
ORDER BY t.pit_baslangictarih DESC", erpConnection);

                        command.Parameters.AddWithValue("@personelKodu", personelKodu);
                        System.Diagnostics.Debug.WriteLine("Executing main query to get approved leave requests");

                        using SqlDataReader leaveReader = await command.ExecuteReaderAsync();
                        int recordCount = 0;
                        while (await leaveReader.ReadAsync())
                        {
                            recordCount++;
                            onayliIzinler.Add(new IzinTalepModel
                            {
                                Guid = leaveReader.GetGuid(leaveReader.GetOrdinal("pit_guid")),
                                PersonelKodu = leaveReader.GetString(leaveReader.GetOrdinal("pit_pers_kod")),
                                PersonelAdSoyad = leaveReader.GetString(leaveReader.GetOrdinal("PersonelAdSoyad")),
                                IdariAmirKodu = !leaveReader.IsDBNull(leaveReader.GetOrdinal("per_IdariAmirKodu")) ? leaveReader.GetString(leaveReader.GetOrdinal("per_IdariAmirKodu")) : null,
                                IdariAmirAdi = !leaveReader.IsDBNull(leaveReader.GetOrdinal("IdariAmirAdi")) ? leaveReader.GetString(leaveReader.GetOrdinal("IdariAmirAdi")) : null,
                                TalepTarihi = leaveReader.GetDateTime(leaveReader.GetOrdinal("pit_talep_tarihi")),
                                IzinTipi = leaveReader.GetByte(leaveReader.GetOrdinal("pit_izin_tipi")),
                                GunSayisi = leaveReader.GetByte(leaveReader.GetOrdinal("pit_gun_sayisi")),
                                IzinSaat = !leaveReader.IsDBNull(leaveReader.GetOrdinal("pit_saat"))
                                    ? (float)leaveReader.GetDouble(leaveReader.GetOrdinal("pit_saat"))
                                    : 0.0f,
                                BaslangicTarihi = leaveReader.GetDateTime(leaveReader.GetOrdinal("pit_baslangictarih")),
                                BitisTarihi = leaveReader.GetDateTime(leaveReader.GetOrdinal("pit_BaslamaSaati")),
                                BaslamaSaati = !leaveReader.IsDBNull(leaveReader.GetOrdinal("pit_BaslamaSaati"))
                                    ? leaveReader.GetDateTime(leaveReader.GetOrdinal("pit_BaslamaSaati")).TimeOfDay.TotalHours
                                    : 0.0,
                                BitisSaati = !leaveReader.IsDBNull(leaveReader.GetOrdinal("pit_saat"))
                                    ? (float)leaveReader.GetDouble(leaveReader.GetOrdinal("pit_saat"))
                                    : 0.0f,
                                Amac = !leaveReader.IsDBNull(leaveReader.GetOrdinal("pit_amac")) ? leaveReader.GetString(leaveReader.GetOrdinal("pit_amac")) : string.Empty,
                                IzinDurumu = leaveReader.GetByte(leaveReader.GetOrdinal("pit_izin_durum")),
                                OlusturmaTarihi = leaveReader.GetDateTime(leaveReader.GetOrdinal("pit_create_date")),
                                OnaylayanKullanici = !leaveReader.IsDBNull(leaveReader.GetOrdinal("pit_onaylayan_kullanici"))
                                    ? leaveReader.GetInt32(leaveReader.GetOrdinal("pit_onaylayan_kullanici")).ToString()
                                    : null
                            });
                        }
                        System.Diagnostics.Debug.WriteLine($"Retrieved {recordCount} approved leave requests");

                        return View(onayliIzinler);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in database operations: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                        return View(new List<IzinTalepModel>());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }

        private async Task SendLeaveStatusChangeNotificationAsync(string email, string durumTipi, IzinTalepModel izinTalebi)
        {
            try
            {
                // SMTP ayarlarını konfigürasyondan al
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderDisplayName = _configuration["EmailSettings:SenderDisplayName"];

                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                // Durum tipine göre başlık ve içerik belirle
                string subject = durumTipi switch
                {
                    "BEKLEMEYE_ALINDI" => $"İzin Talebiniz Beklemeye Alındı: {izinTalebi.PersonelAdSoyad}",
                    _ => $"İzin Talebi Durum Değişikliği: {izinTalebi.PersonelAdSoyad}"
                };

                string durum = durumTipi switch
                {
                    "BEKLEMEYE_ALINDI" => "BEKLEMEYE ALINDI",
                    _ => "GÜNCELLENDI"
                };

                string durumRenk = durumTipi switch
                {
                    "BEKLEMEYE_ALINDI" => "#ffc107", // Warning yellow
                    _ => "#6c757d" // Secondary gray
                };

                // Türkçe tarih formatını ayarla
                System.Globalization.CultureInfo trCulture = new System.Globalization.CultureInfo("tr-TR");
                string talepTarihi = izinTalebi.TalepTarihi.ToString("dd.MM.yyyy", trCulture);
                string baslangicTarihi = izinTalebi.BaslangicTarihi.ToString("dd.MM.yyyy", trCulture);
                string bitisTarihi = izinTalebi.BitisTarihi.ToString("dd.MM.yyyy", trCulture);

                // E-posta içeriği
                string body = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 650px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {durumRenk}; padding: 15px; color: white; }}
        .header h1 {{ color: white; margin: 0; }}
        .content {{ padding: 20px 0; }}
        .status-badge {{ display: inline-block; background-color: {durumRenk}; color: white; padding: 5px 10px; border-radius: 4px; font-weight: bold; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-left: 3px solid {durumRenk}; }}
        .footer {{ font-size: 12px; color: #6c757d; margin-top: 30px; border-top: 1px solid #e9ecef; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>İzin Talebi Durum Değişikliği</h1>
        </div>
        <div class=""content"">
            <p>Sayın <strong>{izinTalebi.PersonelAdSoyad}</strong>,</p>
            
            <p>İzin talebinizin durumu değiştirilmiştir.</p>
            
            <p><span class=""status-badge"">DURUM: {durum}</span></p>
            
            <div class=""details"">
                <h3>İzin Talebi Detayları:</h3>
                <p><strong>Talep Tarihi:</strong> {talepTarihi}</p>
                <p><strong>İzin Başlangıç Tarihi:</strong> {baslangicTarihi}</p>
                <p><strong>İzin Bitiş Tarihi:</strong> {bitisTarihi}</p>
                <p><strong>İzin Günü Sayısı:</strong> {izinTalebi.GunSayisi}</p>
                <p><strong>İzin Amacı:</strong> {izinTalebi.Amac ?? "Belirtilmemiş"}</p>";

                if (!string.IsNullOrEmpty(izinTalebi.ReddetmeNedeni))
                {
                    body += $@"
                <p><strong>Açıklama:</strong> {izinTalebi.ReddetmeNedeni}</p>";
                }

                body += $@"
            </div>
            
            <p>Detaylar için İnsan Kaynakları portalını ziyaret edebilirsiniz.</p>
            
            <p>Bilgilerinize sunarız.</p>
        </div>
        <div class=""footer"">
            <p>Bu e-posta otomatik olarak oluşturulmuştur. Lütfen yanıtlamayınız.</p>
        </div>
    </div>
</body>
</html>";

                using (var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                })
                {
                    using (var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderDisplayName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        mailMessage.To.Add(email);
                        await client.SendMailAsync(mailMessage);
                        System.Diagnostics.Debug.WriteLine($"Durum değişikliği bildirimi gönderildi: {email}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Durum değişikliği e-posta gönderme hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç Hata: {ex.InnerException.Message}");
                }
            }
            finally
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }

        public async Task<IActionResult> IzinTalepleri()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();
                    userNo = Convert.ToInt32(result);
                    System.Diagnostics.Debug.WriteLine($"UserNo: {userNo}");
                }

                List<IzinTalepModel> izinTalepleri = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    // Personel kodunu al
                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand("SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo AND per_cikis_tar = '1899-12-31 00:00:00.000'", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                        System.Diagnostics.Debug.WriteLine($"PersonelKodu: {personelKodu}");
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        System.Diagnostics.Debug.WriteLine("PersonelKodu bulunamadı!");
                        return View(new List<IzinTalepModel>());
                    }

                    // Ana sorgu - Kalan izin hakkı hesaplaması da dahil
                    using SqlCommand command = new SqlCommand(@"
DECLARE @Yil INT = YEAR(GETDATE());

WITH PersonelHiyerarsi AS (
    -- Direct administrative subordinates
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    WHERE p2.per_kod = @personelKodu

    UNION ALL

    -- Recursive hierarchy lookup for deeper levels
    SELECT 
        p1.per_kod,
        p2.per_kod,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
),
IzinHakedisleri AS (
    SELECT 
        ph.AltPersonelKod,
        p.per_giris_tar,
        -- Çalışma süresi hesaplama
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE())
            ELSE 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE()) - 1
        END AS CalismaSuresiYil,
        -- Yıldönümü geçti mi kontrolü
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 1
            ELSE 0
        END AS YildonumuGectiMi
    FROM PersonelHiyerarsi ph
    INNER JOIN PERSONELLER p ON ph.AltPersonelKod = p.per_kod
),
YillikIzinHaklari AS (
    SELECT
        AltPersonelKod,
        CalismaSuresiYil,
        YildonumuGectiMi,
        CASE 
            WHEN YildonumuGectiMi = 0 THEN 0 -- Yıldönümü geçmediyse yeni izin yok
            WHEN CalismaSuresiYil >= 15 THEN 26
            WHEN CalismaSuresiYil > 5 THEN 20
            WHEN CalismaSuresiYil >= 1 THEN 14
            ELSE 0
        END AS HakEdilenYillikIzin
    FROM IzinHakedisleri
),
DevirIzinler AS (
    SELECT
        yih.AltPersonelKod,
        yih.HakEdilenYillikIzin,
        ISNULL((SELECT SUM(pro_gecyil_devir_izin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = yih.AltPersonelKod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinGun,
        ISNULL((SELECT SUM(pro_gecyil_devir_saatlikizin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = yih.AltPersonelKod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinSaat
    FROM YillikIzinHaklari yih
),
KullanilanIzinler AS (
    SELECT
        di.AltPersonelKod,
        di.HakEdilenYillikIzin,
        di.GecenYilDevirIzinGun,
        di.GecenYilDevirIzinSaat,
        ISNULL((
            SELECT SUM(
                CASE
                    WHEN pz_saat IS NOT NULL AND pz_saat > 0 THEN
                        CASE 
                            WHEN pz_saat <= 4 THEN 0.5 + pz_gun_sayisi
                            ELSE 1.0 + pz_gun_sayisi
                        END
                    ELSE pz_gun_sayisi
                END
            )
            FROM PERSONEL_IZINLERI WITH (NOLOCK) 
            WHERE pz_pers_kod = di.AltPersonelKod 
            AND pz_izin_yil = @Yil
AND pz_izin_tipi = 0  -- Only annual leave
        ), 0) AS KullanilanIzinGun
    FROM DevirIzinler di
),
KalanIzinler AS (
    SELECT
        ki.AltPersonelKod,
        CASE
            WHEN ki.GecenYilDevirIzinSaat IS NOT NULL AND ki.GecenYilDevirIzinSaat > 0 THEN
                CASE 
                    WHEN ki.GecenYilDevirIzinSaat <= 4 THEN 0.5 + (ki.GecenYilDevirIzinGun + ki.HakEdilenYillikIzin - ki.KullanilanIzinGun)
                    ELSE 1.0 + (ki.GecenYilDevirIzinGun + ki.HakEdilenYillikIzin - ki.KullanilanIzinGun)
                END
            ELSE (ki.GecenYilDevirIzinGun + ki.HakEdilenYillikIzin - ki.KullanilanIzinGun)
        END AS KalanIzinBakiyesi
    FROM KullanilanIzinler ki
)
SELECT DISTINCT 
    t.pit_guid,
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date,
    t.pit_onaylayan_kullanici,
    CAST(ISNULL(ki.KalanIzinBakiyesi, 0) AS DECIMAL(10,2)) as KalanIzinHakki
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
LEFT JOIN KalanIzinler ki ON t.pit_pers_kod = ki.AltPersonelKod
WHERE t.pit_izin_durum = 0 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)
ORDER BY t.pit_baslangictarih DESC", erpConnection);

                    command.Parameters.AddWithValue("@personelKodu", personelKodu);
                    System.Diagnostics.Debug.WriteLine("Ana sorgu çalıştırılıyor...");

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    int kayitSayisi = 0;
                    while (await reader.ReadAsync())
                    {
                        kayitSayisi++;
                        izinTalepleri.Add(new IzinTalepModel
                        {
                            Guid = reader.GetGuid(reader.GetOrdinal("pit_guid")),
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                            IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            IzinSaat = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
                                : 0.0f,
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            BitisTarihi = reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")),
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                            IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                            OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date")),
                            KalanIzinHakki = !reader.IsDBNull(reader.GetOrdinal("KalanIzinHakki"))
                                ? reader.GetDecimal(reader.GetOrdinal("KalanIzinHakki"))
                                : 0m
                        });
                        System.Diagnostics.Debug.WriteLine($"Kayıt eklendi: {izinTalepleri[izinTalepleri.Count - 1].PersonelAdSoyad}");
                    }

                    return View(izinTalepleri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        [HttpPost]
        [AllowAnonymous]
        private async Task<string> GetPersonelEmailAsync(string personelKodu, SqlConnection connection)
        {
            string emailQuery = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE per_kod = @personelKodu";

            using (SqlCommand emailCommand = new SqlCommand(emailQuery, connection))
            {
                emailCommand.Parameters.AddWithValue("@personelKodu", personelKodu);
                var emailResult = await emailCommand.ExecuteScalarAsync();

                return emailResult?.ToString();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        // İzin talebi detaylarını alma metodu
        private async Task<IzinTalepModel> GetIzinTalebiDetaylariAsync(Guid guid, SqlConnection connection)
        {
            string query = @"
SELECT 
    t.pit_guid,
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_BaslamaSaati AS BitisTarihi,
    t.pit_baslangictarih,
    t.pit_saat, -- İzin saati
    t.pit_amac,
    t.pit_aciklama1 as ReddetmeNedeni
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_guid = @guid";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@guid", guid);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var izinTalep = new IzinTalepModel
                        {
                            Guid = reader.GetGuid(reader.GetOrdinal("pit_guid")),
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            BitisTarihi = reader.GetDateTime(reader.GetOrdinal("BitisTarihi")),
                            IzinSaat = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                ? Convert.ToSingle(reader.GetDouble(reader.GetOrdinal("pit_saat")))
                                : 0.0f,
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac"))
                                ? reader.GetString(reader.GetOrdinal("pit_amac"))
                                : null,
                            ReddetmeNedeni = !reader.IsDBNull(reader.GetOrdinal("ReddetmeNedeni"))
                                ? reader.GetString(reader.GetOrdinal("ReddetmeNedeni"))
                                : null
                        };

                        // Önemli: IzinSaat değerinin 0 olmaması için kontrol ekliyoruz
                        System.Diagnostics.Debug.WriteLine($"İzin Talebi Detayları: Personel={izinTalep.PersonelAdSoyad}, " +
                            $"Talep Tarihi={izinTalep.TalepTarihi:dd.MM.yyyy}, İzin Saati={izinTalep.IzinSaat}");

                        return izinTalep;
                    }

                    return null;
                }
            }
        }
        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> OnaylaIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // MikroDB'den kullanıcı numarasını al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                short onaylayanKullaniciNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
                    }

                    onaylayanKullaniciNo = Convert.ToInt16(result);
                }

                // Ana veritabanı bağlantı dizesini al
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    // Önce izin talebinin tüm detaylarını al
                    string selectQuery = @"
DECLARE @currentGuid UNIQUEIDENTIFIER = @guid;

SELECT 
    pit_DBCno, pit_SpecRECno, pit_iptal, pit_fileid, pit_hidden, pit_kilitli, 
    pit_degisti, pit_checksum, pit_create_user, pit_create_date, 
    pit_special1, pit_special2, pit_special3, pit_pers_kod, 
    pit_mali_yil, pit_satir_no, pit_talep_tarihi, pit_izin_tipi, 
    pit_gun_sayisi, pit_yol_izni, pit_baslangictarih, 
    pit_cadde, pit_mahalle, pit_sokak, pit_Semt, pit_Apt_No, 
    pit_Daire_No, pit_posta_kodu, pit_ilce, pit_il, pit_ulke, 
    pit_adres_kodu, pit_tel1, pit_tel2, pit_email, pit_amac, 
    pit_aciklama1, pit_aciklama2, pit_saat, pit_BaslamaSaati, 
    pit_eksikcalismanedeni
FROM PERSONEL_IZIN_TALEPLERI 
WHERE pit_Guid = @currentGuid";

                    DataTable izinTalebiDetaylari = new DataTable();
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@guid", guid);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(selectCommand))
                        {
                            adapter.Fill(izinTalebiDetaylari);
                        }
                    }

                    if (izinTalebiDetaylari.Rows.Count == 0)
                    {
                        return Json(new { success = false, message = "İzin talebi bulunamadı." });
                    }

                    DataRow izinTalebi = izinTalebiDetaylari.Rows[0];

                    // İzin talebini güncelle
                    string updateQuery = @"
DECLARE @currentGuid UNIQUEIDENTIFIER = @guid;

UPDATE PERSONEL_IZIN_TALEPLERI 
SET 
    pit_izin_durum = 1, 
    pit_onaylayan_kullanici = @onaylayanKullaniciNo,
    pit_lastup_user = @onaylayanKullaniciNo,
    pit_lastup_date = GETDATE(),
    pit_degisti = 1  -- Değişiklik bayrağını işaretle
WHERE pit_Guid = @currentGuid 
AND pit_izin_durum = 0";

                    int affectedRows;
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@guid", guid);
                        updateCommand.Parameters.AddWithValue("@onaylayanKullaniciNo", onaylayanKullaniciNo);
                        affectedRows = await updateCommand.ExecuteNonQueryAsync();
                    }

                    if (affectedRows == 0)
                    {
                        return Json(new { success = false, message = "İzin talebi güncellenemedi." });
                    }

                    // PERSONEL_IZINLERI tablosuna kayıt ekle
                    string insertIzinQuery = @"
DECLARE @currentGuid UNIQUEIDENTIFIER = @guid;
DECLARE @pzGuid UNIQUEIDENTIFIER = NEWID();

INSERT INTO PERSONEL_IZINLERI (
    pz_Guid,
    pz_DBCno,
    pz_SpecRECno,
    pz_iptal,
    pz_fileid,
    pz_hidden,
    pz_kilitli,
    pz_degisti,
    pz_checksum,
    pz_create_user,
    pz_create_date,
    pz_lastup_user,
    pz_lastup_date,
    pz_special1,
    pz_special2,
    pz_special3,
    pz_izin_yil,
    pz_pers_kod,
    pz_izin_no,
    pz_izin_tipi,
    pz_gun_sayisi,
    pz_yol_izni,
    pz_baslangictarih,
    pz_bitistarihi,
    pz_bagli_talep_uid,
    pz_izin_aciklama,
    pz_gerceklesen_donus_tarihi,
    pz_isbasitarihi,
    pz_saat,
    pz_BaslamaSaati
    
) VALUES (
    @pzGuid,
    @dbcNo,
    @specRecNo,
    @iptal,
    @fileId,
    @hidden,
    @kilitli,
    1,  -- Değişiklik bayrağı
    @checksum,
    @createUser,
    GETDATE(),
    @lastupUser,
    GETDATE(),
    @special1,
    @special2,
    @special3,
    YEAR(GETDATE()),
    @personelKodu,
    (SELECT ISNULL(MAX(pz_izin_no), 0) + 1 FROM PERSONEL_IZINLERI WHERE pz_pers_kod = @personelKodu),
    @izinTipi,
    @gunSayisi,
    @yolIzni,
    @baslangicTarihi,
    DATEADD(DAY, @gunSayisi - 1, @baslangicTarihi),
    @currentGuid,
    @izinAmac,
    NULL,  -- Gerçekleşen dönüş tarihi
    DATEADD(DAY, @gunSayisi, @baslangicTarihi),
    @bitisSaati,
    @baslamaSaati
 
   
)";

                    using (SqlCommand insertCommand = new SqlCommand(insertIzinQuery, connection))
                    {
                        // PERSONEL_IZINLERI için parametre atamaları
                        insertCommand.Parameters.AddWithValue("@guid", guid);
                        insertCommand.Parameters.AddWithValue("@dbcNo", izinTalebi["pit_DBCno"]);
                        insertCommand.Parameters.AddWithValue("@specRecNo", izinTalebi["pit_SpecRECno"]);
                        insertCommand.Parameters.AddWithValue("@iptal", izinTalebi["pit_iptal"]);
                        insertCommand.Parameters.AddWithValue("@fileId", izinTalebi["pit_fileid"]);
                        insertCommand.Parameters.AddWithValue("@hidden", izinTalebi["pit_hidden"]);
                        insertCommand.Parameters.AddWithValue("@kilitli", izinTalebi["pit_kilitli"]);
                        insertCommand.Parameters.AddWithValue("@checksum", izinTalebi["pit_checksum"]);
                        insertCommand.Parameters.AddWithValue("@createUser", onaylayanKullaniciNo);
                        insertCommand.Parameters.AddWithValue("@lastupUser", onaylayanKullaniciNo);
                        insertCommand.Parameters.AddWithValue("@special1", izinTalebi["pit_special1"]);
                        insertCommand.Parameters.AddWithValue("@special2", izinTalebi["pit_special2"]);
                        insertCommand.Parameters.AddWithValue("@special3", izinTalebi["pit_special3"]);
                        insertCommand.Parameters.AddWithValue("@personelKodu", izinTalebi["pit_pers_kod"]);
                        insertCommand.Parameters.AddWithValue("@izinTipi", izinTalebi["pit_izin_tipi"]);
                        insertCommand.Parameters.AddWithValue("@gunSayisi", izinTalebi["pit_gun_sayisi"]);
                        insertCommand.Parameters.AddWithValue("@yolIzni", izinTalebi["pit_yol_izni"]);
                        insertCommand.Parameters.AddWithValue("@baslangicTarihi", izinTalebi["pit_baslangictarih"]);
                        insertCommand.Parameters.AddWithValue("@izinAmac", izinTalebi["pit_amac"] == DBNull.Value ? (object)DBNull.Value : izinTalebi["pit_amac"]);
                        insertCommand.Parameters.AddWithValue("@bitisSaati", izinTalebi["pit_saat"]);
                        insertCommand.Parameters.AddWithValue("@baslamaSaati", izinTalebi["pit_BaslamaSaati"]);


                        // Adres ve iletişim bilgileri


                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    // Personel e-posta adresini al
                    string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

                    if (!string.IsNullOrEmpty(personelEmail))
                    {
                        // İzin talebi detaylarını çek
                        var izinTalepModel = await GetIzinTalebiDetaylariAsync(guid, connection);

                        if (izinTalepModel != null)
                        {
                            // E-posta gönder
                            await _emailService.SendLeaveRequestNotificationAsync(
                                personelEmail,
                                true,  // Onaylandı
                                izinTalepModel
                            );
                        }
                        else
                        {
                            // İzin talebi detayları bulunamadı, manuel olarak model oluştur
                            var manuelModel = new IzinTalepModel
                            {
                                PersonelKodu = izinTalebi["pit_pers_kod"].ToString(),
                                TalepTarihi = Convert.ToDateTime(izinTalebi["pit_talep_tarihi"]),
                                IzinTipi = Convert.ToByte(izinTalebi["pit_izin_tipi"]),
                                GunSayisi = Convert.ToByte(izinTalebi["pit_gun_sayisi"]),
                                BaslangicTarihi = Convert.ToDateTime(izinTalebi["pit_baslangictarih"]),
                                BitisTarihi = Convert.ToDateTime(izinTalebi["pit_BaslamaSaati"]),
                                IzinSaat = izinTalebi["pit_saat"] != DBNull.Value
                                    ? Convert.ToSingle(izinTalebi["pit_saat"])
                                    : 0.0f,
                                Amac = izinTalebi["pit_amac"] != DBNull.Value ? izinTalebi["pit_amac"].ToString() : null
                            };

                            // Personel adını almak için ek sorgu
                            string personelBilgiQuery = "SELECT per_adi + ' ' + per_soyadi as PersonelAdSoyad FROM PERSONELLER WHERE per_kod = @personelKodu";
                            using (SqlCommand personelBilgiCmd = new SqlCommand(personelBilgiQuery, connection))
                            {
                                personelBilgiCmd.Parameters.AddWithValue("@personelKodu", personelKodu);
                                var adSoyadResult = await personelBilgiCmd.ExecuteScalarAsync();
                                if (adSoyadResult != null)
                                {
                                    manuelModel.PersonelAdSoyad = adSoyadResult.ToString();
                                }
                            }

                            await _emailService.SendLeaveRequestNotificationAsync(
                                personelEmail,
                                true,  // Onaylandı
                                manuelModel
                            );
                        }
                    }

                    return Json(new { success = true, message = "İzin talebi başarıyla onaylandı." });
                }
            }
            catch (Exception ex)
            {
                // Hatayı log'la
                System.Diagnostics.Debug.WriteLine($"HATA (OnaylaIzinTalebi): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Hata yanıtını döndür
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen sistem yöneticinize başvurun." });
            }
        }
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> ReddetIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi, string reddetmeNedeni)
        //{
        //    try
        //    {
        //        string username = HttpContext.Session.GetString("Username");
        //        string version = HttpContext.Session.GetString("SelectedVersion");

        //        if (string.IsNullOrEmpty(username))
        //        {
        //            return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
        //        }

        //        // MikroDB'den kullanıcı numarasını al
        //        string mikroDbConnectionString = version == "V16"
        //            ? _configuration.GetConnectionString("MikroDB_V16")
        //            : _configuration.GetConnectionString("MikroDesktop");

        //        short onaylayanKullaniciNo;
        //        using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
        //        {
        //            await mikroConnection.OpenAsync();
        //            using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
        //            userCommand.Parameters.AddWithValue("@username", username);
        //            var result = await userCommand.ExecuteScalarAsync();

        //            if (result == null)
        //            {
        //                return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
        //            }

        //            onaylayanKullaniciNo = Convert.ToInt16(result);
        //        }

        //        // Ana veritabanı bağlantı dizesini al
        //        string erpConnectionString = _dbSelectorService.GetConnectionString();

        //        using (SqlConnection connection = new SqlConnection(erpConnectionString))
        //        {
        //            await connection.OpenAsync();

        //            // Güncelleme sorgusu - izin durumunu 2 (Reddedildi) olarak ayarla ve reddetme nedenini ekle
        //            string updateQuery = @"
        //    UPDATE PERSONEL_IZIN_TALEPLERI 
        //    SET pit_izin_durum = 2, 
        //        pit_onaylayan_kullanici = @onaylayanKullaniciNo,
        //        pit_lastup_user = @onaylayanKullaniciNo,
        //        pit_lastup_date = GETDATE(),
        //        pit_aciklama1 = @reddetmeNedeni
        //    WHERE pit_Guid = @guid 
        //    AND pit_izin_durum = 0";

        //            using (SqlCommand command = new SqlCommand(updateQuery, connection))
        //            {
        //                command.Parameters.AddWithValue("@guid", guid);
        //                command.Parameters.AddWithValue("@personelKodu", personelKodu);
        //                command.Parameters.AddWithValue("@talepTarihi", talepTarihi);
        //                command.Parameters.AddWithValue("@onaylayanKullaniciNo", onaylayanKullaniciNo);
        //                command.Parameters.AddWithValue("@reddetmeNedeni", !string.IsNullOrEmpty(reddetmeNedeni) ? reddetmeNedeni : "");

        //                int affectedRows = await command.ExecuteNonQueryAsync();

        //                if (affectedRows > 0)
        //                {
        //                    // Personel e-posta adresini al
        //                    string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

        //                    if (!string.IsNullOrEmpty(personelEmail))
        //                    {
        //                        // İzin talebi detaylarını çek
        //                        var izinTalebi = await GetIzinTalebiDetaylariAsync(guid, connection);

        //                        if (izinTalebi != null)
        //                        {
        //                            izinTalebi.ReddetmeNedeni = reddetmeNedeni;

        //                            // E-posta gönder
        //                            await _emailService.SendLeaveRequestNotificationAsync(
        //                                personelEmail,
        //                                false,  // Reddedildi
        //                                izinTalebi
        //                            );
        //                        }
        //                        else
        //                        {
        //                            // İzin talebi detayları bulunamadı, manuel olarak oluşturalım
        //                            var manuelModel = new IzinTalepModel
        //                            {
        //                                PersonelKodu = personelKodu,
        //                                ReddetmeNedeni = reddetmeNedeni,
        //                                TalepTarihi = talepTarihi,
        //                                // Diğer bilgileri eklemek için ek sorgular gerekebilir
        //                            };

        //                            // Personel adını almak için ek sorgu
        //                            string personelBilgiQuery = "SELECT per_adi + ' ' + per_soyadi as PersonelAdSoyad FROM PERSONELLER WHERE per_kod = @personelKodu";
        //                            using (SqlCommand personelBilgiCmd = new SqlCommand(personelBilgiQuery, connection))
        //                            {
        //                                personelBilgiCmd.Parameters.AddWithValue("@personelKodu", personelKodu);
        //                                var adSoyadResult = await personelBilgiCmd.ExecuteScalarAsync();
        //                                if (adSoyadResult != null)
        //                                {
        //                                    manuelModel.PersonelAdSoyad = adSoyadResult.ToString();
        //                                }
        //                            }

        //                            // İzin talebinin diğer detaylarını al
        //                            string izinDetayQuery = @"
        //SELECT 
        //    pit_baslangictarih, pit_BaslamaSaati, pit_gun_sayisi, pit_izin_tipi, pit_saat, pit_amac
        //FROM PERSONEL_IZIN_TALEPLERI 
        //WHERE pit_guid = @guid";

        //                            using (SqlCommand izinDetayCmd = new SqlCommand(izinDetayQuery, connection))
        //                            {
        //                                izinDetayCmd.Parameters.AddWithValue("@guid", guid);
        //                                using (SqlDataReader detayReader = await izinDetayCmd.ExecuteReaderAsync())
        //                                {
        //                                    if (await detayReader.ReadAsync())
        //                                    {
        //                                        manuelModel.BaslangicTarihi = detayReader.GetDateTime(detayReader.GetOrdinal("pit_baslangictarih"));
        //                                        manuelModel.BitisTarihi = detayReader.GetDateTime(detayReader.GetOrdinal("pit_BaslamaSaati"));
        //                                        manuelModel.GunSayisi = detayReader.GetByte(detayReader.GetOrdinal("pit_gun_sayisi"));
        //                                        manuelModel.IzinTipi = detayReader.GetByte(detayReader.GetOrdinal("pit_izin_tipi"));

        //                                        if (!detayReader.IsDBNull(detayReader.GetOrdinal("pit_saat")))
        //                                            manuelModel.IzinSaat = Convert.ToSingle(detayReader.GetDouble(detayReader.GetOrdinal("pit_saat")));

        //                                        if (!detayReader.IsDBNull(detayReader.GetOrdinal("pit_amac")))
        //                                            manuelModel.Amac = detayReader.GetString(detayReader.GetOrdinal("pit_amac"));
        //                                    }
        //                                }
        //                            }

        //                            await _emailService.SendLeaveRequestNotificationAsync(
        //                                personelEmail,
        //                                false,  // Reddedildi
        //                                manuelModel
        //                            );
        //                        }
        //                    }

        //                    return Json(new { success = true, message = "İzin talebi başarıyla reddedildi." });
        //                }
        //                else
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"İzin talebi reddedilemedi - Personel Kodu: {personelKodu}, Talep Tarihi: {talepTarihi}");
        //                    return Json(new { success = false, message = "İzin talebi reddedilemedi. Lütfen tekrar deneyin." });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Hatayı log'la
        //        System.Diagnostics.Debug.WriteLine($"HATA (ReddetIzinTalebi): {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

        //        // Hata yanıtını döndür
        //        return Json(new { success = false, message = "Bir hata oluştu. Lütfen sistem yöneticinize başvurun." });
        //    }
        //}

        public async Task<IActionResult> ReddedilenIzinler()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();
                    userNo = Convert.ToInt32(result);
                }

                List<IzinTalepModel> reddedilenIzinler = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand("SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo AND per_cikis_tar = '1899-12-31 00:00:00.000'", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        return View(new List<IzinTalepModel>());
                    }

                    // IzinliPersonel metodundan alınan sorguyu, izin_durum = 2 olacak şekilde ve pit_aciklama1 ekleyerek düzenliyoruz
                    using SqlCommand command = new SqlCommand(@"
WITH PersonelHiyerarsi AS (
    -- Direct administrative or reporting subordinates
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    WHERE p2.per_kod = @personelKodu

    UNION ALL

    -- Recursive hierarchy lookup for deeper levels
    SELECT 
        p1.per_kod,
        p2.per_kod,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod OR p1.per_raporlama_yapacagi_per_kod = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
)
SELECT DISTINCT 
    t.pit_guid,  -- Added GUID field
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date,
    t.pit_onaylayan_kullanici,
    t.pit_aciklama1 as ReddetmeNedeni
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_izin_durum = 2 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)
ORDER BY t.pit_baslangictarih DESC", erpConnection);

                    command.Parameters.AddWithValue("@personelKodu", personelKodu);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        // IzinTipiAdi artık kullanılmıyor, bu kodu kaldırıyoruz

                        reddedilenIzinler.Add(new IzinTalepModel
                        {
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                            IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            // IzinTipiAdi artık kullanılmıyor
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),


                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                            IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                            OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date")),
                            OnaylayanKullanici = !reader.IsDBNull(reader.GetOrdinal("pit_onaylayan_kullanici"))
                                ? reader.GetInt32(reader.GetOrdinal("pit_onaylayan_kullanici")).ToString()
                                : null,
                            // Kullanıcı adını ayrıca almıyoruz, sadece ID kullanıyoruz
                            ReddetmeNedeni = !reader.IsDBNull(reader.GetOrdinal("ReddetmeNedeni"))
                                ? reader.GetString(reader.GetOrdinal("ReddetmeNedeni"))
                                : null
                        });
                    }

                    return View(reddedilenIzinler);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        [HttpGet]
        [AllowAnonymous]

        public async Task<IActionResult> Izinlerim()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(userNo))
                {
                    return View(new List<IzinTalepModel>());
                }

                List<IzinTalepModel> izinTalepleri = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand(
                        "SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo AND per_cikis_tar = '1899-12-31 00:00:00.000'", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        return View(new List<IzinTalepModel>());
                    }

                    // İşe başlama tarihlerini de içeren genişletilmiş sorgu
                    string query = @"
            SELECT 
                t.pit_guid,
                t.pit_pers_kod,
                p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
                p.per_IdariAmirKodu,
                ISNULL((SELECT TOP 1 per_adi + ' ' + per_soyadi 
                 FROM PERSONELLER 
                 WHERE per_kod = p.per_IdariAmirKodu), '') as IdariAmirAdi,
                t.pit_talep_tarihi,
                t.pit_izin_tipi,
                t.pit_gun_sayisi,
                t.pit_baslangictarih,
                t.pit_BaslamaSaati,
                t.pit_saat,
                t.pit_amac,
                t.pit_izin_durum,
                t.pit_create_date,
                t.pit_onaylayan_kullanici,
                ISNULL((SELECT TOP 1 per_adi + ' ' + per_soyadi 
                 FROM PERSONELLER 
                 WHERE per_UserNo = t.pit_onaylayan_kullanici), '') as OnaylayanKullanici,
                ISNULL(u.IsbaslamaTarihi, DATEADD(DAY, 1, t.pit_BaslamaSaati)) as IseBaslamaTarihi
            FROM PERSONEL_IZIN_TALEPLERI t
            INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
            LEFT JOIN PERSONEL_IZIN_TALEPLERI_user u ON t.pit_guid = u.Record_uid
            WHERE t.pit_pers_kod = @personelKodu
            ORDER BY t.pit_talep_tarihi DESC";

                    using (SqlCommand command = new SqlCommand(query, erpConnection))
                    {
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                izinTalepleri.Add(new IzinTalepModel
                                {
                                    Guid = reader.GetGuid(reader.GetOrdinal("pit_guid")),
                                    PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                                    PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                                    IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                                    IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                                    TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                                    IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                                    GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                                    IzinSaat = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                        ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
                                        : 0.0f,
                                    BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                                    BitisTarihi = reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")),
                                    BaslamaSaati = !reader.IsDBNull(reader.GetOrdinal("pit_BaslamaSaati"))
                                        ? reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")).TimeOfDay.TotalHours
                                        : 0.0,
                                    BitisSaati = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                        ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
                                        : 0.0f,
                                    Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                                    IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                                    OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date")),
                                    OnaylayanKullanici = !reader.IsDBNull(reader.GetOrdinal("OnaylayanKullanici"))
                                        ? reader.GetString(reader.GetOrdinal("OnaylayanKullanici"))
                                        : null,
                                    // İşe başlama tarihini direkt olarak ata
                                    IseBaslamaTarihi = reader.GetDateTime(reader.GetOrdinal("IseBaslamaTarihi"))
                                });
                            }
                        }
                    }
                }

                return View(izinTalepleri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        public async Task<IActionResult> hakedisraporu()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                string currentPersonelKodu;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();

                    // Get user number
                    using (SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection))
                    {
                        userCommand.Parameters.AddWithValue("@username", username);
                        var result = await userCommand.ExecuteScalarAsync();
                        userNo = Convert.ToInt32(result);
                    }
                }

                List<LeaveEntitlementReportModel> hakedisRaporu = new List<LeaveEntitlementReportModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    // Get current user's personnel code
                    string currentPersonelQuery = "SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo AND per_cikis_tar = '1899-12-31 00:00:00.000'";
                    using (SqlCommand personelCommand = new SqlCommand(currentPersonelQuery, erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        currentPersonelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(currentPersonelKodu))
                    {
                        return View(new List<LeaveEntitlementReportModel>());
                    }

                    // Tamamen GetLeaveEntitlementInfo metoduyla aynı hesaplama mantığına sahip sorgu
                    string query = @"
DECLARE @Yil INT = YEAR(GETDATE());

WITH PersonelHiyerarsi AS (
    -- Alt personelleri ve raporlama yapılan personelleri bul
    SELECT DISTINCT 
        p.per_kod as PersonelKod,
        p.per_adi as PersonelAdi,
        p.per_soyadi as PersonelSoyadi,
        p.per_giris_tar as IseGirisTarihi,
        p.per_dept_kod as DepartmanKodu,
        p.per_kim_gorev as Gorev,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE())
            ELSE 
                DATEDIFF(YEAR, p.per_giris_tar, GETDATE()) - 1
        END AS CalismaSuresiYil,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(p.per_giris_tar), DAY(p.per_giris_tar)) <= GETDATE() THEN 1
            ELSE 0
        END AS YildonumuGectiMi
    FROM PERSONELLER p
    WHERE 
        p.per_IdariAmirKodu = @currentPersonelKodu OR 
        p.per_raporlama_yapacagi_per_kod = @currentPersonelKodu
),
DepartmanBilgileri AS (
    SELECT 
        ph.PersonelKod,
        ph.PersonelAdi,
        ph.PersonelSoyadi,
        ph.IseGirisTarihi,
        ph.CalismaSuresiYil,
        ph.YildonumuGectiMi,
        ph.Gorev,
        d.pdp_adi as DepartmanAdi
    FROM PersonelHiyerarsi ph
    LEFT JOIN DEPARTMANLAR d ON ph.DepartmanKodu = d.pdp_kodu
),
IzinHaklari AS (
    SELECT
        PersonelKod,
        PersonelAdi,
        PersonelSoyadi,
        IseGirisTarihi,
        CalismaSuresiYil,
        YildonumuGectiMi,
        Gorev,
        DepartmanAdi,
        CASE 
            WHEN YildonumuGectiMi = 0 THEN 0 -- Yıldönümü geçmediyse yeni izin yok
            WHEN CalismaSuresiYil >= 15 THEN 26
            WHEN CalismaSuresiYil > 5 THEN 20
            WHEN CalismaSuresiYil >= 1 THEN 14
            ELSE 0
        END AS HakEdilenYillikIzin
    FROM DepartmanBilgileri
),
GecenYilDevredilen AS (
    SELECT
        IH.PersonelKod,
        IH.PersonelAdi,
        IH.PersonelSoyadi,
        IH.IseGirisTarihi,
        IH.CalismaSuresiYil,
        IH.YildonumuGectiMi,
        IH.Gorev,
        IH.DepartmanAdi,
        IH.HakEdilenYillikIzin,
        ISNULL((SELECT SUM(pro_gecyil_devir_izin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.PersonelKod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinGun,
        ISNULL((SELECT SUM(pro_gecyil_devir_saatlikizin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.PersonelKod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinSaat
    FROM IzinHaklari IH
),
KullanilanIzinler AS (
    SELECT
        GYD.PersonelKod,
        GYD.PersonelAdi,
        GYD.PersonelSoyadi,
        GYD.IseGirisTarihi,
        GYD.CalismaSuresiYil,
        GYD.YildonumuGectiMi,
        GYD.Gorev,
        GYD.DepartmanAdi,
        GYD.HakEdilenYillikIzin,
        GYD.GecenYilDevirIzinGun,
        GYD.GecenYilDevirIzinSaat,
        -- GetLeaveEntitlementInfo ile aynı hesaplama mantığı
        ISNULL((
            SELECT SUM(
                CASE
                    -- Saat kaydı varsa, saate göre hesapla (VE GÜN SAYISINA EKLE)
                    WHEN pz_saat IS NOT NULL AND pz_saat > 0 THEN
                        CASE 
                            WHEN pz_saat <= 4 THEN 0.5 + pz_gun_sayisi  -- 4 saat ve altı: yarım gün + gün sayısı
                            ELSE 1.0 + pz_gun_sayisi                   -- 4 saat üstü: tam gün + gün sayısı
                        END
                    -- Saat kaydı yoksa normal gün sayısını kullan
                    ELSE pz_gun_sayisi
                END
            )
            FROM PERSONEL_IZINLERI WITH (NOLOCK) 
            WHERE pz_pers_kod = GYD.PersonelKod 
            AND pz_izin_yil = @Yil
AND pz_izin_tipi = 0  -- Only annual leave
        ), 0) AS KullanilanIzinGun
    FROM GecenYilDevredilen GYD
)
SELECT
    PersonelKod as PersonelKodu,
    PersonelAdi,
    PersonelSoyadi,
    IseGirisTarihi,
    Gorev,
    DepartmanAdi as Departman,
    CalismaSuresiYil,
    YildonumuGectiMi,
    HakEdilenYillikIzin as YillikIzinHakki,
    GecenYilDevirIzinGun as GecenYilDevirGun,
    GecenYilDevirIzinSaat as GecenYilDevirSaat,
    KullanilanIzinGun as KullanilanIzinGun,
    -- GetLeaveEntitlementInfo ile birebir aynı hesaplama
    CASE
        -- Saat kaydı varsa, saate göre hesapla
        WHEN GecenYilDevirIzinSaat IS NOT NULL AND GecenYilDevirIzinSaat > 0 THEN
            CASE 
                WHEN GecenYilDevirIzinSaat <= 4 THEN 0.5 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)  -- 4 saat ve altı: yarım gün
                ELSE 1.0 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)                   -- 4 saat üstü: tam gün
            END
        -- Saat kaydı yoksa normal gün sayısını kullan
        ELSE (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)
    END as KalanIzinBakiyesi
FROM KullanilanIzinler
ORDER BY CalismaSuresiYil DESC, PersonelAdi";

                    using (SqlCommand command = new SqlCommand(query, erpConnection))
                    {
                        command.Parameters.AddWithValue("@currentPersonelKodu", currentPersonelKodu);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                hakedisRaporu.Add(new LeaveEntitlementReportModel
                                {
                                    PersonelKodu = reader["PersonelKodu"].ToString(),
                                    PersonelAdi = reader["PersonelAdi"].ToString(),
                                    PersonelSoyadi = reader["PersonelSoyadi"].ToString(),
                                    IseGirisTarihi = Convert.ToDateTime(reader["IseGirisTarihi"]),
                                    CalismaSuresiYil = Convert.ToInt32(reader["CalismaSuresiYil"]),
                                    YillikIzinHakki = Convert.ToInt32(reader["YillikIzinHakki"]),
                                    GecenYilDevirGun = Convert.ToDecimal(reader["GecenYilDevirGun"]),
                                    GecenYilDevirSaat = Convert.ToDecimal(reader["GecenYilDevirSaat"]),
                                    KullanilanIzinGun = Convert.ToDecimal(reader["KullanilanIzinGun"]),
                                    KalanIzinBakiyesi = Convert.ToDecimal(reader["KalanIzinBakiyesi"]),
                                    Gorev = reader["Gorev"].ToString(),
                                    Departman = reader["Departman"].ToString()
                                });
                            }
                        }
                    }
                }

                return View(hakedisRaporu);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<LeaveEntitlementReportModel>());
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetLeaveEntitlementInfo(string personnelCode, int year)
        {
            try
            {
                if (string.IsNullOrEmpty(personnelCode))
                {
                    return Json(new { success = false, message = "Personel kodu boş olamaz" });
                }

                string erpConnectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    // Comprehensive leave entitlement calculation query
                    string query = @"
           DECLARE @Yil INT = @year;

WITH PersonelBilgileri AS (
    SELECT 
        per_kod,
        per_adi,
        per_soyadi,
        per_giris_tar,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(per_giris_tar), DAY(per_giris_tar)) <= GETDATE() THEN 
                DATEDIFF(YEAR, per_giris_tar, GETDATE())
            ELSE 
                DATEDIFF(YEAR, per_giris_tar, GETDATE()) - 1
        END AS CalismaSuresiYil,
        CASE
            WHEN DATEFROMPARTS(@Yil, MONTH(per_giris_tar), DAY(per_giris_tar)) <= GETDATE() THEN 1
            ELSE 0
        END AS YildonumuGectiMi
    FROM PERSONELLER WITH (NOLOCK)
    WHERE per_kod = @personnelCode
),
IzinHaklari AS (
    SELECT
        per_kod,
        per_adi,
        per_soyadi,
        per_giris_tar,
        CalismaSuresiYil,
        YildonumuGectiMi,
        CASE 
            WHEN YildonumuGectiMi = 0 THEN 0 -- Yıldönümü geçmediyse yeni izin yok
            WHEN CalismaSuresiYil >= 15 THEN 26
            WHEN CalismaSuresiYil > 5 THEN 20
            WHEN CalismaSuresiYil >= 1 THEN 14
            ELSE 0
        END AS HakEdilenYillikIzin
    FROM PersonelBilgileri
),
GecenYilDevredilen AS (
    SELECT
        IH.per_kod,
        IH.per_adi,
        IH.per_soyadi,
        IH.CalismaSuresiYil,
        IH.YildonumuGectiMi,
        IH.HakEdilenYillikIzin,
        ISNULL((SELECT SUM(pro_gecyil_devir_izin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.per_kod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinGun,
        ISNULL((SELECT SUM(pro_gecyil_devir_saatlikizin) 
                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                WHERE pro_kodozet = IH.per_kod 
                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinSaat
    FROM IzinHaklari IH
),
KullanilanIzinler AS (
    SELECT
        GYD.per_kod,
        GYD.per_adi,
        GYD.per_soyadi,
        GYD.CalismaSuresiYil,
        GYD.YildonumuGectiMi,
        GYD.HakEdilenYillikIzin,
        GYD.GecenYilDevirIzinGun,
        GYD.GecenYilDevirIzinSaat,
        -- Kullanılan izin günleri (izin saatine göre ayarlanmış)
        ISNULL((
            SELECT SUM(
                CASE
                    -- Saat kaydı varsa, saate göre hesapla
                    WHEN pz_saat IS NOT NULL AND pz_saat > 0 THEN
                        CASE 
                            WHEN pz_saat <= 4 THEN 0.5 + pz_gun_sayisi  -- 4 saat ve altı: yarım gün
                            ELSE 1.0 + pz_gun_sayisi                    -- 4 saat üstü: tam gün
                        END
                    -- Saat kaydı yoksa normal gün sayısını kullan
                    ELSE pz_gun_sayisi
                END
            )
            FROM PERSONEL_IZINLERI WITH (NOLOCK) 
            WHERE pz_pers_kod = GYD.per_kod 
            AND pz_izin_yil = @Yil
AND pz_izin_tipi = 0  -- Only annual leave

        ), 0) AS KullanilanIzinGun
    FROM GecenYilDevredilen GYD
)
SELECT
    CalismaSuresiYil AS CalismaSuresi,
    YildonumuGectiMi AS YildonumuGectiMi,
    HakEdilenYillikIzin AS YillikIzinHakki,
    GecenYilDevirIzinGun AS GecenYilDevirGun,
    GecenYilDevirIzinSaat AS GecenYilDevirSaat,
    KullanilanIzinGun AS KullanilanIzinGun,
    -- Kalan izin bakiyesi hesaplama
    CASE
        -- Saat kaydı varsa, saate göre hesapla
        WHEN GecenYilDevirIzinSaat IS NOT NULL AND GecenYilDevirIzinSaat > 0 THEN
            CASE 
                WHEN GecenYilDevirIzinSaat <= 4 THEN 0.5 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)  -- 4 saat ve altı: yarım gün
                ELSE 1.0 + (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)                   -- 4 saat üstü: tam gün
            END
        -- Saat kaydı yoksa normal gün sayısını kullan
        ELSE (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun)
    END AS KalanIzinBakiyesi
FROM KullanilanIzinler";

                    using (SqlCommand command = new SqlCommand(query, erpConnection))
                    {
                        command.Parameters.AddWithValue("@personnelCode", personnelCode);
                        command.Parameters.AddWithValue("@year", year);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                double kullanilanIzinGun = Convert.ToDouble(reader["KullanilanIzinGun"]);
                                double gecenYilDevirGun = Convert.ToDouble(reader["GecenYilDevirGun"]);
                                double gecenYilDevirSaat = Convert.ToDouble(reader["GecenYilDevirSaat"]);
                                int yillikIzinHakki = Convert.ToInt32(reader["YillikIzinHakki"]);
                                double kalanIzinBakiyesi = Convert.ToDouble(reader["KalanIzinBakiyesi"]);

                                // Detaylı izin bilgilerini JSON ile dön
                                return Json(new
                                {
                                    success = true,
                                    seniorityYears = Convert.ToInt32(reader["CalismaSuresi"]),
                                    previousYearDays = gecenYilDevirGun,
                                    previousYearHours = gecenYilDevirSaat,
                                    yearlyEntitlement = yillikIzinHakki,
                                    usedDays = kullanilanIzinGun,
                                    remainingDays = kalanIzinBakiyesi,
                                    detailedBreakdown = new
                                    {
                                        carriedOverDays = gecenYilDevirGun,
                                        carriedOverHours = gecenYilDevirSaat,
                                        currentYearEntitlement = yillikIzinHakki,
                                        totalAvailableDays = gecenYilDevirGun + yillikIzinHakki,
                                        totalUsedDays = kullanilanIzinGun,
                                        totalRemainingDays = kalanIzinBakiyesi
                                    }
                                });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Personel bilgisi bulunamadı" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "İzin bilgileri alınırken bir hata oluştu",
                    errorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// İşe giriş tarihine göre yıllık izin hakkını hesaplar
        /// (1-5 yıl arası 14 gün, 5+ yıl 21 gün)
        /// </summary>
        private int CalculateYearlyEntitlement(DateTime startDate)
        {
            int yearsOfService = (DateTime.Now - startDate).Days / 365;

            if (yearsOfService < 1)
            {
                return 0; // 1 yıldan az çalışma süresi varsa izin hakkı yok
            }
            else if (yearsOfService < 5)
            {
                return 14; // 1-5 yıl arası 14 gün
            }
            else
            {
                return 21; // 5+ yıl 21 gün
            }
        }

        [HttpGet]
        [Route("Hr/GetIseBaslamaTarihi")]
        [AllowAnonymous]
        public async Task<IActionResult> GetIseBaslamaTarihi(Guid izinGuid)
        {
            try
            {
                if (izinGuid == Guid.Empty)
                {
                    return Json(new { success = false, message = "Geçersiz izin talebi." });
                }

                string connectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // İşe başlama tarihini PERSONEL_IZIN_TALEPLERI_user tablosundan al
                    string iseBaslamaSorgusu = @"
                SELECT IsbaslamaTarihi 
                FROM PERSONEL_IZIN_TALEPLERI_user 
                WHERE Record_uid = @izinGuid";

                    using (SqlCommand iseBaslamaCommand = new SqlCommand(iseBaslamaSorgusu, connection))
                    {
                        iseBaslamaCommand.Parameters.AddWithValue("@izinGuid", izinGuid);
                        var result = await iseBaslamaCommand.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            DateTime iseBaslamaTarihi = Convert.ToDateTime(result);
                            return Json(new
                            {
                                success = true,
                                iseBaslamaTarihi = iseBaslamaTarihi.ToString("dd.MM.yyyy")
                            });
                        }
                        else
                        {
                            // Eğer işe başlama tarihi yoksa, bitiş tarihinden sonraki günü al
                            string izinBitisSorgusu = @"
                        SELECT pit_BaslamaSaati 
                        FROM PERSONEL_IZIN_TALEPLERI 
                        WHERE pit_guid = @izinGuid";

                            using (SqlCommand bitisTarihiCommand = new SqlCommand(izinBitisSorgusu, connection))
                            {
                                bitisTarihiCommand.Parameters.AddWithValue("@izinGuid", izinGuid);
                                var bitisTarihiResult = await bitisTarihiCommand.ExecuteScalarAsync();

                                if (bitisTarihiResult != null && bitisTarihiResult != DBNull.Value)
                                {
                                    DateTime bitisTarihi = Convert.ToDateTime(bitisTarihiResult);
                                    DateTime iseBaslamaTarihi = bitisTarihi.AddDays(1);

                                    return Json(new
                                    {
                                        success = true,
                                        iseBaslamaTarihi = iseBaslamaTarihi.ToString("dd.MM.yyyy")
                                    });
                                }
                            }
                        }
                    }

                    return Json(new { success = false, message = "İşe başlama tarihi bulunamadı." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

    }
}