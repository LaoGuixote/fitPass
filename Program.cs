using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using fitPass.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 加入資料庫
builder.Services.AddDbContext<GymManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 加入 Session
builder.Services.AddSession();

// 加入第三方登入
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = "620870404380-sj0s8utdh302qt2b4lnildenjim5obdc.apps.googleusercontent.com";      // ← 從 Google Cloud 複製
    options.ClientSecret = "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";
    options.CallbackPath = "/signin-google"; // 預設即可
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 中介軟體
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication(); // 必須
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
