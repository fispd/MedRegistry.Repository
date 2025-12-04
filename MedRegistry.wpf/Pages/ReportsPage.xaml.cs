using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Страница отчётов и статистики.
    /// Отображает общую статистику системы, статистику по врачам и приёмам.
    /// </summary>
    public partial class ReportsPage : Page
    {
        public ObservableCollection<DoctorStatsView> DoctorStats { get; set; } = new();

        /// <summary>
        /// Конструктор страницы отчётов.
        /// </summary>
        public ReportsPage()
        {
            InitializeComponent();
            LoadData();
        }

        /// <summary>
        /// Загружает и отображает всю статистику.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using var db = new MedRegistryContext();

                LoadDoctorStats(db);
                LoadGeneralStats(db);
                LoadAppointmentStats(db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает статистику врачей по специализациям.
        /// </summary>
        private void LoadDoctorStats(MedRegistryContext db)
        {
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
        }

        /// <summary>
        /// Загружает общую статистику системы.
        /// </summary>
        private void LoadGeneralStats(MedRegistryContext db)
        {
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
        }

        /// <summary>
        /// Загружает статистику по статусам приёмов.
        /// </summary>
        private void LoadAppointmentStats(MedRegistryContext db)
        {
            var pending = db.Appointments.Count(a => a.Status == "Ожидает");
            var completed = db.Appointments.Count(a => a.Status == "Выполнено");
            var cancelled = db.Appointments.Count(a => a.Status == "Отменено");

            AppointmentStatsPanel.Children.Clear();
            AppointmentStatsPanel.Children.Add(CreateStatCard("⏳", "Ожидают", pending.ToString(), "#3498DB"));
            AppointmentStatsPanel.Children.Add(CreateStatCard("✅", "Выполнено", completed.ToString(), "#27AE60"));
            AppointmentStatsPanel.Children.Add(CreateStatCard("❌", "Отменено", cancelled.ToString(), "#E74C3C"));
        }

        /// <summary>
        /// Создаёт карточку статистики с иконкой и числовым значением.
        /// </summary>
        /// <param name="icon">Иконка (эмодзи)</param>
        /// <param name="title">Заголовок карточки</param>
        /// <param name="value">Числовое значение</param>
        /// <param name="color">Цвет фона в формате HEX</param>
        /// <returns>Border элемент карточки</returns>
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

    /// <summary>
    /// Модель представления для статистики врачей по специализациям.
    /// </summary>
    public class DoctorStatsView
    {
        public string Specialization { get; set; } = "";
        public int Count { get; set; }
    }
}
