using System;
using System.Collections.Generic;
using System.Linq;

namespace SportsStore.Models
{
    // =================================================================
    // 📄 BASE CLASS VÀ COMPONENT PATTERN
    // =================================================================
    // Mục đích: Cung cấp chức năng cơ bản của giỏ hàng
    // 
    // Cách hoạt động:
    //   1. Lines: List<CartLine> lưu các dòng sản phẩm
    //   2. AddItem(): Thêm sản phẩm vào giỏ (hoặc cập nhật số lượng nếu đã có)
    //   3. RemoveLine(): Xóa sản phẩm khỏi giỏ
    //   4. ComputeTotalValue(): Tính tổng giá trị
    //   5. Clear(): Xóa toàn bộ giỏ
    //
    // 🔄 DESIGN PATTERN:
    //   • KẾ THỪA: PersistentCart kế thừa lừ Cart để mở rộng
    //   • VIRTUAL METHOD: Mỗi method là virtual, cho phép override
    //
    // 📁 LIÊN KẾT VỚI FILE KHÁC:
    //   • Models/PersistentCart.cs: Mở rộng Cart với tính năng persistence
    //   • Program.cs: Đăng ký Cart qua Factory Method GetCart()
    // ==================================================================
    public class Cart
    {
        // Danh sách các dòng sản phẩm trong giỏ hàng
        public List<CartLine> Lines { get; set; } = new();

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng.
        /// </summary>
        public virtual void AddItem(Product product, int quantity, bool isRental = false, int rentalDays = 0)
        {
            if (product == null || quantity <= 0)
                return;

            // Tìm dòng sản phẩm phù hợp (theo ID và loại thuê/mua)
            var line = Lines.FirstOrDefault(p =>
                p.Product.ProductID == product.ProductID &&
                p.IsRental == isRental);

            if (line == null)
            {
                // Thêm dòng mới
                Lines.Add(new CartLine
                {
                    Product = product,
                    Quantity = quantity,
                    IsRental = isRental,
                    RentalDays = isRental ? Math.Max(rentalDays, 1) : 0
                });
            }
            else
            {
                // Cập nhật số lượng và thời gian thuê nếu cần
                line.Quantity += quantity;
                if (isRental && rentalDays > 0)
                {
                    line.RentalDays = Math.Max(rentalDays, 1);
                }
            }
        }

        /// <summary>
        /// Xoá sản phẩm khỏi giỏ hàng theo ID và loại thuê/mua.
        /// </summary>
        public virtual void RemoveLine(Product product, bool isRental = false)
        {
            if (product == null) return;

            Lines.RemoveAll(l =>
                l.Product.ProductID == product.ProductID &&
                l.IsRental == isRental);
        }

        /// <summary>
        /// Tính tổng giá trị của giỏ hàng (bao gồm thuê và mua).
        /// </summary>
        public decimal ComputeTotalValue()
        {
            return Lines.Sum(l => l.LineTotal);
        }

        /// <summary>
        /// Xoá toàn bộ giỏ hàng.
        /// </summary>
        public virtual void Clear()
        {
            Lines.Clear();
        }
    }

    public class CartLine
    {
        public int CartLineID { get; set; }
        public int? OrderID { get; set; }

        public Product Product { get; set; } = new();

        public int Quantity { get; set; }

        public bool IsRental { get; set; } = false;

        public int RentalDays { get; set; } = 0;

        /// <summary>
        /// Tính giá tiền cho dòng sản phẩm hiện tại.
        /// Nếu là thuê thì tính theo giá thuê * số ngày * số lượng.
        /// Nếu là mua thì tính theo giá mua (hoặc giá sale nếu đang sale) * số lượng.
        /// </summary>
        public decimal LineTotal =>
            IsRental
                ? (Product.RentPrice ?? 0) * Math.Max(RentalDays, 1) * Quantity
                : Product.CurrentPrice * Quantity;
    }
}
