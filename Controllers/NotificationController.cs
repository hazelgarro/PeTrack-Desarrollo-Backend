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
                        Message = "Notificación no encontrada.",
                        Data = null
                    });
                }


                notification.IsRead = true;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Notificación marcada como leída.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al marcar la notificación como leída.",
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
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.NotificationDate)
                .ToListAsync();



                if (!notifications.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado notificaciones para este usuario.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<Notification>>
                {
                    Result = true,
                    Message = "Notificaciones recuperadas correctamente.",
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las notificaciones.",
                    Data = new { details = ex.Message }
                });
            }
        }
    }
}
