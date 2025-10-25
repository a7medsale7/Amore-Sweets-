// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Sweets.Utility; // ✅ تأكد إنك ضايف النيمسبيس اللي فيه SD

namespace Sweets_Shop_Web.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // ✅ تسجيل الخروج
            await _signInManager.SignOutAsync();

            // ✅ مسح بيانات السيشن الخاصة بالكارت والويش ليست
            HttpContext.Session.Remove(SD.SessionCart);
            HttpContext.Session.Remove(SD.WishlistCount);

            // ✅ تسجيل الحدث في اللوج
            _logger.LogInformation("User logged out and session cleared.");

            // ✅ إعادة التوجيه
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Redirect to home page instead of same page (أفضل تجربة)
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }
        }
    }
}
