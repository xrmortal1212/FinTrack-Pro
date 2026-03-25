using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using FinTrack_Pro.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(15); // Session 15 days tak valid rahega
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database Connection Setup
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Aapka mojooda Cookie Setup:
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    // YAHAN SE NAYA CODE ADD KAREIN:
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// ==========================================
// ADMIN SEEDING (Auto-create Admin on startup)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Check karein ke kya admin pehle se database mein mojood hai?
    var adminEmail = "raman@gmail.com";
    var adminExists = context.Users.Any(u => u.Email == adminEmail);

    if (!adminExists)
    {
        var adminUser = new User
        {
            FullName = "RAMAN .", // Aap apna naam bhi likh sakte hain
            Email = adminEmail,
            PasswordHash = "kqj123mm", // Aapka password
            Role = "Admin",            // Main cheez: Isay Admin banana hai
            CreatedAt = DateTime.Now
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

// Ye line aapke file mein pehle se hogi, iske baad aur kuch nahi aayega
app.Run();
