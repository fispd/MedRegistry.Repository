using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Media;

namespace MedRegistryApp.wpf.Windows.New
{
    /// <summary>
    /// Логика взаимодействия для RepeatAppointmentWindow.xaml
    /// </summary>
    public partial class RepeatAppointmentWindow : Window
    {
        private readonly int _patientId;
        private readonly int _doctorId;

        public RepeatAppointmentWindow(int patientId, int doctorId)
        {
            InitializeComponent();
            _patientId = patientId;
            _doctorId = doctorId;

            LoadPatientInfo();
            LoadTimeSlots();
            LoadReasons();
            
            // Устанавливаем минимальную дату - завтра
            AppointmentDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            AppointmentDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void LoadPatientInfo()
        {
            using var db = new MedRegistryContext();
            
            var patient = db.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientId == _patientId);

            if (patient?.User != null)
            {
                PatientNameText.Text = $"{patient.User.LastName} {patient.User.FirstName} {patient.User.MiddleName}";
                PatientPhoneText.Text = $"Тел: {patient.User.Phone ?? "не указан"}";
            }
        }

        private void LoadTimeSlots()
        {
            TimeComboBox.Items.Clear();
            
            // Генерируем слоты по 30 минут с 8:00 до 18:00
            for (int hour = 8; hour <= 17; hour++)
            {
                TimeComboBox.Items.Add($"{hour:D2}:00");
                TimeComboBox.Items.Add($"{hour:D2}:30");
            }
        }

        private void LoadReasons()
        {
            ReasonComboBox.ItemsSource = new[]
            {
                "Повторный осмотр",
                "Контрольный приём",
                "Проверка результатов лечения",
                "Коррекция лечения",
                "Снятие швов",
                "Получение результатов анализов",
                "Продление больничного",
                "Другое"
            };
            ReasonComboBox.SelectedIndex = 0;
        }

        private void CheckAvailability()
        {
            AvailabilityText.Text = "";
            
            var date = AppointmentDatePicker.SelectedDate;
            var timeStr = TimeComboBox.SelectedItem as string;

            if (date == null || string.IsNullOrEmpty(timeStr))
                return;

            if (!TimeSpan.TryParse(timeStr, out var time))
                return;

            var start = date.Value.Date + time;
            var end = start.AddMinutes(30);

            using var db = new MedRegistryContext();

            // Проверяем расписание врача
            bool isWorking = db.Schedules.Any(s =>
                s.DoctorId == _doctorId &&
                s.WorkDate == DateOnly.FromDateTime(date.Value) &&
                s.StartTime.TimeOfDay <= time &&
                s.EndTime.TimeOfDay >= time.Add(TimeSpan.FromMinutes(30)));

            // Проверяем занятость
            bool isTaken = db.Appointments.Any(a =>
                a.DoctorId == _doctorId &&
                a.AppointmentStart < end &&
                a.AppointmentEnd > start &&
                a.Status != "Отменено");

            if (!isWorking)
            {
                AvailabilityText.Text = "⚠️ Вы не работаете в это время";
                AvailabilityText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            else if (isTaken)
            {
                AvailabilityText.Text = "❌ Это время уже занято";
                AvailabilityText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            else
            {
                AvailabilityText.Text = "✅ Время свободно";
                AvailabilityText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CheckAvailability();
        }

        private void TimeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CheckAvailability();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var date = AppointmentDatePicker.SelectedDate;
            var timeStr = TimeComboBox.SelectedItem as string;
            var reason = ReasonComboBox.SelectedItem as string;

            if (date == null)
            {
                MessageBox.Show("Выберите дату приёма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(timeStr))
            {
                MessageBox.Show("Выберите время приёма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(timeStr, out var time))
            {
                MessageBox.Show("Некорректное время", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var start = date.Value.Date + time;
            var end = start.AddMinutes(30);

            using var db = new MedRegistryContext();

            // Проверяем расписание
            bool isWorking = db.Schedules.Any(s =>
                s.DoctorId == _doctorId &&
                s.WorkDate == DateOnly.FromDateTime(date.Value) &&
                s.StartTime.TimeOfDay <= time &&
                s.EndTime.TimeOfDay >= time.Add(TimeSpan.FromMinutes(30)));

            if (!isWorking)
            {
                MessageBox.Show("Вы не работаете в выбранное время. Проверьте своё расписание.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем занятость
            bool isTaken = db.Appointments.Any(a =>
                a.DoctorId == _doctorId &&
                a.AppointmentStart < end &&
                a.AppointmentEnd > start &&
                a.Status != "Отменено");

            if (isTaken)
            {
                MessageBox.Show("Выбранное время уже занято другим пациентом.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаём запись
            var appointment = new Appointment
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentStart = start,
                AppointmentEnd = end,
                Purpose = reason ?? "Повторный осмотр",
                Status = "Ожидает"
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();

            MessageBox.Show($"Пациент успешно записан на повторный приём!\n\n" +
                $"Дата: {start:dd.MM.yyyy}\n" +
                $"Время: {start:HH:mm} - {end:HH:mm}", 
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


