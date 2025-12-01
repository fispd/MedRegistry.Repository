using DataLayer.Data;
using DataLayer.Models;
using MedRegistry.webapi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly MedRegistryContext _context;

    public SchedulesController(MedRegistryContext context)
    {
        _context = context;
    }

    // GET: api/schedules
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules(
        [FromQuery] int? doctorId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null)
    {
        var query = _context.Schedules
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .AsQueryable();

        if (doctorId.HasValue)
            query = query.Where(s => s.DoctorId == doctorId.Value);

        if (dateFrom.HasValue)
            query = query.Where(s => s.WorkDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.WorkDate <= dateTo.Value);

        var schedules = await query
            .OrderBy(s => s.WorkDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new ScheduleDto
            {
                ScheduleId = s.ScheduleId,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor.User.LastName + " " + s.Doctor.User.FirstName,
                CabinetNumber = s.Doctor.CabinetNumber,
                WorkDate = s.WorkDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.IsAvailable
            })
            .ToListAsync();

        return Ok(schedules);
    }

    // GET: api/schedules/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .Where(s => s.ScheduleId == id)
            .Select(s => new ScheduleDto
            {
                ScheduleId = s.ScheduleId,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor.User.LastName + " " + s.Doctor.User.FirstName,
                CabinetNumber = s.Doctor.CabinetNumber,
                WorkDate = s.WorkDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.IsAvailable
            })
            .FirstOrDefaultAsync();

        if (schedule == null)
            return NotFound();

        return Ok(schedule);
    }

    // POST: api/schedules
    [HttpPost]
    public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto dto)
    {
        // Проверяем, нет ли уже расписания на эту дату
        var exists = await _context.Schedules
            .AnyAsync(s => s.DoctorId == dto.DoctorId && s.WorkDate == dto.WorkDate);

        if (exists)
            return BadRequest("Расписание на эту дату уже существует");

        var schedule = new Schedule
        {
            DoctorId = dto.DoctorId,
            WorkDate = dto.WorkDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsAvailable = dto.IsAvailable
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.ScheduleId },
            new ScheduleDto { ScheduleId = schedule.ScheduleId });
    }

    // POST: api/schedules/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult> CreateSchedulesBulk(List<CreateScheduleDto> dtos)
    {
        var schedules = new List<Schedule>();
        var skipped = 0;

        foreach (var dto in dtos)
        {
            var exists = await _context.Schedules
                .AnyAsync(s => s.DoctorId == dto.DoctorId && s.WorkDate == dto.WorkDate);

            if (exists)
            {
                skipped++;
                continue;
            }

            schedules.Add(new Schedule
            {
                DoctorId = dto.DoctorId,
                WorkDate = dto.WorkDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAvailable = dto.IsAvailable
            });
        }

        _context.Schedules.AddRange(schedules);
        await _context.SaveChangesAsync();

        return Ok(new { Added = schedules.Count, Skipped = skipped });
    }

    // PUT: api/schedules/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, UpdateScheduleDto dto)
    {
        var schedule = await _context.Schedules.FindAsync(id);

        if (schedule == null)
            return NotFound();

        if (dto.WorkDate.HasValue)
            schedule.WorkDate = dto.WorkDate.Value;

        if (dto.StartTime.HasValue)
            schedule.StartTime = dto.StartTime.Value;

        if (dto.EndTime.HasValue)
            schedule.EndTime = dto.EndTime.Value;

        if (dto.IsAvailable.HasValue)
            schedule.IsAvailable = dto.IsAvailable.Value;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/schedules/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _context.Schedules.FindAsync(id);

        if (schedule == null)
            return NotFound();

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/schedules/cleanup/old
    [HttpDelete("cleanup/old")]
    public async Task<ActionResult> DeleteOldSchedules()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            
            // Удаляем только старое расписание (дата < сегодня)
            var oldSchedules = await _context.Schedules
                .Where(s => s.WorkDate < today)
                .ToListAsync();

            if (!oldSchedules.Any())
            {
                return Ok(new { Message = "Старое расписание отсутствует", DeletedCount = 0 });
            }

            var deletedCount = oldSchedules.Count;
            _context.Schedules.RemoveRange(oldSchedules);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Удалено записей: {deletedCount}", DeletedCount = deletedCount });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Ошибка при удалении старого расписания: {ex.Message}" });
        }
    }
}

