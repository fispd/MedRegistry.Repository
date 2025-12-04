using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.New
{
    /// <summary>
    /// Окно создания нового расписания для врача.
    /// </summary>
    public partial class NewScheduleWindow : Window
    {
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
        public NewScheduleWindow()
        {
            InitializeComponent();
            LoadDoctors();
            LoadTimeComboboxes();
            DatePicker.SelectedDate = DateTime.Today;
        }

        /// <summary>
        /// Загружает список врачей в ComboBox.
        /// </summary>
        private void LoadDoctors()
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
                MessageBox.Show($"Ошибка при загрузке врачей: {ex.Message}", 
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
            if (!string.IsNullOrEmpty(doctor.CabinetNumber))
            {
                name += $" - каб. {doctor.CabinetNumber}";
            }
            return name;
        }

        /// <summary>
        /// Загружает временные слоты в ComboBox.
        /// </summary>
        private void LoadTimeComboboxes()
        {
            StartTimeComboBox.Items.Clear();
            EndTimeComboBox.Items.Clear();

            var start = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(20, 0, 0);

            while (start <= end)
            {
                var timeStr = start.ToString(@"hh\:mm");
                StartTimeComboBox.Items.Add(timeStr);
                EndTimeComboBox.Items.Add(timeStr);
                start = start.Add(TimeSpan.FromMinutes(15));
            }

            StartTimeComboBox.SelectedIndex = 0;
            EndTimeComboBox.SelectedIndex = EndTimeComboBox.Items.Count - 1;
        }

        /// <summary>
        /// Сохраняет новое расписание.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new MedRegistryContext();

                var doctorItem = DoctorComboBox.SelectedItem as DoctorItem;
                if (doctorItem == null)
                {
                    MessageBox.Show("Выберите врача", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StartTimeComboBox.SelectedItem == null || EndTimeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите время начала и окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var date = DatePicker.SelectedDate.Value;
                var startTime = TimeSpan.Parse(StartTimeComboBox.SelectedItem.ToString()!);
                var endTime = TimeSpan.Parse(EndTimeComboBox.SelectedItem.ToString()!);

                var start = date.Date.Add(startTime);
                var end = date.Date.Add(endTime);

                if (end <= start)
                {
                    MessageBox.Show("Время окончания должно быть позже времени начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка на существующее расписание
                var workDate = DateOnly.FromDateTime(date);
                var existingSchedule = db.Schedules
                    .FirstOrDefault(s => s.DoctorId == doctorItem.DoctorId && s.WorkDate == workDate);

                if (existingSchedule != null)
                {
                    MessageBox.Show(
                        $"У этого врача уже есть расписание на {workDate:dd.MM.yyyy}.\n\n" +
                        $"Существующее время: {existingSchedule.StartTime:HH:mm} - {existingSchedule.EndTime:HH:mm}\n\n" +
                        "Если хотите изменить расписание, отредактируйте существующую запись.",
                        "Расписание уже существует", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newSchedule = new Schedule
                {
                    DoctorId = doctorItem.DoctorId,
                    StartTime = start,
                    EndTime = end,
                    IsAvailable = true,
                    WorkDate = workDate
                };

                db.Schedules.Add(newSchedule);
                db.SaveChanges();

                MessageBox.Show("Расписание добавлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
