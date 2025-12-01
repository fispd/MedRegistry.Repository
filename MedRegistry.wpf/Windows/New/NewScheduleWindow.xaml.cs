using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.New
{
    /// <summary>
    /// Логика взаимодействия для NewScheduleWindow.xaml
    /// </summary>
    public partial class NewScheduleWindow : Window
    {
        public NewScheduleWindow()
        {
            InitializeComponent();
            LoadDoctors();
            LoadTimeComboboxes();
            DatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadDoctors()
        {
            using var db = new MedRegistryContext();
            DoctorComboBox.ItemsSource = db.Doctors.Include(d => d.User).ToList();
            DoctorComboBox.DisplayMemberPath = "User.FirstName";
        }

        private void LoadTimeComboboxes()
        {
            StartTimeComboBox.Items.Clear();
            EndTimeComboBox.Items.Clear();

            var start = new TimeSpan(8, 0, 0); // 08:00
            var end = new TimeSpan(20, 0, 0); // 20:00

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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();

            if (DoctorComboBox.SelectedItem is not Doctor doctor)
            {
                MessageBox.Show("Выберите врача");
                return;
            }

            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату");
                return;
            }

            if (StartTimeComboBox.SelectedItem == null || EndTimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите время начала и окончания");
                return;
            }

            var date = DatePicker.SelectedDate.Value;
            var startTime = TimeSpan.Parse(StartTimeComboBox.SelectedItem.ToString());
            var endTime = TimeSpan.Parse(EndTimeComboBox.SelectedItem.ToString());

            var start = date.Date.Add(startTime);
            var end = date.Date.Add(endTime);

            if (end <= start)
            {
                MessageBox.Show("Время окончания должно быть позже времени начала");
                return;
            }

            // Проверка на существующее расписание у этого врача на эту дату
            var workDate = DateOnly.FromDateTime(date);
            var existingSchedule = db.Schedules
                .FirstOrDefault(s => s.DoctorId == doctor.DoctorId && s.WorkDate == workDate);

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
                DoctorId = doctor.DoctorId,
                StartTime = start,
                EndTime = end,
                IsAvailable = true,
                WorkDate = workDate
            };

            db.Schedules.Add(newSchedule);
            db.SaveChanges();

            MessageBox.Show("Расписание добавлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
