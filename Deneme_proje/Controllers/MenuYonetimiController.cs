using Deneme_proje.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class MenuYonetimiController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MenuYonetimiController> _logger;

        public MenuYonetimiController(
            IConfiguration configuration,
            ILogger<MenuYonetimiController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? userNo = null)
        {
            try
            {
                var kullanicilar = await GetKullanicilar();
                var selectedUser = userNo != null
                    ? kullanicilar.FirstOrDefault(k => k.UserNo == userNo)
                    : kullanicilar.FirstOrDefault();

                if (selectedUser == null)
                {
                    return NotFound("Kullanıcı bulunamadı.");
                }

                var menuItems = await GetMenuItems();

                // Kategorilere göre grupla
                var kategoriliMenuler = menuItems
                    .GroupBy(m => m.MenuBaslik ?? "Diğer")
                    .OrderBy(g => GetMenuBaslikSirasi(g.Key))
                    .ToDictionary(g => g.Key, g => g.OrderBy(item => item.SiraNo).ToList());

                var model = new MenuYonetimiViewModel
                {
                    Kullanicilar = kullanicilar,
                    SelectedUserNo = selectedUser.UserNo,
                    MenuItems = menuItems, // Geriye uyumluluk için
                    KategoriliMenuler = kategoriliMenuler
                };

                ViewBag.SelectedUserName = selectedUser.UserName;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menü yönetimi sayfası yüklenirken hata oluştu");
                return View("Error");
            }
        }

        private int GetMenuBaslikSirasi(string MenuBaslik)
        {
            return MenuBaslik switch
            {
                "Finans Raporları" => 1,
                "Satış Raporları" => 2,
                "Dbt Aktarım" => 3,
                "Bakım ve Servis" => 4,
                "Üretim Ayarları" => 5,
                "Üretim " => 6,
                "İş Emirleri" => 7,
                "Sipariş İzleme" => 8,
                "Özel Raporlar" => 9,
                "HR" => 10,
                "Yetki ve Yönetim" => 11,
                _ => 999
            };
        }

        [AllowAnonymous]
        private async Task<List<MenuItemModel>> GetMenuItems()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ERPDatabase");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var items = await connection.QueryAsync<MenuItemModel>(@"
                    SELECT 
                        Id, 
                        ControllerAdi, 
                        ActionAdi, 
                        MenuAdi, 
                        Yetki, 
                        SiraNo, 
                        ParentId, 
                        Icon,
                        ISNULL(MenuBaslik, 'Diğer') as MenuBaslik
                    FROM MenuYonetim 
                    WHERE Aktif = 1 
                    ORDER BY MenuBaslik, SiraNo");

                    return items.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu items çekilirken hata oluştu");
                return new List<MenuItemModel>();
            }
        }

        [AllowAnonymous]
        private async Task<List<KullaniciModel>> GetKullanicilar()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("MikroDB_V16");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    return (await connection.QueryAsync<KullaniciModel>(
                        "SELECT User_No AS UserNo, User_Name AS UserName FROM KULLANICILAR"))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar çekilirken hata oluştu");
                return new List<KullaniciModel>();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateYetki(int menuId, int userNo, bool hasPermission)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ERPDatabase");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    _logger.LogInformation($"MenuId: {menuId}, UserNo: {userNo}, HasPermission: {hasPermission}");

                    // Mevcut yetkileri al
                    var currentYetki = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Yetki FROM MenuYonetim WHERE Id = @menuId",
                        new { menuId }
                    );

                    // Yetkileri listeye çevir
                    var yetkiler = string.IsNullOrEmpty(currentYetki)
                        ? new HashSet<string>()
                        : new HashSet<string>(currentYetki.Split(',', StringSplitOptions.RemoveEmptyEntries));

                    if (hasPermission)
                    {
                        yetkiler.Add(userNo.ToString());
                    }
                    else
                    {
                        yetkiler.Remove(userNo.ToString());
                    }

                    // Yetkileri sırala ve birleştir
                    var yeniYetki = string.Join(",", yetkiler.OrderBy(x => int.Parse(x)));

                    // Güncelle
                    var result = await connection.ExecuteAsync(
                        "UPDATE MenuYonetim SET Yetki = @yetki WHERE Id = @menuId",
                        new { yetki = yeniYetki, menuId }
                    );

                    return Json(new { success = true, yetkiler = yeniYetki });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yetki güncelleme hatası");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}