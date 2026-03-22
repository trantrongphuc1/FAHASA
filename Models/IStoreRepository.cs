using System.Linq;

namespace SportsStore.Models
{
    // 📦 MẪU THIẾT KẾ REPOSITORY - Định nghĩa interface
    // Xác định hợp đồng cho các thao tác data access
    // Cho phép dependency injection và dễ dàng testing
    // 🔗 INTERFACE: IStoreRepository là interface được implement bởi EFStoreRepository
    public interface IStoreRepository
    {
        IQueryable<Product> Products { get; }
        IQueryable<Category> Categories { get; }

        void CreateProduct(Product p);
        void SaveProduct(Product p);
        void DeleteProduct(Product p);

        // Lấy sản phẩm theo category
        IQueryable<Product> GetProductsByCategory(int categoryId);

        // Quản lý ảnh phụ cho sản phẩm
        IQueryable<ProductImage> ProductImages { get; }

        void AddProductImage(ProductImage image);
        void DeleteProductImage(long imageId);            
        ProductImage? GetProductImageById(long imageId);

        IQueryable<ProductImage> GetImagesByProductId(long productId);
    }
}
