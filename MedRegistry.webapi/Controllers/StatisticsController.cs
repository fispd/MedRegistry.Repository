using DataLayer.Data;
using MedRegistry.webapi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly MedRegistryContext _context;

    public StatisticsController(MedRegistryContext context)
    {
        _context = context;
    }

    // GET: api/statistics
    [HttpGet]
    public async Task<ActionResult<StatisticsDto>> GetStatistics()
    {
        var today = DateTime.Today;

        var stats = new StatisticsDto
        {
            TotalDoctors = await _context.Doctors.CountAsync(),
            TotalPatients = await _context.Patients.CountAsync(),
            TotalAppointments = await _context.Appointments.CountAsync(),
            AppointmentsToday = await _context.Appointments
                .CountAsync(a => a.AppointmentStart.Date == today),
            PendingAppointments = await _context.Appointments
                .CountAsync(a => a.Status == "Ожидает"),
            CompletedAppointments = await _context.Appointments
                .CountAsync(a => a.Status == "Выполнено"),
            CancelledAppointments = await _context.Appointments
                .CountAsync(a => a.Status == "Отменено"),
            DoctorsBySpecialization = await _context.Doctors
                .Include(d => d.Specialization)
                .GroupBy(d => d.Specialization.Name)
                .Select(g => new SpecializationStatsDto
                {
                    Specialization = g.Key ?? "Без специализации",
                    Count = g.Count()
                })
                .ToListAsync()
        };

        return Ok(stats);
    }
}

