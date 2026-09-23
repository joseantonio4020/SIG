using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Infrastructure.Persistence;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=VisorDatosSIG;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<VisorDatosSIGContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

builder.Services.AddScoped<ICodigoFijoService, CodigosFijoServiceImpl>();
builder.Services.AddScoped<IManzanaService, ManzanaServiceImpl>();
builder.Services.AddScoped<ILoteService, LoteServiceImpl>();
builder.Services.AddScoped<IViaService, ViaServiceImpl>();
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapGet("/", context => {
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapControllers();

app.Run();
