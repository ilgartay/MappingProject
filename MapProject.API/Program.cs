using System.Text;
using System.Text.Json;
using MapProject.API.Hubs;
using MapProject.Business;
using MapProject.Data;
using MapProject.Business.Services;
using MapProject.Business.Settings;
using MapProject.Business.Simulation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Swagger'a "Authorize" butonu ekliyoruz ki token'ı arayüzden yapıştırabilelim.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Sadece token'ı yapıştırın, başına 'Bearer' yazmayın."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

builder.Services.AddDataServices(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil."));

// appsettings.json -> "Jwt" bölümünü JwtSettings sınıfına bağla.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Aynı şekilde "GeoServer" bölümü. Şifre burada değil; user-secrets ya da
// GeoServer__Password ortam değişkeninden geliyor.
builder.Services.Configure<GeoServerSettings>(
    builder.Configuration.GetSection(GeoServerSettings.SectionName));

// Rota sunucusu (Docker'daki OSRM).
builder.Services.Configure<OsrmSettings>(
    builder.Configuration.GetSection(OsrmSettings.SectionName));

// Araç simülasyonunun hızı ve yayın sıklığı.
builder.Services.Configure<SimulationSettings>(
    builder.Configuration.GetSection(SimulationSettings.SectionName));

builder.Services.AddBusinessServices();

// DİKKAT - alan adlarının biçimi: MVC controller'ları JSON'u varsayılan
// olarak camelCase üretiyor, SignalR ise üretmiyor - C#'taki adı aynen
// gönderiyor. Aynı VehicleState nesnesi REST'ten "routeId", SignalR'dan
// "RouteId" diye gelirdi ve istemci ikisini ayrı ayrı ele almak zorunda
// kalırdı. İkisini burada eşitliyoruz.
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Business "duyur" diyor, SignalR ile duyuran taraf API katmanında.
builder.Services.AddSingleton<ISimulationBroadcaster, SignalRSimulationBroadcaster>();

var jwtSettings = builder.Configuration
                      .GetSection(JwtSettings.SectionName)
                      .Get<JwtSettings>()
                  ?? throw new InvalidOperationException("appsettings.json içinde 'Jwt' bölümü bulunamadı.");

// Anahtar appsettings.json'da değil; geliştirmede user-secrets, sunucuda
// ortam değişkeni (Jwt__Key) üzerinden gelir. Eksikse uygulama açılışta
// dursun - yoksa herkes kendi token'ını imzalayabilirdi.
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key tanımlı değil. Çalıştır: dotnet user-secrets set \"Jwt:Key\" \"<en az 32 karakter>\" --project MapProject.API");
}

// HMAC-SHA256 en az 256 bit (32 bayt) anahtar ister, kısa anahtarla çalışma anında patlar.
if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key en az 32 karakter olmalı (HMAC-SHA256 gereği).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Gelen her token bu kurallara göre doğrulanır.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,          // Süresi dolmuş token reddedilir -> 401
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            // Varsayılan 5 dakikalık tolerans var; token'ımız 10 dakika olduğu için sıfırlıyoruz.
            ClockSkew = TimeSpan.Zero
        };

        // WebSocket el sıkışmasına Authorization başlığı eklenemiyor -
        // tarayıcının WebSocket API'si başlık yazmaya izin vermiyor.
        // SignalR bu yüzden token'ı access_token sorgu parametresiyle
        // gönderiyor; yalnızca /hubs altındaki isteklerde kabul ediyoruz.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR el sıkışması kimlik bilgisi taşıyor; bu olmadan
            // tarayıcı WebSocket yükseltmesini engelliyor.
            .AllowCredentials();
    });
});

var app = builder.Build();

// Migration'ları uygula ve test kullanıcısını oluştur.
// İşin kendisi Business katmanında; API sadece tetikliyor.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Sıra önemli: önce "sen kimsin" (Authentication), sonra "yetkin var mı" (Authorization).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Canlı araç konumları buradan yayınlanıyor.
app.MapHub<SimulationHub>("/hubs/simulation");

app.Run();
