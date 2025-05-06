using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Deneme_proje;
using System.Data.SqlClient;
using Dapper;

public class AuthFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        // Login controller için kontrol yapma
        if (string.Equals(controller, "login", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        // AllowAnonymous attribute kontrolü
        var actionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (actionDescriptor != null)
        {
            // Controller veya Method üzerinde AllowAnonymous varsa, yetki kontrolü yapma
            var controllerHasAllowAnonymous = actionDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();
            var methodHasAllowAnonymous = actionDescriptor.MethodInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();

            if (controllerHasAllowAnonymous || methodHasAllowAnonymous)
            {
                base.OnActionExecuting(context);
                return;
            }
        }

        // Kimlik doğrulaması kontrolü
        var isUserAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false;
        var session = context.HttpContext.Session;
        var isSessionAuthenticated = session.GetString("IsAuthenticated") == "true";

        // Kullanıcı giriş yapmamışsa
        if (!isUserAuthenticated && !isSessionAuthenticated)
        {
            context.Result = new RedirectToActionResult("Index", "Login", null);
            return;
        }

        // Buraya kadar geldiyse kullanıcı kimliği doğrulanmış demektir

        // UserNo değerini al
        var userNo = session.GetString("UserNo");
        if (string.IsNullOrEmpty(userNo))
        {
            var userNoClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserNo");
            userNo = userNoClaim?.Value;

            // UserNo bulunamazsa ve Home Controller değilse Login'e yönlendir
            if (string.IsNullOrEmpty(userNo) && !string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }
        }

        // Home Controller için özel izin ver ve yetki kontrolünü atla
        if (string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        // Yetki kontrolü
        try
        {
            if (string.IsNullOrEmpty(userNo))
            {
                // UserNo yoksa ve buraya kadar geldiyse (Home olduğu için), işleme devam et
                base.OnActionExecuting(context);
                return;
            }

            var configuration = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            if (configuration == null)
            {
                throw new Exception("IConfiguration servisine erişilemedi.");
            }

            var connectionString = configuration.GetConnectionString("ERPDatabase");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ERPDatabase bağlantı dizesi bulunamadı.");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Menü kaydını kontrol et
                var checkQuery = @"
                SELECT Yetki 
                FROM MenuYonetim 
                WHERE ControllerAdi = @controller 
                AND ActionAdi = @action";

                var parameters = new
                {
                    controller = controller,
                    action = action
                };

                // Yetki değerini al
                var yetkiDegeri = connection.QueryFirstOrDefault<string>(checkQuery, parameters);

                // Menü kaydı yoksa Home'a yönlendir
                if (yetkiDegeri == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Controller: {controller}, Action: {action} için menü kaydı bulunamadı.");
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }

                // Yetki "1" ise tüm kullanıcılara izin ver
                if (yetkiDegeri == "1")
                {
                    base.OnActionExecuting(context);
                    return;
                }

                // Yetki değeri, kullanıcı numarası listesi içeriyor mu kontrol et
                bool yetkiVar = yetkiDegeri == userNo ||
                       yetkiDegeri.StartsWith($"{userNo},") ||
                       yetkiDegeri.Contains($",{userNo},") ||
                       yetkiDegeri.EndsWith($",{userNo}");

                System.Diagnostics.Debug.WriteLine($"Controller: {controller}, Action: {action}, UserNo: {userNo}, Yetki: {yetkiDegeri}, YetkiVar: {yetkiVar}");

                if (!yetkiVar)
                {
                    // Yetkisi yoksa Ana Sayfaya yönlendir
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Yetki kontrolü sırasında hata: {ex.Message}");
            context.Result = new RedirectToActionResult("Index", "Home", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}