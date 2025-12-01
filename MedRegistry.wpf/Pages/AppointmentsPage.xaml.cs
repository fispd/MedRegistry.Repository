using MedRegistryApp.wpf.Windows.Edit;
using MedRegistryApp.wpf.Windows;
using MedRegistryApp.wpf.Windows.New;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using DataLayer.Data;
using MedRegistry.wpf.Pages;
using DataLayer.Models;
using MedRegistry.wpf.Windows.Edit;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для AppointmentsPage.xaml
    /// </summary>
    public partial class AppointmentsPage : Page
    {
        private int _userId;
        private string _role;

        public AppointmentsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            var addButton = this.FindName("AddAppointmentButton") as Button;
            if (addButton != null && (_role == "Пациент" || _role == "Врач"))
            {
                addButton.Visibility = Visibility.Collapsed;
            }
            
            LoadAppointments();
        }

        private void LoadAppointments()
        {
            using var db = new MedRegistryContext();

            IQueryable<Appointment> query = db.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User);

            if (_role == "Пациент")
            {
                query = query.Where(a => a.Patient.UserId == _userId);
            }
            else if (_role == "Врач")
            {
                query = query.Where(a => a.Doctor.UserId == _userId);
            }

            List<Appointment> appointments;
            
            // Для пациентов сортируем так, чтобы отмененные и выполненные записи были внизу
            if (_role == "Пациент")
            {
                appointments = query.OrderBy(a => 
                    (a.Status == "Отменено" || a.Status == "Выполнено") ? 1 : 0)
                    .ThenBy(a => a.AppointmentStart)
                    .ToList();
            }
            else
            {
                appointments = query.OrderBy(a => a.AppointmentStart).ToList();
            }
            
            DisplayAppointments(appointments);
        }

        private void DisplayAppointments(List<Appointment> appointments)
        {
            AppointmentsWrapPanel.Children.Clear();

            if (appointments.Count == 0)
            {
                AppointmentsWrapPanel.Children.Add(CreateEmptyMessage("Нет записей на приём"));
                return;
            }

            foreach (var a in appointments)
            {
                double cardWidth = 290;
                double cardHeight = 260;
                
                if (_role == "Администратор")
                {
                    cardHeight = 300;
                }
                else if (_role == "Регистратор")
                {
                    cardHeight = 220;
                }
                else if (_role == "Врач")
                {
                    cardHeight = 310;
                }
                else if (_role == "Пациент")
                {
                    cardHeight = 220;
                }

                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(8),
                    Padding = new Thickness(12),
                    Width = cardWidth,
                    MinHeight = cardHeight,
                    MaxWidth = cardWidth,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 315,
                        ShadowDepth = 3,
                        Opacity = 0.3,
                        BlurRadius = 5
                    }
                };

                var sp = new StackPanel();

                var headerText = new TextBlock
                {
                    Text = $"📅 {a.AppointmentStart:dd.MM.yyyy} в {a.AppointmentStart:HH:mm}",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                sp.Children.Add(headerText);

                if (_role == "Пациент")
                {
                    sp.Children.Add(CreateInfoTextBlock("👨‍⚕️ Врач", $"{a.Doctor?.User?.LastName} {a.Doctor?.User?.FirstName} {a.Doctor?.User?.MiddleName}"));
                }
                else if (_role == "Врач")
                {
                    sp.Children.Add(CreateInfoTextBlock("👤 Пациент", $"{a.Patient?.User?.LastName} {a.Patient?.User?.FirstName} {a.Patient?.User?.MiddleName}"));
                }
                else if (_role == "Регистратор" || _role == "Администратор")
                {
                    sp.Children.Add(CreateInfoTextBlock("👨‍⚕️ Врач", $"{a.Doctor?.User?.LastName} {a.Doctor?.User?.FirstName}"));
                    sp.Children.Add(CreateInfoTextBlock("👤 Пациент", $"{a.Patient?.User?.LastName} {a.Patient?.User?.FirstName}"));
                }

                sp.Children.Add(CreateInfoTextBlock("🏥 Кабинет", a.Doctor?.CabinetNumber?.ToString() ?? "—"));
                sp.Children.Add(CreateInfoTextBlock("⏰ Время", $"{a.AppointmentStart:HH:mm} - {a.AppointmentEnd:HH:mm}"));
                sp.Children.Add(CreateInfoTextBlock("📝 Причина", a.Purpose ?? "—"));
                
                // Статус не отображаем для пациентов
                if (_role != "Пациент")
                {
                    var statusColor = GetStatusColor(a.Status ?? "Ожидает");
                    var statusText = new TextBlock
                    {
                        Text = $"Статус: {a.Status ?? "Ожидает"}",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = statusColor,
                        Margin = new Thickness(0, 5, 0, 8)
                    };
                    sp.Children.Add(statusText);
                }

                var btnPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 4, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var editBtn = new Button
                {
                    Content = "✏️ Изменить",
                    Style = (Style)Application.Current.Resources["EditButtonStyle"]
                };
                editBtn.Click += (s, e) =>
                {
                    var editWindow = new EditAppointmentWindow(a.AppointmentId);
                    editWindow.ShowDialog();
                    LoadAppointments();
                };

                var cancelBtn = new Button
                {
                    Content = "❌ Отменить",
                    Style = (Style)Application.Current.Resources["CancelButtonStyle"]
                };
                cancelBtn.Click += (s, e) =>
                {
                    var result = MessageBox.Show("Вы уверены, что хотите отменить запись?", 
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateAppointmentStatus(a.AppointmentId, "Отменено");
                    }
                };

                var reportBtn = new Button
                {
                    Content = "📄 Отчёт",
                    Style = (Style)Application.Current.Resources["ReportButtonStyle"]
                };
                reportBtn.Click += (s, e) =>
                {
                    this.NavigationService?.Navigate(
                        new NewMedicalRecordPage(a.AppointmentId, a.PatientId, a.DoctorId)
                    );
                };

                var doneBtn = new Button
                {
                    Content = "✅ Выполнено",
                    Style = (Style)Application.Current.Resources["DoneButtonStyle"]
                };
                doneBtn.Click += (s, e) =>
                {
                    UpdateAppointmentStatus(a.AppointmentId, "Выполнено");
                };

                var moveBtn = new Button
                {
                    Content = "🔄 Перенести",
                    Style = (Style)Application.Current.Resources["MoveButtonStyle"]
                };
                moveBtn.Click += (s, e) =>
                {
                    var win = new MoveAppointmentWindow(a.AppointmentId);
                    if (win.ShowDialog() == true)
                        LoadAppointments();
                };

                var repeatBtn = new Button
                {
                    Content = "🔁 Повторный",
                    Style = (Style)Application.Current.Resources["EditButtonStyle"]
                };
                repeatBtn.Click += (s, ev) =>
                {
                    using var dbDoc = new MedRegistryContext();
                    var doctor = dbDoc.Doctors.FirstOrDefault(d => d.UserId == _userId);
                    if (doctor != null)
                    {
                        var repeatWindow = new RepeatAppointmentWindow(a.PatientId, doctor.DoctorId);
                        if (repeatWindow.ShowDialog() == true)
                        {
                            LoadAppointments();
                        }
                    }
                };

                var patientDocsBtn = new Button
                {
                    Content = "📋 Документы",
                    Style = (Style)Application.Current.Resources["ReportButtonStyle"]
                };
                patientDocsBtn.Click += (s, e) =>
                {
                    this.NavigationService?.Navigate(
                        new MedicalRecordsPage(_userId, _role, a.PatientId)
                    );
                };

                if (_role == "Пациент")
                {
                    // Скрываем кнопки, если запись отменена или выполнена
                    bool isCompletedOrCancelled = (a.Status == "Отменено" || a.Status == "Выполнено");
                    
                    if (!isCompletedOrCancelled)
                    {
                        btnPanel.Children.Add(moveBtn);
                        btnPanel.Children.Add(cancelBtn);
                    }
                    else
                    {
                        // Если запись отменена или выполнена, добавляем информационный текст вместо кнопок
                        var infoText = new TextBlock
                        {
                            Text = a.Status == "Отменено" 
                                ? "❌ Запись отменена" 
                                : "✅ Запись выполнена",
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = a.Status == "Отменено" 
                                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                                : new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                            Margin = new Thickness(0, 5, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        btnPanel.Children.Add(infoText);
                    }
                    
                    // Делаем карточку более блеклой, если запись отменена или выполнена
                    if (isCompletedOrCancelled)
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(230, 245, 245, 245)); // Светло-серый фон
                        border.Opacity = 0.75; // Немного прозрачная
                    }
                }
                else if (_role == "Врач")
                {
                    btnPanel.Children.Add(reportBtn);
                    btnPanel.Children.Add(patientDocsBtn);
                    btnPanel.Children.Add(repeatBtn);
                    btnPanel.Children.Add(doneBtn);
                }
                else if (_role == "Регистратор")
                {
                }
                else if (_role == "Администратор")
                {
                    btnPanel.Children.Add(moveBtn);
                    btnPanel.Children.Add(editBtn);
                    btnPanel.Children.Add(cancelBtn);
                    btnPanel.Children.Add(reportBtn);
                    btnPanel.Children.Add(doneBtn);
                }

                sp.Children.Add(btnPanel);
                border.Child = sp;
                AppointmentsWrapPanel.Children.Add(border);
            }
        }

        private TextBlock CreateInfoTextBlock(string label, string value)
        {
            return new TextBlock
            {
                Text = $"{label}: {value}",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "Ожидает" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                "Выполнено" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                "Отменено" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                _ => Brushes.Gray
            };
        }

        private UIElement CreateEmptyMessage(string text)
        {
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(20),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private void UpdateAppointmentStatus(int appointmentId, string status)
        {
            using var db = new MedRegistryContext();
            var appointment = db.Appointments.Find(appointmentId);
            if (appointment != null)
            {
                appointment.Status = status;
                db.SaveChanges();
                LoadAppointments();
            }
        }

        private void AddAppointment_Click(object sender, RoutedEventArgs e)
        {
            var newWindow = new NewAppointmentWindow(_userId);
            if (newWindow.ShowDialog() == true)
            {
                LoadAppointments();
            }
        }

        public void NotifyUpcomingAppointments()
        {
            using var db = new MedRegistryContext();
            var now = DateTime.Now;

            var upcoming = db.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.AppointmentStart > now &&
                            a.AppointmentStart <= now.AddHours(1) &&
                            a.Status == "Ожидает")
                .ToList();

            foreach (var appt in upcoming)
            {
                MessageBox.Show(
                    $"Скоро приём:\nПациент: {appt.Patient?.User?.FirstName} {appt.Patient?.User?.LastName}\n" +
                    $"Врач: {appt.Doctor?.User?.FirstName} {appt.Doctor?.User?.LastName}\n" +
                    $"Время: {appt.AppointmentStart:HH:mm} - {appt.AppointmentEnd:HH:mm}",
                    "Напоминание о приёме", MessageBoxButton.OK, MessageBoxImage.Information
                );
            }
        }
    }
}
