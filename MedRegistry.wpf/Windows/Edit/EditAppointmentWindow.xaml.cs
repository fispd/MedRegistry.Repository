using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Окно редактирования записи на приём.
    /// </summary>
    public partial class EditAppointmentWindow : Window
    {
        private readonly int _appointmentId;
        private List<PatientItem> _patients = new();
        private List<DoctorItem> _doctors = new();

        /// <summary>
        /// Вспомогательный класс для отображения пациента.
        /// </summary>
        private class PatientItem
        {
            public int PatientId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        /// <summary>
        /// Вспомогательный класс для отображения врача.
        /// </summary>
        private class DoctorItem
        {
            public int DoctorId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        /// <summary>
        /// Конструктор окна.
        /// </summary>
        /// <param name="appointmentId">ID редактируемой записи</param>
        public EditAppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
            LoadData();
        }

        /// <summary>
        /// Загружает данные записи, списки пациентов и врачей.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using var db = new MedRegistryContext();
                var appointment = db.Appointments.Find(_appointmentId);

                if (appointment == null)
                {
                    MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Загружаем пациентов
                var patients = db.Patients.Include(p => p.User).OrderBy(p => p.User.LastName).ToList();
                _patients = patients.Select(p => new PatientItem
                {
                    PatientId = p.PatientId,
                    DisplayName = $"{p.User?.LastName} {p.User?.FirstName} {p.User?.MiddleName}".Trim()
                }).ToList();

                PatientComboBox.ItemsSource = _patients;
                PatientComboBox.DisplayMemberPath = "DisplayName";
                PatientComboBox.SelectedItem = _patients.FirstOrDefault(p => p.PatientId == appointment.PatientId);

                // Загружаем врачей
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
                DoctorComboBox.SelectedItem = _doctors.FirstOrDefault(d => d.DoctorId == appointment.DoctorId);

                // Заполняем остальные поля
                StartDatePicker.SelectedDate = appointment.AppointmentStart.Date;
                StartTimeBox.Text = appointment.AppointmentStart.ToString("HH:mm");
                PurposeBox.Text = appointment.Purpose;
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
        /// Сохраняет изменения записи.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new MedRegistryContext();
                var appointment = db.Appointments.Find(_appointmentId);

                if (appointment == null)
                {
                    MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var patientItem = PatientComboBox.SelectedItem as PatientItem;
                var doctorItem = DoctorComboBox.SelectedItem as DoctorItem;

                if (patientItem == null || doctorItem == null)
                {
                    MessageBox.Show("Выберите пациента и врача", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(StartTimeBox.Text, out var time))
                {
                    MessageBox.Show("Введите корректное время в формате ЧЧ:ММ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StartDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                appointment.PatientId = patientItem.PatientId;
                appointment.DoctorId = doctorItem.DoctorId;
                appointment.AppointmentStart = StartDatePicker.SelectedDate.Value.Date.Add(time);
                appointment.AppointmentEnd = appointment.AppointmentStart.AddMinutes(30);
                appointment.Purpose = PurposeBox.Text;

                db.SaveChanges();
                MessageBox.Show("Запись обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Удаляет запись на приём.
        /// </summary>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить запись?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new MedRegistryContext();
                    var appointment = db.Appointments.Find(_appointmentId);
                    if (appointment != null)
                    {
                        db.Appointments.Remove(appointment);
                        db.SaveChanges();
                        MessageBox.Show("Запись удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
