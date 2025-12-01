using ClosedXML.Excel;
using DataLayer.Data;
using DataLayer.Models;
using MedRegistryApp.wpf.Windows.Edit;
using MedRegistryApp.wpf.Windows.New;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : Page
    {
        private int _userId;
        private string _role;
        private List<Schedule> _allSchedules = new();

        public SchedulePage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            this.Loaded += (s, e) =>
            {
                bool hasAccess = _role == "Администратор" || _role == "Регистратор";
                
                if (ButtonsPanel != null)
                    ButtonsPanel.Visibility = hasAccess ? Visibility.Visible : Visibility.Collapsed;
            };
            
            LoadDoctorsFilter();
            LoadSchedule();
        }

        private void LoadDoctorsFilter()
        {
            using var db = new MedRegistryContext();
            
            var doctors = db.Doctors
                .Include(d => d.User)
                .OrderBy(d => d.User.LastName)
                .ToList();

            DoctorFilter.Items.Clear();
            DoctorFilter.Items.Add("Все врачи");
            
            foreach (var d in doctors)
            {
                DoctorFilter.Items.Add($"{d.User?.LastName} {d.User?.FirstName} (ID: {d.DoctorId})");
            }
            
            DoctorFilter.SelectedIndex = 0;
        }

        public void LoadSchedule()
        {
            try
            {
                using var db = new MedRegistryContext();

                _allSchedules = db.Schedules
                    .Include(s => s.Doctor)
                        .ThenInclude(d => d.User)
                    .OrderBy(s => s.WorkDate)
                    .ThenBy(s => s.Doctor.User.LastName)
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке расписания: {ex.Message}\n\nДетали: {ex.InnerException?.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allSchedules.AsEnumerable();

            if (DoctorFilter.SelectedIndex > 0)
            {
                var selectedText = DoctorFilter.SelectedItem?.ToString() ?? "";
                var idStart = selectedText.LastIndexOf(" ");
                if (idStart >= 0)
                {
                    var idStr = selectedText.Substring(idStart + 5).TrimEnd(' ');
                    if (int.TryParse(idStr, out int doctorId))
                    {
                        filtered = filtered.Where(s => s.DoctorId == doctorId);
                    }
                }
            }

            if (DateFromFilter.SelectedDate.HasValue)
            {
                var dateFrom = DateOnly.FromDateTime(DateFromFilter.SelectedDate.Value);
                filtered = filtered.Where(s => s.WorkDate >= dateFrom);
            }

            if (DateToFilter.SelectedDate.HasValue)
            {
                var dateTo = DateOnly.FromDateTime(DateToFilter.SelectedDate.Value);
                filtered = filtered.Where(s => s.WorkDate <= dateTo);
            }

            var schedules = filtered.ToList();
            
            ScheduleCountText.Text = $"Найдено записей: {schedules.Count}";
            
            DisplaySchedules(schedules);
        }

        private void DisplaySchedules(List<Schedule> schedules)
        {
            using var db = new MedRegistryContext();
            
            var appointments = db.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.Status != "Отменено")
                .ToList();

            ScheduleWrapPanel.Children.Clear();

            if (schedules.Count == 0)
            {
                ScheduleWrapPanel.Children.Add(CreateEmptyMessage("Нет записей расписания"));
                return;
            }

            foreach (var s in schedules)
            {
                int intervalMinutes = 30;

                List<(DateTime Start, DateTime End)> slots = new();

                var t = s.StartTime;
                while (t < s.EndTime)
                {
                    slots.Add((t, t.AddMinutes(intervalMinutes)));
                    t = t.AddMinutes(intervalMinutes);
                }

                bool HasFreeSlots = false;

                foreach (var slot in slots)
                {
                    bool slotTaken = appointments.Any(a =>
                        a.DoctorId == s.DoctorId &&
                        a.Status != "Отменено" &&
                        DateOnly.FromDateTime(a.AppointmentStart) == s.WorkDate &&
                        a.AppointmentStart < slot.End &&
                        a.AppointmentEnd > slot.Start
                    );

                    if (!slotTaken)
                    {
                        HasFreeSlots = true;
                        break;
                    }
                }

                string availability = HasFreeSlots
                    ? "✅ Есть свободные талоны"
                    : "❌ Нет свободных талонов";

                Brush availabilityColor = HasFreeSlots 
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96)) 
                    : new SolidColorBrush(Color.FromRgb(231, 76, 60));

                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(8),
                    Padding = new Thickness(15),
                    Width = 290,
                    MinHeight = 200,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 315,
                        ShadowDepth = 3,
                        Opacity = 0.3,
                        BlurRadius = 5
                    }
                };

                var sp = new StackPanel();

                sp.Children.Add(new TextBlock
                {
                    Text = $"📅 {s.WorkDate:dd.MM.yyyy}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                    Margin = new Thickness(0, 0, 0, 10)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"👨‍⚕️ {s.Doctor?.User?.LastName} {s.Doctor?.User?.FirstName}",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"🏥 Кабинет: {s.Doctor?.CabinetNumber ?? "—"}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"⏰ {s.StartTime:HH:mm} - {s.EndTime:HH:mm}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = availability,
                    Foreground = availabilityColor,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var btnPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                if (_role == "Администратор" || _role == "Регистратор")
                {
                    var editBtn = new Button
                    {
                        Content = "✏️ Изменить",
                        Style = (Style)Application.Current.Resources["EditButtonStyle"]
                    };

                    editBtn.Click += (sender, e) =>
                    {
                        var editWindow = new EditScheduleWindow(s.ScheduleId);
                        editWindow.ShowDialog();
                        LoadSchedule();
                    };

                    var deleteBtn = new Button
                    {
                        Content = "🗑️ Удалить",
                        Style = (Style)Application.Current.Resources["DeleteButtonStyle"]
                    };

                    deleteBtn.Click += (sender, e) =>
                    {
                        var result = MessageBox.Show(
                            "Вы уверены, что хотите удалить это расписание?",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            using var dbDel = new MedRegistryContext();
                            var scheduleToDelete = dbDel.Schedules.Find(s.ScheduleId);

                            if (scheduleToDelete != null)
                            {
                                dbDel.Schedules.Remove(scheduleToDelete);
                                dbDel.SaveChanges();
                            }

                            LoadSchedule();
                        }
                    };

                    btnPanel.Children.Add(editBtn);
                    btnPanel.Children.Add(deleteBtn);
                }

                sp.Children.Add(btnPanel);
                border.Child = sp;

                ScheduleWrapPanel.Children.Add(border);
            }
        }

        private UIElement CreateEmptyMessage(string text)
        {
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(20),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private void ToggleFilter_Click(object sender, MouseButtonEventArgs e)
        {
            if (FilterPanel.Visibility == Visibility.Collapsed)
            {
                FilterPanel.Visibility = Visibility.Visible;
                FilterToggleIcon.Text = "▲";
                FilterToggleText.Text = "Скрыть фильтры";
            }
            else
            {
                FilterPanel.Visibility = Visibility.Collapsed;
                FilterToggleIcon.Text = "▼";
                FilterToggleText.Text = "Показать фильтры";
            }
        }

        private void DoctorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            DoctorFilter.SelectedIndex = 0;
            DateFromFilter.SelectedDate = null;
            DateToFilter.SelectedDate = null;
            ApplyFilters();
        }

        private void AddNewSchedule_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewScheduleWindow();
            window.ShowDialog();
            LoadSchedule();
        }

        /// <summary>
        /// Скачивание Excel шаблона для заполнения расписания
        /// </summary>
        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Шаблон_расписания_{DateTime.Now:yyyy-MM-dd}",
                    Title = "Сохранить шаблон расписания"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    
                    var worksheet = workbook.Worksheets.Add("Расписание");
                    
                    worksheet.Cell(1, 1).Value = "ID врача";
                    worksheet.Cell(1, 2).Value = "ФИО врача (для справки)";
                    worksheet.Cell(1, 3).Value = "Специализация";
                    worksheet.Cell(1, 4).Value = "Кабинет";
                    worksheet.Cell(1, 5).Value = "Дата работы (дд.мм.гггг)";
                    worksheet.Cell(1, 6).Value = "Время начала (чч:мм)";
                    worksheet.Cell(1, 7).Value = "Время окончания (чч:мм)";
                    worksheet.Cell(1, 8).Value = "Доступен (да/нет)";

                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    using var db = new MedRegistryContext();
                    var doctors = db.Doctors
                        .Include(d => d.User)
                        .Include(d => d.Specialization)
                        .ToList();

                    var doctorsSheet = workbook.Worksheets.Add("Справочник врачей");
                    doctorsSheet.Cell(1, 1).Value = "ID врача";
                    doctorsSheet.Cell(1, 2).Value = "ФИО";
                    doctorsSheet.Cell(1, 3).Value = "Специализация";
                    doctorsSheet.Cell(1, 4).Value = "Кабинет";

                    var doctorHeaderRange = doctorsSheet.Range(1, 1, 1, 4);
                    doctorHeaderRange.Style.Font.Bold = true;
                    doctorHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    doctorHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    doctorHeaderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    int row = 2;
                    foreach (var doctor in doctors)
                    {
                        doctorsSheet.Cell(row, 1).Value = doctor.DoctorId;
                        doctorsSheet.Cell(row, 2).Value = $"{doctor.User?.LastName} {doctor.User?.FirstName} {doctor.User?.MiddleName}".Trim();
                        doctorsSheet.Cell(row, 3).Value = doctor.Specialization?.Name ?? "";
                        doctorsSheet.Cell(row, 4).Value = doctor.CabinetNumber ?? "";
                        row++;
                    }

                    if (doctors.Any())
                    {
                        var firstDoctor = doctors.First();
                        worksheet.Cell(2, 1).Value = firstDoctor.DoctorId;
                        worksheet.Cell(2, 2).Value = $"{firstDoctor.User?.LastName} {firstDoctor.User?.FirstName}";
                        worksheet.Cell(2, 3).Value = firstDoctor.Specialization?.Name ?? "";
                        worksheet.Cell(2, 4).Value = firstDoctor.CabinetNumber ?? "";
                        worksheet.Cell(2, 5).Value = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
                        worksheet.Cell(2, 6).Value = "09:00";
                        worksheet.Cell(2, 7).Value = "17:00";
                        worksheet.Cell(2, 8).Value = "да";

                        var exampleRange = worksheet.Range(2, 1, 2, 8);
                        exampleRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }

                    var instructionSheet = workbook.Worksheets.Add("Инструкция");
                    instructionSheet.Cell(1, 1).Value = "Инструкция по заполнению шаблона расписания";
                    instructionSheet.Cell(1, 1).Style.Font.Bold = true;
                    instructionSheet.Cell(1, 1).Style.Font.FontSize = 14;

                    instructionSheet.Cell(3, 1).Value = "1. Перейдите на лист 'Расписание'";
                    instructionSheet.Cell(4, 1).Value = "2. В столбце 'ID врача' укажите ID врача из листа 'Справочник врачей'";
                    instructionSheet.Cell(5, 1).Value = "3. Столбцы 'ФИО врача', 'Специализация', 'Кабинет' заполняются автоматически при загрузке (можно оставить пустыми)";
                    instructionSheet.Cell(6, 1).Value = "4. Дата работы: формат дд.мм.гггг (например: 25.11.2025)";
                    instructionSheet.Cell(7, 1).Value = "5. Время начала и окончания: формат чч:мм (например: 09:00, 17:30)";
                    instructionSheet.Cell(8, 1).Value = "6. Доступен: введите 'да' или 'нет'";
                    instructionSheet.Cell(9, 1).Value = "7. Желтая строка - это пример, её можно удалить или изменить";
                    instructionSheet.Cell(10, 1).Value = "8. Добавляйте новые строки ниже заголовка для каждого дня расписания";

                    worksheet.Columns().AdjustToContents();
                    doctorsSheet.Columns().AdjustToContents();
                    instructionSheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveDialog.FileName);

                    MessageBox.Show(
                        $"Шаблон успешно сохранён!\n\nФайл: {saveDialog.FileName}\n\nОткройте файл, заполните расписание и загрузите обратно.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании шаблона: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузка и парсинг Excel документа с расписанием
        /// </summary>
        private void UploadDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                    Title = "Выберите файл с расписанием"
                };

                if (openDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook(openDialog.FileName);
                    
                    // Ищем лист "Расписание"
                    var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Расписание");
                    if (worksheet == null)
                    {
                        worksheet = workbook.Worksheets.First();
                    }

                    var schedules = new List<Schedule>();
                    var errors = new List<string>();
                    var skipped = new List<string>();
                    int successCount = 0;

                    using var db = new MedRegistryContext();
                    var doctors = db.Doctors.Include(d => d.User).ToList();
                    
                    var existingSchedules = db.Schedules.ToList();

                    int row = 2;
                    while (!worksheet.Cell(row, 1).IsEmpty() || !worksheet.Cell(row, 5).IsEmpty())
                    {
                        try
                        {
                            var doctorIdCell = worksheet.Cell(row, 1).GetString().Trim();
                            if (string.IsNullOrEmpty(doctorIdCell))
                            {
                                row++;
                                continue;
                            }

                            if (!int.TryParse(doctorIdCell, out int doctorId))
                            {
                                errors.Add($"Строка {row}: Неверный ID врача '{doctorIdCell}'");
                                row++;
                                continue;
                            }

                            var doctor = doctors.FirstOrDefault(d => d.DoctorId == doctorId);
                            if (doctor == null)
                            {
                                errors.Add($"Строка {row}: Врач с ID {doctorId} не найден");
                                row++;
                                continue;
                            }

                            var dateStr = worksheet.Cell(row, 5).GetString().Trim();
                            if (!DateTime.TryParseExact(dateStr, new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy" }, 
                                null, System.Globalization.DateTimeStyles.None, out DateTime workDate))
                            {
                                if (worksheet.Cell(row, 5).TryGetValue(out DateTime excelDate))
                                {
                                    workDate = excelDate;
                                }
                                else
                                {
                                    errors.Add($"Строка {row}: Неверный формат даты '{dateStr}'");
                                    row++;
                                    continue;
                                }
                            }

                            var startTimeStr = worksheet.Cell(row, 6).GetString().Trim();
                            if (!TimeSpan.TryParse(startTimeStr, out TimeSpan startTime))
                            {
                                errors.Add($"Строка {row}: Неверный формат времени начала '{startTimeStr}'");
                                row++;
                                continue;
                            }

                            var endTimeStr = worksheet.Cell(row, 7).GetString().Trim();
                            if (!TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
                            {
                                errors.Add($"Строка {row}: Неверный формат времени окончания '{endTimeStr}'");
                                row++;
                                continue;
                            }

                            var availableStr = worksheet.Cell(row, 8).GetString().Trim().ToLower();
                            bool isAvailable = availableStr == "да" || availableStr == "yes" || availableStr == "1" || availableStr == "true";

                            var startDateTime = workDate.Date.Add(startTime);
                            var endDateTime = workDate.Date.Add(endTime);

                            if (endDateTime <= startDateTime)
                            {
                                errors.Add($"Строка {row}: Время окончания должно быть больше времени начала");
                                row++;
                                continue;
                            }

                            var scheduleWorkDate = DateOnly.FromDateTime(workDate);

                            var existsInDb = existingSchedules.Any(s => 
                                s.DoctorId == doctorId && s.WorkDate == scheduleWorkDate);

                            if (existsInDb)
                            {
                                var doctorName = $"{doctor.User?.LastName} {doctor.User?.FirstName}";
                                skipped.Add($"Строка {row}: {doctorName} уже имеет расписание на {scheduleWorkDate:dd.MM.yyyy}");
                                row++;
                                continue;
                            }

                            var existsInImport = schedules.Any(s => 
                                s.DoctorId == doctorId && s.WorkDate == scheduleWorkDate);

                            if (existsInImport)
                            {
                                var doctorName = $"{doctor.User?.LastName} {doctor.User?.FirstName}";
                                skipped.Add($"Строка {row}: Дубликат в файле - {doctorName} на {scheduleWorkDate:dd.MM.yyyy}");
                                row++;
                                continue;
                            }

                            var schedule = new Schedule
                            {
                                DoctorId = doctorId,
                                WorkDate = scheduleWorkDate,
                                StartTime = startDateTime,
                                EndTime = endDateTime,
                                IsAvailable = isAvailable
                            };

                            schedules.Add(schedule);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Строка {row}: Ошибка обработки - {ex.Message}");
                        }

                        row++;
                    }

                    if (schedules.Count == 0)
                    {
                        var errorMessage = "Не найдено записей для импорта.";
                        
                        if (skipped.Any())
                        {
                            errorMessage += $"\n\n⚠️ Пропущено (уже существуют): {skipped.Count}\n{string.Join("\n", skipped.Take(10))}";
                            if (skipped.Count > 10)
                                errorMessage += $"\n... и ещё {skipped.Count - 10}";
                        }
                        
                        if (errors.Any())
                        {
                            errorMessage += $"\n\n❌ Ошибки: {errors.Count}\n{string.Join("\n", errors.Take(10))}";
                            if (errors.Count > 10)
                                errorMessage += $"\n... и ещё {errors.Count - 10}";
                        }
                        
                        if (!skipped.Any() && !errors.Any())
                        {
                            errorMessage += "\nПроверьте, что файл заполнен правильно.";
                        }
                        
                        MessageBox.Show(errorMessage, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var confirmMessage = $"✅ Найдено записей для импорта: {schedules.Count}";
                    
                    if (skipped.Any())
                    {
                        confirmMessage += $"\n\n⚠️ Пропущено (уже существуют): {skipped.Count}";
                        confirmMessage += $"\n{string.Join("\n", skipped.Take(5))}";
                        if (skipped.Count > 5)
                            confirmMessage += $"\n... и ещё {skipped.Count - 5}";
                    }
                    
                    if (errors.Any())
                    {
                        confirmMessage += $"\n\n❌ Ошибки ({errors.Count}):\n{string.Join("\n", errors.Take(5))}";
                        if (errors.Count > 5)
                            confirmMessage += $"\n... и ещё {errors.Count - 5}";
                    }
                    
                    confirmMessage += "\n\nДобавить расписание в базу данных?";

                    var result = MessageBox.Show(confirmMessage, "Подтверждение импорта", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        db.Schedules.AddRange(schedules);
                        db.SaveChanges();

                        MessageBox.Show($"Успешно добавлено {schedules.Count} записей расписания!", 
                            "Импорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadSchedule();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}\n\nДетали: {ex.InnerException?.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очистка старого расписания (дата < сегодня)
        /// </summary>
        private async void CleanupOldSchedules_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить все старое расписание (дата < сегодня)?\n\nЭто действие нельзя отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Используем прямое подключение к БД, так как WPF использует прямой доступ к БД, а не API
                using var db = new MedRegistryContext();
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                var oldSchedules = db.Schedules
                    .Where(s => s.WorkDate < today)
                    .ToList();

                if (!oldSchedules.Any())
                {
                    MessageBox.Show("Старое расписание отсутствует.", 
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var deletedCount = oldSchedules.Count;
                db.Schedules.RemoveRange(oldSchedules);
                db.SaveChanges();

                MessageBox.Show($"Успешно удалено записей старого расписания: {deletedCount}", 
                    "Очистка выполнена", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSchedule();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении старого расписания: {ex.Message}\n\nДетали: {ex.InnerException?.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
