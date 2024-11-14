using APIPetrack.Context;
using APIPetrack.Models.Adoptions;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Notificacions;
using APIPetrack.Models.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransferController : Controller
    {
        private readonly DbContextPetrack _context;

        public TransferController(DbContextPetrack pContext)
        {
            _context = pContext;
        }

        [Authorize]
        [HttpPost("RequestTransfer")]
        public async Task<IActionResult> RequestTransfer([FromBody] TransferRequestDto request)
        {
            if (request == null || request.PetId <= 0 || string.IsNullOrEmpty(request.NewOwnerEmail))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Datos de solicitud de transferencia no válidos.",
                    Data = null
                });
            }

            var pet = await _context.Pet.FindAsync(request.PetId);
            if (pet == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Mascota no encontrada.",
                    Data = null
                });
            }

            if (pet.OwnerId != request.CurrentOwnerId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Usted no es el propietario de esta mascota.",
                    Data = null
                });
            }

            var existingTransferRequest = await _context.TransferRequest
                .FirstOrDefaultAsync(tr => tr.PetId == request.PetId && tr.Status == "Pending");

            if (existingTransferRequest != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Ya existe una solicitud de transferencia pendiente para esta mascota.",
                    Data = null
                });
            }


            var owner = await _context.AppUser.FindAsync(request.CurrentOwnerId);
            if (owner == null || !BCrypt.Net.BCrypt.Verify(request.Password, owner.Password))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Contraseña inválida.",
                    Data = null
                });
            }

            var newOwner = await _context.AppUser.FirstOrDefaultAsync(u => u.Email == request.NewOwnerEmail);
            if (newOwner == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Nuevo propietario no encontrado.",
                    Data = null
                });
            }

            var transferRequest = new TransferRequest
            {
                PetId = request.PetId,
                CurrentOwnerId = request.CurrentOwnerId,
                NewOwnerId = newOwner.Id,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.TransferRequest.Add(transferRequest);

            var notification = new Notification
            {
                UserId = newOwner.Id,
                Message = $"Ha recibido una solicitud de traslado de {owner.Email} para la mascota {pet.Name}.",
                IsRead = false,
                NotificationDate = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitud de transferencia enviada correctamente.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al enviar la solicitud de transferencia.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error inesperado.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPut("RespondToTransfer/{transferRequestId}")]
        public async Task<IActionResult> RespondToTransfer(int transferRequestId, [FromBody] TransferResponseDto response)
        {
            var transferRequest = await _context.TransferRequest
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r => r.Id == transferRequestId);

            if (transferRequest == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud de transferencia no encontrada.",
                    Data = null
                });
            }

            if (transferRequest.Status != "Pending")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Esta solicitud de transferencia ya ha sido tramitada.",
                    Data = null
                });
            }

            if (response.Accepted)
            {
                transferRequest.Status = "Accepted";
                transferRequest.Pet.OwnerId = transferRequest.NewOwnerId;

                var notification = new Notification
                {
                    UserId = transferRequest.CurrentOwnerId,
                    Message = $"La solicitud de transferencia de {transferRequest.Pet.Name} ha sido aceptada.",
                    IsRead = false,
                    NotificationDate = DateTime.UtcNow
                };
                _context.Notification.Add(notification);
            }
            else
            {
                transferRequest.Status = "Rejected";

                var notification = new Notification
                {
                    UserId = transferRequest.CurrentOwnerId,
                    Message = $"La solicitud de transferencia para {transferRequest.Pet.Name} ha sido rechazada.",
                    IsRead = false,
                    NotificationDate = DateTime.UtcNow
                };
                _context.Notification.Add(notification);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = response.Accepted
                        ? "Solicitud de transferencia aceptada."
                        : "Solicitud de transferencia rechazada.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al procesar la solicitud de transferencia.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error inesperado.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [HttpGet("GetAllTransferRequests")]
        public async Task<IActionResult> GetAllTransferRequests()
        {
            try
            {
                var transferRequests = await _context.TransferRequest
                    .Include(tr => tr.Pet)
                    .Include(tr => tr.CurrentOwner)
                    .Include(tr => tr.NewOwner)
                    .Select(tr => new
                    {
                        Id = tr.Id,
                        PetId = tr.Pet.Id,
                        PetName = tr.Pet.Name,
                        PetSpecies = tr.Pet.Species,
                        PetBreed = tr.Pet.Breed,
                        PetGender = tr.Pet.Gender,
                        PetPicture = tr.Pet.PetPicture,
                        CurrentOwner = new
                        {
                            Email = tr.CurrentOwner.Email,
                            ProfilePicture = tr.CurrentOwner.ProfilePicture,
                            PhoneNumber = tr.CurrentOwner.PhoneNumber
                        },
                        Requester = new
                        {
                            Email = tr.NewOwner.Email,
                            ProfilePicture = tr.NewOwner.ProfilePicture,
                            PhoneNumber = tr.NewOwner.PhoneNumber
                        },
                        Status = tr.Status,
                        RequestDate = tr.RequestDate
                    })
                    .ToListAsync();

                if (!transferRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de transferencia.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitudes de transferencia recuperadas con éxito.",
                    Data = transferRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error inesperado.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("GetTransferRequestsByUserId/{userId}")]
        public async Task<IActionResult> GetTransferRequestsByUserId(int userId)
        {
            try
            {
                var transferRequests = await _context.TransferRequest
                    .Include(tr => tr.Pet)
                    .Include(tr => tr.CurrentOwner)
                    .Include(tr => tr.NewOwner)
                    .Where(tr => tr.CurrentOwnerId == userId || tr.NewOwnerId == userId)
                    .Select(tr => new
                    {
                        Id = tr.Id,
                        PetId = tr.Pet.Id,
                        PetName = tr.Pet.Name,
                        PetSpecies = tr.Pet.Species,
                        PetBreed = tr.Pet.Breed,
                        PetGender = tr.Pet.Gender,
                        PetPicture = tr.Pet.PetPicture,
                        CurrentOwner = new
                        {
                            Email = tr.CurrentOwner.Email,
                            ProfilePicture = tr.CurrentOwner.ProfilePicture,
                            PhoneNumber = tr.CurrentOwner.PhoneNumber
                        },
                        Requester = new
                        {
                            Email = tr.NewOwner.Email,
                            ProfilePicture = tr.NewOwner.ProfilePicture,
                            PhoneNumber = tr.NewOwner.PhoneNumber
                        },
                        Status = tr.Status,
                        RequestDate = tr.RequestDate
                    })
                    .ToListAsync();

                if (!transferRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de transferencia para el usuario especificado.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitudes de transferencia recuperadas con éxito.",
                    Data = transferRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error inesperado.",
                    Data = new { details = ex.Message }
                });
            }
        }


    }
}
