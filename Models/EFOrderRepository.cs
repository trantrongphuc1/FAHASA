using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SportsStore.Models
{
    // =================================================================
    // 📚 MẪU THIẾT KẾ REPOSITORY - Quản lý Data Access cho Order
    // =================================================================
    // Mục đích: Định nghĩa interface thống nhất của Order repository
    // 
    // Các kỹ năng:
    //   1. SaveOrder(): Lưu order MỚI + TRỪ SỐ LƯỢNG sản phẩm nếu mua (không thuê)
    //   2. Orders Property: Lấy tất cả order (với eager load Lines + Product)
    //   3. DeleteOrder(): Xóa order và các lines liên quan
    //
    // 📚 LIÊN QUAN:
    //   • REPOSITORY PATTERN: Tạo abstract layer cho Order data access
    //   • UNIT OF WORK: SaveOrder() là atomic operation - tất cả hoặc không cái gì
    //
    // 📄 LIÊN KẾT VỚI FILE KHÁC:
    //   • Models/IOrderRepository.cs: Interface definition
    //   • Models/Order.cs: Entity model
    //   • Program.cs: Đăng ký qua DI: `AddScoped<IOrderRepository, EFOrderRepository>()`
    //   • Controllers/OrderController.cs: Dùng repository qua DI
    // ==================================================================
    public class EFOrderRepository : IOrderRepository
    {
        // 💼 Context: DbContext cho database
        // Tạo cầu nối giữa business layer và data access layer
        private StoreDbContext context;

        public EFOrderRepository(StoreDbContext ctx)
        {
            context = ctx;
        }

        // 📄 Orders Property: Trả Query<Order> - Lazy Loading
        // .Include(o => o.Lines) - Eager load các CartLine
        // .ThenInclude(l => l.Product) - Eager load Product trong từng line
        // Giúp tránh N+1 query problem
        public IQueryable<Order> Orders => context.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product);

        // 📚 SaveOrder: Lưu order với logic trừ số lượng
        // UNIT OF WORK pattern: Tất cả hoặc không có gì
        public void SaveOrder(Order order)
        {
            // Attach products từ order lines 
            context.AttachRange(order.Lines.Select(l => l.Product));

            // 💼 BUSINESS LOGIC: Trừ số lượng sản phẩm khi MUA (KHÔNG áp dụng cho THUÊ)
            // Difference:
            //   - IsRental = false: MUA → Trừ quantity (sản phẩm bị bán đi)
            //   - IsRental = true: THUÊ → Không trừ quantity (tạo Rental record thay vì)
            foreach (var line in order.Lines)
            {
                var product = context.Products.FirstOrDefault(p => p.ProductID == line.Product.ProductID);

                if (product != null && line.IsRental == false)
                {
                    // Chỉ trừ số lượng nếu LÀ PURCHASE, không phải RENTAL
                    product.Quantity -= line.Quantity;

                    if (product.Quantity < 0)
                    {
                        product.Quantity = 0; // Bảo vệ: không để số lượng âm
                    }
                }
            }

            if (order.OrderID == 0)
            {
                context.Orders.Add(order);
            }

            // ⚛️ ATOMIC: SaveChanges() - tất cả hoặc không có gì
            // Nếu có lỗi, toàn bộ transaction bị rollback
            context.SaveChanges();
        }

        public void DeleteOrder(Order order)
        {
            context.RemoveRange(order.Lines);
            context.Orders.Remove(order);
            context.SaveChanges();
        }
    }
}
