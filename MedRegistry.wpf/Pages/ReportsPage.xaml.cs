using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReportsPage.xaml
    /// </summary>
    public partial class ReportsPage : Page
    {
        public ObservableCollection<DoctorStatsView> DoctorStats { get; set; } = new();

        public ReportsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using var db = new MedRegistryContext();

            // Статистика врачей по специализациям
            DoctorStats = new ObservableCollection<DoctorStatsView>(
                db.Doctors.Include(d => d.Specialization)
                .GroupBy(d => d.Specialization.Name)
                .Select(g => new DoctorStatsView
                {
                    Specialization = g.Key ?? "Без специализации",
                    Count = g.Count()
                }).ToList()
            );
            
            DoctorStatsControl.ItemsSource = DoctorStats;

            // Общая статистика
            var today = DateTime.Today;
            var totalDoctors = db.Doctors.Count();
            var totalPatients = db.Patients.Count();
            var totalAppointments = db.Appointments.Count();
            var appointmentsToday = db.Appointments.Count(a => a.AppointmentStart.Date == today);

            GeneralStatsPanel.Children.Clear();
            GeneralStatsPanel.Children.Add(CreateStatCard("👨‍⚕️", "Врачей", totalDoctors.ToString(), "#3498DB"));
            GeneralStatsPanel.Children.Add(CreateStatCard("👥", "Пациентов", totalPatients.ToString(), "#27AE60"));
            GeneralStatsPanel.Children.Add(CreateStatCard("📅", "Всего приёмов", totalAppointments.ToString(), "#9B59B6"));
            GeneralStatsPanel.Children.Add(CreateStatCard("📆", "Приёмов сегодня", appointmentsToday.ToString(), "#E67E22"));

            // Статистика по статусам приёмов
            var pending = db.Appointments.Count(a => a.Status == "Ожидает");
            var completed = db.Appointments.Count(a => a.Status == "Выполнено");
            var cancelled = db.Appointments.Count(a => a.Status == "Отменено");

            AppointmentStatsPanel.Children.Clear();
            AppointmentStatsPanel.Children.Add(CreateStatCard("⏳", "Ожидают", pending.ToString(), "#3498DB"));
            AppointmentStatsPanel.Children.Add(CreateStatCard("✅", "Выполнено", completed.ToString(), "#27AE60"));
            AppointmentStatsPanel.Children.Add(CreateStatCard("❌", "Отменено", cancelled.ToString(), "#E74C3C"));
        }

        private Border CreateStatCard(string icon, string title, string value, string color)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 15, 0),
                Padding = new Thickness(20, 15, 20, 15),
                MinWidth = 140
            };

            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            
            sp.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.9
            });

            border.Child = sp;
            return border;
        }
    }

    public class DoctorStatsView
    {
        public string Specialization { get; set; } = "";
        public int Count { get; set; }
    }
}
