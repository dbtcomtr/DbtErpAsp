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

                var model = new MenuYonetimiViewModel
                {
                    Kullanicilar = kullanicilar,
                    SelectedUserNo = selectedUser.UserNo,
                    MenuItems = await GetMenuItems()
                };

                // Seçili kullanıcı bilgisini ViewBag'e ekleyelim
                ViewBag.SelectedUserName = selectedUser.UserName;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menü yönetimi sayfası yüklenirken hata oluştu");
                return View("Error");
            }
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
                        Icon 
                    FROM MenuYonetim 
                    WHERE Aktif = 1 
                    ORDER BY SiraNo");

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
        [ValidateAntiForgeryToken]  // Bunu ekleyin
        [AllowAnonymous]
        public async Task<IActionResult> UpdateYetki(int menuId, int userNo, bool hasPermission)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ERPDatabase");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Debug log
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
                        // Yetki verme
                        yetkiler.Add(userNo.ToString());
                    }
                    else
                    {
                        // Yetki alma
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