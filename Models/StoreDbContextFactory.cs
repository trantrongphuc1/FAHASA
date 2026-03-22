using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SportsStore.Models
{
    // 🏭 MẪU THIẾT KẾ FACTORY - IDesignTimeDbContextFactory<T>
    // Tạo các instance StoreDbContext cho các thao tác design-time (EF migrations)
    // Đóng gói logic tạo DbContext trong một factory class chuyên dụng
    // 🔗 IMPLEMENT: StoreDbContextFactory implement interface IDesignTimeDbContextFactory<StoreDbContext>
    public class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
    {
        public StoreDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<StoreDbContext>();
            var connectionString = configuration.GetConnectionString("SportsStoreConnection"); // Đúng tên key

            optionsBuilder.UseSqlServer(connectionString);

            return new StoreDbContext(optionsBuilder.Options);
        }
    }
}
