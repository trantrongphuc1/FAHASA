# 🗺️ QUICK REFERENCE: Design Patterns Mapping

## 📍 Tất cả Design Patterns và Vị trí (Dễ tra cứu)

### 🔴 PATTERN CÓ THỂ TÌM THẤY Ở ĐAU?

---

## 1️⃣ BUILDER PATTERN
**Files:** `Program.cs`
```
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
```
**Dòng:** 13-30

---

## 2️⃣ ADAPTER PATTERN  
**Files:** 
- `Services/EmailSender.cs` (IEmailSender interface)
- **Interface:** Chuẩn hóa email gửi
- **Implementation:** Wraps .NET SmtpClient

**Dòng:** IEmailSender interface, EmailSender class

**Sử dụng tại:** `Controllers/AccountController.cs`

---

## 3️⃣ STRATEGY PATTERN - Notification

**Files:**
1. `Services/INotificationService.cs` - Interface
   - `CreateOrderNotificationAsync()`
   - `GetUserNotificationsAsync()`
   - `MarkAsReadAsync()`

2. `Services/NotificationService.cs` - Implementation
   - Broadcast qua Database + SignalR
   - Sử dụng `IHubContext<NotificationHub>`

**Sử dụng tại:** `Controllers/OrderController.cs`

---

## 4️⃣ STRATEGY PATTERN - Voucher

**Files:**
1. `Services/IVoucherService.cs` - Interface
   - `ApplyVoucherAsync()`

2. `Services/VoucherService.cs` - Implementation
   - Validate daily limit, min order total
   - Tính discount amount

**Sử dụng tại:** `Controllers/OrderController.cs`

---

## 5️⃣ REPOSITORY PATTERN - Store

**Files:**
1. `Models/IStoreRepository.cs` - Interface
   - `Products` property
   - `Categories` property
   - `CreateProduct()`, `SaveProduct()`, `DeleteProduct()`

2. `Models/EFStoreRepository.cs` - Implementation (Entity Framework)
   - Queries Products table
   - Eager loading: .Include() for Category, ProductImages

**Sử dụng tại:** 
- `Controllers/HomeController.cs`
- `Models/PersistentCart.cs`

---

## 6️⃣ REPOSITORY PATTERN - Order

**Files:**
1. `Models/IOrderRepository.cs` - Interface
   - `Orders` property
   - `SaveOrder()`, `DeleteOrder()`

2. `Models/EFOrderRepository.cs` - Implementation
   - BUSINESS LOGIC: Trừ quantity nếu IsRental = false
   - Eager loading: .Include(Lines).ThenInclude(Product)

**Sử dụng tại:** `Controllers/OrderController.cs`

---

## 7️⃣ REPOSITORY PATTERN - Rental

**Files:**
1. `Models/IRentalRepository.cs` - Interface
2. `Models/EFRentalRepository.cs` - Implementation

**Sử dụng tại:** `Controllers/RentalController.cs`

---

## 8️⃣ MEMENTO PATTERN - Order State Management

**Files:**
- `Models/OrderStateManager.cs` - **Facade** cho memento pattern
  - `ChangeOrderStatus(orderId, newStatus)` - SAVE state before change
  - `UndoOrderStatus()` - RESTORE previous state
  - `RedoOrderStatus()` - REDO state

- `Models/OrderOriginator.cs` - **Originator** (lưu state)
  - `CreateMemento()` - Tạo snapshot
  - `RestoreFromMemento()` - Load từ snapshot

- `Models/OrderMemento.cs` - **Memento** (snapshot)
  - OrderId, Status, CreatedAt, Description

- `Models/OrderCaretaker.cs` - **Caretaker** (quản lý stack)
  - undoStack, redoStack
  - `SaveMemento()`, `Undo()`, `Redo()`

**Flow:**
```
ChangeOrderStatus(Order1, DangXuLy)
  → CreateMemento("ChoXacNhan") [state snapshot]
  → SaveMemento(memento) [push to undoStack]
  → order.Status = DangXuLy
  → SaveChangesAsync()

UndoOrderStatus()
  → Undo() [pop from undoStack]
  → RestoreFromMemento() [restore quay về ChoXacNhan]
```

**Sử dụng tại:** `Controllers/OrderController.cs`

---

## 9️⃣ DECORATOR PATTERN

**Files:**
1. `Models/Cart.cs` - **Base class**
   - AddItem(), RemoveLine(), Clear()
   - Virtual methods (có thể override)

2. `Models/PersistentCart.cs` - **Decorator** (mở rộng Cart)
   - Kế thừa từ Cart
   - Thêm persistence: LoadFromDatabase(), LoadFromSession()
   - Override AddItem() để auto-save

**UML:**
```
Cart (base)
  ↑
  |
PersistentCart (decorator - mở rộng)
```

---

## 🔟 FACADE PATTERN

**Files:** `Models/PersistentCart.cs`

**Method:** `GetCart(IServiceProvider services)` - STATIC FACTORY

**Ẩn đi:**
- HttpContextAccessor
- ISession handling
- StoreDbContext queries
- IStoreRepository calls
- User authentication checking
- Database vs Session logic

**Client chỉ gọi:**
```csharp
var cart = PersistentCart.GetCart(services);  // ← FACADE!
cart.AddItem(product, 2);
```

---

## 1️⃣1️⃣ FACTORY METHOD PATTERN

**Files:** `Models/PersistentCart.cs`

**Method:** `public static Cart GetCart(IServiceProvider services)`

**Logic:**
```
Nếu user đăng nhập:
  → LoadFromDatabase(userId)
Nếu user ẩn danh:
  → LoadFromSession()
Trả về cart đã populate
```

**Lợi ích:**
- Client không cần biết logic tạo cart
- Có thể thay đổi logic mà không cần thay DI container

---

## 1️⃣2️⃣ OBSERVER PATTERN - Chat

**Files:**
1. `Hubs/ChatHub.cs` - **Subject** (Server)
   - `OnConnectedAsync()` - Subscribe vào group
   - `SendMessage()` - Publish event
   - Groups: "Admins", userId
   - Methods: `Clients.Group()`, `Clients.User()`, `Clients.Caller`

2. `wwwroot/js/admin-chat.js` - **Observer** (Client JS)
   ```javascript
   connection.on('ReceiveMessage', function() {
       // Update UI with new message
       updateChatUI();
       updateBadgeCount();
   });
   ```

3. `Pages/Admin/Chat.razor` - **Observer** (C# Component)
   ```csharp
   [JSInvokable]
   public void ReceiveFromHub(...)
   {
       // Update Blazor component
       StateHasChanged();
   }
   ```

**Flow:**
```
Admin connects
  → OnConnectedAsync()
  → Groups.AddToGroupAsync("Admins")

User sends message
  → SendMessage(..., isAdmin=false)
  → Clients.Group("Admins").SendAsync("ReceiveMessage", ...)
  → Admin JS: connection.on('ReceiveMessage')
  → Admin C#: ReceiveFromHub() JSInvokable method
```

---

## 1️⃣3️⃣ OBSERVER PATTERN - Notification

**Files:**
1. `Hubs/NotificationHub.cs` - **Subject**
   - Groups by userId

2. `Services/NotificationService.cs`
   - `_hubContext.Clients.User(userId).SendAsync("ReceiveNotification")`

3. Client lắng nghe events

---

## 1️⃣4️⃣ HUB PATTERN (Real-time WebSocket)

**Files:**
- `Hubs/ChatHub.cs`
- `Hubs/NotificationHub.cs`
- `Program.cs` - `AddSignalR()`

**Endpoints:**
- `/chatHub` - Client kết nối
- `/notificationHub` - Notification hub

---

## 1️⃣5️⃣ MVC PATTERN

**Model Layer:**
- `Models/Order.cs`, `Models/Product.cs`, etc.
- Entities + Business logic

**View Layer:**
- `Views/` - ASP.NET MVC views
- `Pages/` - Blazor components
- `.cshtml` files

**Controller Layer:**
- `Controllers/OrderController.cs`
- `Controllers/HomeController.cs`
- `Controllers/ChatController.cs`
- Actions handle HTTP requests

**Flow:**
```
HTTP Request
  → Route match
  → Controller.Action()
  → Query Models via Repository
  → Render View
  → HTML Response
```

---

## 1️⃣6️⃣ DEPENDENCY INJECTION + IOC CONTAINER

**Files:** `Program.cs`

**Lifetimes:**
```csharp
// TRANSIENT: New instance mỗi lần
builder.Services.AddTransient<...>();

// SCOPED: New instance mỗi HTTP request
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddScoped<Cart>(sp => PersistentCart.GetCart(sp));
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<OrderStateManager>();

// SINGLETON: 1 instance toàn bộ app
builder.Services.AddSingleton<IHttpContextAccessor>();
```

**Sử dụng:**
```csharp
public class OrderController : Controller
{
    private readonly IOrderRepository _repo;
    private readonly Cart _cart;
    private readonly INotificationService _notif;
    
    // DI Container tự inject
    public OrderController(IOrderRepository repo, Cart cart, INotificationService notif)
    {
        _repo = repo;
        _cart = cart;
        _notif = notif;
    }
}
```

---

## 1️⃣7️⃣ ASYNC/AWAIT PATTERN

**Tất cả Files:**
- Services: `async Task`, `async Task<T>`
- Controllers: `async Task<IActionResult>`
- Hubs: `public async Task SendMessage()`
- Database: `await _context.SaveChangesAsync()`

**Example:**
```csharp
public async Task<bool> ChangeOrderStatus(int orderId, OrderStatus newStatus)
{
    var order = await _context.Orders.FindAsync(orderId);
    order.Status = newStatus;
    await _context.SaveChangesAsync();
    await _notificationService.CreateOrderNotificationAsync(...);
    return true;
}
```

---

## 📋 PATTERN OCCURRENCE COUNT

| Pattern | Count | Files |
|---------|-------|-------|
| BUILDER | 1 | Program.cs |
| ADAPTER | 2 | EmailSender.cs, HttpContextAccessor |
| STRATEGY | 2 | NotificationService, VoucherService |
| REPOSITORY | 3 | StoreRepo, OrderRepo, RentalRepo |
| MEMENTO | 4 | StateManager, Originator, Memento, Caretaker |
| DECORATOR | 1 | PersistentCart |
| FACADE | 1 | PersistentCart.GetCart() |
| FACTORY | 1 | PersistentCart.GetCart() |
| OBSERVER | 2 | ChatHub, NotificationHub |
| HUB | 2 | ChatHub, NotificationHub |
| MVC | All | Controllers, Views, Models |
| DI/IoC | 1 | Program.cs |
| ASYNC | All | Everywhere |

---

## 🎯 Tra cứu nhanh

### \"Tôi muốn tìm code liên quan đến Order processing\"
1. Repository: `Models/IOrderRepository.cs` + `EFOrderRepository.cs`
2. Business logic: `Controllers/OrderController.cs`
3. State management: `Models/OrderStateManager.cs` (Memento)
4. Notification: `Services/NotificationService.cs` (Strategy)
5. Voucher: `Services/VoucherService.cs` (Strategy)

### \"Tôi muốn tìm code liên quan đến Chat\"
1. Server-side: `Hubs/ChatHub.cs` (Observer, Hub)
2. Real-time: SignalR groups
3. Client JS: `wwwroot/js/admin-chat.js`, `wwwroot/js/app-core.js`
4. Blazor Component: `Pages/Admin/Chat.razor`
5. API: `Controllers/ChatController.cs`

### \"Tôi muốn tìm Dependency Injection\"
1. Main: `Program.cs` (lines 1-80)
2. Usage: Mỗi Controller action

### \"Tôi muốn tìm Database queries\"
1. DbContext: `Models/StoreDbContext.cs`
2. Repositories: `Models/EF*Repository.cs`
3. Migrations: `Migrations/` folder

---

## 🔗 File Dependencies Map

```
Program.cs (DI Configuration)
  ├─ Services/EmailSender.cs (Adapter)
  ├─ Services/NotificationService.cs (Strategy, Observer)
  │  └─ Hubs/NotificationHub.cs
  ├─ Services/VoucherService.cs (Strategy)
  ├─ Models/EFStoreRepository.cs (Repository)
  ├─ Models/EFOrderRepository.cs (Repository)
  ├─ Models/PersistentCart.cs (Decorator, Facade, Factory)
  │  └─ Models/Cart.cs (Base)
  └─ Models/OrderStateManager.cs (Memento)
     ├─ Models/OrderOriginator.cs
     ├─ Models/OrderMemento.cs
     └─ Models/OrderCaretaker.cs

Controllers/
  ├─ OrderController.cs
  │  ├─ IOrderRepository (Repository)
  │  ├─ Cart (Factory, Decorator, Facade)
  │  ├─ INotificationService (Strategy)
  │  ├─ OrderStateManager (Memento)
  │  └─ IVoucherService (Strategy)
  ├─ ChatController.cs
  │ └─ StoreDbContext
  └─ HomeController.cs
     └─ IStoreRepository (Repository)

Hubs/
  ├─ ChatHub.cs (Observer, Hub)
  │  └─ SIGNALR
  └─ NotificationHub.cs (Observer, Hub)
     └─ SIGNALR

Views/
  └─ Shared/_ChatBox.cshtml
     └─ wwwroot/js/app-core.js (Observer JS)

Pages/
  └─ Admin/Chat.razor (Blazor)
     └─ wwwroot/js/admin-chat.js (Observer JS)
```

---

**Last Updated:** 2026-03-22

