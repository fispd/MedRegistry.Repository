using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Окно создания новой записи на приём.
    /// Позволяет пациенту выбрать врача, дату и время визита.
    /// </summary>
    public partial class NewAppointmentWindow : Window
    {
        private readonly int _userId;
        private List<DoctorItem> _doctors = new();

        /// <summary>
        /// Вспомогательный класс для отображения врача в ComboBox.
        /// </summary>
        private class DoctorItem
        {
            public int DoctorId { get; set; }
            public string DisplayName { get; set; } = "";
            public Doctor? Doctor { get; set; }
        }

        /// <summary>
        /// Конструктор окна.
        /// </summary>
        /// <param name="userId">ID текущего пользователя (пациента)</param>
        public NewAppointmentWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadData();
            LoadTimeSlots();
            LoadPurposes();
        }

        /// <summary>
        /// Загружает список врачей в ComboBox.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using var db = new MedRegistryContext();

                var doctors = db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .OrderBy(d => d.User.LastName)
                    .ToList();

                _doctors = doctors.Select(d => new DoctorItem
                {
                    DoctorId = d.DoctorId,
                    DisplayName = FormatDoctorName(d),
                    Doctor = d
                }).ToList();

                DoctorComboBox.ItemsSource = _doctors;
                DoctorComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Форматирует отображаемое имя врача.
        /// </summary>
        private string FormatDoctorName(Doctor doctor)
        {
            var name = $"{doctor.User?.LastName} {doctor.User?.FirstName}";
            if (!string.IsNullOrEmpty(doctor.Specialization?.Name))
            {
                name += $" ({doctor.Specialization.Name})";
            }
            return name;
        }

        /// <summary>
        /// Загружает временные слоты в ComboBox.
        /// </summary>
        private void LoadTimeSlots()
        {
            StartTimeComboBox.Items.Clear();
            for (int hour = 8; hour <= 17; hour++)
            {
                for (int min = 0; min < 60; min += 15)
                {
                    StartTimeComboBox.Items.Add($"{hour:D2}:{min:D2}");
                }
            }
        }

        /// <summary>
        /// Загружает список причин обращения.
        /// </summary>
        private void LoadPurposes()
        {
            PurposeComboBox.ItemsSource = new[]
            {
                "Консультация",
                "Первичный осмотр",
                "Повторный приём",
                "Сдача анализов",
                "Получение справки",
                "Выписка рецепта",
                "Вакцинация",
                "Плановый осмотр",
                "Другое"
            };
            PurposeComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Проверяет доступность выбранного времени у врача.
        /// </summary>
        private void CheckAvailability()
        {
            AvailabilityTextBlock.Text = "";
            
            var doctorItem = DoctorComboBox.SelectedItem as DoctorItem;
            var date = StartDatePicker.SelectedDate;
            var timeStr = StartTimeComboBox.SelectedItem as string;

            if (doctorItem == null || date == null || string.IsNullOrEmpty(timeStr))
                return;

            if (!TimeSpan.TryParse(timeStr, out var time))
                return;

            var start = date.Value.Date + time;
            var end = start.AddMinutes(30);

            using var db = new MedRegistryContext();

            // Проверка занятости врача
            bool isTaken = db.Appointments.Any(a =>
                a.DoctorId == doctorItem.DoctorId &&
                a.AppointmentStart < end &&
                a.AppointmentEnd > start &&
                a.Status != "Отменено");

            // Проверка расписания врача
            bool isWorking = db.Schedules.Any(s =>
                s.DoctorId == doctorItem.DoctorId &&
                s.WorkDate == DateOnly.FromDateTime(date.Value) &&
                s.StartTime.TimeOfDay <= time &&
                s.EndTime.TimeOfDay >= time.Add(TimeSpan.FromMinutes(30)));

            if (!isWorking)
            {
                AvailabilityTextBlock.Text = "⚠️ Врач не работает в это время";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (isTaken)
            {
                AvailabilityTextBlock.Text = "❌ Врач занят в это время";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                AvailabilityTextBlock.Text = "✅ Свободно";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void DoctorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();
        private void StartDatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();
        private void StartTimeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();

        /// <summary>
        /// Сохраняет новую запись на приём.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doctorItem = DoctorComboBox.SelectedItem as DoctorItem;
                var date = StartDatePicker.SelectedDate;
                var timeStr = StartTimeComboBox.SelectedItem as string;
                var purpose = PurposeComboBox.SelectedItem as string;

                if (doctorItem == null || date == null || string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(purpose))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // Получаем пациента
                var user = db.Users.Include(u => u.Patient).FirstOrDefault(u => u.UserId == _userId);
                if (user == null || user.RoleId != 4 || user.Patient == null)
                {
                    MessageBox.Show("Текущий пользователь не является пациентом", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var patientId = user.Patient.PatientId;

                // Проверка расписания врача
                bool isWorking = db.Schedules.Any(s =>
                    s.DoctorId == doctorItem.DoctorId &&
                    s.WorkDate == DateOnly.FromDateTime(date.Value) &&
                    s.StartTime.TimeOfDay <= time &&
                    s.EndTime.TimeOfDay >= time.Add(TimeSpan.FromMinutes(30)));

                // Проверка занятости
                bool isTaken = db.Appointments.Any(a =>
                    a.DoctorId == doctorItem.DoctorId &&
                    a.AppointmentStart < end &&
                    a.AppointmentEnd > start &&
                    a.Status != "Отменено");

                if (!isWorking)
                {
                    MessageBox.Show("Врач не работает в выбранное время.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (isTaken)
                {
                    MessageBox.Show("Врач занят в выбранное время.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorItem.DoctorId,
                    AppointmentStart = start,
                    AppointmentEnd = end,
                    Purpose = purpose,
                    Status = "Ожидает"
                };

                db.Appointments.Add(appointment);
                db.SaveChanges();

                MessageBox.Show("Вы успешно записались на приём!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
