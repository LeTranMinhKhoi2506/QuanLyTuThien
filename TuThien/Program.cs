using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using TuThien.Services;
using TuThien.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TuThienContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Options Pattern for settings
builder.Services.Configure<DonationSettings>(
    builder.Configuration.GetSection(DonationSettings.SectionName));
builder.Services.Configure<BankSettings>(
    builder.Configuration.GetSection(BankSettings.SectionName));
builder.Services.Configure<VNPaySettings>(
    builder.Configuration.GetSection(VNPaySettings.SectionName));
builder.Services.Configure<MoMoSettings>(
    builder.Configuration.GetSection(MoMoSettings.SectionName));

// Add Donation Validation Service
builder.Services.AddScoped<IDonationValidationService, DonationValidationService>();

// Add Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

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

// Enable Session
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TrangChu}/{action=Index}/{id?}");

app.Run();
