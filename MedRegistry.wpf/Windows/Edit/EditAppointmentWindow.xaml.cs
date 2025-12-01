using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для EditAppointmentWindow.xaml
    /// </summary>
    public partial class EditAppointmentWindow : Window
    {
        private readonly int _appointmentId;

        public EditAppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
            LoadData();
        }

        private void LoadData()
        {
            using var db = new MedRegistryContext();
            var appointment = db.Appointments.Find(_appointmentId);

            PatientComboBox.ItemsSource = db.Patients.Include(p => p.User).ToList();
            PatientComboBox.DisplayMemberPath = "User.FirstName";
            PatientComboBox.SelectedItem = db.Patients.FirstOrDefault(p => p.PatientId == appointment.PatientId);

            DoctorComboBox.ItemsSource = db.Doctors.Include(d => d.User).ToList();
            DoctorComboBox.DisplayMemberPath = "User.FirstName";
            DoctorComboBox.SelectedItem = db.Doctors.FirstOrDefault(d => d.DoctorId == appointment.DoctorId);

            StartDatePicker.SelectedDate = appointment.AppointmentStart.Date;
            StartTimeBox.Text = appointment.AppointmentStart.ToString("HH:mm");
            PurposeBox.Text = appointment.Purpose;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();
            var appointment = db.Appointments.Find(_appointmentId);

            if (!(PatientComboBox.SelectedItem is Patient patient) || !(DoctorComboBox.SelectedItem is Doctor doctor))
            {
                ShowError("Выберите пациента и врача");
                return;
            }

            if (!DateTime.TryParse(StartTimeBox.Text, out var time))
            {
                ShowError("Введите корректное время");
                return;
            }

            appointment.PatientId = patient.PatientId;
            appointment.DoctorId = doctor.DoctorId;
            appointment.AppointmentStart = StartDatePicker.SelectedDate?.Add(time.TimeOfDay) ?? DateTime.Now;
            appointment.AppointmentEnd = appointment.AppointmentStart.AddMinutes(30);
            appointment.Purpose = PurposeBox.Text;

            db.SaveChanges();
            MessageBox.Show("Запись обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void ShowError(string msg)
        {
            MessageBox.Show("Ошибка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using var db = new MedRegistryContext();
                var appointment = db.Appointments.Find(_appointmentId);
                if (appointment != null)
                {
                    db.Appointments.Remove(appointment);
                    db.SaveChanges();
                    MessageBox.Show("Запись удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                this.Close();
            }
        }
    }
}
