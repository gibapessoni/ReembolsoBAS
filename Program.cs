using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReembolsoBAS.Data;
using ReembolsoBAS.Services;
using System;
using System;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// bloco para gerar hash na senha
var senhaEmTextoPlano = "Senha123!";
var hashGerado = BCrypt.Net.BCrypt.HashPassword(senhaEmTextoPlano, workFactor: 12);
Console.WriteLine($"[DEBUG] Hash gerado em runtime para 'Senha123!': {hashGerado}");

// 1. Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 2. DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ReembolsoService>();

// 3. Configurar JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Chave JWT não configurada.");
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Authorization
builder.Services.AddAuthorization();

// 5. Swagger (com Bearer/JWT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ReembolsoBAS API", Version = "v1" });

    // 1) Defina o esquema de segurança como “Bearer” e crie uma referência
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Insira o token JWT desta forma: Bearer <seu_token>",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    // 2) Registre este esquema no Swagger
    c.AddSecurityDefinition("Bearer", jwtScheme);

    // 3) Exija esse esquema para TODOS os endpoints protegidos
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }
        ] = new string[] { }
    });
});


// 6. CORS (dev)
builder.Services.AddCors(p => p.AddPolicy("Dev",
    b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

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
app.UseAuthentication(); // habilita validação JWT
app.UseAuthorization();

app.MapControllers();
app.Run();
