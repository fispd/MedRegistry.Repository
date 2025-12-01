using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistry.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для NewMedicalRecordPage.xaml
    /// </summary>
    public partial class NewMedicalRecordPage : Page
    {
        private int _appointmentId;
        private int _patientId;
        private int _doctorId;

        public NewMedicalRecordPage(int appointmentId, int patientId, int doctorId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
            _patientId = patientId;
            _doctorId = doctorId;

            LoadAppointmentInfo();
        }

        private void LoadAppointmentInfo()
        {
            using var db = new MedRegistryContext();

            var appointment = db.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefault(a => a.AppointmentId == _appointmentId);

            if (appointment?.Patient?.User != null)
            {
                var user = appointment.Patient.User;
                PatientNameText.Text = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                AppointmentDateText.Text = appointment.AppointmentStart.ToString("dd MMMM yyyy, HH:mm");
            }
            else
            {
                PatientNameText.Text = "Не указан";
                AppointmentDateText.Text = DateTime.Now.ToString("dd MMMM yyyy");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DiagnosisBox.Text))
            {
                MessageBox.Show("Введите диагноз", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TreatmentBox.Text))
            {
                MessageBox.Show("Введите назначенное лечение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new MedRegistryContext();
            var record = new MedicalRecord
            {
                AppointmentId = _appointmentId,
                PatientId = _patientId,
                DoctorId = _doctorId,
                RecordDate = DateTime.Now,
                Diagnosis = DiagnosisBox.Text.Trim(),
                Treatment = TreatmentBox.Text.Trim(),
                Notes = NotesBox.Text?.Trim()
            };

            db.MedicalRecords.Add(record);
            db.SaveChanges();

            MessageBox.Show("Медицинский отчёт успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}
