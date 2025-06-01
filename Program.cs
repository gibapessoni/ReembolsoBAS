using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReembolsoBAS.Auth;          // AllowAllHandler (apenas Dev)
using ReembolsoBAS.Data;
using ReembolsoBAS.Services;

var builder = WebApplication.CreateBuilder(args);

/* 1. Controllers */
builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

/* 2. DbContext */
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/* 3. Serviços de domínio */
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ReembolsoService>();

/* 4. Autenticação */
var jwtKey = builder.Configuration["Jwt:Key"]
              ?? throw new InvalidOperationException("Chave JWT não configurada.");
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

// —> Sempre registramos **ambos** os esquemas;
//     o *default* muda conforme o ambiente.
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme =
    opt.DefaultChallengeScheme =
        builder.Environment.IsDevelopment()
            ? "AllowAll"
            : JwtBearerDefaults.AuthenticationScheme;
})
.AddScheme<AuthenticationSchemeOptions, AllowAllHandler>("AllowAll", _ => { }) // Dev-only
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    opt.RequireHttpsMetadata = true;             // desligue se hospedar sem TLS em dev
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

        ValidateIssuer = false,                // defina se quiser usar "iss"
        ValidateAudience = false,                // idem para "aud"
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

/* 5. Swagger (com segurança JWT) */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ReembolsoBAS API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Insira: **Bearer &lt;seu_token&gt;**"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtScheme] = Array.Empty<string>()
    });
});

/* 6. CORS (liberal em Dev) */
builder.Services.AddCors(p => p.AddPolicy("Dev",
    b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

/* 7. Build & pipeline */
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Dev");
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();    // middleware JWT (ou AllowAll em Dev)
app.UseAuthorization();

app.MapControllers();
app.Run();
