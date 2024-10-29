using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Notificacions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : Controller
    {
        private readonly DbContextPetrack _context;
        public NotificationController(DbContextPetrack pContext)
        {
            _context = pContext;
        }

        [Authorize]
        [HttpPut("MarkNotificationAsRead/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var notification = await _context.Notification.FirstOrDefaultAsync(n => n.Id == notificationId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Notification not found.",
                        Data = null
                    });
                }


                notification.IsRead = true;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Notification marked as read.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while marking the notification as read.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("GetUserNotifications/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                var notifications = await _context.Notification
                .Where(n => n.UserId == userId && n.IsRead == false)
                .OrderByDescending(n => n.NotificationDate)
                .ToListAsync();



                if (!notifications.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No notifications found for this user.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<Notification>>
                {
                    Result = true,
                    Message = "Notifications retrieved successfully.",
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving notifications.",
                    Data = new { details = ex.Message }
                });
            }
        }
    }
}
