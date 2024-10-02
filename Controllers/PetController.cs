using APIPetrack.Context;
using APIPetrack.Models.Users;
using APIPetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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
                return BadRequest(new { message = "Request cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
                return BadRequest(new { message = $"{(request.OwnerType == "O" ? "Pet owner" : "Pet store shelter")} not found." });
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
                PetOwner = request.OwnerType == "O" ? owner as PetOwner : null,
                PetStoreShelter = request.OwnerType == "S" ? owner as PetStoreShelter : null
            };

            _context.Pet.Add(pet);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Pet registered successfully.", petId = pet.Id, petName = pet.Name });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while registering the pet.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
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
                        OwnerType = p.OwnerTypeId == "O" ? "PetOwner" : p.OwnerTypeId == "S" ? "PetStoreShelter" : "Veterinarian",
                        p.HealthIssues,
                        p.PetPicture
                    })
                    .ToListAsync();

                if (pets == null || !pets.Any())
                {
                    return NotFound(new { message = "No pets found." });
                }

                return Ok(pets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pets.", details = ex.Message });
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
                    return NotFound(new { message = "Owner not found." });
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
                        p.PetPicture
                    })
                    .ToListAsync();

                if (pets == null || !pets.Any())
                {
                    return NotFound(new { message = "No pets found for the given owner." });
                }

                return Ok(pets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pets.", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("EditPet/{petId}")]
        public async Task<IActionResult> EditPet(int petId, [FromBody] RegisterPetRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Buscar la mascota que se va a editar
            var pet = await _context.Pet.FindAsync(petId);
            if (pet == null)
            {
                return NotFound(new { message = "Pet not found." });
            }

            // Validar el propietario según el tipo (PetOwner o PetStoreShelter)
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
                return BadRequest(new { message = $"{(request.OwnerType == "O" ? "Pet owner" : "Pet store shelter")} not found." });
            }

            // Actualizar los datos de la mascota
            pet.Name = request.Name;
            pet.DateOfBirth = request.DateOfBirth;
            pet.Species = request.Species;
            pet.Breed = request.Breed;
            pet.Gender = request.Gender;
            pet.Weight = request.Weight;
            pet.Location = request.Location;
            pet.OwnerId = request.OwnerId;
            pet.OwnerTypeId = request.OwnerType;
            pet.HealthIssues = request.HealthIssues;
            pet.PetPicture = request.PetPicture;
            pet.PetOwner = request.OwnerType == "O" ? owner as PetOwner : null;
            pet.PetStoreShelter = request.OwnerType == "S" ? owner as PetStoreShelter : null;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Pet updated successfully.", petId = pet.Id, petName = pet.Name });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while updating the pet.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
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
                    return NotFound(new { message = "Pet not found." });
                }

                // Eliminar la mascota
                _context.Pet.Remove(pet);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Pet deleted successfully.", petId = petId });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Database error occurred while deleting the pet.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
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
                        p.PetPicture
                    })
                    .FirstOrDefaultAsync();

                if (pet == null)
                {
                    return NotFound(new { message = "Pet not found." });
                }

                return Ok(pet);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the pet.", details = ex.Message });
            }
        }




    }
}
