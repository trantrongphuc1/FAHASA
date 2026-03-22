# 🏗️ HƯ ỚNGNG DESIGN PATTERNS - FAHASA E-COMMERCE

## 📋 Tóm tắt các Design Pattern được sử dụng

Dự án FAHASA Sports Store sử dụng **12 Design Patterns** chính:

---

## 1. 🏭 BUILDER PATTERN
**Vị trí:** `Program.cs`

### Cách hoạt động:
```csharp
// Tạo WebApplicationBuilder sử dụng Fluent API
var builder = WebApplication.CreateBuilder(args);

// Thêm services từng cái một (Fluent API)
builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
```

### Lợi ích:
- ✅ Cấu hình phức tạp trở nên dễ đọc
- ✅ Fluent interface cho chaining methods
- ✅ Có thể tùy chỉnh từng bước

### Kết nối:
- `Program.cs` - Cấu hình ứng dụng

---

## 2. 🔌 ADAPTER PATTERN
**Vị trí:** `Services/EmailSender.cs`

### Cách hoạt động:
```csharp
// Interface chuẩn hóa
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}

// Adapter wraps SmtpClient
public class EmailSender : IEmailSender
{
    public async Task SendEmailAsync(...)
    {
        var smtpClient = new SmtpClient(...);
        await smtpClient.SendMailAsync(...);
    }
}
```

### Lợi ích:
- ✅ Chuẩn hóa interface email
- ✅ Dễ thay thế: Email → SMS → Firebase Cloud Messaging
- ✅ Dễ mock trong unit tests

### Kết nối:
- `Services/EmailSender.cs` - Email adapter
- `Program.cs` - Đăng ký DI: `AddScoped<IEmailSender, EmailSender>()`
- `Controllers/AccountController.cs` - Dùng để gửi email xác minh

---

## 3. 🎯 STRATEGY PATTERN
**Vị trí:** Multiple files (Notification, Voucher)

### Strategy 1: Notification Strategy
```csharp
// Interface chiến lược thông báo
public interface INotificationService
{
    Task CreateOrderNotificationAsync(...);
    Task MarkAsReadAsync(...);
}

// Implementation: SignalR + Database
public class NotificationService : INotificationService
{
    // Kênh 1: Database (persistent)
    // Kênh 2: SignalR (real-time)
}

// Có thể mở rộng:
// - EmailNotificationService
// - SMSNotificationService  
// - PushNotificationService
```

### Strategy 2: Voucher Strategy
```csharp
public interface IVoucherService
{
    Task<ApplyVoucherResult> ApplyVoucherAsync(...);
}

public class VoucherService : IVoucherService
{
    // Tính toán discount, validate daily limit, user limit
}

// Có thể mở rộng:
// - PercentageVoucherService
// - FixedAmountVoucherService
// - FreeShippingVoucherService
```

### Lợi ích:
- ✅ Dễ thay đổi strategy runtime
- ✅ Unit test dễ dàng (mock)
- ✅ Mở rộng mà không sửa existing code

### Kết nối:
- `Services/INotificationService.cs` + `Services/NotificationService.cs` - Notification strategy
- `Services/IVoucherService.cs` + `Services/VoucherService.cs` - Voucher strategy
- `Program.cs` - Đăng ký:
  ```csharp
  AddScoped<INotificationService, NotificationService>();
  AddScoped<IVoucherService, VoucherService>();
  ```
- `Controllers/OrderController.cs` - Dùng strategies

---

## 4. 📚 REPOSITORY PATTERN
**Vị trí:** `Models/EFStoreRepository.cs`, `Models/EFOrderRepository.cs`

### Cách hoạt động:
```csharp
// Interface định nghĩa contract
public interface IStoreRepository
{
    IQueryable<Product> Products { get; }
    void CreateProduct(Product p);
    void SaveProduct(Product p);
}

// Implementation: Entity Framework
public class EFStoreRepository : IStoreRepository
{
    private StoreDbContext context;
    
    public IQueryable<Product> Products => 
        context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages);
}
```

### Lợi ích:
- ✅ Tách tách business logic khỏi data access
- ✅ Dễ thay thế database: EF → Dapper → SqlDataReader
- ✅ Dễ unit test (mock repositories)

### Kết nối:
- `Models/IStoreRepository.cs` - Interface
- `Models/EFStoreRepository.cs` - Implementation cho Product
- `Models/EFOrderRepository.cs` - Implementation cho Order
- `Program.cs` - Đăng ký DI:
  ```csharp
  AddScoped<IStoreRepository, EFStoreRepository>();
  AddScoped<IOrderRepository, EFOrderRepository>();
  ```
- `Controllers/HomeController.cs`, `Controllers/OrderController.cs` - Dùng repositories

---

## 5. 🎭 MEMENTO PATTERN
**Vị trí:** `Models/OrderStateManager.cs` + `Models/OrderOriginator.cs` + `Models/OrderCaretaker.cs` + `Models/OrderMemento.cs`

### Các thành phần:
```csharp
// 1. ORIGINATOR: Object cần lưu state (Order)
public class OrderOriginator
{
    private Order _order;
    
    public OrderMemento CreateMemento(string description)
    {
        // Lưu state hiện tại vào memento
        return new OrderMemento(_order.Status, ...);
    }
    
    public void RestoreFromMemento(OrderMemento memento)
    {
        // Khôi phục state từ memento
        _order.Status = memento.Status;
    }
}

// 2. MEMENTO: Snapshot của state
public class OrderMemento
{
    public int OrderId { get; }
    public OrderStatus Status { get; }
    public DateTime CreatedAt { get; }
    // Snapshot của state tại 1 thời điểm
}

// 3. CARETAKER: Quản lý memento stack
public class OrderCaretaker
{
    private Stack<OrderMemento> _undoStack = new();
    private Stack<OrderMemento> _redoStack = new();
    
    public void SaveMemento(OrderMemento memento) => _undoStack.Push(memento);
    public OrderMemento Undo() => _undoStack.Pop();
    public OrderMemento Redo() => _redoStack.Pop();
}

// 4. STATE MANAGER: Orchestrate ba thành phần
public class OrderStateManager
{
    private OrderOriginator _originator;
    private OrderCaretaker _caretaker;
    
    public async Task<bool> ChangeOrderStatus(int orderId, OrderStatus newStatus)
    {
        var memento = _originator.CreateMemento(...);  // Lưu state cũ
        _caretaker.SaveMemento(memento);               // Vào stack
        order.Status = newStatus;                       // Thay đổi state
        await _context.SaveChangesAsync();             // Lưu DB
    }
    
    public async Task<bool> UndoOrderStatus(int orderId)
    {
        var memento = _caretaker.Undo();               // Lấy state cũ
        await _originator.RestoreFromMemento(memento); // Khôi phục
    }
}
```

### Flow Undo/Redo:
```
ChangeOrderStatus(ChoXacNhan → DangXuLy)
  ↓
CreateMemento("Trước khi thay đổi") [state = ChoXacNhan]
  ↓
SaveMemento vào caretaker.undoStack
  ↓
order.Status = DangXuLy
  ↓
SaveChangesAsync()

---

UndoOrderStatus()
  ↓
caretaker.Undo() → memento(quay về ChoXacNhan)
  ↓
RestoreFromMemento(memento)
```

### Lợi ích:
- ✅ Admin có thể undo thay đổi trạng thái
- ✅ Audit trail tự động (lịch sử thay đổi)
- ✅ Không cần SQL version control

### Kết nối:
- `Models/OrderOriginator.cs` - Lưu state
- `Models/OrderMemento.cs` - Snapshot
- `Models/OrderCaretaker.cs` - Quản lý stack
- `Models/OrderStateManager.cs` - Orchestrate
- `Program.cs` - Đăng ký: `AddScoped<OrderStateManager>();`
- `Controllers/OrderController.cs` - Gọi `ChangeOrderStatus()`, `UndoOrderStatus()`

---

## 6. 🏰 DECORATOR PATTERN
**Vị trí:** `Models/PersistentCart.cs` (mở rộng `Models/Cart.cs`)

### Cách hoạt động:
```csharp
// Base class: Giỏ hàng cơ bản
public class Cart
{
    public List<CartLine> Lines { get; set; }
    public virtual void AddItem(Product product, int quantity) { ... }
    public virtual void RemoveLine(Product product) { ... }
}

// DECORATOR: Mở rộng Cart với persistence
public class PersistentCart : Cart
{
    public ISession Session { get; set; }
    public StoreDbContext DbContext { get; set; }
    
    // Thêm chức năng: LoadFromDatabase, LoadFromSession, SaveToDatabase
    private void LoadFromDatabase(string userId) { ... }
    private void LoadFromSession() { ... }
    
    // Override base methods để thêm persistence
    public override void AddItem(Product product, int quantity)
    {
        base.AddItem(product, quantity);  // Gọi base implementation
        SaveToDatabase();                  // Thêm persistence
    }
}

// UML:
// Cart (base)
//   ↑
//   |
// PersistentCart (decorator - mở rộng)
```

### Lợi ích:
- ✅ Mở rộng chức năng mà không thay đổi Cart
- ✅ Composition thay vì inheritance
- ✅ Dễ dàng thêm/bỏ persistence

### Kết nối:
- `Models/Cart.cs` - Base class
- `Models/PersistentCart.cs` - Decorator
- `Program.cs` - Đăng ký: `AddScoped<Cart>(sp => PersistentCart.GetCart(sp));`
- `Controllers/*` - Dùng Cart (thực tế là PersistentCart)

---

## 7. 🎪 FACADE PATTERN
**Vị trí:** `Models/PersistentCart.cs`, `Models/StoreDbContext.cs`

### Cách hoạt động:
```csharp
// FACADE: PersistentCart ẩn độ phức tạp
public class PersistentCart : Cart
{
    // Client chỉ gọi GetCart() - FACADE
    public static Cart GetCart(IServiceProvider services)
    {
        var session = services.GetService<IHttpContextAccessor>()?.HttpContext?.Session;
        var db = services.GetService<StoreDbContext>();
        var repo = services.GetService<IStoreRepository>();
        var userId = context?.User?.Identity?.Name;
        
        var cart = new PersistentCart { Session = session, DbContext = db, ... };
        
        if (!string.IsNullOrEmpty(userId))
            cart.LoadFromDatabase(userId);  // ẩn đi
        else
            cart.LoadFromSession();          // ẩn đi
            
        return cart;  // Trả về ready-to-use cart
    }
}

// Client dùng:
var cart = PersistentCart.GetCart(services);  // FACADE che giấu complexity
cart.AddItem(product, 2);                     // Đơn giản!

// Nếu không có FACADE, client phải:
// var cart = new PersistentCart();
// var userId = GetCurrentUserId();
// if (!string.IsNullOrEmpty(userId))
//     cart.LoadFromDatabase(userId);
// else
//     cart.LoadFromSession();
```

### Lợi ích:
- ✅ Ẩn độ phức tạp của cart creation
- ✅ Client code đơn giản
- ✅ Tập trung logic ở một chỗ

### Kết nối:
- `Models/PersistentCart.cs` - Facade
- `Program.cs` - Đăng ký qua facade

---

## 8. 🏭 FACTORY METHOD PATTERN
**Vị trí:** `Models/PersistentCart.cs` - `GetCart()` static method

### Cách hoạt động:
```csharp
// FACTORY METHOD: Tạo Cart dựa trên context
public static Cart GetCart(IServiceProvider services)
{
    var userId = GetUserIdFromContext(services);
    
    var cart = new PersistentCart { ... };
    
    if (!string.IsNullOrEmpty(userId))
    {
        // Factory decision: Authenticated user → load from DB
        cart.LoadFromDatabase(userId);
    }
    else
    {
        // Factory decision: Anonymous → load from session
        cart.LoadFromSession();
    }
    
    return cart;  // Return fully configured instance
}

// Client không quan tâm chi tiết:
var cart = PersistentCart.GetCart(services);  // Factory handle complexity
```

### So sánh Constructor vs Factory Method:
```csharp
// ❌ Sai: Constructor quá phức tạp
public Cart(IServiceProvider services, string userId, bool isAuthenticated)
{
    if (isAuthenticated) LoadFromDatabase(userId);
    else LoadFromSession();
}

// ✅ Đúng: Factory method
var cart = Cart.GetCart(services);  // Declarative, readable
```

### Lợi ích:
- ✅ Tạo object phức tạp với logic rõ ràng
- ✅ Client không cần biết chi tiết
- ✅ Dễ thay đổi logic creation

### Kết nối:
- `Models/PersistentCart.cs` - Factory method `GetCart()`
- `Program.cs` - DI gọi factory

---

## 9. 👁️ OBSERVER PATTERN
**Vị trí:** `Hubs/ChatHub.cs`, `Hubs/NotificationHub.cs`, `wwwroot/js/admin-chat.js`

### Cách hoạt động:
```csharp
// SUBJECT: SignalR Hub - quản lý subscribers
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        
        // Subscribe vào group
        if (isAdmin)
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        else
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        
        await base.OnConnectedAsync();
    }
    
    public async Task SendMessage(string message, bool isAdmin)
    {
        // PUBLISH: Gửi event tới subscribers
        if (isAdmin)
        {
            await Clients.Group("Admins").SendAsync("ReceiveMessage", ...);
            await Clients.User(userId).SendAsync("ReceiveMessage", ...);
        }
        else
        {
            await Clients.Group("Admins").SendAsync("ReceiveMessage", ...);
            await Clients.Caller.SendAsync("ReceiveMessage", ...);
        }
    }
}

// OBSERVER: JavaScript client lắng nghe
// wwwroot/js/admin-chat.js
connection.on('ReceiveMessage', function(userId, message) {
    console.log('Received message:', message);
    // Update UI
    displayMessage(userId, message);
});
```

### Flow:
```
Admin/User connects
  ↓
OnConnectedAsync() - Subscribe vào group
  ↓
Khoảng thời gian: Listener chờ events
  ↓
SendMessage() - Publish event
  ↓
Groups/Users nhận ws message
  ↓
JS event handler: connection.on(...) được gọi
```

### Lợi ích:
- ✅ Loose coupling: Server/Client không dependency
- ✅ Real-time: Event-driven architecture
- ✅ Scalable: Groups thay vì broadcast all

### Kết nối:
- `Hubs/ChatHub.cs` - Subject (Groups)
- `Hubs/NotificationHub.cs` - Subject (Notifications)
- `wwwroot/js/admin-chat.js` - Observer (JS)
- `wwwroot/js/app-core.js` - Observer (JS)
- `Pages/Admin/Chat.razor` - Observer (C# JSInterop)

---

## 10. 🎪 HUB PATTERN (MVC không phải Design Pattern cơ bản)
**Vị trí:** Architecture tier (Controllers, Views, Models)

### MVC Layers:
```csharp
// MODEL: Business logic & data
public class Order { public int OrderID; public OrderStatus Status; }

// CONTROLLER: Handles requests
public class OrderController : Controller
{
    public IActionResult Checkout() { ... }
    [HttpPost]
    public async Task<IActionResult> Checkout(Order order) { ... }
}

// VIEW: Presentation layer
// Views/Order/Checkout.cshtml - HTML + Razor templates
// Pages/Admin/Chat.razor - Blazor component
```

### Interaction:
```
Client Request
  ↓
Controller route to action
  ↓
Action queries Model via Repository
  ↓
Model processes business logic
  ↓
Controller renders View
  ↓
View generates HTML response
```

---

## 11. 🔌 DEPENDENCY INJECTION (DI) + INVERSION OF CONTROL (IoC)
**Vị trí:** `Program.cs`

### Cách hoạt động:
```csharp
// Program.cs - DI Container configuration
builder.Services.AddScoped<Cart>(sp => PersistentCart.GetCart(sp));
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddSingleton<IHttpContextAccessor>();

// USAGE: Automatic injection
[Authorize]
public class OrderController : Controller
{
    private readonly IOrderRepository _orderRepository;
    private readonly Cart _cart;
    private readonly INotificationService _notificationService;
    
    // DI Container tự động inject dependencies
    public OrderController(IOrderRepository orderRepo, Cart cart, INotificationService notif)
    {
        _orderRepository = orderRepo;
        _cart = cart;
        _notificationService = notif;
    }
}

// DI Lifetimes:
// 1. Transient: New instance mỗi lần GetService
// 2. Scoped: New instance mỗi request HTTP
// 3. Singleton: 1 instance cho toàn bộ application lifetime
```

### Diagram:
```
Program.cs (DI Configuration)
  ↓
IServiceProvider (DI Container)
  ↓
Controller constructor (automatic injection)
  ↓
[IOrderRepository: EFOrderRepository instance]
[Cart: PersistentCart instance]
[INotificationService: NotificationService instance]
```

### Lợi ích:
- ✅ Loose coupling: OrderController không create dependencies
- ✅ Unit test dễ: Mock IOrderRepository
- ✅ Centralized configuration: Program.cs
- ✅ Runtime switching: Thay implementation mà không sửa code

---

## 12. 🔄 ASYNCHRONOUS PATTERN (Async/Await)
**Vị trí:** Toàn bộ dự án (Services, Controllers, Hub)

### Cách hoạt động:
```csharp
// ASYNC: Non-blocking operations
public async Task<bool> ChangeOrderStatus(int orderId, OrderStatus newStatus)
{
    // await giải phóng thread
    var order = await _context.Orders.FindAsync(orderId);
    
    // Lưu changes
    await _context.SaveChangesAsync();
    
    // Notify user
    await _notificationService.CreateOrderNotificationAsync(...);
}

// USAGE:
var result = await orderStateManager.ChangeOrderStatus(1, OrderStatus.DangXuLy);

// Server có thể phục vụ requests khác trong lúc chờ I/O
// Thread-pool được giải phóng liên tục
```

### Lợi ích:
- ✅ Scalability: Xử lý nhiều requests với ít threads
- ✅ Responsive: UI không block
- ✅ Better resource utilization

---

## 📊 PATTERN INTERACTION DIAGRAM

```
┌─────────────────────────────────────────────────────────┐
│                    PROGRAM.CS (Main)                    │
├─────────────────────────────────────────────────────────┤
│  BUILDER: WebApplication.CreateBuilder()                │
│  ├─ Add MVC, SignalR, DbContext, Identity              │
│  ├─ FACTORY: Cart.GetCart() – TẠO CART                 │
│  ├─ REPOSITORY: IStoreRepository → EFStoreRepository   │
│  ├─ STRATEGY: INotificationService → NotificationSvc   │
│  ├─ STRATEGY: IVoucherService → VoucherService         │
│  └─ SINGLETON: IHttpContextAccessor               │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│              DEPENDENCY INJECTION CONTAINER              │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│         OrderController (Async/Await Pattern)           │
├─────────────────────────────────────────────────────────┤
│  MVC Pattern: ModelBinding → Action → View              │
│  Injected:                                              │
│  ├─ IOrderRepository (REPOSITORY)                       │
│  ├─ Cart (FACTORY → DECORATOR → FACADE)                │
│  ├─ INotificationService (STRATEGY)                     │
│  ├─ OrderStateManager (MEMENTO)                        │
│  └─ IVoucherService (STRATEGY)                         │
└─────────────────────────────────────────────────────────┘
         ↓                        ↓                    ↓
    ┌---------┐          ┌────────────┐      ┌──────────────┐
    │OrderSvc │          │NotificationSvc   │VoucherService│
    ├─OBSERVER│          ├─ADAPTER    │      ├─VALIDATION  │
    │SignalR  │          │SignalR Hub │      │DB Queries   │
    │Groups   │          │+ Database  │      │Calculations │
    └─────────┘          └────────────┘      └──────────────┘
        ↓
    ┌────────────────┐
    │ChatHub (OBSERVER)      │
    ├────────────────┤
    │Groups: "Admins"       │
    │Groups: userId         │
    │SendAsync -> JS clients │
    └────────────────┘
        ↓
    ┌────────────────┐
    │   JS (Browser) │ (OBSERVER Pattern)
    ├────────────────┤
    │admin-chat.js  │
    │connection.on() │
    │Update UI Badge │
    └────────────────┘
```

---

## 🎯 PATTERN SUMMARY TABLE

| Pattern | File | Purpose | Benefit |
|---------|------|---------|---------|
| 1. Builder | Program.cs | Cấu hình fluent | Readability |
| 2. Adapter | EmailSender.cs | Chuẩn hóa interface | Extensibility |
| 3. Strategy | NotificationService, VoucherService | Multiple implementations | Flexibility, Testing |
| 4. Repository | EFStoreRepository, EFOrderRepository | Data access abstraction | Decoupling, Testing |
| 5. Memento | OrderStateManager | Undo/Redo states | Audit trail, UX |
| 6. Decorator | PersistentCart | Extend functionality | Composition >Inheritance |
| 7. Facade | PersistentCart.GetCart() | Simplify API | Easy to use |
| 8. Factory Method | GetCart() | Object creation | Flexibility |
| 9. Observer | ChatHub, NotificationHub | Publish-subscribe | Real-time, Loose coupling |
| 10. MVC | Controllers/Views/Models | Layered architecture | Separation of concerns |
| 11. DI/IoC | Program.cs | Dependency management | Testability, Maintainability |
| 12. Async/Await | Services, Controllers | Non-blocking I/O | Scalability, Performance |

---

## 📚 Files Reference

### Core Patterns
- `Program.cs` - BUILDER, DI, Singleton patterns
- `Models/` - Entities, Repository, Memento, Decorator, Factory
-Services/ - Adapter, Strategy patterns
- `Hubs/` - Observer, Hub patterns
- `Controllers/` - MVC, Async patterns
- `Views/` - MVC Presentation
- `Pages/` - Blazor Components

### Key Classes
- `OrderStateManager.cs` - MEMENTO pattern orchestrator
- `PersistentCart.cs` - DECORATOR, FACADE, FACTORY patterns
- `NotificationService.cs` - STRATEGY, OBSERVER patterns
- `VoucherService.cs` - STRATEGY pattern
- `EFStoreRepository.cs` - REPOSITORY pattern
- `ChatHub.cs` - OBSERVER pattern

---

## 🚀 Cách mở rộng (Extension Points)

### Thêm notification channel mới
```csharp
// Implement INotificationService
public class EmailNotificationService : INotificationService
{
    public async Task CreateOrderNotificationAsync(...) {
        // Send email instead of SignalR
    }
}

// Register dalam Program.cs
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
```

### Thêm voucher type mới
```csharp
public class FreeShippingVoucherService : IVoucherService
{
    // Implement free shipping logic
}
```

### Thêm state type mới
```csharp
public class OrderCancellationStateManager
{
    // Extend OrderStateManager for cancellation workflow
}
```

---

## 💡 Best Practices Áp dụng

1. ✅ Dependency Injection: Tất cả dependencies được inject, không new()
2. ✅ Interface-based: Dùng interface, không concrete classes
3. ✅ Async/Await: Tất cả I/O operations là async
4. ✅ Logging: Mỗi method quan trọng log tác động
5. ✅ Error Handling: Try-catch với logging
6. ✅ Null Checking: Guard clauses để check null/empty
7. ✅ Comments: Lồng comments tiếng Việt giải thích patterns

---

**Generated:** 2026-03-22 | **Version:** 1.0

