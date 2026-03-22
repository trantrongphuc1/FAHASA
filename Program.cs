using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SportsStore.Services;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;

// ========================================
// 🏗️ MẪU THIẾT KẾ BUILDER - WebApplication.CreateBuilder()
// Tạo instance WebApplicationBuilder với fluent API
// Cấu hình services một cách linh hoạt
// ========================================
var builder = WebApplication.CreateBuilder(args);

// ===================
// DỊCH VỤ (DI CONFIG)
// ===================

// MVC, Razor, Blazor
// ✅ Thêm localization service
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ✅ Cấu hình Blazor Server với lỗi chi tiết
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => { options.DetailedErrors = true; });

// SignalR cho Chat
builder.Services.AddSignalR();

// SignalR cho Chat
builder.Services.AddSignalR();

// Kết nối CSDL chính
builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration["ConnectionStrings:SportsStoreConnection"]);
});

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    options.DefaultChallengeScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
});

// Session và Cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".SportsStore.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔒 MẪU THIẾT KẾ SINGLETON - IHttpContextAccessor
// Chỉ có 1 instance duy nhất trong toàn bộ ứng dụng
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// 🏭 MẪU THIẾT KẾ FACTORY METHOD - PersistentCart.GetCart()
// Factory method tạo Cart instance dựa trên context (user, session, database)
builder.Services.AddScoped<Cart>(sp => PersistentCart.GetCart(sp));

// 📦 MẪU THIẾT KẾ REPOSITORY - Lớp trừu tượng cho Data Access
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddScoped<IRentalRepository, EFRentalRepository>();

// 🔌 MẪU THIẾT KẾ ADAPTER - EmailSender adapts SmtpClient
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddScoped<SaleService>();

// 📢 MẪU THIẾT KẾ STRATEGY - Các chiến lược thông báo khác nhau
builder.Services.AddScoped<INotificationService, NotificationService>();

// 🎯 MẪU THIẾT KẾ STRATEGY - Các chiến lược voucher khác nhau
builder.Services.AddScoped<SportsStore.Services.IVoucherService, SportsStore.Services.VoucherService>();

// 💾 MẪU THIẾT KẾ MEMENTO - Quản lý trạng thái Order với undo/redo
builder.Services.AddScoped<OrderStateManager>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // ghi log ra terminal
builder.Logging.SetMinimumLevel(LogLevel.Information);



// ✅ HttpClient cho Blazor
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

var app = builder.Build();
// ✅ Cấu hình các ngôn ngữ hỗ trợ
var supportedCultures = new[] { "vi", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi") // Ngôn ngữ mặc định
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// ✅ Áp dụng localization
app.UseRequestLocalization(localizationOptions);

// ===================
// MIDDLEWARE (ĐÚNG THỨ TỰ)
// ===================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStaticFiles();       
app.UseRouting();           

app.UseSession();           

app.UseAuthentication();   
app.UseAuthorization();     

// ===================
// ĐỊNH TUYẾN
// ===================

app.MapControllerRoute("catpage",
    "{category}/Page{productPage:int}",
    new { Controller = "Home", action = "Index" });

app.MapControllerRoute("page", "Page{productPage:int}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("category", "{category}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapControllerRoute("pagination",
    "Products/Page{productPage}",
    new { Controller = "Home", action = "Index", productPage = 1 });

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.UseStaticFiles();


// ✅ Đặt đúng thứ tự: BlazorHub trước fallback
app.MapBlazorHub();
app.MapHub<SportsStore.Hubs.ChatHub>("/chathub");
app.MapHub<SportsStore.Hubs.NotificationHub>("/notificationhub");
app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

// ===================
// SEED DỮ LIỆU
// ===================
SeedData.EnsurePopulated(app); // Sản phẩm
await IdentitySeedData.EnsurePopulatedAsync(app); // Tài khoản admin

// Seed vouchers
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    if (!ctx.Vouchers.Any())
    {
        ctx.Vouchers.AddRange(new[]
        {
            new Voucher { Code = "SALE5", DiscountType = VoucherDiscountType.Percentage, Value = 0.05m, DailyLimit = 30, MinOrderTotal = 0, IsActive = true, PerUserDailyLimit = 1, PerUserWeeklyLimit = 2 },
            new Voucher { Code = "SALE10", DiscountType = VoucherDiscountType.Percentage, Value = 0.10m, DailyLimit = 20, MinOrderTotal = 0, IsActive = true, PerUserDailyLimit = 1, PerUserWeeklyLimit = 2 },
            new Voucher { Code = "GIAM10K", DiscountType = VoucherDiscountType.FixedAmount, Value = 10000m, DailyLimit = 50, MinOrderTotal = 0, IsActive = true, PerUserDailyLimit = 2, PerUserWeeklyLimit = 4 },
            new Voucher { Code = "GIAM50K1M", DiscountType = VoucherDiscountType.FixedAmount, Value = 50000m, DailyLimit = 15, MinOrderTotal = 1000000m, IsActive = true, PerUserDailyLimit = 1, PerUserWeeklyLimit = 2 }
        });
        ctx.SaveChanges();
    }
}

// ===================
// CHẠY ỨNG DỤNG
// ===================
app.Run();
