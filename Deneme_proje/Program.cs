using Deneme_proje.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using Deneme_proje;

var builder = WebApplication.CreateBuilder(args);

// MVC servisini ekleyin
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthFilter());
});

// Authentication servisleri - Oturum süresiz açık kalacak şekilde ayarlandı
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.Cookie.Name = "ERPAuth";
        options.Cookie.HttpOnly = true;

        // Maksimum değeri kullanarak süresiz oturum sağlanıyor
        options.ExpireTimeSpan = TimeSpan.FromDays(3650); // 10 yıl (pratikte süresiz)
        options.SlidingExpiration = true;

        // Süresiz kalıcı oturum için
        options.Cookie.MaxAge = TimeSpan.FromDays(3650); // 10 yıl (pratikte süresiz)

        // IsPersistent özelliği CookieBuilder sınıfında bulunmaz, bu nedenle kaldırıldı
        // Kalıcılık, Login Controller'da AuthenticationProperties ile ayarlanacak
    });

builder.Services.AddHttpContextAccessor();

// Logging ekleyin
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
    configure.SetMinimumLevel(LogLevel.Information);
});

// Session servisi - Süresiz olarak ayarlandı
builder.Services.AddSession(options =>
{
    // Session süresini maksimum değere ayarlıyoruz - pratikte süresiz
    options.IdleTimeout = TimeSpan.FromDays(3650); // 10 yıl (pratikte süresiz)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    // Süresiz oturum için maksimum çerez yaşı
    options.Cookie.MaxAge = TimeSpan.FromDays(3650); // 10 yıl (pratikte süresiz)
});

// Veritabanı ve Repository servisleri
builder.Services.AddScoped<DatabaseSelectorService>();
builder.Services.AddScoped<CariRepository>();
builder.Services.AddScoped<FaturaRepository>();
builder.Services.AddScoped<DenizlerRepository>();
builder.Services.AddScoped<SirketDurumuRepository>();
builder.Services.AddScoped<GunayRepository>();
builder.Services.AddScoped<DiokiRepository>();
builder.Services.AddScoped<EmailNotificationService>();

// Singleton Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Oturum verilerinin disk üzerinde saklanması için distributed cache ekleyin
// Uygulama yeniden başlatılsa bile oturumların korunmasını sağlar
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware sıralaması önemli
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");
});

app.Run();