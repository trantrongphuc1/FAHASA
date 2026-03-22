using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace SportsStore.Controllers
{
    public class ChatController : Controller
    {
        private readonly StoreDbContext _context;

        public ChatController(StoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.Message)) return BadRequest();

            // Only persist message if sender is authenticated (has UserId) or the message is from an admin.
            if (request.IsAdmin || !string.IsNullOrEmpty(request.UserId))
            {
                var message = new ChatMessage
                {
                    UserId = request.UserId,
                    UserName = request.UserName,
                    Message = request.Message,
                    IsFromAdmin = request.IsAdmin,
                    SentAt = DateTime.Now
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();
            }

            // If sender is anonymous, we accept the request but do not persist it.
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string? sessionId)
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            string? userId = null;
            if (isAuth)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            var query = _context.ChatMessages.OrderBy(m => m.SentAt).AsQueryable();

            if (!isAuth)
            {
                // If anonymous but has a sessionId (client-side assigned), return messages for that session only.
                if (!string.IsNullOrEmpty(sessionId))
                {
                    query = query.Where(m => m.UserId == sessionId);
                }
                else
                {
                    // No session -> return empty list
                    return Json(Array.Empty<object>());
                }
            }
            else
            {
                // For authenticated users, return their messages and admin messages addressed to them.
                query = query.Where(m => m.UserId == userId || m.IsFromAdmin || m.UserId == userId);
            }

            var messages = await query
                .Take(50)
                .Select(m => new
                {
                    m.UserName,
                    m.Message,
                    m.IsFromAdmin,
                    m.SentAt,
                    m.UserId,
                    m.IsRead
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            if (!isAuth)
            {
                return Json(new { count = 0 });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _context.ChatMessages
                .Where(m => m.UserId == userId && m.IsFromAdmin && !m.IsRead)
                .CountAsync();

            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> MarkMessagesAsRead()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            if (!isAuth)
            {
                return BadRequest();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var unreadMessages = await _context.ChatMessages
                .Where(m => m.UserId == userId && m.IsFromAdmin && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class ChatMessageRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}

