using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    // 🏗️ MẪU THIẾT KẾ DECORATOR - Mở rộng Cart với tính năng persistence
    // Nâng cấp Cart với khả năng lưu trữ database/session
    // 🎯 MẪU THIẾT KẾ FACADE - Cung cấp giao diện thống nhất cho các thao tác cart
    // Ẩn độ phức tạp của logic lưu trữ session vs database
    // 🔗 KẾ THỪA: PersistentCart kế thừa từ class Cart (base cart functionality)
    public class PersistentCart : Cart
    {
        // 🏭 MẪU THIẾT KẾ FACTORY METHOD - Phương thức factory tĩnh
        // Tạo instance Cart phù hợp dựa trên context người dùng
        // Trả về instance Cart được cấu hình cho user đăng nhập hoặc ẩn danh
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
