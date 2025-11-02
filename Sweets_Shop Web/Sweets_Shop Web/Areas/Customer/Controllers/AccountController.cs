using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sweet_Shop.DataAccess.Data;
using Sweet_Shop.Repository;
using Sweets.Models.Models;
using Sweets.Models.ViewData;
using System.Security.Claims;

namespace Sweets_Shop_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager ,
                ApplicationDbContext context
)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ===============================
        // GOOGLE LOGIN CALLBACK
        // ===============================
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction("Login", "Account");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (!result.Succeeded)
            {
                if (email != null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new IdentityUser
                        {
                            UserName = email,
                            Email = email
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (createResult.Succeeded)
                        {
                            await _userManager.AddLoginAsync(user, info);

                            // ✅ إنشاء UserProfile جديد
                            var profile = new ApplicationUser
                            {
                                Id = user.Id,
                                Name = name,
                                Email = user.Email,
                                City = "",
                                StreetAddress = "",
                                State = "",
                                PostalCode = "",
                                PhoneNumber = ""
                            };

                            _unitOfWork.ApplicationUser.Add(profile);
                            _unitOfWork.save();
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Error creating user from external login.");
                            return RedirectToAction("Login", "Account");
                        }
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                }
            }

            // ✅ بعد تسجيل الدخول، تأكد من استكمال البيانات
            var loggedUser = await _userManager.FindByEmailAsync(email);
            if (loggedUser != null)
            {
                var profile = _unitOfWork.ApplicationUser.Get(u => u.Id == loggedUser.Id);
                if (profile == null ||
                    string.IsNullOrEmpty(profile.City) ||
                    string.IsNullOrEmpty(profile.StreetAddress) ||
                    string.IsNullOrEmpty(profile.State) ||
                    string.IsNullOrEmpty(profile.PostalCode) ||
                    string.IsNullOrEmpty(profile.PhoneNumber))
                {
                    return RedirectToAction("CompleteProfile", "Account", new { area = "Customer", returnUrl });
                }
            }

            return LocalRedirect(returnUrl);
        }

        // ===============================
        // COMPLETE PROFILE - GET
        // ===============================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CompleteProfile(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = _unitOfWork.ApplicationUser.Get(u => u.Id == user.Id);
            if (profile == null)
            {
                profile = new ApplicationUser
                {
                    Id = user.Id,
                    Email = user.Email
                };
            }

            var vm = new CompleteProfileVM
            {
                ApplicationUser = profile,
                ReturnUrl = returnUrl
            };

            return View(vm);
        }

        // ===============================
        // COMPLETE PROFILE - POST
        // ===============================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfileAjax([FromBody] CompleteProfileVM model)
        {
            // 1️⃣ التحقق من وجود المستخدم
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            // 2️⃣ حفظ عربات التسوق الحالية للمستخدم القديم
            var userCarts = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == user.Id).ToList();

            // 3️⃣ الحصول على بيانات المستخدم القديم
            var oldUser = await _userManager.FindByIdAsync(user.Id);
            if (oldUser == null)
                return Json(new { success = false, message = "Old user not found." });

            // 4️⃣ إنشاء مستخدم جديد بنفس Id ودمج البيانات
            var newUser = new ApplicationUser
            {
                Id = oldUser.Id,
                Email = oldUser.Email,
                UserName = oldUser.UserName,
                NormalizedEmail = oldUser.NormalizedEmail,
                NormalizedUserName = oldUser.NormalizedUserName,
                SecurityStamp = oldUser.SecurityStamp,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PasswordHash = oldUser.PasswordHash,
                PhoneNumber = model.ApplicationUser.PhoneNumber,
                PhoneNumberConfirmed = oldUser.PhoneNumberConfirmed,
                TwoFactorEnabled = oldUser.TwoFactorEnabled,
                AccessFailedCount = oldUser.AccessFailedCount,

                Name = model.ApplicationUser.Name,
                City = model.ApplicationUser.City,
                StreetAddress = model.ApplicationUser.StreetAddress,
                State = model.ApplicationUser.State,
                PostalCode = model.ApplicationUser.PostalCode
            };

            try
            {
                // 5️⃣ حذف المستخدم القديم
                var deleteResult = await _userManager.DeleteAsync(oldUser);
                if (!deleteResult.Succeeded)
                    return Json(new { success = false, message = "Failed to delete old user." });

                _context.Entry(oldUser).State = EntityState.Detached;

                // 6️⃣ إضافة المستخدم الجديد
                _unitOfWork.ApplicationUser.Add(newUser);
                _context.Entry(newUser).State = EntityState.Added;
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            try
            {
                // 7️⃣ تعديل عربات التسوق
                foreach (var cart in userCarts)
                {
                    cart.ApplicationUserId = newUser.Id;
                    _unitOfWork.ShoppingCart.Update(cart);
                }
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true });
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileVM model)
        {
            // 1️⃣ التحقق من وجود المستخدم
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            // 2️⃣ تسجيل الدخول بالمستخدم الحالي (المعدل بالفعل في AJAX)
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["success"] = "Profile updated successfully!";

            // 3️⃣ إعادة التوجيه
            if (!string.IsNullOrEmpty(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = _unitOfWork.ApplicationUser.Get(u => u.Id == user.Id);
            if (profile == null)
            {
                TempData["error"] = "Profile not found!";
                return RedirectToAction("Index", "Home");
            }

            var vm = new CompleteProfileVM
            {
                ApplicationUser = profile
            };

            return View(vm);
        }

        [Authorize]
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(CompleteProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill all required fields correctly.";
                return View(model);
            }

            var profile = _unitOfWork.ApplicationUser.Get(u => u.Id == user.Id, tracked: true);
            if (profile == null)
            {
                TempData["error"] = "Profile not found!";
                return View(model);
            }

            // ✅ تحديث البيانات مباشرة على الكائن المتتبع
            profile.Name = model.ApplicationUser.Name;
            profile.City = model.ApplicationUser.City;
            profile.StreetAddress = model.ApplicationUser.StreetAddress;
            profile.State = model.ApplicationUser.State;
            profile.PostalCode = model.ApplicationUser.PostalCode;
            profile.PhoneNumber = model.ApplicationUser.PhoneNumber;

            // ✅ حفظ التغييرات فقط (بدون Attach أو Update)
            _unitOfWork.save();

            // ✅ تحديث بيانات الـ Identity User
            user.PhoneNumber = model.ApplicationUser.PhoneNumber;
            await _userManager.UpdateAsync(user);

            TempData["success"] = "Profile updated successfully!";
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }


    }
}
