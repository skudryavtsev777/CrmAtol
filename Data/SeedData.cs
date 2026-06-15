using Microsoft.AspNetCore.Identity;
using CrmAtol.Models;

namespace CrmAtol.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await context.Database.EnsureCreatedAsync();

                // Создание ролей
                string[] roles = { "Admin", "Manager", "Engineer" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                // Создание пользователей
                var users = new[]
                {
                    new { Email = "admin@crmdemo.ru", Password = "Admin123!", Role = "Admin" },
                    new { Email = "manager@crmdemo.ru", Password = "Manager123!", Role = "Manager" },
                    new { Email = "engineer@crmdemo.ru", Password = "Engineer123!", Role = "Engineer" }
                };

                foreach (var userData in users)
                {
                    if (await userManager.FindByEmailAsync(userData.Email) == null)
                    {
                        var user = new IdentityUser { UserName = userData.Email, Email = userData.Email };
                        await userManager.CreateAsync(user, userData.Password);
                        await userManager.AddToRoleAsync(user, userData.Role);
                    }
                }

                // Тестовый клиент
                if (!context.Clients.Any())
                {
                    context.Clients.Add(new Client
                    {
                        Name = "ООО Тест",
                        Inn = "1234567890",
                        Phone = "+7(123)456-78-90",
                        Email = "test@test.ru",
                        Address = "г. Москва, ул. Тестовая, д. 1"
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}