using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace SportsStore.Models
{
    public static class SeedData
    {
        public static void EnsurePopulated(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            StoreDbContext context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            // Xóa seed cũ
            // Seed Authors
            if (!context.Authors.Any())
            {
                context.Authors.AddRange(
                    new Author { Name = "Nguyễn Nhật Ánh", Description = "Tác giả nổi tiếng với truyện thiếu nhi" },
                    new Author { Name = "Toán Khoa", Description = "Giáo viên dạy toán, biên soạn sách tham khảo" },
                    new Author { Name = "Nam Cao", Description = "Nhà văn hiện thực nổi tiếng VN" },
                    new Author { Name = "Hồ Ngọc Đức", Description = "Biên soạn sách tiếng Anh phổ thông" },
                    new Author { Name = "Lê Hồng Hà", Description = "Chủ biên tập sách luyện thi các khối" },
                    new Author { Name = "Nguyễn Du", Description = "Đại thi hào dân tộc" },
                    new Author { Name = "Xuân Diệu", Description = "Nhà thơ tình nổi tiếng" },
                    new Author { Name = "Tố Hữu", Description = "Nhà thơ cách mạng" },
                    new Author { Name = "Vũ Trọng Phụng", Description = "Nhà văn hiện thực" },
                    new Author { Name = "Thạch Lam", Description = "Nhà văn lãng mạn" },
                    new Author { Name = "Nguyễn Trãi", Description = "Danh nhân văn hóa" },
                    new Author { Name = "Hồ Chí Minh", Description = "Chủ tịch nước, nhà thơ" },
                    new Author { Name = "Nguyễn Đình Chiểu", Description = "Nhà thơ yêu nước" },
                    new Author { Name = "Ngô Tất Tố", Description = "Nhà văn hiện thực" },
                    new Author { Name = "Kim Lân", Description = "Nhà văn hiện thực" }
                );
            context.SaveChanges();
            }

            // Seed Categories - đảm bảo đủ category bắt buộc để tránh lỗi KeyNotFound khi seed Products
            var requiredCategoryNames = new[]
            {
                "Sách lớp 1",
                "Sách lớp 2",
                "Sách lớp 3",
                "Sách Khoa học",
                "Sách Văn học",
                "Sách Ngoại ngữ"
            };

            var existingCategoryNames = context.Categories
                .Select(c => c.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingCategories = requiredCategoryNames
                .Where(name => !existingCategoryNames.Contains(name))
                .Select(name => new Category { Name = name, AllowRent = false })
                .ToList();

            if (missingCategories.Any())
            {
                context.Categories.AddRange(missingCategories);
                context.SaveChanges();
            }

            // Seed Products - Xóa sản phẩm cũ nếu có để seed lại
            if (!context.Products.Any() || context.Products.Count() < 50)
            {
                // Xóa tất cả sản phẩm cũ để seed lại
                if (context.Products.Any())
                {
                    context.Products.RemoveRange(context.Products);
                    context.SaveChanges();
                }
                var authors = context.Authors.ToDictionary(a => a.Name, a => a.AuthorId);
                var categories = context.Categories.ToDictionary(c => c.Name, c => c.CategoryId);

                var products = new List<Product>();

                // Sách lớp 1 (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Tiếng Việt lớp 1 - Tập 1", Description = "Sách giáo khoa tiếng Việt lớp 1", Price = 25000, CategoryId = categories["Sách lớp 1"], Quantity = 30, IsForSale = true },
                    new Product { Name = "Toán lớp 1", Description = "Sách giáo khoa toán lớp 1", Price = 24000, CategoryId = categories["Sách lớp 1"], Quantity = 28, IsForSale = true },
                    new Product { Name = "Tập Viết lớp 1", Description = "Vở tập viết chữ đẹp", Price = 15000, CategoryId = categories["Sách lớp 1"], Quantity = 35, IsForSale = true },
                    new Product { Name = "Bài Tập Tiếng Việt lớp 1", Description = "Sách bài tập bổ trợ", Price = 20000, CategoryId = categories["Sách lớp 1"], Quantity = 25, IsForSale = true },
                    new Product { Name = "Bài Tập Toán lớp 1", Description = "Sách bài tập toán nâng cao", Price = 22000, CategoryId = categories["Sách lớp 1"], Quantity = 27, IsForSale = true },
                    new Product { Name = "Tự nhiên và Xã hội lớp 1", Description = "SGK tự nhiên xã hội", Price = 18000, CategoryId = categories["Sách lớp 1"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Đạo Đức lớp 1", Description = "Sách giáo dục đạo đức", Price = 16000, CategoryId = categories["Sách lớp 1"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Mỹ Thuật lớp 1", Description = "Sách mỹ thuật cơ bản", Price = 17000, CategoryId = categories["Sách lớp 1"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Âm Nhạc lớp 1", Description = "Sách học nhạc", Price = 16000, CategoryId = categories["Sách lớp 1"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Thể Dục lớp 1", Description = "Sách hướng dẫn thể dục", Price = 15000, CategoryId = categories["Sách lớp 1"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Luyện Chữ Đẹp lớp 1", Description = "Vở luyện chữ", Price = 14000, CategoryId = categories["Sách lớp 1"], Quantity = 30, IsForSale = true },
                    new Product { Name = "Truyện Cổ Tích Việt Nam", Description = "Tuyển tập truyện cổ tích", Price = 35000, CategoryId = categories["Sách lớp 1"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Bé Học Chữ Cái", Description = "Sách học chữ cái", Price = 28000, CategoryId = categories["Sách lớp 1"], Quantity = 24, IsForSale = true },
                    new Product { Name = "Bé Tập Đếm", Description = "Sách học đếm số", Price = 26000, CategoryId = categories["Sách lớp 1"], Quantity = 26, IsForSale = true },
                    new Product { Name = "Vui Học Tiếng Việt lớp 1", Description = "Sách học tiếng Việt vui nhộn", Price = 32000, CategoryId = categories["Sách lớp 1"], Quantity = 21, IsForSale = true }
                });

                // Sách lớp 2 (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Tiếng Việt lớp 2 - Tập 1", Description = "SGK tiếng Việt lớp 2", Price = 26000, CategoryId = categories["Sách lớp 2"], Quantity = 28, IsForSale = true },
                    new Product { Name = "Toán lớp 2", Description = "SGK toán lớp 2", Price = 25000, CategoryId = categories["Sách lớp 2"], Quantity = 26, IsForSale = true },
                    new Product { Name = "Bài Tập Tiếng Việt lớp 2", Description = "Sách bài tập bổ trợ", Price = 21000, CategoryId = categories["Sách lớp 2"], Quantity = 24, IsForSale = true },
                    new Product { Name = "Bài Tập Toán lớp 2", Description = "Sách bài tập toán", Price = 23000, CategoryId = categories["Sách lớp 2"], Quantity = 25, IsForSale = true },
                    new Product { Name = "Tự nhiên và Xã hội lớp 2", Description = "SGK tự nhiên xã hội", Price = 19000, CategoryId = categories["Sách lớp 2"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Đạo Đức lớp 2", Description = "Sách giáo dục đạo đức", Price = 17000, CategoryId = categories["Sách lớp 2"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Luyện Viết Chữ Đẹp lớp 2", Description = "Vở luyện chữ", Price = 16000, CategoryId = categories["Sách lớp 2"], Quantity = 30, IsForSale = true },
                    new Product { Name = "Tập Đọc lớp 2", Description = "Sách tập đọc", Price = 22000, CategoryId = categories["Sách lớp 2"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Chính Tả lớp 2", Description = "Sách chính tả", Price = 20000, CategoryId = categories["Sách lớp 2"], Quantity = 23, IsForSale = true },
                    new Product { Name = "Luyện Từ và Câu lớp 2", Description = "Sách luyện từ vựng", Price = 24000, CategoryId = categories["Sách lớp 2"], Quantity = 21, IsForSale = true },
                    new Product { Name = "Tập Làm Văn lớp 2", Description = "Sách tập làm văn", Price = 25000, CategoryId = categories["Sách lớp 2"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Kể Chuyện lớp 2", Description = "Sách kể chuyện", Price = 27000, CategoryId = categories["Sách lớp 2"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Toán Nâng Cao lớp 2", Description = "Sách toán nâng cao", Price = 35000, CategoryId = categories["Sách lớp 2"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Tiếng Việt Nâng Cao lớp 2", Description = "Sách tiếng Việt nâng cao", Price = 33000, CategoryId = categories["Sách lớp 2"], Quantity = 16, IsForSale = true },
                    new Product { Name = "Tuyển Tập Đề Thi lớp 2", Description = "Sách đề thi tham khảo", Price = 40000, CategoryId = categories["Sách lớp 2"], Quantity = 14, IsForSale = true }
                });

                // Sách lớp 3 (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Cho Tôi Xin Một Vé Đi Tuổi Thơ", Description = "Tác phẩm của Nguyễn Nhật Ánh", Price = 72000, CategoryId = categories["Sách lớp 3"], AuthorId = authors["Nguyễn Nhật Ánh"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Bài Tập Toán Nâng Cao lớp 3", Description = "Sách tham khảo toán lớp 3 nâng cao", Price = 35000, CategoryId = categories["Sách lớp 3"], AuthorId = authors["Toán Khoa"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Tiếng Việt lớp 3 - Tập 1", Description = "SGK tiếng Việt lớp 3", Price = 27000, CategoryId = categories["Sách lớp 3"], Quantity = 27, IsForSale = true },
                    new Product { Name = "Toán lớp 3", Description = "SGK toán lớp 3", Price = 26000, CategoryId = categories["Sách lớp 3"], Quantity = 25, IsForSale = true },
                    new Product { Name = "Bài Tập Tiếng Việt lớp 3", Description = "Sách bài tập bổ trợ", Price = 22000, CategoryId = categories["Sách lớp 3"], Quantity = 23, IsForSale = true },
                    new Product { Name = "Bài Tập Toán lớp 3", Description = "Sách bài tập toán", Price = 24000, CategoryId = categories["Sách lớp 3"], Quantity = 24, IsForSale = true },
                    new Product { Name = "Tự nhiên và Xã hội lớp 3", Description = "SGK tự nhiên xã hội", Price = 20000, CategoryId = categories["Sách lớp 3"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Đạo Đức lớp 3", Description = "Sách giáo dục đạo đức", Price = 18000, CategoryId = categories["Sách lớp 3"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Luyện Viết Chữ Đẹp lớp 3", Description = "Vở luyện chữ", Price = 17000, CategoryId = categories["Sách lớp 3"], Quantity = 29, IsForSale = true },
                    new Product { Name = "Tập Đọc lớp 3", Description = "Sách tập đọc", Price = 23000, CategoryId = categories["Sách lớp 3"], Quantity = 21, IsForSale = true },
                    new Product { Name = "Chính Tả lớp 3", Description = "Sách chính tả", Price = 21000, CategoryId = categories["Sách lớp 3"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Luyện Từ và Câu lớp 3", Description = "Sách luyện từ vựng", Price = 25000, CategoryId = categories["Sách lớp 3"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Tập Làm Văn lớp 3", Description = "Sách tập làm văn", Price = 26000, CategoryId = categories["Sách lớp 3"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Kể Chuyện lớp 3", Description = "Sách kể chuyện", Price = 28000, CategoryId = categories["Sách lớp 3"], Quantity = 16, IsForSale = true },
                    new Product { Name = "Tuyển Tập Đề Thi lớp 3", Description = "Sách đề thi tham khảo", Price = 42000, CategoryId = categories["Sách lớp 3"], Quantity = 13, IsForSale = true }
                });

                // Sách Văn học (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Ngữ Văn 12 - Tập 1", Description = "SGK cơ bản, dành cho học sinh lớp 12", Price = 28000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nam Cao"], Quantity = 12, IsForSale = true },
                    new Product { Name = "Truyện Kiều", Description = "Tác phẩm kinh điển của Nguyễn Du", Price = 45000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nguyễn Du"], Quantity = 25, IsForSale = true },
                    new Product { Name = "Chí Phèo", Description = "Truyện ngắn nổi tiếng của Nam Cao", Price = 38000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nam Cao"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Số Đỏ", Description = "Tiểu thuyết của Vũ Trọng Phụng", Price = 55000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Vũ Trọng Phụng"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Hai Đứa Trẻ", Description = "Truyện ngắn của Thạch Lam", Price = 32000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Thạch Lam"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Vợ Nhặt", Description = "Truyện ngắn của Kim Lân", Price = 35000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Kim Lân"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Tắt Đèn", Description = "Tiểu thuyết của Ngô Tất Tố", Price = 48000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Ngô Tất Tố"], Quantity = 16, IsForSale = true },
                    new Product { Name = "Thơ Xuân Diệu", Description = "Tuyển tập thơ tình", Price = 42000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Xuân Diệu"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Thơ Tố Hữu", Description = "Tuyển tập thơ cách mạng", Price = 40000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Tố Hữu"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Bình Ngô Đại Cáo", Description = "Tác phẩm của Nguyễn Trãi", Price = 36000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nguyễn Trãi"], Quantity = 14, IsForSale = true },
                    new Product { Name = "Nhật Ký Trong Tù", Description = "Thơ của Hồ Chí Minh", Price = 38000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Hồ Chí Minh"], Quantity = 13, IsForSale = true },
                    new Product { Name = "Lục Vân Tiên", Description = "Truyện thơ của Nguyễn Đình Chiểu", Price = 44000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nguyễn Đình Chiểu"], Quantity = 11, IsForSale = true },
                    new Product { Name = "Dế Mèn Phiêu Lưu Ký", Description = "Truyện thiếu nhi kinh điển", Price = 50000, CategoryId = categories["Sách Văn học"], Quantity = 21, IsForSale = true },
                    new Product { Name = "Tôi Thấy Hoa Vàng Trên Cỏ Xanh", Description = "Truyện của Nguyễn Nhật Ánh", Price = 68000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nguyễn Nhật Ánh"], Quantity = 23, IsForSale = true },
                    new Product { Name = "Mắt Biếc", Description = "Tiểu thuyết của Nguyễn Nhật Ánh", Price = 75000, CategoryId = categories["Sách Văn học"], AuthorId = authors["Nguyễn Nhật Ánh"], Quantity = 24, IsForSale = true }
                });

                // Sách Ngoại ngữ (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Tiếng Anh lớp 9 nâng cao", Description = "Giáo trình tiếng Anh biên soạn bởi Hồ Ngọc Đức", Price = 54000, CategoryId = categories["Sách Ngoại ngữ"], AuthorId = authors["Hồ Ngọc Đức"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 6", Description = "SGK tiếng Anh lớp 6", Price = 32000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 26, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 7", Description = "SGK tiếng Anh lớp 7", Price = 33000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 24, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 8", Description = "SGK tiếng Anh lớp 8", Price = 34000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 10", Description = "SGK tiếng Anh lớp 10", Price = 36000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 11", Description = "SGK tiếng Anh lớp 11", Price = 37000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Tiếng Anh lớp 12", Description = "SGK tiếng Anh lớp 12", Price = 38000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Luyện Thi TOEIC", Description = "Sách luyện thi TOEIC", Price = 85000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 12, IsForSale = true },
                    new Product { Name = "Luyện Thi IELTS", Description = "Sách luyện thi IELTS", Price = 95000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 10, IsForSale = true },
                    new Product { Name = "Ngữ Pháp Tiếng Anh Cơ Bản", Description = "Sách ngữ pháp", Price = 62000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Từ Vựng Tiếng Anh Theo Chủ Đề", Description = "Sách từ vựng", Price = 58000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 16, IsForSale = true },
                    new Product { Name = "Giao Tiếp Tiếng Anh Hàng Ngày", Description = "Sách giao tiếp", Price = 72000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 14, IsForSale = true },
                    new Product { Name = "Tiếng Anh Cho Trẻ Em", Description = "Sách tiếng Anh thiếu nhi", Price = 45000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 21, IsForSale = true },
                    new Product { Name = "Phát Âm Tiếng Anh Chuẩn", Description = "Sách phát âm", Price = 55000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 13, IsForSale = true },
                    new Product { Name = "Luyện Nghe Tiếng Anh", Description = "Sách luyện kỹ năng nghe", Price = 68000, CategoryId = categories["Sách Ngoại ngữ"], Quantity = 11, IsForSale = true }
                });

                // Sách Khoa học (15 sản phẩm)
                products.AddRange(new[]
                {
                    new Product { Name = "Sổ Tay Ôn Thi THPT Quốc Gia Khoa Học Tự Nhiên", Description = "Sổ tay tổng hợp các kiến thức quan trọng lý-hóa-sinh", Price = 61000, CategoryId = categories["Sách Khoa học"], AuthorId = authors["Lê Hồng Hà"], Quantity = 10, IsForSale = true },
                    new Product { Name = "Vật Lý lớp 10", Description = "SGK vật lý lớp 10", Price = 29000, CategoryId = categories["Sách Khoa học"], Quantity = 23, IsForSale = true },
                    new Product { Name = "Vật Lý lớp 11", Description = "SGK vật lý lớp 11", Price = 30000, CategoryId = categories["Sách Khoa học"], Quantity = 21, IsForSale = true },
                    new Product { Name = "Vật Lý lớp 12", Description = "SGK vật lý lớp 12", Price = 31000, CategoryId = categories["Sách Khoa học"], Quantity = 19, IsForSale = true },
                    new Product { Name = "Hóa Học lớp 10", Description = "SGK hóa học lớp 10", Price = 28000, CategoryId = categories["Sách Khoa học"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Hóa Học lớp 11", Description = "SGK hóa học lớp 11", Price = 29000, CategoryId = categories["Sách Khoa học"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Hóa Học lớp 12", Description = "SGK hóa học lớp 12", Price = 30000, CategoryId = categories["Sách Khoa học"], Quantity = 18, IsForSale = true },
                    new Product { Name = "Sinh Học lớp 10", Description = "SGK sinh học lớp 10", Price = 27000, CategoryId = categories["Sách Khoa học"], Quantity = 24, IsForSale = true },
                    new Product { Name = "Sinh Học lớp 11", Description = "SGK sinh học lớp 11", Price = 28000, CategoryId = categories["Sách Khoa học"], Quantity = 22, IsForSale = true },
                    new Product { Name = "Sinh Học lớp 12", Description = "SGK sinh học lớp 12", Price = 29000, CategoryId = categories["Sách Khoa học"], Quantity = 20, IsForSale = true },
                    new Product { Name = "Bài Tập Vật Lý Nâng Cao", Description = "Sách bài tập nâng cao", Price = 45000, CategoryId = categories["Sách Khoa học"], Quantity = 15, IsForSale = true },
                    new Product { Name = "Bài Tập Hóa Học Nâng Cao", Description = "Sách bài tập nâng cao", Price = 44000, CategoryId = categories["Sách Khoa học"], Quantity = 16, IsForSale = true },
                    new Product { Name = "Bài Tập Sinh Học Nâng Cao", Description = "Sách bài tập nâng cao", Price = 43000, CategoryId = categories["Sách Khoa học"], Quantity = 17, IsForSale = true },
                    new Product { Name = "Đề Thi THPT Quốc Gia Vật Lý", Description = "Tuyển tập đề thi", Price = 52000, CategoryId = categories["Sách Khoa học"], Quantity = 12, IsForSale = true },
                    new Product { Name = "Đề Thi THPT Quốc Gia Hóa Học", Description = "Tuyển tập đề thi", Price = 51000, CategoryId = categories["Sách Khoa học"], Quantity = 13, IsForSale = true }
                });

                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }
    }
}
