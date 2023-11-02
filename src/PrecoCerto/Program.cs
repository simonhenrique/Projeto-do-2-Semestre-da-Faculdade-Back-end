using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PrecoCerto.Models;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = Context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/Usuario/AccessDenied/";
        options.LoginPath = "/Usuario/Login";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

var defaultCulture = new CultureInfo("pt-BR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Inserir dados a partir do arquivo SQL
InserirDadosAPartirDoArquivoSQL(app, app.Environment, app.Services);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

void InserirDadosAPartirDoArquivoSQL(WebApplication app, IWebHostEnvironment env, IServiceProvider serviceProvider)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var configuration = app.Configuration.GetSection("AppConfiguration");
    var operacoesRealizadas = configuration.GetValue<bool>("OperacoesRealizadas");

    if (!operacoesRealizadas)
    {
        // Combine o caminho com a pasta wwwroot
        string filePath = Path.Combine(env.WebRootPath, "sql", "Produtos.sql");

        if (File.Exists(filePath))
        {
            string sql = System.IO.File.ReadAllText(filePath);

            // Execute os comandos SQL em lote
            dbContext.Database.ExecuteSqlRaw(sql);

            dbContext.SaveChanges(); // Salve as altera��es no banco de dados

            // Atualize a vari�vel de configura��o para indicar que as opera��es foram realizadas
            configuration["OperacoesRealizadas"] = "true";
        }
        else
        {
            Console.WriteLine("O arquivo SQL n�o foi encontrado.");
        }
    }
    else
    {
        Console.WriteLine("Opera��es j� realizadas.");
    }
}