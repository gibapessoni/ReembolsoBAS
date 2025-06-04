using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Services;

var builder = WebApplication.CreateBuilder(args);

/* 1. Controllers */
builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

/* 2. DbContext */
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ReembolsoService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ReembolsoBAS API", Version = "v1" });    
});

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

app.MapControllers();
app.Run();
