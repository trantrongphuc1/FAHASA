using System;
using System.Collections.Generic;
using SportsStore.Models;

namespace SportsStore.Models
{
    // 💾 MẪU THIẾT KẾ MEMENTO - Lưu trữ trạng thái của Order
    // Memento class chứa snapshot của trạng thái Order tại một thời điểm
    // 🔗 PART OF: Memento pattern cho Order state management
    public class OrderMemento
    {
        // Thuộc tính private để đảm bảo encapsulation
        private readonly int _orderId;
        private readonly OrderStatus _status;
        private readonly DateTime _timestamp;
        private readonly string? _notes;

        // Constructor chỉ được gọi bởi OrderOriginator
        internal OrderMemento(int orderId, OrderStatus status, string? notes = null)
        {
            _orderId = orderId;
            _status = status;
            _timestamp = DateTime.Now;
            _notes = notes;
        }

        // Getters public để đọc trạng thái (nhưng không thể thay đổi)
        public int OrderId => _orderId;
        public OrderStatus Status => _status;
        public DateTime Timestamp => _timestamp;
        public string? Notes => _notes;
    }
}