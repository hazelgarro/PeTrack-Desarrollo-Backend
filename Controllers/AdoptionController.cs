using APIPetrack.Context;
using APIPetrack.Models.Adoptions;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Notificacions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdoptionController : Controller
    {
        private readonly DbContextPetrack _context;
        public AdoptionController(DbContextPetrack pContext)
        {
            _context = pContext;
        }

        [HttpGet("GetAllAdoptionPets")]
        public async Task<IActionResult> GetAllAdoptionPets()
        {
            try
            {
                var pets = await _context.Pet
                    .Where(p => p.OwnerTypeId == "S") // Filtrar por PetStoreShelter
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.DateOfBirth,
                        p.Species,
                        p.Breed,
                        p.Gender,
                        p.Weight,
                        p.Location,
                        p.OwnerId,
                        OwnerType = "PetStoreShelter", // Fijamos el tipo como PetStoreShelter
                        p.HealthIssues,
                        p.PetPicture,
                        p.ImagePublicId
                    })
                    .ToListAsync();

                if (pets == null || !pets.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No hay mascotas disponibles para adopción.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascotas en adopción recuperadas con éxito.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las mascotas de adopción.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPost("RequestAdoption")]
        public async Task<IActionResult> RequestAdoption([FromBody] CreateAdoptionRequest request)
        {
            if (request == null || request.PetId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Datos de solicitud de adopción no válidos.",
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

            if (pet.OwnerTypeId != "S")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Sólo se pueden adoptar mascotas procedentes de refugios.",
                    Data = null
                });
            }

            var existingRequest = await _context.AdoptionRequest
                .FirstOrDefaultAsync(ar => ar.PetId == request.PetId && ar.NewOwnerId == request.NewOwnerId);

            if (existingRequest != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Ya has enviado una solicitud de adopción para esta mascota.",
                    Data = null
                });
            }

            var adoptionRequest = new AdoptionRequest
            {
                PetId = request.PetId,
                CurrentOwnerId = pet.OwnerId, 
                NewOwnerId = request.NewOwnerId, 
                OwnerType = "O", 
                IsAccepted = "Pending",
                RequestDate = DateTime.UtcNow
            };

            _context.AdoptionRequest.Add(adoptionRequest);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitud de adopción enviada correctamente.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al enviar la solicitud de adopción.",
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
        [HttpPut("AcceptAdoptionRequest/{adoptionRequestId}")]
        public async Task<IActionResult> AcceptAdoptionRequest(int adoptionRequestId)
        {
            var adoptionRequest = await _context.AdoptionRequest
                .Include(r => r.Pet) 
                .FirstOrDefaultAsync(r => r.Id == adoptionRequestId);

            if (adoptionRequest == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud de adopción no encontrada.",
                    Data = null
                });
            }

            bool alreadyAccepted = await _context.AdoptionRequest.AnyAsync(r => r.PetId == adoptionRequest.PetId &&
                       (r.IsAccepted == "Accepted" || r.IsAccepted == "Delivered"));

            if (alreadyAccepted)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Ya hay una solicitud de adopción aceptada o entregada para esta mascota.",
                    Data = null
                });
            }

            adoptionRequest.IsAccepted = "Accepted";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitud de adopción aceptada, pero aún no entregada.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al aceptar la solicitud de adopción.",
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
        [HttpPut("ConfirmDelivery/{adoptionRequestId}")]
        public async Task<IActionResult> ConfirmDelivery(int adoptionRequestId)
        {
            var adoptionRequest = await _context.AdoptionRequest
                .Include(r => r.Pet) 
                .FirstOrDefaultAsync(r => r.Id == adoptionRequestId);

            if (adoptionRequest == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud de adopción no encontrada.",
                    Data = null
                });
            }

            if (adoptionRequest.IsAccepted != "Accepted")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "No se puede entregar la mascota. La solicitud de adopción no ha sido aceptada.",
                    Data = null
                });
            }

            adoptionRequest.IsAccepted = "Delivered";
            adoptionRequest.IsDelivered = true;
            adoptionRequest.DeliveryDate = DateTime.Now;

            await UpdatePetOwner(adoptionRequest.PetId, adoptionRequest.NewOwnerId, adoptionRequest.OwnerType);

            var otherRequests = await _context.AdoptionRequest
                .Where(r => r.PetId == adoptionRequest.PetId && r.Id != adoptionRequestId)
                .ToListAsync();

            foreach (var request in otherRequests)
            {
                if (request.IsAccepted != "Delivered")
                {
                    request.IsAccepted = "Rejected";

                    var notification = new Notification
                    {
                        UserId = request.NewOwnerId,
                        Message = $"Su solicitud de adopción para la mascota {adoptionRequest.Pet.Name} ha sido rechazada.",
                        IsRead = false,
                        NotificationDate = DateTime.Now
                    };
                    _context.Notification.Add(notification);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascota entregada. Se han rechazado otras solicitudes de adopción y se han enviado notificaciones.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al confirmar la entrega.",
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
        [HttpPut("RejectAdoptionRequest/{adoptionRequestId}")]
        public async Task<IActionResult> RejectAdoptionRequest(int adoptionRequestId)
        {
            var adoptionRequest = await _context.AdoptionRequest
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r => r.Id == adoptionRequestId);
            
            if (adoptionRequest == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud de adopción no encontrada.",
                    Data = null
                });
            }
            
            if (adoptionRequest.IsAccepted == "Delivered")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "No se puede rechazar esta solicitud de adopción porque ya se ha entregado.",
                    Data = null
                });
            }

            adoptionRequest.IsAccepted = "Rejected";

            var notification = new Notification
            {
                UserId = adoptionRequest.NewOwnerId,
                Message = $"Su solicitud de adopción para la mascota {adoptionRequest.Pet.Name} ha sido rechazada.",
                IsRead = false,
                NotificationDate = DateTime.Now
            };
            _context.Notification.Add(notification);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Solicitud de adopción rechazada y notificación enviada.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al rechazar la solicitud de adopción.",
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
        [HttpPut("CancelAdoptionRequest/{adoptionRequestId}")]
        public async Task<IActionResult> CancelAdoptionRequest(int adoptionRequestId)
        {
            var adoptionRequest = await _context.AdoptionRequest
                .FirstOrDefaultAsync(r => r.Id == adoptionRequestId);

            if (adoptionRequest == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud de adopción no encontrada.",
                    Data = null
                });
            }

            if (adoptionRequest.IsAccepted == "Delivered")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "No se puede anular una solicitud de adopción que ya ha sido entregada.",
                    Data = null
                });
            }
            adoptionRequest.IsAccepted = "Cancelled";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Se ha cancelado la solicitud de adopción.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al cancelar la solicitud de adopción.",
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
        [HttpGet("ListAllAdoptionRequests")]
        public async Task<IActionResult> GetAllAdoptionRequests()
        {
            try
            {
                var requests = await _context.AdoptionRequest
                .Include(ar => ar.Pet) 
                .Include(ar => ar.CurrentOwner) 
                .Include(ar => ar.NewOwner) 
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate
                })
                .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción recuperadas con éxito.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListAdoptionRequestsForPet/{petId}")]
        public async Task<IActionResult> GetAdoptionRequestsByPet(int petId)
        {
            try
            {
                var requests = await _context.AdoptionRequest
                .Where(ar => ar.PetId == petId)
                .Include(ar => ar.Pet) 
                .Include(ar => ar.CurrentOwner) 
                .Include(ar => ar.NewOwner) 
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate
                })
                .ToListAsync();

                if (!requests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de adopción para esta mascota.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción de la mascota recuperadas con éxito.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción de esta mascota.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListAllAdoptionRequestsForUser/{userId}")]
        public async Task<IActionResult> GetAdoptionRequestsByUser(int userId)
        {
            try
            {
                var requests = await _context.AdoptionRequest
                .Where(ar => ar.CurrentOwnerId == userId || ar.NewOwnerId == userId)
                .Include(ar => ar.Pet)
                .Include(ar => ar.CurrentOwner)
                .Include(ar => ar.NewOwner)
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate
                })
                .ToListAsync();

                if (!requests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de adopción para este usuario.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción para el usuario recuperadas con éxito.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción de este usuario.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListPendingAdoptionRequestsForUser/{userId}")]
        public async Task<IActionResult> GetPendingAdoptionRequestsByUser(int userId)
        {
            try
            {
                var requests = await _context.AdoptionRequest
                .Where(ar => ar.CurrentOwnerId == userId || ar.NewOwnerId == userId)
                .Where(r => r.IsAccepted == "Pending")
                .Include(ar => ar.Pet)
                .Include(ar => ar.CurrentOwner)
                .Include(ar => ar.NewOwner)
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate
                })
                .ToListAsync();

                if (!requests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de adopción para este usuario.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción para el usuario recuperadas con éxito.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción de este usuario.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListPendingAdoptionRequest")]
        public async Task<IActionResult> GetPendingAdoptionRequests()
        {
            try
            {
                var pendingRequests = await _context.AdoptionRequest
                .Where(r => r.IsAccepted == "Pending") 
                .Include(r => r.Pet)
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate
                })
                .ToListAsync();

                if (!pendingRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de adopción.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción recuperadas con éxito.",
                    Data = pendingRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListAcceptedAdoptionRequest")]
        public async Task<IActionResult> GetAcceptedAdoptionRequests()
        {
            try
            {
                var acceptedRequests = await _context.AdoptionRequest
            .Where(r => r.IsAccepted == "Accepted") 
            .Include(r => r.Pet)
            .Select(ar => new
            {
                Id = ar.Id,
                PetId = ar.PetId,
                PetName = ar.Pet.Name,
                PetSpecies = ar.Pet.Species,
                PetBreed = ar.Pet.Breed,
                PetGender = ar.Pet.Gender,
                PetPicture = ar.Pet.PetPicture,
                PreviousOwner = new
                {
                    ar.CurrentOwner.Email,
                    ar.CurrentOwner.ProfilePicture,
                    ar.CurrentOwner.PhoneNumber
                },
                CurrentOwner = new
                {
                    ar.NewOwner.Email,
                    ar.NewOwner.ProfilePicture,
                    ar.NewOwner.PhoneNumber
                },
                IsAccepted = ar.IsAccepted,
                RequestDate = ar.RequestDate
            })
            .ToListAsync();

                if (!acceptedRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes aceptadas.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción aceptadas recuperadas con éxito.",
                    Data = acceptedRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción aceptadas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListCancelledAdoptionRequest")]
        public async Task<IActionResult> GetCancelledAdoptionRequests()
        {
            try
            {
                var acceptedRequests = await _context.AdoptionRequest
            .Where(r => r.IsAccepted == "Cancelled") 
            .Include(r => r.Pet)
            .Select(ar => new
            {
                Id = ar.Id,
                PetId = ar.PetId,
                PetName = ar.Pet.Name,
                PetSpecies = ar.Pet.Species,
                PetBreed = ar.Pet.Breed,
                PetGender = ar.Pet.Gender,
                PetPicture = ar.Pet.PetPicture,
                PreviousOwner = new
                {
                    ar.CurrentOwner.Email,
                    ar.CurrentOwner.ProfilePicture,
                    ar.CurrentOwner.PhoneNumber
                },
                CurrentOwner = new
                {
                    ar.NewOwner.Email,
                    ar.NewOwner.ProfilePicture,
                    ar.NewOwner.PhoneNumber
                },
                IsAccepted = ar.IsAccepted,
                RequestDate = ar.RequestDate
            })
            .ToListAsync();

                if (!acceptedRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes canceladas.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción canceladas recuperadas con éxito.",
                    Data = acceptedRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción canceladas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListRejectedAdoptionRequest")]
        public async Task<IActionResult> GetRejectedAdoptionRequests()
        {
            try
            {
                var acceptedRequests = await _context.AdoptionRequest
            .Where(r => r.IsAccepted == "Rejected")
            .Include(r => r.Pet)
            .Select(ar => new
            {
                Id = ar.Id,
                PetId = ar.PetId,
                PetName = ar.Pet.Name,
                PetSpecies = ar.Pet.Species,
                PetBreed = ar.Pet.Breed,
                PetGender = ar.Pet.Gender,
                PetPicture = ar.Pet.PetPicture,
                PreviousOwner = new
                {
                    ar.CurrentOwner.Email,
                    ar.CurrentOwner.ProfilePicture,
                    ar.CurrentOwner.PhoneNumber
                },
                CurrentOwner = new
                {
                    ar.NewOwner.Email,
                    ar.NewOwner.ProfilePicture,
                    ar.NewOwner.PhoneNumber
                },
                IsAccepted = ar.IsAccepted,
                RequestDate = ar.RequestDate
            })
            .ToListAsync();

                if (!acceptedRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes rechazadas.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes de adopción rechazadas recuperadas con éxito.",
                    Data = acceptedRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción rechazadas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("ListDeliveredAdoption")]
        public async Task<IActionResult> GetDeliveredAdoptionRequests()
        {
            try
            {
                var pendingRequests = await _context.AdoptionRequest
                .Where(r => r.IsAccepted == "Delivered")
                .Include(r => r.Pet)
                .Select(ar => new
                {
                    Id = ar.Id,
                    PetId = ar.PetId,
                    PetName = ar.Pet.Name,
                    PetSpecies = ar.Pet.Species,
                    PetBreed = ar.Pet.Breed,
                    PetGender = ar.Pet.Gender,
                    PetPicture = ar.Pet.PetPicture,
                    CurrentOwner = new
                    {
                        ar.CurrentOwner.Email,
                        ar.CurrentOwner.ProfilePicture,
                        ar.CurrentOwner.PhoneNumber
                    },
                    Requester = new
                    {
                        ar.NewOwner.Email,
                        ar.NewOwner.ProfilePicture,
                        ar.NewOwner.PhoneNumber
                    },
                    IsAccepted = ar.IsAccepted,
                    RequestDate = ar.RequestDate,
                    DeliveryDate = ar.DeliveryDate,
                })
                .ToListAsync();

                if (!pendingRequests.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "No se han encontrado solicitudes de adopción entregadas.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Solicitudes entregadas recuperadas con éxito.",
                    Data = pendingRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar las solicitudes de adopción entregadas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        

        private async Task UpdatePetOwner(int petId, int newOwnerId, string newOwnerType)
        {
            var pet = await _context.Pet.FindAsync(petId);

            if (pet == null)
            {
                throw new Exception("Mascota no encontrada.");
            }

            pet.OwnerId = newOwnerId;
            pet.OwnerTypeId = newOwnerType;

            await _context.SaveChangesAsync();
        }
    }
}
