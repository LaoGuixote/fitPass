using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using fitPass.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// �[�J��Ʈw
builder.Services.AddDbContext<GymManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// �[�J Session
builder.Services.AddSession();

// �[�J�ĤT��n�J
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = "620870404380-sj0s8utdh302qt2b4lnildenjim5obdc.apps.googleusercontent.com";      // �� �q Google Cloud �ƻs
    options.ClientSecret = "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";
    options.CallbackPath = "/signin-google"; // �w�]�Y�i
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// �����n��
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication(); // ����
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
