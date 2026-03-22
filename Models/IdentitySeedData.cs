using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SportsStore.Models
{
    public static class IdentitySeedData
    {
        private const string adminUser = "Admin";
        private const string adminPassword = "Secret123$";

        public static async Task EnsurePopulatedAsync(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Apply pending migrations
                if (context.Database.GetPendingMigrations().Any())
                {
                    context.Database.Migrate();
                }

                // Tạo role nếu chưa có
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }

                // Tạo admin user nếu chưa có
                ApplicationUser user = await userManager.FindByNameAsync(adminUser);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = adminUser,
                        Email = "admin@example.com",
                        PhoneNumber = "555-1234",
                        FullName = "Quản trị viên",
                        Address = "TP.HCM",
                        BirthDate = new DateTime(1990, 1, 1),
                        IsAdmin = true
                    };

                    var result = await userManager.CreateAsync(user, adminPassword);
                    if (!result.Succeeded)
                    {
                        throw new Exception("Tạo tài khoản admin thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else if (!user.IsAdmin)
                {
                    user.IsAdmin = true;
                    await userManager.UpdateAsync(user);
                }

                // Gán role Admin nếu chưa có
                var rolesForUser = await userManager.GetRolesAsync(user);
                if (!rolesForUser.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
