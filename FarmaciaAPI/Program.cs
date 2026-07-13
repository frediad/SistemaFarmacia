using Microsoft.EntityFrameworkCore;
using FarmaciaAPI.Data;

var builder = WebApplication.CreateBuilder(args);


// ==============================
// CONEXIÓN A SQL SERVER / AZURE SQL
// ==============================
string conexionActiva =
    builder.Configuration["BaseDatosActiva"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(conexionActiva)));


// ==============================
// SERVICIOS DE LA API
// ==============================

builder.Services.AddControllers();


// ==============================
// SWAGGER
// ==============================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// ==============================
// CORS
// PERMITE CONEXIÓN CON WPF Y BLAZOR
// ==============================

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


// ==============================
// CONSTRUIR APP
// ==============================

var app = builder.Build();


// ==============================
// CONFIGURACIÓN DEL PIPELINE
// ==============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ==============================
// HTTPS
// ==============================

app.UseHttpsRedirection();


// ==============================
// CORS
// ==============================

app.UseCors("PermitirTodo");


// ==============================
// AUTORIZACIÓN
// ==============================

app.UseAuthorization();


// ==============================
// CONTROLADORES
// ==============================

app.MapControllers();


// ==============================
// EJECUTAR API
// ==============================

app.Run();