using fitPass;
using fitPass.Models; // 根據你的命名空間修改
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;


public class AccountController : Controller
{
    private readonly GymManagementContext _context;

    public AccountController(GymManagementContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        var user = _context.Accounts.FirstOrDefault(a => a.Email == email && a.IsActive == true);

        if (user == null || user.PasswordHash != password)
        {
            ViewBag.Error = "帳號或密碼錯誤";
            return View();
        }

        // 記錄登入資訊
        HttpContext.Session.SetInt32("MemberId", user.MemberId);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetInt32("Admin", user.Admin);

        user.LastLoginTime = DateTime.Now;
        _context.SaveChanges();

        if(user.Admin >= 3)
        {
            return RedirectToAction("dashbord", "Admin");
        }

        // ✅ 所有身份統一導向 MemberController 的 Index
        return RedirectToAction("Index", "Member");
    }
    public IActionResult ExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback));
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> ExternalLoginCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return RedirectToAction("Login");

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            ViewBag.Error = "第三方登入無法取得 Email";
            return View("Login");
        }

        var user = _context.Accounts.FirstOrDefault(a => a.Email == email);
        if (user == null)
        {
            user = new Account
            {
                Email = email,
                Name = name ?? "Google 使用者",
                PasswordHash = "",
                Admin = 1,
                IsActive = true,
                JoinDate = DateOnly.FromDateTime(DateTime.Now),
                Type = 1,
                Point = 0
            };
            _context.Accounts.Add(user);
            _context.SaveChanges();
        }

        HttpContext.Session.SetInt32("MemberId", user.MemberId);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetInt32("Admin", user.Admin);

        return RedirectToAction("Index", "Member");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string email, string password, string confirmPassword, string name, string phone)
    {
        if (password != confirmPassword)
        {
            ViewBag.Error = "兩次輸入的密碼不一致";
            return View();
        }

        if (_context.Accounts.Any(a => a.Email == email))
        {
            ViewBag.Error = "此信箱已被註冊";
            return View();
        }

        var account = new Account
        {
            Email = email,
            PasswordHash = password,
            Name = name,
            Phone = phone,
            Admin = 1,
            IsActive = true,
            JoinDate = DateOnly.FromDateTime(DateTime.Now),
            Type = 1,
            Point = 0
        };

        TempData["account"] = JsonSerializer.Serialize(account); // 將帳號資訊存入 TempData

        int captchaValue = new Random().Next(100000, 999999); // 產生六位數的隨機數字
        HttpContext.Session.SetInt32("CAPTCHA", captchaValue);

        //string to = ""; 會員信箱
        string title = "會員註冊認證碼";
        string body = captchaValue.ToString();
        new email().SendMail(email, title, body);

        return View("VerificationCode");

    }

    public IActionResult VerificationCode()
    {
        TempData.Keep("account");
        return View();
    }

    [HttpPost]
    public IActionResult VerificationCode(int VerificationCode)
    {
        var captchaValue = HttpContext.Session.GetInt32("CAPTCHA");
        if (VerificationCode == captchaValue)
        {
            var accountData = JsonSerializer.Deserialize<Account>(TempData["account"].ToString());
            _context.Accounts.Add(accountData);
            _context.SaveChanges();


            // 自動登入
            HttpContext.Session.SetInt32("MemberId", accountData.MemberId);
            HttpContext.Session.SetString("UserName", accountData.Name);
            HttpContext.Session.SetInt32("Admin", accountData.Admin);

            return RedirectToAction("Index", "Member");
        }
        else
        {
            ViewBag.text = "驗證碼錯誤!";
            return View();
        }
    }
    public JsonResult ResendCode()
    {
        TempData.Keep("account");

        int captchaValue = new Random().Next(100000, 999999); // 產生六位數的隨機數字
        HttpContext.Session.SetInt32("CAPTCHA", captchaValue);

        var accountData = JsonSerializer.Deserialize<Account>(TempData["account"].ToString());

        //string to = ""; 會員信箱
        string title = "會員註冊認證碼";
        string body = captchaValue.ToString();
        new email().SendMail(accountData.Email, title, body);

        return Json(new { success = true, message = $"已重新寄送驗證碼至 {accountData.Email}" });
    }

    public IActionResult ForgetPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgetPassword(string email)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (account == null)
        {
            ViewBag.Message = "查無此 Email";
            return View();
        }

        //產生 Token
        var token = Guid.NewGuid().ToString();

        //寫入資料庫
        var reset = new PasswordReset
        {
            Email = email,
            Token = token,
            ExpiredAt = DateTime.Now.AddMinutes(30)
        };
        _context.PasswordResets.Add(reset);
        await _context.SaveChangesAsync();

        //連結
        string url = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

        //寄信
        var mailService = new fitPass.email();
        mailService.SendMail(email, "忘記密碼通知", $"請點選連結重設密碼：<a href='{url}'>重設密碼</a>");

        ViewBag.Message = "重設連結已寄出，請至信箱查收。";
        return View();
    }

    public IActionResult ResetPassword(string token)
    {
        var reset = _context.PasswordResets.FirstOrDefault(t => t.Token == token && t.ExpiredAt > DateTime.Now);
        if (reset == null)
        {
            return Content("連結已失效或錯誤");
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            ViewBag.Message = "密碼與確認密碼不一致";
            ViewBag.Token = token;
            return View();
        }

        var reset = await _context.PasswordResets.FirstOrDefaultAsync(t => t.Token == token && t.ExpiredAt > DateTime.Now);
        if (reset == null)
        {
            return Content("連結已失效或無效");
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == reset.Email);
        if (account == null)
        {
            return Content("帳號不存在");
        }

        account.PasswordHash = newPassword;
        _context.PasswordResets.Remove(reset);
        TempData["Notify"] = "密碼修改成功，請重新登入。";
        await _context.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
