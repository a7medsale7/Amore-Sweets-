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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration error: {ex.Message}");
            }

            // ✅ Ensure Roles exist
            CreateRoleIfNotExists(SD.Role_Customer);
            CreateRoleIfNotExists(SD.Role_Employee);
            CreateRoleIfNotExists(SD.Role_Admin);

            // ✅ Ensure Admin user exists
            var adminEmail = "AmoreAdmin@gmail.com";
            var adminPassword = "Admin123*??";

            var existingAdmin = _userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

            if (existingAdmin == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = _userManager.CreateAsync(adminUser, adminPassword).GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    Console.WriteLine("✅ Admin user created successfully!");
                    _userManager.AddToRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();
                }
                else
                {
                    Console.WriteLine("❌ Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($" - {error.Description}");
                    }
                }
            }
            else
            {
                // ✅ Make sure admin has Admin role
                var roles = _userManager.GetRolesAsync(existingAdmin).GetAwaiter().GetResult();
                if (!roles.Contains(SD.Role_Admin))
                {
                    _userManager.AddToRoleAsync(existingAdmin, SD.Role_Admin).GetAwaiter().GetResult();
                    Console.WriteLine("✅ Admin role re-assigned to existing admin user.");
                }
            }
        }

        // ✅ Helper Method to safely create role if it doesn't exist
        private void CreateRoleIfNotExists(string roleName)
        {
            if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                Console.WriteLine($"✅ Role '{roleName}' created successfully!");
            }
        }
    }
}
