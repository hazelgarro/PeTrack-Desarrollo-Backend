using APIPetrack.Context;
using APIPetrack.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using APIPetrack.Models.Pets;
using APIPetrack.Models.Custom;
using System.Security.Claims;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : Controller
    {

        private readonly DbContextPetrack _context;

        public PetController(DbContextPetrack pContext)
        {
            _context = pContext;
        }

        [Authorize]
        [HttpPost("RegisterPet")]
        public async Task<IActionResult> RegisterPet([FromBody] RegisterPetRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "La solicitud no puede ser nula.",
                    Data = null
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Solicitud no válida.",
                    Data = ModelState
                });
            }

            object owner = null;
            if (request.OwnerType == "O")
            {
                owner = await _context.PetOwner.FindAsync(request.OwnerId);
            }
            else if (request.OwnerType == "S")
            {
                owner = await _context.PetStoreShelter.FindAsync(request.OwnerId);
            }

            if (owner == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = $"{(request.OwnerType == "O" ? "Dueño de mascota" : "Refugio de animales")} no encontrado.",
                    Data = null
                });
            }

            var pet = new Pet
            {
                Name = request.Name,
                DateOfBirth = request.DateOfBirth,
                Species = request.Species,
                Breed = request.Breed,
                Gender = request.Gender,
                Weight = request.Weight,
                Location = request.Location,
                OwnerId = request.OwnerId,
                OwnerTypeId = request.OwnerType,
                HealthIssues = request.HealthIssues,
                PetPicture = request.PetPicture,
                ImagePublicId = request.ImagePublicId,
                PetOwner = request.OwnerType == "O" ? owner as PetOwner : null,
                PetStoreShelter = request.OwnerType == "S" ? owner as PetStoreShelter : null
            };

            _context.Pet.Add(pet);

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascota registrada correctamente.",
                    Data = new { petId = pet.Id, petName = pet.Name }
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al registrar la mascota.",
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

        [HttpGet("GetAllPets")]
        public async Task<IActionResult> GetAllPets()
        {
            try
            {
                var pets = await _context.Pet
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
                        OwnerType = p.OwnerTypeId == "O" ? "PetOwner" : "PetStoreShelter",
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
                        Message = "No se han encontrado mascotas.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascotas recuperadas con éxito.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar mascotas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("GetPetsByOwner/{ownerId}")]
        public async Task<IActionResult> GetPetsByOwner(int ownerId)
        {
            try
            {
                var petOwner = await _context.PetOwner.FindAsync(ownerId);
                var ownerType = "";

                if (petOwner != null)
                {
                    ownerType = "O"; // PetOwner
                }
                else
                {
                    var petStoreShelter = await _context.PetStoreShelter.FindAsync(ownerId);
                    if (petStoreShelter != null)
                    {
                        ownerType = "S"; // PetStoreShelter
                    }
                }

                if (string.IsNullOrEmpty(ownerType))
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Propietario no encontrado.",
                        Data = null
                    });
                }

                var pets = await _context.Pet
                    .Where(p => p.OwnerId == ownerId && p.OwnerTypeId == ownerType)
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
                        OwnerType = p.OwnerTypeId == "O" ? "PetOwner" : "PetStoreShelter",
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
                        Message = "No se han encontrado mascotas para el propietario indicado.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascotas recuperadas con éxito.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar mascotas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("GetPetsByOwner")]
        public async Task<IActionResult> GetPetsByOwner()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int authenticatedOwnerId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Identificador de usuario no válido o ausente.",
                        Data = null
                    });
                }

                var petOwner = await _context.PetOwner.FindAsync(authenticatedOwnerId);
                var ownerType = petOwner != null ? "O" : "S"; // "O" = PetOwner, "S" = PetStoreShelter

                if (petOwner == null && await _context.PetStoreShelter.FindAsync(authenticatedOwnerId) == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Propietario no encontrado.",
                        Data = null
                    });
                }

                var pets = await _context.Pet
                    .Where(p => p.OwnerId == authenticatedOwnerId && p.OwnerTypeId == ownerType)
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
                        OwnerType = p.OwnerTypeId == "O" ? "PetOwner" : "PetStoreShelter",
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
                        Message = "No se han encontrado mascotas para el propietario autentificado.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascotas recuperadas con éxito.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar mascotas.",
                    Data = new { details = ex.Message }
                });
            }
        }

        [HttpGet("GetPetsByShelter/{shelterId}")]
        public async Task<IActionResult> GetPetsByShelter(int shelterId)
        {
            try
            {
                var petStoreShelter = await _context.PetStoreShelter.FindAsync(shelterId);

                if (petStoreShelter == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Refugio no encontrado.",
                        Data = null
                    });
                }

                var pets = await _context.Pet
                    .Where(p => p.OwnerId == shelterId && p.OwnerTypeId == "S") 
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
                        OwnerType = "PetStoreShelter", 
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
                        Message = "No se han encontrado mascotas para este refugio.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascotas recuperadas con éxito.",
                    Data = pets
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar mascotas.",
                    Data = new { details = ex.Message }
                });
            }
        }


        [Authorize]
        [HttpPut("EditPet/{petId}")]
        public async Task<IActionResult> EditPet(int petId, [FromBody] EditPetRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "La solicitud no puede ser nula.",
                    Data = null
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Estado del modelo no válido.",
                    Data = ModelState
                });
            }

            var pet = await _context.Pet.FindAsync(petId);
            if (pet == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Result = false,
                    Message = "Mascota no encontrada.",
                    Data = null
                });
            }

            pet.Name = request.Name;
            pet.DateOfBirth = request.DateOfBirth;
            pet.Species = request.Species;
            pet.Breed = request.Breed;
            pet.Gender = request.Gender;
            pet.Weight = request.Weight;
            pet.Location = request.Location;
            pet.HealthIssues = request.HealthIssues;
            pet.PetPicture = request.PetPicture;
            pet.ImagePublicId = request.ImagePublicId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascota actualizada correctamente.",
                    Data = new { petId = pet.Id, petName = pet.Name }
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al actualizar la mascota.",
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
        [HttpDelete("DeletePet/{petId}")]
        public async Task<IActionResult> DeletePet(int petId)
        {
            try
            {
                // Buscar la mascota por ID
                var pet = await _context.Pet.FindAsync(petId);
                if (pet == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Mascota no encontrada.",
                        Data = null
                    });
                }

                // Eliminar la mascota
                _context.Pet.Remove(pet);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascota eliminada con éxito.",
                    Data = new { petId = petId }
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error en la base de datos al eliminar la mascota.",
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

        [HttpGet("SearchById/{id}")]
        public async Task<IActionResult> SearchById(int id)
        {
            try
            {
                var pet = await _context.Pet
                    .Where(p => p.Id == id)
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
                        OwnerType = p.OwnerTypeId == "O" ? "PetOwner" : "PetStoreShelter",
                        p.HealthIssues,
                        p.PetPicture,
                        p.ImagePublicId
                    })
                    .FirstOrDefaultAsync();

                if (pet == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Result = false,
                        Message = "Mascota no encontrada.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Result = true,
                    Message = "Mascota recuperada con éxito.",
                    Data = pet
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Result = false,
                    Message = "Se ha producido un error al recuperar la mascota.",
                    Data = new { details = ex.Message }
                });
            }
        }




    }
}
