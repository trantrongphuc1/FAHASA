using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SportsStore.Models
{
    // 📦 MẪU THIẾT KẾ REPOSITORY - Lớp trừu tượng cho data access
    // Đóng gói các truy vấn Entity Framework
    // Cung cấp giao diện thống nhất IStoreRepository cho data access
    // Tách biệt business logic khỏi database implementation
    // 🔗 IMPLEMENT: EFStoreRepository implement interface IStoreRepository
    public class EFStoreRepository : IStoreRepository
    {
        private StoreDbContext context;

        public EFStoreRepository(StoreDbContext ctx)
        {
            context = ctx;
        }

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
