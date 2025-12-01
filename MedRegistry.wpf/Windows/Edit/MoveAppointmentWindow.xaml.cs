using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MedRegistry.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для MoveAppointmentWindow.xaml
    /// </summary>
    public partial class MoveAppointmentWindow : Window
    {
        private int _appointmentId;
        private int _doctorId;

        private DateTime ScheduleStart;
        private DateTime ScheduleEnd;

        public MoveAppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
            LoadData();
        }

        private void LoadData()
        {
            using var db = new MedRegistryContext();

            var appt = db.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.AppointmentId == _appointmentId);

            if (appt == null)
            {
                MessageBox.Show("Запись не найдена");
                Close();
                return;
            }

            _doctorId = appt.DoctorId;

            DateBox.SelectedDate = appt.AppointmentStart.Date;

            // загрузить время работы врача на этот день
            LoadScheduleForDate(appt.AppointmentStart.Date);

            // выбрать текущее время
            TimeBox.SelectedItem = appt.AppointmentStart.ToString("HH:mm");
        }

        private void LoadScheduleForDate(DateTime date)
        {
            using var db = new MedRegistryContext();

            var schedule = db.Schedules
                .FirstOrDefault(s =>
                    s.DoctorId == _doctorId &&
                    s.WorkDate == DateOnly.FromDateTime(date)
                );

            TimeBox.Items.Clear();

            if (schedule == null)
            {
                MessageBox.Show("У врача нет рабочего расписания на выбранную дату!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeBox.IsEnabled = false;
                return;
            }

            if (schedule.IsAvailable == false)
            {
                MessageBox.Show("Врач недоступен в выбранный день!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeBox.IsEnabled = false;
                return;
            }

            // Правильно устанавливаем время расписания для выбранной даты
            var scheduleStartTime = schedule.StartTime.TimeOfDay;
            var scheduleEndTime = schedule.EndTime.TimeOfDay;
            ScheduleStart = date.Date + scheduleStartTime;
            ScheduleEnd = date.Date + scheduleEndTime;

            TimeBox.IsEnabled = true;

            // Интервал 30 минут
            var interval = TimeSpan.FromMinutes(30);
            var t = ScheduleStart;

            while (t < ScheduleEnd)
            {
                TimeBox.Items.Add(t.ToString("HH:mm"));
                t = t.Add(interval);
            }
        }

        private void DateBox_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateBox.SelectedDate != null)
            {
                LoadScheduleForDate(DateBox.SelectedDate.Value);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DateBox.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату.");
                return;
            }

            if (TimeBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите время.");
                return;
            }

            var selectedStartTime = TimeSpan.Parse(TimeBox.SelectedItem.ToString());

            using var db = new MedRegistryContext();

            var appt = db.Appointments
                .FirstOrDefault(a => a.AppointmentId == _appointmentId);

            if (appt == null)
            {
                MessageBox.Show("Ошибка: запись не найдена.");
                return;
            }

            // Используем выбранную дату
            var selectedDate = DateBox.SelectedDate.Value;
            var newStart = selectedDate.Date + selectedStartTime;
            var duration = appt.AppointmentEnd - appt.AppointmentStart;
            var newEnd = newStart + duration;

            // --- 1. Проверка расписания на выбранную дату ---
            var scheduleForDate = db.Schedules
                .FirstOrDefault(s =>
                    s.DoctorId == _doctorId &&
                    s.WorkDate == DateOnly.FromDateTime(selectedDate)
                );

            if (scheduleForDate == null)
            {
                MessageBox.Show("У врача нет рабочего расписания на выбранную дату!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (scheduleForDate.IsAvailable == false)
            {
                MessageBox.Show("Врач недоступен в выбранный день!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем время начала и конца рабочего дня из расписания
            var scheduleStartTime = scheduleForDate.StartTime.TimeOfDay;
            var scheduleEndTime = scheduleForDate.EndTime.TimeOfDay;
            var scheduleStart = selectedDate.Date + scheduleStartTime;
            var scheduleEnd = selectedDate.Date + scheduleEndTime;

            // Проверка входит ли время в график врача
            if (newStart < scheduleStart || newEnd > scheduleEnd)
            {
                MessageBox.Show("Выбранное время не входит в рабочее расписание врача!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- 2. Проверка занятости врача на выбранную дату ---
            bool doctorBusy = db.Appointments.Any(a =>
                a.DoctorId == appt.DoctorId &&
                a.AppointmentId != appt.AppointmentId &&
                a.Status != "Отменено" &&
                DateOnly.FromDateTime(a.AppointmentStart.Date) == DateOnly.FromDateTime(selectedDate) &&
                !(newEnd <= a.AppointmentStart || newStart >= a.AppointmentEnd)
            );

            if (doctorBusy)
            {
                MessageBox.Show("Врач уже занят в это время!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- 3. Проверка занятости пациента на выбранную дату ---
            bool patientBusy = db.Appointments.Any(a =>
                a.PatientId == appt.PatientId &&
                a.AppointmentId != appt.AppointmentId &&
                a.Status != "Отменено" &&
                DateOnly.FromDateTime(a.AppointmentStart.Date) == DateOnly.FromDateTime(selectedDate) &&
                !(newEnd <= a.AppointmentStart || newStart >= a.AppointmentEnd)
            );

            if (patientBusy)
            {
                MessageBox.Show("У пациента уже есть запись в это время!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            appt.AppointmentStart = newStart;
            appt.AppointmentEnd = newEnd;

            db.SaveChanges();

            MessageBox.Show("Запись успешно перенесена!",
                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
    }
}
