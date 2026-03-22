using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SportsStore.Models
{
    // =================================================================
    // 📚 MẪU THIẾT KẾ REPOSITORY - Quản lý Data Access
    // =================================================================
    // Mục đích: Định nghĩa interface thống nhất cho truy cập dữ liệu
    // 
    // Cách hoạt động:
    //   1. Định nghĩa abstract interface IStoreRepository
    //   2. Implementation cụ thể: EFStoreRepository dùng Entity Framework
    //   3. Tất cả truy vấn đều đi qua repository, không trực tiếp context
    //
    // Lợi ích:
    //   • Tách business logic khỏi database implementation detail
    //   • Dễ dàng mock trong unit tests
    //   • Dễ dàng thử trường cơ sở dữ liệu khác (thay EF bằng Dapper, Sql Raw, etc.)
    //
    // 🔄 LIÊN QUAN:
    //   • ADAPTER PATTERN: Adapts DbContext để provide unified interface
    //   • FACADE PATTERN: Ẩn chi tiết của EF behind repository methods
    //   • STRATEGY PATTERN: Có thể có nhiều EFStoreRepository, SqlStoreRepository, v.v.
    //
    // 📄 LIÊN KẾT VỚI FILE KHÁC:
    //   • Models/IStoreRepository.cs: Interface definition
    //   • Models/StoreDbContext.cs: Database context (DbSet<Product>, etc.)
    //   • Program.cs: Đăng ký qua DI: `AddScoped<IStoreRepository, EFStoreRepository>()`
    //   • Controllers/HomeController.cs: Dùng repository qua dependency injection
    // ==================================================================
    public class EFStoreRepository : IStoreRepository
    {
        // 💼 Context: Tương tác với database
        // Repository pattern làm cầu nối giữa business layer và data layer
        private StoreDbContext context;

        public EFStoreRepository(StoreDbContext ctx)
        {
            context = ctx;
        }

        // 📄 Products Property: Trả Query<Product> - Lazy Loading
        // Dùng .Include() để eager load related entities
        // Client có thể đọng động filter, sort thêm trước khi execute
        public IQueryable<Product> Products => context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages); // Load ảnh phụ

        public IQueryable<Category> Categories => context.Categories;

        public IQueryable<Product> GetProductsByCategory(int categoryId)
        {
            return context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == categoryId);
        }

        public void CreateProduct(Product p)
        {
            context.Add(p);
            context.SaveChanges();
        }

        public void DeleteProduct(Product p)
        {
            // Xóa hình ảnh phụ trước
            var images = context.ProductImages.Where(img => img.ProductID == p.ProductID);
            context.ProductImages.RemoveRange(images);

            context.Remove(p);
            context.SaveChanges();
        }

        public void SaveProduct(Product p)
        {
            context.SaveChanges();
        }

        //  Quản lý ảnh phụ
        public void AddProductImage(ProductImage image)
        {
            context.ProductImages.Add(image);
            context.SaveChanges();
        }

        public void DeleteProductImage(long imageId)
        {
            var image = context.ProductImages.Find(imageId);
            if (image != null)
            {
                context.ProductImages.Remove(image);
                context.SaveChanges();
            }
        }

        public ProductImage? GetProductImageById(long imageId)
        {
            return context.ProductImages.Find(imageId);
        }

        public IQueryable<ProductImage> GetImagesByProductId(long productId)
        {
            return context.ProductImages.Where(img => img.ProductID == productId);
        }

        public IQueryable<ProductImage> ProductImages => context.ProductImages;
    }
}
