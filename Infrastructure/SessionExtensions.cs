using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SportsStore.Infrastructure
{
    // 🛠️ MẪU THIẾT KẾ EXTENSION METHODS - Mở rộng interface ISession
    // Thêm khả năng JSON serialization cho ASP.NET Core sessions
    // Cho phép SetJson/GetJson<T> methods trên bất kỳ ISession instance nào
    // 🔗 EXTENDS: Mở rộng interface ISession (ASP.NET Core built-in)
    public static class SessionExtensions
    {
        public static void SetJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetJson<T>(this ISession session, string key)
        {
            var data = session.GetString(key);
            return data == null ? default : JsonSerializer.Deserialize<T>(data);
        }
    }
}
