using System.Collections.Generic;
using System.Linq;

namespace SportsStore.Models
{
    // 🗂️ MẪU THIẾT KẾ MEMENTO - Caretaker class
    // Quản lý lịch sử các memento và cung cấp undo/redo functionality
    // 🔗 PART OF: Memento pattern cho Order state management
    public class OrderCaretaker
    {
        private readonly List<OrderMemento> _mementos = new();
        private int _currentIndex = -1;

        // Lưu memento vào history
        public void SaveMemento(OrderMemento memento)
        {
            // Xóa các memento phía sau current index (khi undo rồi làm action mới)
            if (_currentIndex < _mementos.Count - 1)
            {
                _mementos.RemoveRange(_currentIndex + 1, _mementos.Count - _currentIndex - 1);
            }

            _mementos.Add(memento);
            _currentIndex = _mementos.Count - 1;
        }

        // Lấy memento hiện tại
        public OrderMemento? GetCurrentMemento()
        {
            return _currentIndex >= 0 && _currentIndex < _mementos.Count
                ? _mementos[_currentIndex]
                : null;
        }

        // Undo - quay về trạng thái trước đó
        public OrderMemento? Undo()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                return _mementos[_currentIndex];
            }
            return null;
        }

        // Redo - làm lại thao tác đã undo
        public OrderMemento? Redo()
        {
            if (_currentIndex < _mementos.Count - 1)
            {
                _currentIndex++;
                return _mementos[_currentIndex];
            }
            return null;
        }

        // Lấy lịch sử các memento
        public IEnumerable<OrderMemento> GetHistory()
        {
            return _mementos.AsReadOnly();
        }

        // Lấy số lượng memento trong history
        public int GetHistoryCount() => _mementos.Count;

        // Kiểm tra có thể undo không
        public bool CanUndo() => _currentIndex > 0;

        // Kiểm tra có thể redo không
        public bool CanRedo() => _currentIndex < _mementos.Count - 1;
    }
}