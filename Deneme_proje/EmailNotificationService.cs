using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using static Deneme_proje.Models.HrEntities;

// Bu sınıf, izin talepleri için e-posta gönderme işlemlerini yönetir
public class EmailNotificationService
{
    // Konfigürasyon ayarlarını almak için bağımlılık enjeksiyonu
    private readonly IConfiguration _configuration;

    // Yapıcı metot - Konfigürasyon ayarlarını alır
    public EmailNotificationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // E-posta gönderme metodunun asıl işlevi
    public async Task SendLeaveRequestNotificationAsync(
        string recipientEmail,  // Alıcı e-posta adresi
        bool isApproved,         // İzin onaylandı mı? 
        IzinTalepModel leaveRequest)  // İzin talebi detayları
    {
        // Temel doğrulamalar
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            System.Diagnostics.Debug.WriteLine("E-posta adresi boş olamaz.");
            return;
        }

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

            // E-posta başlığını duruma göre belirle
            string subject = isApproved
                ? "İzin Talebiniz Onaylandı"
                : "İzin Talebiniz Reddedildi";

            // E-posta içeriğini hazırla
            string body = PrepareEmailBody(leaveRequest, isApproved);

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
                    mailMessage.To.Add(recipientEmail);

                    try
                    {
                        // E-postayı gönder
                        await client.SendMailAsync(mailMessage);

                        // Başarılı gönderim log'u
                        System.Diagnostics.Debug.WriteLine(
                            $"E-posta gönderildi: {recipientEmail} - Onay Durumu: {(isApproved ? "Onaylandı" : "Reddedildi")}");
                    }
                    catch (Exception sendEx)
                    {
                        // Gönderim sırasındaki hatayı detaylı logla
                        System.Diagnostics.Debug.WriteLine(
                            $"E-posta gönderme hatası: {sendEx.Message}");

                        // İç içe hata varsa onu da logla
                        if (sendEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"İç Hata: {sendEx.InnerException.Message}");
                        }

                        // Hata durumunda üst katmana hata fırlat
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Genel hata yakalama
            System.Diagnostics.Debug.WriteLine(
                $"E-posta ayarlama hatası: {ex.Message}");

            // İç içe hata varsa onu da logla
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"İç Hata: {ex.InnerException.Message}");
            }

            throw;
        }
        finally
        {
            // Güvenlik için sertifika doğrulamasını geri yükle
            System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
        }
    }

    // E-posta içeriğini HTML formatında hazırlama metodu
    private string PrepareEmailBody(IzinTalepModel leaveRequest, bool isApproved)
    {
        CultureInfo trCulture = new CultureInfo("tr-TR");
        string talepTarihi = leaveRequest.TalepTarihi.ToString("dd.MM.yyyy", trCulture);
        string baslangicTarihi = leaveRequest.BaslangicTarihi.ToString("dd.MM.yyyy", trCulture);
        string bitisTarihi = leaveRequest.BitisTarihi.ToString("dd.MM.yyyy", trCulture);

        string izinSaatText = "Belirtilmemiş";
        if (leaveRequest.IzinSaat > 0)
        {
            if (Math.Abs(leaveRequest.IzinSaat % 1) < 0.001)
                izinSaatText = ((int)leaveRequest.IzinSaat).ToString();
            else
                izinSaatText = leaveRequest.IzinSaat.ToString("N2", trCulture).Replace(".00", "");
        }

        string loginUrl = _configuration["AppSettings:LoginUrl"] ?? "https://hr.dioki.com.tr/Login/LoginKullanici";

        return $@"
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
        a {{ color: #007bff; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{(isApproved ? "İzin Talebiniz Onaylandı" : "İzin Talebiniz Reddedildi")}</h1>
        </div>
        <div class=""content"">
            <p>Sayın {leaveRequest.PersonelAdSoyad},</p>
            <p>{(isApproved
                    ? "İzin talebiniz başarıyla onaylanmıştır."
                    : "Üzülerek belirtmek isteriz ki, izin talebiniz reddedilmiştir.")} <a href=""{loginUrl}"">Giriş Yap</a></p>
            <div class=""details"">
                <h3>İzin Detayları:</h3>
                <p><strong>Talep Tarihi:</strong> {talepTarihi}</p>
                <p><strong>İzin Başlangıç Tarihi:</strong> {baslangicTarihi}</p>
                <p><strong>İzin Bitiş Tarihi:</strong> {bitisTarihi}</p>
                <p><strong>İzin Günü Sayısı:</strong> {leaveRequest.GunSayisi}</p>
                <p><strong>İzin Saati:</strong> {izinSaatText}</p>
                <p><strong>İzin Amacı:</strong> {leaveRequest.Amac ?? "Belirtilmemiş"}</p>
                {(isApproved ? "" : $@"
                <p><strong>Red Nedeni:</strong> {leaveRequest.ReddetmeNedeni ?? "Neden belirtilmemiş"}</p>
                ")}
            </div>
            <p>Detaylı bilgi için İnsan Kaynakları departmanı ile iletişime geçebilirsiniz.</p>
        </div>
    </div>
</body>
</html>";
    }
}