using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spice.Models;
using Spice.Utility;
using System;
using System.Linq;

namespace Spice.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                    _db.Database.Migrate();
            }
            catch (Exception ex)
            {
                //logging
            }

            if (_db.Roles.Any(r => r.Name == Constants.ManagerUser)) return;

            _roleManager.CreateAsync(new IdentityRole(Constants.CustomerEndUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Constants.FrontDeskUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Constants.KitchenUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Constants.ManagerUser)).GetAwaiter().GetResult();

            _userManager.CreateAsync(new ApplicationUser()
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                Name = "admin user",
                EmailConfirmed = true,
                PhoneNumber = "1122334455",
            }, "Admin@123").GetAwaiter().GetResult();

            var user = _db.Users.FirstOrDefault(u => u.Email == "admin@gmail.com");
            _userManager.AddToRoleAsync(user, Constants.ManagerUser).GetAwaiter().GetResult();
        }
    }
}
