using System;
using System.Collections.Generic;
using System.Linq;

namespace SportsStore.Models
{
    // 🛒 CLASS CƠ SỞ - Base cart functionality
    // Chứa logic cơ bản của giỏ hàng (thêm/xóa sản phẩm)
    // 🔗 ĐƯỢC KẾ THỪA: PersistentCart kế thừa từ Cart để thêm persistence
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
