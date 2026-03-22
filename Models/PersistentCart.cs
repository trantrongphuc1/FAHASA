using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    // =================================================================
    // 🎯 MẦU THIẾT KẾ: FACTORY METHOD + DECORATOR + FACADE
    // =================================================================
    //
    // 1️⃣ FACTORY METHOD PATTERN (Phương thức Factory tĩnh)
    // -----------------------------------------------
    // Mục đích: GetCart() là factory method tạo Cart instance
    // Context:
    //   - User đăng nhập: Tạo PersistentCart, load từ DATABASE
    //   - User ẩn dạnh: Tạo PersistentCart, load từ SESSION
    // Lợi ích: Loại mũ xử lý cart creation complexity từ client
    //
    // 2️⃣ DECORATOR PATTERN
    // -----------------------------------------------
    // Mục đích: Mở rộng Cart với tính năng persistence
    // PersistentCart WRAPS Cart base class
    // Thêm chức năng: LoadFromDatabase(), SaveToDatabase(), LoadFromSession()
    //
    // 3️⃣ FACADE PATTERN
    // -----------------------------------------------
    // Mục đích: Cung cấp giao diện thống nhất cho cart operations
    // Ẩn đi:
    //   - PhUC tạp của session vs database logic
    //   - Authentication checking
    //   - Product loading từ repository
    //
    // Cách hoạt động:
    //   GetCart(IServiceProvider) 
    //     → Kiểm tra user ăng nhập hay không
    //     → Load từ Database (nếu ăng nhập) hoặc Session (nếu ẩn dạnh)
    //     → Trả về Cart instance đã populate
    //
    // 💂 DEPENDENCY INJECTION:
    //   - IServiceProvider: Truy cập tất cả dịch vụ đăng ký (DI container)
    //   - IHttpContextAccessor: Lấy HTTP context, session, user info
    //   - StoreDbContext: Database context
    //   - IStoreRepository: Repository pattern để lấy sản phẩm
    //
    // 📁 LIÊN KẾT VỚI FILE KHÁC:
    //   • Program.cs: Đăng ký qua: `AddScoped<Cart>(sp => PersistentCart.GetCart(sp))`
    //   • Models/Cart.cs: Base class của PersistentCart
    //   • Models/IStoreRepository.cs: để lấy Product
    //   • Models/StoreDbContext.cs: để access UserCartItems table
    // ==================================================================
    public class PersistentCart : Cart
    {
        // � FACTORY METHOD: Phương thức tĩnh
        // Tạo instance Cart thích hợp dựa trên context
        // 
        // Logic:
        //   1. Nếu user ăng nhập: LoadFromDatabase() → persistent cart
        //   2. Nếu user ẩn dạnh: LoadFromSession() → temporary cart
        //
        // Trê lợi: 
        //   - Người gọi không cần biết chi tiết
        //   - Factory quản lý cart creation complexity
        public static Cart GetCart(IServiceProvider services)
        {
            var session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Session;
            var context = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext;
            var db = services.GetRequiredService<StoreDbContext>();
            var repo = services.GetRequiredService<IStoreRepository>();

            var cart = new PersistentCart
            {
                Session = session,
                DbContext = db,
                Repository = repo
            };

            var userId = context?.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            cart.UserId = userId;

            // Load cart from database if user is logged in
            if (!string.IsNullOrEmpty(userId))
            {
                cart.LoadFromDatabase(userId);
            }
            else
            {
                // Load from session for anonymous users
                cart.LoadFromSession();
            }

            return cart;
        }

        public ISession? Session { get; set; }
        public StoreDbContext? DbContext { get; set; }
        public IStoreRepository? Repository { get; set; }
        public string? UserId { get; set; }

        private void LoadFromDatabase(string userId)
        {
            if (DbContext == null || Repository == null) return;

            var dbCartItems = DbContext.UserCartItems
                .Where(c => c.UserId == userId)
                .ToList();

            Lines.Clear();

            foreach (var item in dbCartItems)
            {
                var product = Repository.Products.FirstOrDefault(p => p.ProductID == item.ProductId);
                if (product != null)
                {
                    Lines.Add(new CartLine
                    {
                        Product = product,
                        Quantity = item.Quantity,
                        IsRental = item.IsRental,
                        RentalDays = item.RentalDays
                    });
                }
            }

            Console.WriteLine($"[PersistentCart] Loaded {Lines.Count} items from database for user {userId}");
        }

        private void LoadFromSession()
        {
            if (Session == null || Repository == null) return;

            var cartSession = Session.GetJson<List<CartLineSession>>("Cart");
            if (cartSession != null)
            {
                foreach (var item in cartSession)
                {
                    var product = Repository.Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                    if (product != null)
                    {
                        Lines.Add(new CartLine
                        {
                            Product = product,
                            Quantity = item.Quantity,
                            IsRental = item.IsRental,
                            RentalDays = item.RentalDays
                        });
                    }
                }
            }

            Console.WriteLine($"[PersistentCart] Loaded {Lines.Count} items from session");
        }

        public override void AddItem(Product product, int quantity, bool isRental = false, int rentalDays = 0)
        {
            base.AddItem(product, quantity, isRental, rentalDays);
            SaveCart();
        }

        public override void RemoveLine(Product product, bool isRental = false)
        {
            base.RemoveLine(product, isRental);
            SaveCart();
        }

        public override void Clear()
        {
            base.Clear();
            
            if (!string.IsNullOrEmpty(UserId) && DbContext != null)
            {
                var items = DbContext.UserCartItems.Where(c => c.UserId == UserId);
                DbContext.UserCartItems.RemoveRange(items);
                DbContext.SaveChanges();
            }

            Session?.Remove("Cart");
        }

        private void SaveCart()
        {
            if (!string.IsNullOrEmpty(UserId) && DbContext != null)
            {
                SaveToDatabase(UserId);
            }
            else
            {
                SaveToSession();
            }
        }

        private void SaveToDatabase(string userId)
        {
            if (DbContext == null) return;

            try
            {
                // Remove all existing cart items for this user
                var existingItems = DbContext.UserCartItems.Where(c => c.UserId == userId).ToList();
                DbContext.UserCartItems.RemoveRange(existingItems);

                // Add current cart items
                foreach (var line in Lines)
                {
                    DbContext.UserCartItems.Add(new UserCartItem
                    {
                        UserId = userId,
                        ProductId = line.Product.ProductID ?? 0,
                        Quantity = line.Quantity,
                        IsRental = line.IsRental,
                        RentalDays = line.RentalDays,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                DbContext.SaveChanges();
                Console.WriteLine($"[PersistentCart] Saved {Lines.Count} items to database for user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PersistentCart] Error saving to database: {ex.Message}");
            }
        }

        private void SaveToSession()
        {
            if (Session == null) return;

            var cartSession = Lines.Select(line => new CartLineSession
            {
                ProductID = line.Product.ProductID ?? 0,
                Quantity = line.Quantity,
                IsRental = line.IsRental,
                RentalDays = line.RentalDays
            }).ToList();

            Session.SetJson("Cart", cartSession);
            Console.WriteLine($"[PersistentCart] Saved {Lines.Count} items to session");
        }

        /// <summary>
        /// Migrate session cart to database when user logs in
        /// </summary>
        public void MigrateSessionToDatabase(string userId)
        {
            if (string.IsNullOrEmpty(userId) || DbContext == null) return;

            UserId = userId;

            // Load existing database cart
            var dbItems = DbContext.UserCartItems.Where(c => c.UserId == userId).ToList();
            var dbCart = new List<CartLine>();

            foreach (var item in dbItems)
            {
                var product = Repository?.Products.FirstOrDefault(p => p.ProductID == item.ProductId);
                if (product != null)
                {
                    dbCart.Add(new CartLine
                    {
                        Product = product,
                        Quantity = item.Quantity,
                        IsRental = item.IsRental,
                        RentalDays = item.RentalDays
                    });
                }
            }

            // Merge session cart with database cart
            foreach (var sessionLine in Lines.ToList())
            {
                var existingLine = dbCart.FirstOrDefault(l => 
                    l.Product.ProductID == sessionLine.Product.ProductID && 
                    l.IsRental == sessionLine.IsRental);

                if (existingLine != null)
                {
                    // Merge quantities
                    existingLine.Quantity += sessionLine.Quantity;
                }
                else
                {
                    dbCart.Add(sessionLine);
                }
            }

            // Update Lines with merged cart
            Lines.Clear();
            Lines.AddRange(dbCart);

            // Save to database
            SaveToDatabase(userId);

            // Clear session
            Session?.Remove("Cart");

            Console.WriteLine($"[PersistentCart] Migrated session cart to database for user {userId}");
        }
    }
}
