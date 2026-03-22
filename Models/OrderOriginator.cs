using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    // 🎭 MẪU THIẾT KẾ MEMENTO - Originator class
    // Quản lý trạng thái hiện tại của Order và tạo/cập nhật memento
    // 🔗 PART OF: Memento pattern cho Order state management
    public class OrderOriginator
    {
        private readonly StoreDbContext _context;
        private Order? _currentOrder;

        public OrderOriginator(StoreDbContext context)
        {
            _context = context;
        }

        // Thiết lập Order hiện tại để quản lý
        public void SetOrder(Order order)
        {
            _currentOrder = order;
        }

        // Tạo memento lưu trạng thái hiện tại
        public OrderMemento CreateMemento(string? notes = null)
        {
            if (_currentOrder == null)
                throw new InvalidOperationException("Không có Order nào được thiết lập để tạo memento");

            return new OrderMemento(_currentOrder.OrderID, _currentOrder.Status, notes);
        }

        // Khôi phục trạng thái từ memento
        public async Task RestoreFromMemento(OrderMemento memento)
        {
            if (_currentOrder == null)
                throw new InvalidOperationException("Không có Order nào được thiết lập để khôi phục");

            // Cập nhật trạng thái trong memory
            _currentOrder.Status = memento.Status;

            // Cập nhật trong database
            var orderInDb = await _context.Orders.FindAsync(memento.OrderId);
            if (orderInDb != null)
            {
                orderInDb.Status = memento.Status;
                await _context.SaveChangesAsync();
            }
        }

        // Lấy thông tin Order hiện tại
        public Order? GetCurrentOrder() => _currentOrder;
    }
}