using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для EditScheduleWindow.xaml
    /// </summary>
    public partial class EditScheduleWindow : Window
    {
        private readonly int _scheduleId;
        private Schedule _schedule;

        public EditScheduleWindow(int scheduleId)
        {
            InitializeComponent();
            _scheduleId = scheduleId;
            LoadData();
        }

        private void LoadData()
        {
            using var db = new MedRegistryContext();

            // Загружаем расписание
            _schedule = db.Schedules
                          .Include(s => s.Doctor)
                          .ThenInclude(d => d.User)
                          .FirstOrDefault(s => s.ScheduleId == _scheduleId);

            if (_schedule == null)
            {
                MessageBox.Show("Расписание не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Заполняем ComboBox врачами
            var doctors = db.Doctors.Include(d => d.User).ToList();
            DoctorComboBox.ItemsSource = doctors;

            // Выбираем текущего врача
            DoctorComboBox.SelectedItem = doctors.FirstOrDefault(d => d.DoctorId == _schedule.DoctorId);

            // Заполняем остальные поля
            DatePicker.SelectedDate = _schedule.StartTime.Date;
            StartTimeBox.Text = _schedule.StartTime.ToString("HH:mm");
            EndTimeBox.Text = _schedule.EndTime.ToString("HH:mm");
            IsAvailableCheckBox.IsChecked = _schedule.IsAvailable;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();

            var selectedDoctor = DoctorComboBox.SelectedItem as Doctor;
            if (selectedDoctor == null)
            {
                MessageBox.Show("Выберите врача", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateTime.TryParse(StartTimeBox.Text, out var startTime) ||
                !DateTime.TryParse(EndTimeBox.Text, out var endTime))
            {
                MessageBox.Show("Введите корректное время", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var date = DatePicker.SelectedDate ?? DateTime.Today;

            // Обновляем запись
            var schedule = db.Schedules.Find(_scheduleId);
            schedule.DoctorId = selectedDoctor.DoctorId;
            schedule.StartTime = date.Date.Add(startTime.TimeOfDay);
            schedule.EndTime = date.Date.Add(endTime.TimeOfDay);
            schedule.IsAvailable = IsAvailableCheckBox.IsChecked;

            db.SaveChanges();
            MessageBox.Show("Расписание обновлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
