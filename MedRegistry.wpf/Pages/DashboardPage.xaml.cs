using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadStats();
            LoadAppointments();
        }

        private void LoadStats()
        {
            using var db = new MedRegistryContext();
            
            var today = DateTime.Today;
            var appointmentsToday = db.Appointments
                .Count(a => a.AppointmentStart.Date == today);
            
            var totalDoctors = db.Doctors.Count();
            var totalPatients = db.Patients.Count();
            var pendingAppointments = db.Appointments
                .Count(a => a.Status == "Ожидает" && a.AppointmentStart >= DateTime.Now);

            StatsPanel.Children.Clear();
            
            StatsPanel.Children.Add(CreateStatCard("📅", "Приёмов сегодня", appointmentsToday.ToString(), "#3498DB"));
            StatsPanel.Children.Add(CreateStatCard("⏳", "Ожидают приёма", pendingAppointments.ToString(), "#E67E22"));
            StatsPanel.Children.Add(CreateStatCard("👨‍⚕️", "Врачей", totalDoctors.ToString(), "#27AE60"));
            StatsPanel.Children.Add(CreateStatCard("👥", "Пациентов", totalPatients.ToString(), "#9B59B6"));
        }

        private Border CreateStatCard(string icon, string title, string value, string color)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 15, 0),
                Padding = new Thickness(20, 15, 20, 15),
                MinWidth = 150
            };

            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            
            sp.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 28,
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

        private void LoadAppointments()
        {
            using var db = new MedRegistryContext();
            var appointments = db.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.AppointmentStart >= DateTime.Now && a.Status != "Отменено")
                .OrderBy(a => a.AppointmentStart)
                .Take(6)
                .ToList();

            AppointmentsWrapPanel.Children.Clear();

            if (appointments.Count == 0)
            {
                AppointmentsWrapPanel.Children.Add(new TextBlock
                {
                    Text = "Нет ближайших приёмов",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 0)
                });
                return;
            }

            foreach (var a in appointments)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(15),
                    Width = 280,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 315,
                        ShadowDepth = 2,
                        Opacity = 0.2,
                        BlurRadius = 4
                    }
                };

                var sp = new StackPanel();

                // Дата и время
                sp.Children.Add(new TextBlock
                {
                    Text = $"📅 {a.AppointmentStart:dd.MM.yyyy} в {a.AppointmentStart:HH:mm}",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                // Врач
                sp.Children.Add(new TextBlock
                {
                    Text = $"👨‍⚕️ {a.Doctor?.User?.LastName} {a.Doctor?.User?.FirstName}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                // Пациент
                sp.Children.Add(new TextBlock
                {
                    Text = $"👤 {a.Patient?.User?.LastName} {a.Patient?.User?.FirstName}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                // Кабинет
                sp.Children.Add(new TextBlock
                {
                    Text = $"🏥 Кабинет: {a.Doctor?.CabinetNumber ?? "—"}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                // Статус
                var statusColor = a.Status switch
                {
                    "Ожидает" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    "Выполнено" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                    "Отменено" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    _ => Brushes.Gray
                };

                sp.Children.Add(new TextBlock
                {
                    Text = $"Статус: {a.Status ?? "Ожидает"}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = statusColor,
                    Margin = new Thickness(0, 5, 0, 0)
                });

                border.Child = sp;
                AppointmentsWrapPanel.Children.Add(border);
            }
        }
    }
}
