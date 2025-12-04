using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Окно редактирования расписания врача.
    /// </summary>
    public partial class EditScheduleWindow : Window
    {
        private readonly int _scheduleId;
        private Schedule? _schedule;
        private List<DoctorItem> _doctors = new();

        /// <summary>
        /// Вспомогательный класс для отображения врача в ComboBox.
        /// </summary>
        private class DoctorItem
        {
            public int DoctorId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        /// <summary>
        /// Конструктор окна.
        /// </summary>
        /// <param name="scheduleId">ID редактируемого расписания</param>
        public EditScheduleWindow(int scheduleId)
        {
            InitializeComponent();
            _scheduleId = scheduleId;
            LoadData();
        }

        /// <summary>
        /// Загружает данные расписания и список врачей.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using var db = new MedRegistryContext();

                // Загружаем расписание
                _schedule = db.Schedules
                    .Include(s => s.Doctor)
                    .ThenInclude(d => d.User)
                    .Include(s => s.Doctor)
                    .ThenInclude(d => d.Specialization)
                    .FirstOrDefault(s => s.ScheduleId == _scheduleId);

                if (_schedule == null)
                {
                    MessageBox.Show("Расписание не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Загружаем список врачей
                var doctors = db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .OrderBy(d => d.User.LastName)
                    .ToList();

                _doctors = doctors.Select(d => new DoctorItem
                {
                    DoctorId = d.DoctorId,
                    DisplayName = FormatDoctorName(d)
                }).ToList();

                DoctorComboBox.ItemsSource = _doctors;
                DoctorComboBox.DisplayMemberPath = "DisplayName";

                // Выбираем текущего врача
                var selectedDoctor = _doctors.FirstOrDefault(d => d.DoctorId == _schedule.DoctorId);
                DoctorComboBox.SelectedItem = selectedDoctor;

                // Заполняем остальные поля
                DatePicker.SelectedDate = _schedule.StartTime.Date;
                StartTimeBox.Text = _schedule.StartTime.ToString("HH:mm");
                EndTimeBox.Text = _schedule.EndTime.ToString("HH:mm");
                IsAvailableCheckBox.IsChecked = _schedule.IsAvailable;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
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
        /// Сохраняет изменения расписания.
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

                if (!TimeSpan.TryParse(StartTimeBox.Text, out var startTime) ||
                    !TimeSpan.TryParse(EndTimeBox.Text, out var endTime))
                {
                    MessageBox.Show("Введите корректное время в формате ЧЧ:ММ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var date = DatePicker.SelectedDate ?? DateTime.Today;

                if (endTime <= startTime)
                {
                    MessageBox.Show("Время окончания должно быть позже времени начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем запись
                var schedule = db.Schedules.Find(_scheduleId);
                if (schedule == null)
                {
                    MessageBox.Show("Расписание не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                schedule.DoctorId = doctorItem.DoctorId;
                schedule.WorkDate = DateOnly.FromDateTime(date);
                schedule.StartTime = date.Date.Add(startTime);
                schedule.EndTime = date.Date.Add(endTime);
                schedule.IsAvailable = IsAvailableCheckBox.IsChecked ?? true;

                db.SaveChanges();
                MessageBox.Show("Расписание обновлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
