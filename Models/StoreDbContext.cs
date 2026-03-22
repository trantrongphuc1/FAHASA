using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    // 🗄️ ENTITY FRAMEWORK CONTEXT - Database context chính
    // Quản lý kết nối và mapping giữa models và database tables
    // 🔗 KẾ THỪA: StoreDbContext kế thừa từ IdentityDbContext<ApplicationUser>
    public class StoreDbContext : IdentityDbContext<ApplicationUser>
    {
        public StoreDbContext(DbContextOptions<StoreDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TutorBooking> TutorBookings { get; set; }
        public DbSet<CartLine> CartLines { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherUserUsage> VoucherUserUsages { get; set; }
        public DbSet<UserCartItem> UserCartItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure decimal precision for HourlyRate to avoid silent truncation
            modelBuilder.Entity<Tutor>()
                .Property(t => t.HourlyRate)
                .HasPrecision(18, 2);
        }
    }
}
