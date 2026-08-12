using System.Text;
using MapProject.Business;
using MapProject.Business.Services;
using MapProject.Business.Settings;
using MapProject.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()));

// appsettings.json -> "Jwt" bölümünü JwtSettings sınıfına bağla.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddBusinessServices();

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
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Migration'ları uygula ve test kullanıcısını oluştur.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
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
app.Run();
