using Deneme_proje.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class UserAccessFilter : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var username = context.HttpContext.Session.GetString("Username");

        // Eğer oturum açılmamışsa login sayfasına yönlendir
        if (string.IsNullOrEmpty(username))
        {
            context.Result = new RedirectToActionResult("Index", "Login", null);
            return;
        }

        // Servis kullanıcısı sadece ServisHareketleri Controller'a erişebilir
        if (username == "servis" &&
            context.RouteData.Values["controller"]?.ToString() != "ServisHareketleri")
        {
            context.Result = new ForbidResult();
        }
    }
}