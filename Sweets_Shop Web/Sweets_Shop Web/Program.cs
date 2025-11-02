using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Sweet_Shop.DataAccess.Data;
using Sweet_Shop.DataAccess.DbInitializer;
using Sweet_Shop.Repository;
using Sweets.Utility;

var builder = WebApplication.CreateBuilder(args);

// ====================== Database Connection ======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ====================== Identity Configuration ======================
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // لو مش عايز تأكيد بالإيميل خليه false
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    options.SlidingExpiration = true;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ====================== Dependency Injection ======================
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings")); // ✅ أضف السطر ده

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IEmailSender, EmailService>();

// ====================== Google Authentication ======================
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = "766335828089-jf0t70u8mvf014fotvhv5qndd7vkj8r3.apps.googleusercontent.com";
        options.ClientSecret = "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";

        // 🔁 مهم: لازم نفس المسار تضيفه في Google Cloud Console
        options.CallbackPath = "/signin-google";
    });

// ====================== MVC and JSON Config ======================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddRazorPages();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

// ====================== Build the App ======================
var app = builder.Build();

// ====================== Middleware Pipeline ======================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

SeedDatabase();

// ====================== Routing ======================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}"
);

app.MapRazorPages();

// ====================== Run App ======================
app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}