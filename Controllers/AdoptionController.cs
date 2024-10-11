using APIPetrack.Context;
using APIPetrack.Models.Adoptions;
using APIPetrack.Models.Custom;
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
                        Message = "No pets available for adoption.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Adoption pets retrieved successfully.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving adoption pets.",
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
                    Message = "Invalid adoption request data.",
                    Data = null
                });
            }

            var pet = await _context.Pet.FindAsync(request.PetId);
            if (pet == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Pet not found.",
                    Data = null
                });
            }

            if (pet.OwnerTypeId != "S")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Only pets from shelters can be adopted.",
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
                    Message = "Adoption request submitted successfully.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Database error occurred while submitting the adoption request.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An unexpected error occurred.",
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
                    Message = "Adoption request not found.",
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
                    Message = "Adoption request accepted, but not yet delivered.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Database error occurred while accepting the adoption request.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An unexpected error occurred.",
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
                    Message = "Adoption request not found.",
                    Data = null
                });
            }

            if (adoptionRequest.IsAccepted != "Accepted")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Cannot deliver pet. The adoption request has not been accepted.",
                    Data = null
                });
            }

            adoptionRequest.IsDelivered = true;

            await UpdatePetOwner(adoptionRequest.PetId, adoptionRequest.NewOwnerId, adoptionRequest.OwnerType);

            var otherRequests = await _context.AdoptionRequest
                .Where(r => r.PetId == adoptionRequest.PetId && r.Id != adoptionRequestId)
                .ToListAsync();

            foreach (var request in otherRequests)
            {
                if (request.IsAccepted != "Approved")
                {
                    var notification = new Notification
                    {
                        UserId = request.NewOwnerId,
                        Message = $"Your adoption request for pet {adoptionRequest.Pet.Name} has been rejected.",
                        IsRead = false,
                        NotificationDate = DateTime.Now
                    };
                    _context.Notification.Add(notification);
                }
            }

            _context.AdoptionRequest.RemoveRange(otherRequests.Where(r => r.IsAccepted != "Accepted"));

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Pet delivered. Other adoption requests have been rejected and notifications have been sent.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Database error occurred while confirming the delivery.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An unexpected error occurred.",
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
                    Message = "Adoption request not found.",
                    Data = null
                });
            }

            if (adoptionRequest.IsAccepted != "Accepted")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Only accepted requests can be canceled.",
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
                    Message = "Adoption request has been canceled.",
                    Data = null
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Database error occurred while canceling the adoption request.",
                    Data = new { details = dbEx.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An unexpected error occurred.",
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
                    Message = "Adoption requests retrieved successfully.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving adoption requests.",
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
                        Message = "No adoption requests found for this pet.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Adoption requests for the pet retrieved successfully.",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving adoption requests for this pet.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("GetPendingAdoptionRequest")]
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
                        Message = "No adoption requests found.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Adoption requests retrieved successfully.",
                    Data = pendingRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving adoption requests.",
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
                        Message = "No accepted requests found.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Accepted adoption requests retrieved successfully.",
                    Data = acceptedRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving accepted adoption requests.",
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
                        Message = "No cancelled requests found.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Result = true,
                    Message = "Accepted adoption requests retrieved successfully.",
                    Data = acceptedRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "An error occurred while retrieving accepted adoption requests.",
                    Data = new { details = ex.Message }
                });
            }
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

        private async Task UpdatePetOwner(int petId, int newOwnerId, string newOwnerType)
        {
            var pet = await _context.Pet.FindAsync(petId);

            if (pet == null)
            {
                throw new Exception("Pet not found.");
            }

            pet.OwnerId = newOwnerId;
            pet.OwnerTypeId = newOwnerType;

            await _context.SaveChangesAsync();
        }
    }
}
