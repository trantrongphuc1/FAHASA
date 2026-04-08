# 🗺️ QUICK REFERENCE: Design Patterns Mapping

## 📍 Tất cả Design Patterns và Vị trí (Dễ tra cứu)

## 🧭 BẢNG MAP CHÍNH: FILE CHÍNH - INTERFACE - IMPLEMENT - NƠI SỬ DỤNG

### A. Nhóm có Interface (dễ test, dễ thay thế)

| STT | Pattern | File interface (hợp đồng) | File triển khai chính | Đăng ký DI trong Program.cs | Nơi sử dụng thực tế |
|---|---|---|---|---|---|
| 1 | Adapter Email | Services/EmailSender.cs (IEmailSender) | Services/EmailSender.cs (EmailSender) | AddScoped<IEmailSender, EmailSender>() | Controllers/AccountController.cs, Controllers/EmailTestController.cs |
| 2 | Strategy Notification | Services/INotificationService.cs | Services/NotificationService.cs | AddScoped<INotificationService, NotificationService>() | Controllers/NotificationController.cs |
| 3 | Strategy Voucher | Services/IVoucherService.cs | Services/VoucherService.cs | AddScoped<IVoucherService, VoucherService>() | Controllers/OrderController.cs |
| 4 | Repository Store | Models/IStoreRepository.cs | Models/EFStoreRepository.cs | AddScoped<IStoreRepository, EFStoreRepository>() | Controllers/RentalController.cs, Models/PersistentCart.cs |
| 5 | Repository Order | Models/IOrderRepository.cs | Models/EFOrderRepository.cs | AddScoped<IOrderRepository, EFOrderRepository>() | Controllers/OrderController.cs |
| 6 | Repository Rental | Models/IRentalRepository.cs | Models/EFRentalRepository.cs | AddScoped<IRentalRepository, EFRentalRepository>() | Controllers/OrderController.cs, Controllers/RentalController.cs |

### B. Nhóm không tách Interface riêng (theo class trung tâm)

| STT | Pattern | File trung tâm (chính) | Thành phần đi kèm | Cách nối trong Program.cs | Nơi dùng |
|---|---|---|---|---|---|
| 1 | Builder + DI | Program.cs | WebApplicationBuilder, IServiceCollection | WebApplication.CreateBuilder(args) + AddScoped/AddSingleton | Toàn bộ ứng dụng |
| 2 | Factory Method + Facade + Decorator (Cart) | Models/PersistentCart.cs | Models/Cart.cs (base class) | AddScoped<Cart>(sp => PersistentCart.GetCart(sp)) | Controllers/OrderController.cs, Controllers/AccountController.cs |
| 3 | Memento (trạng thái đơn hàng) | Models/OrderStateManager.cs | OrderOriginator.cs, OrderMemento.cs, OrderCaretaker.cs | AddScoped<OrderStateManager>() | Controllers/OrderController.cs |
| 4 | Observer Notification realtime | Hubs/NotificationHub.cs + Services/NotificationService.cs | SignalR Hub + IHubContext | AddSignalR() | NotificationService push ra client |
| 5 | Observer Chat realtime | Hubs/ChatHub.cs | Group/Client của SignalR | AddSignalR() | Client JS/Blazor chat |

### Ghi chú nhanh (tiếng Việt, dễ nhớ)

- Quy tắc đọc nhanh: ưu tiên tìm interface trước, sau đó xem file implement và cuối cùng xem chỗ inject ở controller.
- File gốc để lần theo toàn bộ quan hệ pattern là Program.cs vì đây là nơi map interface -> implementation.
- Với Cart, file chính là PersistentCart.cs vì nó gom 3 vai trò cùng lúc: tạo đối tượng, che giấu logic, mở rộng hành vi.
- Với Memento, OrderStateManager.cs là bộ điều phối; 3 file còn lại là thành phần lưu snapshot và quản lý lịch sử.

## ✅ DANH SÁCH CHÍNH THỨC (KHÔNG LẶP): 13 PATTERN

### 1) 13 pattern chính thức

| STT | Pattern chính thức | Ghi chú chuẩn hóa |
|---|---|---|
| 1 | Builder | Cấu hình app trong Program.cs |
| 2 | Adapter | EmailSender adapter cho SMTP |
| 3 | Strategy | Bao gồm 2 biến thể: Notification, Voucher |
| 4 | Repository | Bao gồm 3 biến thể: Store, Order, Rental |
| 5 | Memento | Quản lý lịch sử trạng thái đơn hàng |
| 6 | Decorator | PersistentCart mở rộng Cart |
| 7 | Facade | PersistentCart.GetCart che giấu logic tạo cart |
| 8 | Factory Method | GetCart(IServiceProvider) tạo cart theo context |
| 9 | Observer | Bao gồm Chat và Notification realtime |
| 10 | Hub | SignalR Hub cho realtime |
| 11 | MVC | Controller -> Model -> View |
| 12 | Dependency Injection / IoC | Interface -> Implementation trong Program.cs |
| 13 | Async/Await | Bất đồng bộ ở controller/service/hub |

### 2) Các mục lặp và cách gộp về pattern chính thức

| Mục đang tách nhỏ trong tài liệu | Gộp vào pattern chính thức |
|---|---|
| Strategy Notification + Strategy Voucher | Strategy |
| Repository Store + Repository Order + Repository Rental | Repository |
| Observer Chat + Observer Notification | Observer |
| Factory + Factory Method (nếu ghi chung ngữ cảnh GetCart) | Factory Method |

## 🧱 GIẢI THÍCH CẶN KẼ: FILE .CS, VIEW, PAGE CHỨA GÌ VÀ XÀI GÌ

### A. Nhóm .cs ở Controllers (điểm nhận request)

| Nhóm file | Chứa gì | Xài gì |
|---|---|---|
| Controllers/HomeController.cs | Action hiển thị trang chủ, lọc/sắp xếp sản phẩm | StoreDbContext, SaleService, trả View |
| Controllers/OrderController.cs | Checkout, quản lý đơn mua/thuê, đổi trạng thái | IOrderRepository, IRentalRepository, Cart, OrderStateManager, IVoucherService |
| Controllers/NotificationController.cs | API đọc/đánh dấu thông báo | INotificationService |
| Controllers/AccountController.cs | Đăng nhập/đăng xuất/đăng ký | UserManager, SignInManager, IEmailSender, Cart |
| Controllers/RentalController.cs | Luồng thuê và trả sách | IRentalRepository, IStoreRepository, UserManager |

### B. Nhóm .cs ở Models và Services (nghiệp vụ + dữ liệu)

| Nhóm file | Chứa gì | Xài gì |
|---|---|---|
| Models/I*Repository.cs | Hợp đồng truy cập dữ liệu | Được inject vào Controller/Service |
| Models/EF*Repository.cs | Triển khai data access bằng EF Core | StoreDbContext |
| Models/PersistentCart.cs | Cart persistent theo Session/Database | IHttpContextAccessor, StoreDbContext, IStoreRepository |
| Models/OrderStateManager.cs | Điều phối Memento (undo/redo) | OrderOriginator, OrderMemento, OrderCaretaker |
| Services/INotificationService.cs + NotificationService.cs | Hợp đồng + triển khai thông báo | StoreDbContext, IHubContext<NotificationHub> |
| Services/IVoucherService.cs + VoucherService.cs | Hợp đồng + triển khai áp mã giảm giá | StoreDbContext |
| Services/EmailSender.cs | Adapter gửi email | IConfiguration, SmtpClient |

### C. Nhóm View/Page/JS (giao diện hiển thị + realtime)

| Nhóm file | Chứa gì | Xài gì |
|---|---|---|
| Views/*.cshtml | Razor View cho MVC | Model/ViewModel từ Controller |
| Views/Shared/*.cshtml | Layout, partial dùng lại | Render chung nhiều trang |
| Pages/**/*.razor | Razor Component (Blazor) | C# component lifecycle + JS interop |
| wwwroot/js/admin-chat.js | Client chat realtime | SignalR connection.on/Send |
| wwwroot/js/app-core.js | Script nền dùng toàn site | Event UI, realtime hooks |

### D. Luồng chạy tổng quát giữa Controller - Service - View

1. Người dùng gửi HTTP request vào route.
2. Controller nhận request và gọi Service/Repository qua DI.
3. Service/Repository làm nghiệp vụ + truy vấn DbContext.
4. Controller trả về View (.cshtml) hoặc JSON API.
5. Với realtime, Hub phát sự kiện qua SignalR và JS/Blazor client nhận ngay.

### 🔴 PATTERN CÓ THỂ TÌM THẤY Ở ĐÂU?

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

**Sử dụng tại:** `Controllers/NotificationController.cs`

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
- `Controllers/RentalController.cs`
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

## 📋 PATTERN OCCURRENCE COUNT (BẢN CHUẨN HÓA)

| Pattern chính thức | Số biến thể/điểm xuất hiện |
|---|---|
| Builder | 1 |
| Adapter | 1 |
| Strategy | 2 (Notification, Voucher) |
| Repository | 3 (Store, Order, Rental) |
| Memento | 1 cụm (gồm 4 class thành phần) |
| Decorator | 1 |
| Facade | 1 |
| Factory Method | 1 |
| Observer | 2 (Chat, Notification) |
| Hub | 2 (ChatHub, NotificationHub) |
| MVC | Xuyên suốt ứng dụng |
| Dependency Injection / IoC | 1 cấu hình trung tâm |
| Async/Await | Xuyên suốt ứng dụng |

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

