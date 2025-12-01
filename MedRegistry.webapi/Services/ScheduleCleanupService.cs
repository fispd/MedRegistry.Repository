using DataLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Services;

public class ScheduleCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduleCleanupService> _logger;
    private DateOnly? _lastCleanupDate;

    public ScheduleCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ScheduleCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Небольшая задержка при запуске, чтобы дать приложению полностью инициализироваться
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var today = DateOnly.FromDateTime(now);
                
                // Проверяем, является ли сегодня воскресенье
                if (now.DayOfWeek == DayOfWeek.Sunday)
                {
                    // Выполняем очистку, если еще не выполняли сегодня
                    if (_lastCleanupDate != today)
                    {
                        // Выполняем очистку в начале воскресенья (между 00:00 и 06:00)
                        if (now.Hour >= 0 && now.Hour < 6)
                        {
                            await CleanupSchedulesAsync();
                            _lastCleanupDate = today;
                            
                            _logger.LogInformation(
                                "Автоматическая очистка расписания запланирована на каждое воскресенье. Последняя очистка: {Date}", 
                                today);
                        }
                    }
                }
                else
                {
                    // Сбрасываем флаг, если уже не воскресенье, чтобы подготовиться к следующему воскресенью
                    if (_lastCleanupDate.HasValue && _lastCleanupDate.Value < today)
                    {
                        _lastCleanupDate = null;
                    }
                }

                // Проверяем каждый час
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении очистки расписания");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task CleanupSchedulesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MedRegistryContext>();

            var today = DateOnly.FromDateTime(DateTime.Now);
            
            // Удаляем только старое расписание (дата < сегодня)
            var oldSchedules = await context.Schedules
                .Where(s => s.WorkDate < today)
                .ToListAsync();
            
            if (oldSchedules.Any())
            {
                context.Schedules.RemoveRange(oldSchedules);
                await context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Автоматическая очистка старого расписания выполнена. Удалено записей: {Count}", 
                    oldSchedules.Count);
            }
            else
            {
                _logger.LogInformation("Старое расписание отсутствует, очистка не требуется");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении старого расписания");
            throw;
        }
    }
}

