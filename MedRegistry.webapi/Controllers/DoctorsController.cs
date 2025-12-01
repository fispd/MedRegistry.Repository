using DataLayer.Data;
using DataLayer.Models;
using MedRegistry.webapi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly MedRegistryContext _context;

    public DoctorsController(MedRegistryContext context)
    {
        _context = context;
    }

    // GET: api/doctors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctors()
    {
        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialization)
            .Select(d => new DoctorDto
            {
                DoctorId = d.DoctorId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                MiddleName = d.User.MiddleName,
                Specialization = d.Specialization.Name,
                WorkExperienceYears = d.WorkExperienceYears,
                CabinetNumber = d.CabinetNumber,
                Phone = d.User.Phone,
                Email = d.User.Email
            })
            .ToListAsync();

        return Ok(doctors);
    }

    // GET: api/doctors/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DoctorDto>> GetDoctor(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialization)
            .Where(d => d.DoctorId == id)
            .Select(d => new DoctorDto
            {
                DoctorId = d.DoctorId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                MiddleName = d.User.MiddleName,
                Specialization = d.Specialization.Name,
                WorkExperienceYears = d.WorkExperienceYears,
                CabinetNumber = d.CabinetNumber,
                Phone = d.User.Phone,
                Email = d.User.Email
            })
            .FirstOrDefaultAsync();

        if (doctor == null)
            return NotFound();

        return Ok(doctor);
    }

    // GET: api/doctors/specializations
    [HttpGet("specializations")]
    public async Task<ActionResult<IEnumerable<Specialization>>> GetSpecializations()
    {
        return await _context.Specializations.ToListAsync();
    }

    // POST: api/doctors
    [HttpPost]
    public async Task<ActionResult<DoctorDto>> CreateDoctor(CreateDoctorDto dto)
    {
        // Создаём пользователя
        var user = new User
        {
            Username = $"doctor_{DateTime.Now.Ticks}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("temp123"),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MiddleName = dto.MiddleName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            MedicalPolicy = dto.MedicalPolicy,
            RoleId = 2 // Врач
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Создаём врача
        var doctor = new Doctor
        {
            UserId = user.UserId,
            SpecializationId = dto.SpecializationId,
            WorkExperienceYears = dto.WorkExperienceYears,
            CabinetNumber = dto.CabinetNumber
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDoctor), new { id = doctor.DoctorId }, 
            new DoctorDto
            {
                DoctorId = doctor.DoctorId,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
    }

    // DELETE: api/doctors/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Appointments)
            .FirstOrDefaultAsync(d => d.DoctorId == id);

        if (doctor == null)
            return NotFound();

        if (doctor.Appointments.Any())
            return BadRequest("Нельзя удалить врача с существующими приёмами");

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

