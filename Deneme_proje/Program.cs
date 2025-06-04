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
builder.Services.AddScoped<DiokiRepository>();
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
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");

    // Health check endpoint ekleyin
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

        // Temel servisleri initialize edin
        var dbSelector = scope.ServiceProvider.GetRequiredService<DatabaseSelectorService>();

        // Database bağlantısını test edin
        // await dbSelector.TestConnection(); // Eğer böyle bir method varsa

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
                // Her 5 dakikada bir keep-alive işlemi
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                // Basit bir database ping veya health check
                _logger.LogDebug("Keep-alive ping - {Time}", DateTime.Now);

                // Buraya kendi health check kodunuzu ekleyebilirsiniz
                // Örneğin: database connection test, cache refresh vb.
            }
            catch (OperationCanceledException)
            {
                // Normal kapatma
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keep-alive işlemi sırasında hata oluştu");
            }
        }
    }
}