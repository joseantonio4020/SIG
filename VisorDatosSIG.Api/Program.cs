using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Infrastructure.Persistence;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=VisorDatosSIG;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<VisorDatosSIGContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ICodigoFijoService, CodigosFijoServiceImpl>();
builder.Services.AddScoped<IManzanaService, ManzanaServiceImpl>();
builder.Services.AddScoped<ILoteService, LoteServiceImpl>();
builder.Services.AddScoped<IViaService, ViaServiceImpl>();
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = "VisorDatosSIG",
            ValidAudience = "VisorDatosSIG",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateActor = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("Catastro", policy => policy.RequireRole("Catastro"));
    options.AddPolicy("Lecturador", policy => policy.RequireRole("Lecturador"));
    options.AddPolicy("Cortador", policy => policy.RequireRole("Cortador"));
    options.AddPolicy("Reconexion", policy => policy.RequireRole("Reconexion"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VisorDatosSIG API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
