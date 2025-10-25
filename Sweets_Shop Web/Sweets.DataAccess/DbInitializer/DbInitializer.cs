using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sweet_Shop.DataAccess.Data;
using Sweets.Models.Models;
using Sweets.Utility;
using System;
using System.Linq;

namespace Sweet_Shop.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }

        public void Initialize()
        {
            // ✅ Apply pending migrations automatically
            try
            {
                if (_db.Database.GetPendingMigrations().Any())
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception)
            {
                // ignore errors
            }

            // ✅ Create roles if not exist
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();

                // ✅ Create Admin user (basic IdentityUser)
                var adminUser = new IdentityUser
                {
                    UserName = "Admin@gmail.com",
                    Email = "Admin@gmail.com",
                    EmailConfirmed = true
                };

                _userManager.CreateAsync(adminUser, "Admin123*??").GetAwaiter().GetResult();

                var user = _db.Users.FirstOrDefault(u => u.Email == "Admin@gmail.com");
                if (user != null)
                {
                    _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
                }
            }
        }
    }
}
