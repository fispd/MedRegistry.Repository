using DataLayer.Data;
using DataLayer.Models;
using MedRegistry.webapi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly MedRegistryContext _context;

    public AppointmentsController(MedRegistryContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments(
        [FromQuery] string? status = null,
        [FromQuery] int? doctorId = null,
        [FromQuery] int? patientId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var query = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (dateFrom.HasValue)
            query = query.Where(a => a.AppointmentStart >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(a => a.AppointmentStart <= dateTo.Value);

        var appointments = await query
            .OrderBy(a => a.AppointmentStart)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                PatientName = a.Patient.User.LastName + " " + a.Patient.User.FirstName,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.User.LastName + " " + a.Doctor.User.FirstName,
                DoctorSpecialization = a.Doctor.Specialization.Name,
                CabinetNumber = a.Doctor.CabinetNumber,
                AppointmentStart = a.AppointmentStart,
                AppointmentEnd = a.AppointmentEnd,
                Status = a.Status,
                Purpose = a.Purpose
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
            .Where(a => a.AppointmentId == id)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                PatientName = a.Patient.User.LastName + " " + a.Patient.User.FirstName,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.User.LastName + " " + a.Doctor.User.FirstName,
                DoctorSpecialization = a.Doctor.Specialization.Name,
                CabinetNumber = a.Doctor.CabinetNumber,
                AppointmentStart = a.AppointmentStart,
                AppointmentEnd = a.AppointmentEnd,
                Status = a.Status,
                Purpose = a.Purpose
            })
            .FirstOrDefaultAsync();

        if (appointment == null)
            return NotFound();

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto)
    {
        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentStart = dto.AppointmentStart,
            AppointmentEnd = dto.AppointmentEnd,
            Purpose = dto.Purpose,
            Status = "Ожидает"
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.AppointmentId },
            new AppointmentDto { AppointmentId = appointment.AppointmentId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, UpdateAppointmentDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound();

        if (dto.AppointmentStart.HasValue)
            appointment.AppointmentStart = dto.AppointmentStart.Value;

        if (dto.AppointmentEnd.HasValue)
            appointment.AppointmentEnd = dto.AppointmentEnd.Value;

        if (!string.IsNullOrEmpty(dto.Status))
            appointment.Status = dto.Status;

        if (dto.Purpose != null)
            appointment.Purpose = dto.Purpose;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound();

        appointment.Status = status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound();

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

