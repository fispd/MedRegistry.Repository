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
    /// Страница управления расписанием врачей.
    /// Позволяет просматривать, фильтровать, добавлять, редактировать и удалять записи расписания.
    /// Поддерживает импорт/экспорт данных из Excel.
    /// </summary>
    public partial class SchedulePage : Page
    {
        private int _userId;
        private string _role;
        private List<Schedule> _allSchedules = new();
        private Dictionary<int, int> _doctorFilterMap = new(); // Индекс в ComboBox -> DoctorId

        /// <summary>
        /// Конструктор страницы расписания.
        /// Инициализирует компоненты и загружает данные.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль пользователя (Администратор, Регистратор, Врач, Пациент)</param>
        public SchedulePage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            this.Loaded += OnPageLoaded;
            
            LoadDoctorsFilter();
            LoadSchedule();
        }

        /// <summary>
        /// Обработчик загрузки страницы.
        /// Настраивает видимость элементов в зависимости от роли пользователя.
        /// </summary>
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Панель кнопок управления доступна только администраторам и регистраторам
            bool hasAccess = _role == "Администратор" || _role == "Регистратор";
            
            if (ButtonsPanel != null)
                ButtonsPanel.Visibility = hasAccess ? Visibility.Visible : Visibility.Collapsed;
            
            // Врач видит только своё расписание, поэтому фильтр по врачам скрыт
            if (_role == "Врач")
            {
                DoctorFilterPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Загружает список врачей в фильтр.
        /// Создаёт словарь соответствия индексов ComboBox и ID врачей.
        /// </summary>
        private void LoadDoctorsFilter()
        {
            try
            {
                using var db = new MedRegistryContext();
                
                var doctors = db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .OrderBy(d => d.User.LastName)
                    .ToList();

                DoctorFilter.Items.Clear();
                _doctorFilterMap.Clear();
                
                DoctorFilter.Items.Add("Все врачи");
                _doctorFilterMap[0] = 0; // Индекс 0 = все врачи
                
                int index = 1;
                foreach (var d in doctors)
                {
                    // Формируем отображаемое имя без ID
                    var specialization = d.Specialization?.Name ?? "";
                    var displayName = $"{d.User?.LastName} {d.User?.FirstName}";
                    if (!string.IsNullOrEmpty(specialization))
                    {
                        displayName += $" ({specialization})";
                    }
                    
                    DoctorFilter.Items.Add(displayName);
                    _doctorFilterMap[index] = d.DoctorId;
                    index++;
                }
                
                DoctorFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка врачей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает расписание из базы данных.
        /// Для роли "Врач" загружает только его личное расписание.
        /// </summary>
        public void LoadSchedule()
        {
            try
            {
                using var db = new MedRegistryContext();

                var query = db.Schedules
                    .Include(s => s.Doctor)
                        .ThenInclude(d => d.User)
                    .Include(s => s.Doctor)
                        .ThenInclude(d => d.Specialization)
                    .AsQueryable();

                // Врач видит только своё расписание
                if (_role == "Врач")
                {
                    var doctor = db.Doctors.FirstOrDefault(d => d.UserId == _userId);
                    if (doctor != null)
                    {
                        query = query.Where(s => s.DoctorId == doctor.DoctorId);
                    }
                    else
                    {
                        _allSchedules = new List<Schedule>();
                        ApplyFilters();
                        return;
                    }
                }

                _allSchedules = query
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

        /// <summary>
        /// Применяет фильтры к загруженному расписанию.
        /// Фильтрует по врачу и диапазону дат.
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                var filtered = _allSchedules.AsEnumerable();

                // Фильтр по врачу (используем словарь для получения ID)
                if (DoctorFilter.SelectedIndex > 0 && _doctorFilterMap.TryGetValue(DoctorFilter.SelectedIndex, out int doctorId))
                {
                    filtered = filtered.Where(s => s.DoctorId == doctorId);
                }

                // Фильтр по начальной дате
                if (DateFromFilter.SelectedDate.HasValue)
                {
                    var dateFrom = DateOnly.FromDateTime(DateFromFilter.SelectedDate.Value);
                    filtered = filtered.Where(s => s.WorkDate >= dateFrom);
                }

                // Фильтр по конечной дате
                if (DateToFilter.SelectedDate.HasValue)
                {
                    var dateTo = DateOnly.FromDateTime(DateToFilter.SelectedDate.Value);
                    filtered = filtered.Where(s => s.WorkDate <= dateTo);
                }

                var schedules = filtered.ToList();
                
                ScheduleCountText.Text = $"Найдено записей: {schedules.Count}";
                
                DisplaySchedules(schedules);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Отображает карточки расписания на странице.
        /// Проверяет наличие свободных слотов для каждой записи.
        /// </summary>
        /// <param name="schedules">Список записей расписания для отображения</param>
        private void DisplaySchedules(List<Schedule> schedules)
        {
            using var db = new MedRegistryContext();
            
            // Загружаем активные записи на приём для проверки занятости слотов
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
                // Проверяем наличие свободных слотов (по 30 минут)
                bool hasFreeSlots = CheckFreeSlots(s, appointments);

                var card = CreateScheduleCard(s, hasFreeSlots);
                ScheduleWrapPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// Проверяет наличие свободных временных слотов в расписании.
        /// </summary>
        /// <param name="schedule">Запись расписания</param>
        /// <param name="appointments">Список существующих записей на приём</param>
        /// <returns>True если есть свободные слоты</returns>
        private bool CheckFreeSlots(Schedule schedule, List<Appointment> appointments)
        {
            int intervalMinutes = 30;
            var currentTime = schedule.StartTime;
            
            while (currentTime < schedule.EndTime)
            {
                var slotEnd = currentTime.AddMinutes(intervalMinutes);
                
                bool slotTaken = appointments.Any(a =>
                    a.DoctorId == schedule.DoctorId &&
                    a.Status != "Отменено" &&
                    DateOnly.FromDateTime(a.AppointmentStart) == schedule.WorkDate &&
                    a.AppointmentStart < slotEnd &&
                    a.AppointmentEnd > currentTime
                );

                if (!slotTaken)
                    return true;
                    
                currentTime = currentTime.AddMinutes(intervalMinutes);
            }
            
            return false;
        }

        /// <summary>
        /// Создаёт визуальную карточку записи расписания.
        /// </summary>
        /// <param name="schedule">Данные расписания</param>
        /// <param name="hasFreeSlots">Наличие свободных слотов</param>
        /// <returns>Border элемент с содержимым карточки</returns>
        private Border CreateScheduleCard(Schedule schedule, bool hasFreeSlots)
        {
            string availability = hasFreeSlots
                ? "✅ Есть свободные талоны"
                : "❌ Нет свободных талонов";

            Brush availabilityColor = hasFreeSlots 
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

            // Дата
            sp.Children.Add(new TextBlock
            {
                Text = $"📅 {schedule.WorkDate:dd.MM.yyyy}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // ФИО врача
            sp.Children.Add(new TextBlock
            {
                Text = $"👨‍⚕️ {schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Кабинет
            sp.Children.Add(new TextBlock
            {
                Text = $"🏥 Кабинет: {schedule.Doctor?.CabinetNumber ?? "—"}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Время работы
            sp.Children.Add(new TextBlock
            {
                Text = $"⏰ {schedule.StartTime:HH:mm} - {schedule.EndTime:HH:mm}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Статус доступности
            sp.Children.Add(new TextBlock
            {
                Text = availability,
                Foreground = availabilityColor,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Кнопки управления (только для админа и регистратора)
            var btnPanel = CreateScheduleButtons(schedule);
            sp.Children.Add(btnPanel);
            
            border.Child = sp;
            return border;
        }

        /// <summary>
        /// Создаёт панель кнопок для карточки расписания.
        /// </summary>
        /// <param name="schedule">Данные расписания</param>
        /// <returns>Панель с кнопками</returns>
        private WrapPanel CreateScheduleButtons(Schedule schedule)
        {
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
                    var editWindow = new EditScheduleWindow(schedule.ScheduleId);
                    editWindow.ShowDialog();
                    LoadSchedule();
                };

                var deleteBtn = new Button
                {
                    Content = "🗑️ Удалить",
                    Style = (Style)Application.Current.Resources["DeleteButtonStyle"]
                };
                deleteBtn.Click += (sender, e) => DeleteSchedule(schedule.ScheduleId);

                btnPanel.Children.Add(editBtn);
                btnPanel.Children.Add(deleteBtn);
            }

            return btnPanel;
        }

        /// <summary>
        /// Удаляет запись расписания после подтверждения.
        /// </summary>
        /// <param name="scheduleId">ID записи для удаления</param>
        private void DeleteSchedule(int scheduleId)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить это расписание?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var db = new MedRegistryContext();
                var scheduleToDelete = db.Schedules.Find(scheduleId);

                if (scheduleToDelete != null)
                {
                    db.Schedules.Remove(scheduleToDelete);
                    db.SaveChanges();
                }

                LoadSchedule();
            }
        }

        /// <summary>
        /// Создаёт сообщение для отображения при отсутствии данных.
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <returns>UI элемент с сообщением</returns>
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

        /// <summary>
        /// Переключает видимость панели фильтров.
        /// </summary>
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

        /// <summary>
        /// Обработчик изменения выбора врача в фильтре.
        /// </summary>
        private void DoctorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        /// <summary>
        /// Обработчик изменения даты в фильтре.
        /// </summary>
        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        /// <summary>
        /// Сбрасывает все фильтры к значениям по умолчанию.
        /// </summary>
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            DoctorFilter.SelectedIndex = 0;
            DateFromFilter.SelectedDate = null;
            DateToFilter.SelectedDate = null;
            ApplyFilters();
        }

        /// <summary>
        /// Открывает окно добавления нового расписания.
        /// </summary>
        private void AddNewSchedule_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewScheduleWindow();
            window.ShowDialog();
            LoadSchedule();
        }

        /// <summary>
        /// Создаёт и сохраняет Excel-шаблон для импорта расписания.
        /// Включает листы: Расписание, Справочник врачей, Инструкция.
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

                if (saveDialog.ShowDialog() != true) return;

                using var workbook = new XLWorkbook();
                
                // Создаём лист расписания
                var worksheet = workbook.Worksheets.Add("Расписание");
                CreateScheduleTemplateHeaders(worksheet);

                // Создаём справочник врачей
                using var db = new MedRegistryContext();
                var doctors = db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .ToList();

                var doctorsSheet = workbook.Worksheets.Add("Справочник врачей");
                CreateDoctorsReference(doctorsSheet, doctors);

                // Добавляем пример в расписание
                if (doctors.Any())
                {
                    AddScheduleExample(worksheet, doctors.First());
                }

                // Создаём инструкцию
                var instructionSheet = workbook.Worksheets.Add("Инструкция");
                CreateInstructions(instructionSheet);

                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();
                doctorsSheet.Columns().AdjustToContents();
                instructionSheet.Columns().AdjustToContents();

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show(
                    $"Шаблон успешно сохранён!\n\nФайл: {saveDialog.FileName}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании шаблона: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Создаёт заголовки для листа расписания в Excel.
        /// </summary>
        private void CreateScheduleTemplateHeaders(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = "ID врача";
            worksheet.Cell(1, 2).Value = "ФИО врача (для справки)";
            worksheet.Cell(1, 3).Value = "Специализация";
            worksheet.Cell(1, 4).Value = "Кабинет";
            worksheet.Cell(1, 5).Value = "Дата работы (дд.мм.гггг)";
            worksheet.Cell(1, 6).Value = "Время начала (чч:мм)";
            worksheet.Cell(1, 7).Value = "Время окончания (чч:мм)";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        /// <summary>
        /// Создаёт справочник врачей в Excel.
        /// </summary>
        private void CreateDoctorsReference(IXLWorksheet sheet, List<Doctor> doctors)
        {
            sheet.Cell(1, 1).Value = "ID врача";
            sheet.Cell(1, 2).Value = "ФИО";
            sheet.Cell(1, 3).Value = "Специализация";
            sheet.Cell(1, 4).Value = "Кабинет";

            var headerRange = sheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var doctor in doctors)
            {
                sheet.Cell(row, 1).Value = doctor.DoctorId;
                sheet.Cell(row, 2).Value = $"{doctor.User?.LastName} {doctor.User?.FirstName} {doctor.User?.MiddleName}".Trim();
                sheet.Cell(row, 3).Value = doctor.Specialization?.Name ?? "";
                sheet.Cell(row, 4).Value = doctor.CabinetNumber ?? "";
                row++;
            }
        }

        /// <summary>
        /// Добавляет пример заполнения в шаблон расписания.
        /// </summary>
        private void AddScheduleExample(IXLWorksheet worksheet, Doctor doctor)
        {
            worksheet.Cell(2, 1).Value = doctor.DoctorId;
            worksheet.Cell(2, 2).Value = $"{doctor.User?.LastName} {doctor.User?.FirstName}";
            worksheet.Cell(2, 3).Value = doctor.Specialization?.Name ?? "";
            worksheet.Cell(2, 4).Value = doctor.CabinetNumber ?? "";
            worksheet.Cell(2, 5).Value = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            worksheet.Cell(2, 6).Value = "09:00";
            worksheet.Cell(2, 7).Value = "17:00";

            var exampleRange = worksheet.Range(2, 1, 2, 7);
            exampleRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        /// <summary>
        /// Создаёт лист с инструкцией по заполнению шаблона.
        /// </summary>
        private void CreateInstructions(IXLWorksheet sheet)
        {
            sheet.Cell(1, 1).Value = "Инструкция по заполнению шаблона расписания";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            sheet.Cell(3, 1).Value = "1. Перейдите на лист 'Расписание'";
            sheet.Cell(4, 1).Value = "2. В столбце 'ID врача' укажите ID врача из листа 'Справочник врачей'";
            sheet.Cell(5, 1).Value = "3. Столбцы 'ФИО врача', 'Специализация', 'Кабинет' - для справки (можно оставить пустыми)";
            sheet.Cell(6, 1).Value = "4. Дата работы: формат дд.мм.гггг (например: 25.12.2025)";
            sheet.Cell(7, 1).Value = "5. Время начала и окончания: формат чч:мм (например: 09:00, 17:30)";
            sheet.Cell(8, 1).Value = "6. Желтая строка - это пример, её можно удалить или изменить";
        }

        /// <summary>
        /// Загружает расписание из Excel-файла и сохраняет в базу данных.
        /// Выполняет валидацию данных и проверку на дубликаты.
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

                if (openDialog.ShowDialog() != true) return;

                using var workbook = new XLWorkbook(openDialog.FileName);
                
                var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Расписание") 
                    ?? workbook.Worksheets.First();

                var importResult = ParseScheduleFromExcel(worksheet);
                
                if (importResult.Schedules.Count == 0)
                {
                    ShowImportErrors(importResult);
                    return;
                }

                if (ConfirmImport(importResult))
                {
                    SaveImportedSchedules(importResult.Schedules);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Парсит данные расписания из Excel листа.
        /// </summary>
        private (List<Schedule> Schedules, List<string> Errors, List<string> Skipped) ParseScheduleFromExcel(IXLWorksheet worksheet)
        {
            var schedules = new List<Schedule>();
            var errors = new List<string>();
            var skipped = new List<string>();

            using var db = new MedRegistryContext();
            var doctors = db.Doctors.Include(d => d.User).ToList();
            var existingSchedules = db.Schedules.ToList();

            int row = 2;
            while (!worksheet.Cell(row, 1).IsEmpty() || !worksheet.Cell(row, 5).IsEmpty())
            {
                var result = ParseScheduleRow(worksheet, row, doctors, existingSchedules, schedules);
                
                if (result.Error != null)
                    errors.Add(result.Error);
                else if (result.Skipped != null)
                    skipped.Add(result.Skipped);
                else if (result.Schedule != null)
                    schedules.Add(result.Schedule);

                row++;
            }

            return (schedules, errors, skipped);
        }

        /// <summary>
        /// Парсит одну строку расписания из Excel.
        /// </summary>
        private (Schedule? Schedule, string? Error, string? Skipped) ParseScheduleRow(
            IXLWorksheet worksheet, int row, List<Doctor> doctors, 
            List<Schedule> existingSchedules, List<Schedule> importedSchedules)
        {
            try
            {
                var doctorIdCell = worksheet.Cell(row, 1).GetString().Trim();
                if (string.IsNullOrEmpty(doctorIdCell))
                    return (null, null, null);

                if (!int.TryParse(doctorIdCell, out int doctorId))
                    return (null, $"Строка {row}: Неверный ID врача '{doctorIdCell}'", null);

                var doctor = doctors.FirstOrDefault(d => d.DoctorId == doctorId);
                if (doctor == null)
                    return (null, $"Строка {row}: Врач с ID {doctorId} не найден", null);

                // Парсинг даты
                var dateStr = worksheet.Cell(row, 5).GetString().Trim();
                if (!TryParseDate(worksheet.Cell(row, 5), dateStr, out DateTime workDate))
                    return (null, $"Строка {row}: Неверный формат даты '{dateStr}'", null);

                // Парсинг времени
                var startTimeStr = worksheet.Cell(row, 6).GetString().Trim();
                if (!TimeSpan.TryParse(startTimeStr, out TimeSpan startTime))
                    return (null, $"Строка {row}: Неверный формат времени начала '{startTimeStr}'", null);

                var endTimeStr = worksheet.Cell(row, 7).GetString().Trim();
                if (!TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
                    return (null, $"Строка {row}: Неверный формат времени окончания '{endTimeStr}'", null);

                var startDateTime = workDate.Date.Add(startTime);
                var endDateTime = workDate.Date.Add(endTime);

                if (endDateTime <= startDateTime)
                    return (null, $"Строка {row}: Время окончания должно быть больше времени начала", null);

                var scheduleWorkDate = DateOnly.FromDateTime(workDate);
                var doctorName = $"{doctor.User?.LastName} {doctor.User?.FirstName}";

                // Проверка на дубликаты
                if (existingSchedules.Any(s => s.DoctorId == doctorId && s.WorkDate == scheduleWorkDate))
                    return (null, null, $"Строка {row}: {doctorName} уже имеет расписание на {scheduleWorkDate:dd.MM.yyyy}");

                if (importedSchedules.Any(s => s.DoctorId == doctorId && s.WorkDate == scheduleWorkDate))
                    return (null, null, $"Строка {row}: Дубликат в файле - {doctorName} на {scheduleWorkDate:dd.MM.yyyy}");

                return (new Schedule
                {
                    DoctorId = doctorId,
                    WorkDate = scheduleWorkDate,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    IsAvailable = true
                }, null, null);
            }
            catch (Exception ex)
            {
                return (null, $"Строка {row}: Ошибка обработки - {ex.Message}", null);
            }
        }

        /// <summary>
        /// Пытается распарсить дату из ячейки Excel.
        /// </summary>
        private bool TryParseDate(IXLCell cell, string dateStr, out DateTime result)
        {
            if (DateTime.TryParseExact(dateStr, new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy" }, 
                null, System.Globalization.DateTimeStyles.None, out result))
                return true;

            return cell.TryGetValue(out result);
        }

        /// <summary>
        /// Показывает сообщение об ошибках импорта.
        /// </summary>
        private void ShowImportErrors((List<Schedule> Schedules, List<string> Errors, List<string> Skipped) result)
        {
            var message = "Не найдено записей для импорта.";
            
            if (result.Skipped.Any())
                message += $"\n\n⚠️ Пропущено: {result.Skipped.Count}\n{string.Join("\n", result.Skipped.Take(10))}";
            
            if (result.Errors.Any())
                message += $"\n\n❌ Ошибки: {result.Errors.Count}\n{string.Join("\n", result.Errors.Take(10))}";

            MessageBox.Show(message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Показывает диалог подтверждения импорта.
        /// </summary>
        private bool ConfirmImport((List<Schedule> Schedules, List<string> Errors, List<string> Skipped) result)
        {
            var message = $"✅ Найдено записей для импорта: {result.Schedules.Count}";
            
            if (result.Skipped.Any())
                message += $"\n\n⚠️ Пропущено: {result.Skipped.Count}";
            
            if (result.Errors.Any())
                message += $"\n\n❌ Ошибки: {result.Errors.Count}";
            
            message += "\n\nДобавить расписание в базу данных?";

            return MessageBox.Show(message, "Подтверждение импорта", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Сохраняет импортированные записи расписания в базу данных.
        /// </summary>
        private void SaveImportedSchedules(List<Schedule> schedules)
        {
            using var db = new MedRegistryContext();
            db.Schedules.AddRange(schedules);
            db.SaveChanges();

            MessageBox.Show($"Успешно добавлено {schedules.Count} записей расписания!", 
                "Импорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadSchedule();
        }

        /// <summary>
        /// Удаляет все записи расписания с прошедшими датами.
        /// </summary>
        private void CleanupOldSchedules_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить все старое расписание (дата < сегодня)?\n\nЭто действие нельзя отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

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

                MessageBox.Show($"Успешно удалено записей: {deletedCount}", 
                    "Очистка выполнена", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSchedule();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
