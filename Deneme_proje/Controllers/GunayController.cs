using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient; // ADO.NET için
using Microsoft.Extensions.Configuration; // appsettings.json'dan connection string çekmek için
using System.Collections.Generic;
using static Deneme_proje.Models.GunayEntities;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class GunayController : BaseController
    {
        private readonly GunayRepository _gunayRepository;
        private readonly ILogger<GunayController> _logger;
        private readonly IConfiguration _configuration; // appsettings.json'dan connection string almak için
        private readonly string _connectionString;

        public GunayController(GunayRepository gunayRepository, ILogger<GunayController> logger, IConfiguration configuration)
        {
            _gunayRepository = gunayRepository ?? throw new ArgumentNullException(nameof(gunayRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ERPDatabase");  // Connection string'i appsettings.json'dan alıyoruz
        }

        public IActionResult GenelKurallar()
        {
            return View();
        }
        public IActionResult FiloKiralama(DateTime? startDate, DateTime? endDate, string srmrkodu)

        {
            // Set default dates if not provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, 1, 1); // January 1st of the current year
            }
            if (!endDate.HasValue)
            {
                endDate = new DateTime(DateTime.Now.Year, 12, 31); // December 31st of the current year
            }

            try
            {
                // Fetch the list of responsible codes for the dropdown
                var sorumluKodlari = _gunayRepository.GetTumSorumluKodlari().ToList();
                ViewBag.SorumluKodlari = new SelectList(sorumluKodlari, "SorumluKodu", "SorumluAdi");

                // Keep selected dates in ViewBag
                ViewBag.SelectedStartDate = startDate.Value;
                ViewBag.SelectedEndDate = endDate.Value;

                // Get prim data from WEB_ayarlar_prim table
                var primData = GetPrimData();

                if (!string.IsNullOrEmpty(srmrkodu))
                {
                    var data = _gunayRepository.GetFiloKiralamaData(startDate.Value, endDate.Value, srmrkodu);

                    // Prim değerlerini ViewBag'e ekleyelim
                    ViewBag.Prim1Gunluk = primData.BirGunluk;
                    ViewBag.Prim2_7Gunluk = primData.IkiYediGunluk;
                    ViewBag.Prim7_15Gunluk = primData.YediOnbesGunluk;
                    ViewBag.Prim15PlusGunluk = primData.OnbesPlusGunluk;

                    // Yeni eklenen prim değerlerini ViewBag'e ekleyelim
                    ViewBag.EkSigortaKapsamiEkstraAylik = primData.EkSigortaKapsamiEkstraAylik;
                    ViewBag.EkSurucuGunluk = primData.EkSurucuGunluk;
                    ViewBag.EkSurucu = primData.EkSurucu;
                    ViewBag.BebekKoltuguEkstrasiAylik = primData.BebekKoltuguEkstrasiAylik;

                    return View(data);
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading FiloKiralama data.");
                return View("Error");
            }
        }


        public IActionResult AgirVasitaSatis(
      DateTime? startDate,
      DateTime? endDate,
      List<string> selectedScopes)
        {
            // Varsayılan tarih aralığını ayarla
            startDate ??= new DateTime(DateTime.Now.Year, 1, 1);
            endDate ??= new DateTime(DateTime.Now.Year, 12, 31);

            try
            {
                // Seçilen tarih aralığını ViewBag'e kaydet
                ViewBag.SelectedStartDate = startDate.Value;
                ViewBag.SelectedEndDate = endDate.Value;

                // Seçilen kapsamları ViewBag'e kaydet
                ViewBag.SelectedScopes = selectedScopes ?? new List<string>();

                // Satış prim verilerini al
                var primData = GetPrimDataForSatis();

                // Prim değerlerini ViewBag'e ekle
                PopulatePrimValuesInViewBag(primData);

                // Ağır Vasıta Satış verilerini al
                var data = _gunayRepository.GetAgirVasitaSatisData(startDate.Value, endDate.Value, selectedScopes);
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ağır Vasıta Satış verilerini yüklerken hata oluştu.");
                return View("Error");
            }
        }
        private void PopulatePrimValuesInViewBag(WebAyarlarPrim primData)
        {
            ViewBag.SKO_FP60 = primData.SKO_FP60;
            ViewBag.SCS = primData.SCS;
            ViewBag.SCU_FP45_FPK_DD = primData.SCU_FP45_FPK_DD;
            ViewBag.Rayic0_250 = primData.Rayic0_250;
            ViewBag.Rayic250_500 = primData.Rayic250_500;
            ViewBag.Rayic500Uzeri = primData.Rayic500Uzeri;
            ViewBag.Thermoking = primData.Thermoking;
            ViewBag.Valohr = primData.valohr; // Düzeltilmiş: 'Thermoking' yerine 'Valohr'
        }

        public IActionResult AgirVasitaServis(DateTime? startDate, DateTime? endDate, string srmrkodu)
        {
            // Set default dates if not provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, 1, 1); // January 1st of the current year
            }
            if (!endDate.HasValue)
            {
                endDate = new DateTime(DateTime.Now.Year, 12, 31); // December 31st of the current year
            }

            try
            {
                // Fetch the list of responsible codes for the dropdown
                var sorumluKodlari = _gunayRepository.GetTumSorumluKodlari().ToList();
                ViewBag.SorumluKodlari = new SelectList(sorumluKodlari, "SorumluKodu", "SorumluAdi");

                // Keep selected dates in ViewBag
                ViewBag.SelectedStartDate = startDate.Value;
                ViewBag.SelectedEndDate = endDate.Value;

                // Get prim data for TanimlarServis (Servis Primleri)
                var primData = GetPrimDataForServis();

                if (!string.IsNullOrEmpty(srmrkodu))
                {
                    var data = _gunayRepository.GetFiloKiralamaData(startDate.Value, endDate.Value, srmrkodu);

                    // Servis Prim değerlerini ViewBag'e ekleyelim
                    ViewBag.Servis250Alti = primData.Servis250Alti;
                    ViewBag.Servis250_2500 = primData.Servis250_2500;
                    ViewBag.Servis2500_5000 = primData.Servis2500_5000;
                    ViewBag.Servis5000Uzeri = primData.Servis5000Uzeri;
                    ViewBag.ServisEkstraMontaj = primData.ServisEkstraMontaj;
                    ViewBag.ServisYeniMotorMontaj = primData.ServisYeniMotorMontaj;

                    return View(data);
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading AgirVasitaServis data.");
                return View("Error");
            }
        }

        private WebAyarlarPrim GetPrimDataForSatis()
        {
            var prim = new WebAyarlarPrim();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 SKO_FP60, SCS, SCU_FP45_FPK_DD, rayic_0_250, rayic_250_500, rayic_500uzeri, thermoking,valohr " +
                                   "FROM WEB_ayarlar_prim"; // Sadece satış primlerini çekiyoruz

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                prim.SKO_FP60 = Convert.ToSingle(reader["SKO_FP60"]);
                                prim.SCS = Convert.ToSingle(reader["SCS"]);
                                prim.SCU_FP45_FPK_DD = Convert.ToSingle(reader["SCU_FP45_FPK_DD"]);
                                prim.Rayic0_250 = Convert.ToSingle(reader["rayic_0_250"]);
                                prim.Rayic250_500 = Convert.ToSingle(reader["rayic_250_500"]);
                                prim.Rayic500Uzeri = Convert.ToSingle(reader["rayic_500uzeri"]);
                                prim.Thermoking = Convert.ToSingle(reader["thermoking"]);
                                prim.Thermoking = Convert.ToSingle(reader["valohr"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış Prim verileri yüklenirken hata oluştu.");
            }

            return prim;
        }

        private WebAyarlarPrim GetPrimDataForServis()
        {
            var prim = new WebAyarlarPrim();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 servis_250alti, servis_250_2500, servis_2500_5000, servis_5000uzeri, servis_ekstramontaj, servis_yeni_motor_montaj " +
                                   "FROM WEB_ayarlar_prim"; // Sadece servis primlerini çekiyoruz

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                prim.Servis250Alti = Convert.ToSingle(reader["servis_250alti"]);
                                prim.Servis250_2500 = Convert.ToSingle(reader["servis_250_2500"]);
                                prim.Servis2500_5000 = Convert.ToSingle(reader["servis_2500_5000"]);
                                prim.Servis5000Uzeri = Convert.ToSingle(reader["servis_5000uzeri"]);
                                prim.ServisEkstraMontaj = Convert.ToSingle(reader["servis_ekstramontaj"]);
                                prim.ServisYeniMotorMontaj = Convert.ToSingle(reader["servis_yeni_motor_montaj"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis Prim verileri yüklenirken hata oluştu.");
            }

            return prim;
        }

        private WebAyarlarPrim GetPrimDataForOtokoc()
        {
            var prim = new WebAyarlarPrim();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 gunay_grup_satis, sirket_disi_pesin, sirket_disi_otuz_gun, sirket_disi_altmis_gun " +
                                   "FROM WEB_ayarlar_prim"; // Sadece servis primlerini çekiyoruz

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                prim.gunay_grup_satis = Convert.ToSingle(reader["gunay_grup_satis"]);
                                prim.sirket_disi_pesin = Convert.ToSingle(reader["sirket_disi_pesin"]);
                                prim.sirket_disi_otuz_gun = Convert.ToSingle(reader["sirket_disi_otuz_gun"]);
                                prim.sirket_disi_altmis_gun = Convert.ToSingle(reader["sirket_disi_altmis_gun"]);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis Prim verileri yüklenirken hata oluştu.");
            }

            return prim;
        }




        public IActionResult Otokoc()
        {
            try
            {
                // Prosedürden verileri çekiyoruz
                var primData = GetPrimDataForOtokoc();
                var data = _gunayRepository.OtokocData();

                // Servis Prim değerlerini ViewBag'e ekleyelim
                ViewBag.gunay_grup_satis = primData.gunay_grup_satis;
                ViewBag.sirket_disi_pesin = primData.sirket_disi_pesin;
                ViewBag.sirket_disi_otuz_gun = primData.sirket_disi_otuz_gun;
                ViewBag.sirket_disi_altmis_gun = primData.sirket_disi_altmis_gun;

                return View(data);
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapabiliriz
                _logger.LogError(ex, "Stok hareketleri yüklenirken bir hata oluştu.");

                // Hata mesajını ViewBag ile göndererek aynı sayfaya dönün
                ViewBag.ErrorMessage = "Veriler yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(new List<OtokocViewModel>()); // Boş liste gönder
            }
        }



        // WEB_ayarlar_prim tablosundan prim verilerini çeken yardımcı metot
        private WebAyarlarPrim GetPrimData()
        {
            var prim = new WebAyarlarPrim();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 bir_gunluk, iki_yedi_gunluk, yedi_onbes_gunluk, onbes_plus_gunluk, EkSigortaKapsamiEkstraAylik,EkSurucuGunluk,EkSurucu,BebekKoltuguEkstrasiAylik  FROM WEB_ayarlar_prim"; // İlk satırı çekiyoruz

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                prim.BirGunluk = Convert.ToSingle(reader["bir_gunluk"]);
                                prim.IkiYediGunluk = Convert.ToSingle(reader["iki_yedi_gunluk"]);
                                prim.YediOnbesGunluk = Convert.ToSingle(reader["yedi_onbes_gunluk"]);
                                prim.OnbesPlusGunluk = Convert.ToSingle(reader["onbes_plus_gunluk"]);
                                prim.EkSigortaKapsamiEkstraAylik = Convert.ToSingle(reader["EkSigortaKapsamiEkstraAylik"]);
                                prim.EkSurucuGunluk = Convert.ToSingle(reader["EkSurucuGunluk"]);
                                prim.EkSurucu = Convert.ToSingle(reader["EkSurucu"]);
                                prim.BebekKoltuguEkstrasiAylik = Convert.ToSingle(reader["BebekKoltuguEkstrasiAylik"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prim verileri yüklenirken hata oluştu.");
            }

            return prim;
        }


        [HttpGet]
        public IActionResult Tanimlar()
        {
            var primList = new List<WebAyarlarPrim>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT id, bir_gunluk, iki_yedi_gunluk, yedi_onbes_gunluk, onbes_plus_gunluk, " +
                                   "EkSigortaKapsamiEkstraAylik, EkSurucuGunluk, EkSurucu, BebekKoltuguEkstrasiAylik, " +
                                   "servis_250alti, servis_250_2500, servis_2500_5000, servis_5000uzeri, " +
                                   "servis_ekstramontaj, SKO_FP60, SCS, SCU_FP45_FPK_DD, rayic_0_250, rayic_250_500, " +
                                   "rayic_500uzeri, thermoking, valohr, servis_yeni_motor_montaj, gunay_grup_satis, sirket_disi_pesin, sirket_disi_otuz_gun, sirket_disi_altmis_gun FROM WEB_ayarlar_prim";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var prim = new WebAyarlarPrim
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    BirGunluk = Convert.ToSingle(reader["bir_gunluk"]),
                                    IkiYediGunluk = Convert.ToSingle(reader["iki_yedi_gunluk"]),
                                    YediOnbesGunluk = Convert.ToSingle(reader["yedi_onbes_gunluk"]),
                                    OnbesPlusGunluk = Convert.ToSingle(reader["onbes_plus_gunluk"]),
                                    EkSigortaKapsamiEkstraAylik = Convert.ToSingle(reader["EkSigortaKapsamiEkstraAylik"]),
                                    EkSurucuGunluk = Convert.ToSingle(reader["EkSurucuGunluk"]),
                                    EkSurucu = Convert.ToSingle(reader["EkSurucu"]),
                                    BebekKoltuguEkstrasiAylik = Convert.ToSingle(reader["BebekKoltuguEkstrasiAylik"]),

                                    // Yeni sütunlar
                                    Servis250Alti = Convert.ToSingle(reader["servis_250alti"]),
                                    Servis250_2500 = Convert.ToSingle(reader["servis_250_2500"]),
                                    Servis2500_5000 = Convert.ToSingle(reader["servis_2500_5000"]),
                                    Servis5000Uzeri = Convert.ToSingle(reader["servis_5000uzeri"]),
                                    ServisEkstraMontaj = Convert.ToSingle(reader["servis_ekstramontaj"]),
                                    SKO_FP60 = Convert.ToSingle(reader["SKO_FP60"]),
                                    SCS = Convert.ToSingle(reader["SCS"]),
                                    SCU_FP45_FPK_DD = Convert.ToSingle(reader["SCU_FP45_FPK_DD"]),
                                    Rayic0_250 = Convert.ToSingle(reader["rayic_0_250"]),
                                    Rayic250_500 = Convert.ToSingle(reader["rayic_250_500"]),
                                    Rayic500Uzeri = Convert.ToSingle(reader["rayic_500uzeri"]),
                                    Thermoking = Convert.ToSingle(reader["thermoking"]),
                                    valohr = Convert.ToSingle(reader["valohr"]),
                                    ServisYeniMotorMontaj = Convert.ToSingle(reader["servis_yeni_motor_montaj"]),

                                    gunay_grup_satis = Convert.ToSingle(reader["gunay_grup_satis"]),
                                    sirket_disi_pesin = Convert.ToSingle(reader["sirket_disi_pesin"]),
                                    sirket_disi_otuz_gun = Convert.ToSingle(reader["sirket_disi_otuz_gun"]),
                                    sirket_disi_altmis_gun = Convert.ToSingle(reader["sirket_disi_altmis_gun"])
                                };
                                primList.Add(prim);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);  // Hata durumunda loglama
                return View("Error");
            }

            return View(primList);  // Verileri View'e gönderiyoruz
        }



        // POST metodu: Formdan gelen verilerle güncelleme işlemi yapar
        [HttpPost]
        public IActionResult Tanimlar(int Id, float BirGunluk, float IkiYediGunluk, float YediOnbesGunluk, float OnbesPlusGunluk,
                                  float EkSigortaKapsamiEkstraAylik, float EkSurucuGunluk, float EkSurucu, float BebekKoltuguEkstrasiAylik)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "UPDATE WEB_ayarlar_prim SET bir_gunluk = @BirGunluk, iki_yedi_gunluk = @IkiYediGunluk, " +
                                   "yedi_onbes_gunluk = @YediOnbesGunluk, onbes_plus_gunluk = @OnbesPlusGunluk, " +
                                   "EkSigortaKapsamiEkstraAylik = @EkSigortaKapsamiEkstraAylik, EkSurucuGunluk = @EkSurucuGunluk, " +
                                   "EkSurucu = @EkSurucu, BebekKoltuguEkstrasiAylik = @BebekKoltuguEkstrasiAylik " + // Fazladan virgül kaldırıldı
                                   "WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        command.Parameters.AddWithValue("@BirGunluk", BirGunluk);
                        command.Parameters.AddWithValue("@IkiYediGunluk", IkiYediGunluk);
                        command.Parameters.AddWithValue("@YediOnbesGunluk", YediOnbesGunluk);
                        command.Parameters.AddWithValue("@OnbesPlusGunluk", OnbesPlusGunluk);
                        command.Parameters.AddWithValue("@EkSigortaKapsamiEkstraAylik", EkSigortaKapsamiEkstraAylik);
                        command.Parameters.AddWithValue("@EkSurucuGunluk", EkSurucuGunluk);
                        command.Parameters.AddWithValue("@EkSurucu", EkSurucu);
                        command.Parameters.AddWithValue("@BebekKoltuguEkstrasiAylik", BebekKoltuguEkstrasiAylik);

                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Tanimlar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);  // Hata durumunda loglama
                return View("Error");
            }
        }


        // POST metodu: Satis primlerini güncelleme işlemi yapar
        [HttpPost]
        public IActionResult TanimlarSatis(int Id, float SKO_FP60, float SCS, float SCU_FP45_FPK_DD,
                              float Rayic0_250, float Rayic250_500, float Rayic500Uzeri, float Thermoking, float valohr)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();


                    string query = "UPDATE WEB_ayarlar_prim SET SKO_FP60 = @SKO_FP60, SCS = @SCS, SCU_FP45_FPK_DD = @SCU_FP45_FPK_DD, " +
                                   "rayic_0_250 = @Rayic0_250, rayic_250_500 = @Rayic250_500,  @valohr=valohr, rayic_500uzeri = @Rayic500Uzeri, thermoking = @Thermoking " + // Virgül kaldırıldı
                                   "WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        command.Parameters.AddWithValue("@SKO_FP60", SKO_FP60);
                        command.Parameters.AddWithValue("@SCS", SCS);
                        command.Parameters.AddWithValue("@SCU_FP45_FPK_DD", SCU_FP45_FPK_DD);
                        command.Parameters.AddWithValue("@Rayic0_250", Rayic0_250);
                        command.Parameters.AddWithValue("@Rayic250_500", Rayic250_500);
                        command.Parameters.AddWithValue("@valohr", valohr);
                        command.Parameters.AddWithValue("@Rayic500Uzeri", Rayic500Uzeri);
                        command.Parameters.AddWithValue("@Thermoking", Thermoking);


                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Tanimlar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);  // Hata durumunda loglama
                return View("Error");
            }
        }


        // POST metodu: Servis primlerini güncelleme işlemi yapar
        [HttpPost]
        public IActionResult TanimlarServis(int Id, float Servis250Alti, float Servis250_2500, float Servis2500_5000, float Servis5000Uzeri,
                              float ServisEkstraMontaj, float ServisYeniMotorMontaj)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "UPDATE WEB_ayarlar_prim SET servis_250alti = @Servis250Alti, servis_250_2500 = @Servis250_2500, " +
                                   "servis_2500_5000 = @Servis2500_5000, servis_5000uzeri = @Servis5000Uzeri, servis_ekstramontaj = @ServisEkstraMontaj, " +
                                   "servis_yeni_motor_montaj = @ServisYeniMotorMontaj " + // Virgül kaldırıldı
                                   "WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        command.Parameters.AddWithValue("@Servis250Alti", Servis250Alti);
                        command.Parameters.AddWithValue("@Servis250_2500", Servis250_2500);
                        command.Parameters.AddWithValue("@Servis2500_5000", Servis2500_5000);
                        command.Parameters.AddWithValue("@Servis5000Uzeri", Servis5000Uzeri);
                        command.Parameters.AddWithValue("@ServisEkstraMontaj", ServisEkstraMontaj);
                        command.Parameters.AddWithValue("@ServisYeniMotorMontaj", ServisYeniMotorMontaj);

                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Tanimlar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);  // Hata durumunda loglama
                return View("Error");
            }
        }



        public IActionResult TanimlarOtokoc(int Id, float gunay_grup_satis, float sirket_disi_pesin, float sirket_disi_otuz_gun, float sirket_disi_altmis_gun)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "UPDATE WEB_ayarlar_prim SET gunay_grup_satis = @gunay_grup_satis, sirket_disi_pesin = @sirket_disi_pesin " +
                                   "sirket_disi_otuz_gun = @sirket_disi_otuz_gun, sirket_disi_altmis_gun = @sirket_disi_altmis_gun" + // Virgül kaldırıldı
                                   "WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        command.Parameters.AddWithValue("@gunay_grup_satis", gunay_grup_satis);
                        command.Parameters.AddWithValue("@sirket_disi_pesin", sirket_disi_pesin);
                        command.Parameters.AddWithValue("@sirket_disi_otuz_gun", sirket_disi_otuz_gun);
                        command.Parameters.AddWithValue("@sirket_disi_altmis_gun", sirket_disi_altmis_gun);


                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Tanimlar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);  // Hata durumunda loglama
                return View("Error");
            }
        }

    }
}

