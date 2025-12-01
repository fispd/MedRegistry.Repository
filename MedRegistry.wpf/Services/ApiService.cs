using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MedRegistry.wpf.Services;

/// <summary>
/// Сервис для работы с Web API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000/api";

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    #region Doctors

    public async Task<List<DoctorApiModel>?> GetDoctorsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<DoctorApiModel>>("doctors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return null;
        }
    }

    public async Task<DoctorApiModel?> GetDoctorAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DoctorApiModel>($"doctors/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<SpecializationApiModel>?> GetSpecializationsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<SpecializationApiModel>>("doctors/specializations");
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Appointments

    public async Task<List<AppointmentApiModel>?> GetAppointmentsAsync(
        string? status = null,
        int? doctorId = null,
        int? patientId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
            if (doctorId.HasValue) query.Add($"doctorId={doctorId}");
            if (patientId.HasValue) query.Add($"patientId={patientId}");
            if (dateFrom.HasValue) query.Add($"dateFrom={dateFrom:yyyy-MM-dd}");
            if (dateTo.HasValue) query.Add($"dateTo={dateTo:yyyy-MM-dd}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
            return await _httpClient.GetFromJsonAsync<List<AppointmentApiModel>>($"appointments{queryString}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateAppointmentStatusAsync(int id, string status)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"appointments/{id}/status", status);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateAppointmentAsync(CreateAppointmentApiModel appointment)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("appointments", appointment);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Schedules

    public async Task<List<ScheduleApiModel>?> GetSchedulesAsync(
        int? doctorId = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null)
    {
        try
        {
            var query = new List<string>();
            if (doctorId.HasValue) query.Add($"doctorId={doctorId}");
            if (dateFrom.HasValue) query.Add($"dateFrom={dateFrom:yyyy-MM-dd}");
            if (dateTo.HasValue) query.Add($"dateTo={dateTo:yyyy-MM-dd}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
            return await _httpClient.GetFromJsonAsync<List<ScheduleApiModel>>($"schedules{queryString}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CreateScheduleAsync(CreateScheduleApiModel schedule)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("schedules", schedule);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BulkResultApiModel?> CreateSchedulesBulkAsync(List<CreateScheduleApiModel> schedules)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("schedules/bulk", schedules);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BulkResultApiModel>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"schedules/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message, int? DeletedCount)> DeleteOldSchedulesAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("schedules/cleanup/old");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DeleteOldSchedulesResultApiModel>();
                return (true, result?.Message ?? "Очистка выполнена", result?.DeletedCount);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, $"Ошибка: {errorContent}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка при удалении старого расписания: {ex.Message}", null);
        }
    }

    #endregion

    #region Auth

    public async Task<LoginResponseApiModel?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new { Username = username, Password = password });
            return await response.Content.ReadFromJsonAsync<LoginResponseApiModel>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<LoginResponseApiModel?> RegisterAsync(RegisterApiModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", model);
            return await response.Content.ReadFromJsonAsync<LoginResponseApiModel>();
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Statistics

    public async Task<StatisticsApiModel?> GetStatisticsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StatisticsApiModel>("statistics");
        }
        catch
        {
            return null;
        }
    }

    #endregion

    /// <summary>
    /// Проверка доступности API
    /// </summary>
    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("statistics");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

#region API Models

public class DoctorApiModel
{
    public int DoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Specialization { get; set; }
    public int? WorkExperienceYears { get; set; }
    public string? CabinetNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class SpecializationApiModel
{
    public int SpecializationId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AppointmentApiModel
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
    public string? CabinetNumber { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentEnd { get; set; }
    public string? Status { get; set; }
    public string? Purpose { get; set; }
}

public class CreateAppointmentApiModel
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentEnd { get; set; }
    public string? Purpose { get; set; }
}

public class ScheduleApiModel
{
    public int ScheduleId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? CabinetNumber { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool? IsAvailable { get; set; }
}

public class CreateScheduleApiModel
{
    public int DoctorId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class BulkResultApiModel
{
    public int Added { get; set; }
    public int Skipped { get; set; }
}

public class UserApiModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? MedicalPolicy { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class LoginResponseApiModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserApiModel? User { get; set; }
}

public class RegisterApiModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? MedicalPolicy { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
}

public class StatisticsApiModel
{
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public int AppointmentsToday { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public List<SpecializationStatsApiModel> DoctorsBySpecialization { get; set; } = new();
}

public class SpecializationStatsApiModel
{
    public string Specialization { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DeleteOldSchedulesResultApiModel
{
    public string? Message { get; set; }
    public int? DeletedCount { get; set; }
}

#endregion

