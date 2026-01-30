using Deneme_proje.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using Deneme_proje;
using Deneme_proje.Helpers;

var builder = WebApplication.CreateBuilder(args);

// MVC servisini ekleyin
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthFilter());
});

// ✅ HttpClient servisini ekleyin - EN ÜSTE
builder.Services.AddHttpClient();

// Authentication servisleri - Oturum süresiz açık kalacak şekilde ayarlandı
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.Cookie.Name = "ERPAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(3650);
        options.SlidingExpiration = true;
        options.Cookie.MaxAge = TimeSpan.FromDays(3650);
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
    options.IdleTimeout = TimeSpan.FromDays(3650);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.Cookie.MaxAge = TimeSpan.FromDays(3650);
});

// Veritabanı ve Repository servisleri
builder.Services.AddScoped<DatabaseSelectorService>();
builder.Services.AddScoped<CariRepository>();
builder.Services.AddScoped<FaturaRepository>();
builder.Services.AddScoped<DenizlerRepository>();
builder.Services.AddScoped<SirketDurumuRepository>();
builder.Services.AddScoped<GunayRepository>();
builder.Services.AddScoped<SarfCikisRepository>();
builder.Services.AddHttpContextAccessor();

// ✅ CrmRepository'ye IConfiguration inject et
builder.Services.AddScoped<CrmRepository>(sp =>
    new CrmRepository(
        sp.GetRequiredService<DatabaseSelectorService>(),
        sp.GetRequiredService<IConfiguration>(), 
        sp.GetRequiredService<IHttpContextAccessor>()
        
    )
);

builder.Services.AddScoped<DiokiRepository>();
builder.Services.AddScoped<ApiRepository>();
builder.Services.AddScoped<EmailNotificationService>();

// Singleton Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Distributed cache
builder.Services.AddDistributedMemoryCache();

// HostedService ekleyerek always running sağlayın
builder.Services.AddHostedService<WarmupService>();

// Kestrel sunucu ayarları - Timeout değerlerini artırın
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

ConnectionHelper.Initialize(builder.Configuration);

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
    endpoints.MapControllers();

    // PDF indirme route'ları
    endpoints.MapControllerRoute(
        name: "TeklifYazdir",
        pattern: "crm/teklifyazdir/{teklifNo}",
        defaults: new { controller = "Crm", action = "TeklifYazdir" }
    );

    endpoints.MapControllerRoute(
        name: "TeklifYazdirCase",
        pattern: "crm/TeklifYazdir/{teklifNo}",
        defaults: new { controller = "Crm", action = "TeklifYazdir" }
    );

    endpoints.MapControllerRoute(
        name: "TeklifDuzenle",
        pattern: "crm/teklifduzenle/{teklifNo}",
        defaults: new { controller = "Crm", action = "TeklifDuzenle" }
    );

    // ✅ API Route ekleyin
    endpoints.MapControllerRoute(
        name: "ApiAyarlari",
        pattern: "api/apiayarlari",
        defaults: new { controller = "Api", action = "ApiAyarlari" }
    );

    // Default route en sonda olmalı
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");

    endpoints.MapGet("/health", async context =>
    {
        await context.Response.WriteAsync("OK");
    });
});

// Uygulama başlatıldığında warm-up işlemi
await WarmupApplication(app);

app.Run();

// Warm-up fonksiyonu
static async Task WarmupApplication(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Uygulama warm-up işlemi başlatılıyor...");

        var dbSelector = scope.ServiceProvider.GetRequiredService<DatabaseSelectorService>();

        logger.LogInformation("Uygulama warm-up işlemi tamamlandı.");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Warm-up işlemi sırasında hata oluştu");
    }
}

// Background service - Keep alive
public class WarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WarmupService> _logger;

    public WarmupService(IServiceProvider serviceProvider, ILogger<WarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                _logger.LogDebug("Keep-alive ping - {Time}", DateTime.Now);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keep-alive işlemi sırasında hata oluştu");
            }
        }
    }
}