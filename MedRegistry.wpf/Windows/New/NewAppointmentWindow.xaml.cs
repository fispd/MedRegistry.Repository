using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для NewAppointmentWindow.xaml
    /// </summary>
    public partial class NewAppointmentWindow : Window
    {
        private readonly int _userId;

        public NewAppointmentWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadData();
            LoadTimeSlots();
            LoadPurposes();
        }

        private void LoadData()
        {
            using var db = new MedRegistryContext();

            DoctorComboBox.ItemsSource = db.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .ToList();

            DoctorComboBox.DisplayMemberPath = "User.FirstName";
        }

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

        private void CheckAvailability()
        {
            AvailabilityTextBlock.Text = "";
            var doctor = DoctorComboBox.SelectedItem as Doctor;
            var date = StartDatePicker.SelectedDate;
            var timeStr = StartTimeComboBox.SelectedItem as string;

            if (doctor == null || date == null || string.IsNullOrEmpty(timeStr))
                return;

            if (!TimeSpan.TryParse(timeStr, out var time))
                return;

            var start = date.Value.Date + time;
            var end = start.AddMinutes(30);

            using var db = new MedRegistryContext();

            bool isTaken = db.Appointments.Any(a =>
                a.DoctorId == doctor.DoctorId &&
                a.AppointmentStart < end &&
                a.AppointmentEnd > start &&
                a.Status != "Отменено");

            bool isWorking = db.Schedules.Any(s =>
                s.DoctorId == doctor.DoctorId &&
                s.StartTime.Date <= date.Value.Date &&
                s.EndTime.Date >= date.Value.Date &&
                s.StartTime.TimeOfDay <= time &&
                s.EndTime.TimeOfDay >= time);

            if (!isWorking)
            {
                AvailabilityTextBlock.Text = "Врач не работает в это время";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (isTaken)
            {
                AvailabilityTextBlock.Text = "Врач занят в это время";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                AvailabilityTextBlock.Text = "Свободно";
                AvailabilityTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void DoctorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();
        private void StartDatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();
        private void StartTimeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => CheckAvailability();

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var doctor = DoctorComboBox.SelectedItem as Doctor;
            var date = StartDatePicker.SelectedDate;
            var timeStr = StartTimeComboBox.SelectedItem as string;
            var purpose = PurposeComboBox.SelectedItem as string;

            if (doctor == null || date == null || string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(purpose))
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

            var user = db.Users.Include(u => u.Patient).FirstOrDefault(u => u.UserId == _userId);
            if (user == null || user.RoleId != 4 || user.Patient == null)
            {
                MessageBox.Show("Текущий пользователь не является пациентом", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var patientId = user.Patient.PatientId;

            bool isWorking = db.Schedules.Any(s =>
                s.DoctorId == doctor.DoctorId &&
                s.StartTime.Date <= date.Value.Date &&
                s.EndTime.Date >= date.Value.Date &&
                s.StartTime.TimeOfDay <= time &&
                s.EndTime.TimeOfDay >= time);

            bool isTaken = db.Appointments.Any(a =>
                a.DoctorId == doctor.DoctorId &&
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
                DoctorId = doctor.DoctorId,
                AppointmentStart = start,
                AppointmentEnd = end,
                Purpose = purpose,
                Status = "Ожидает"
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();

            MessageBox.Show("Вы успешно записались на приём", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
