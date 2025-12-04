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
using System;
using System.Collections.Generic;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Страница управления записями на приём (талонами).
    /// Отображает записи в зависимости от роли пользователя.
    /// </summary>
    public partial class AppointmentsPage : Page
    {
        private int _userId;
        private string _role;

        /// <summary>
        /// Конструктор страницы записей на приём.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль пользователя</param>
        public AppointmentsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            ConfigureButtonVisibility();
            LoadAppointments();
        }

        /// <summary>
        /// Настраивает видимость кнопок в зависимости от роли.
        /// </summary>
        private void ConfigureButtonVisibility()
        {
            var addButton = this.FindName("AddAppointmentButton") as Button;
            if (addButton != null && (_role == "Пациент" || _role == "Врач"))
            {
                addButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Загружает записи на приём из базы данных.
        /// Фильтрует по роли: пациент видит свои записи, врач - свои приёмы на сегодня.
        /// </summary>
        private void LoadAppointments()
        {
            try
            {
                using var db = new MedRegistryContext();

                IQueryable<Appointment> query = db.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Include(a => a.Doctor).ThenInclude(d => d.User);

                // Фильтрация по роли
                if (_role == "Пациент")
                {
                    query = query.Where(a => a.Patient.UserId == _userId);
                }
                else if (_role == "Врач")
                {
                    // Врач видит только свои приёмы на сегодня
                    var today = DateTime.Today;
                    query = query.Where(a => a.Doctor.UserId == _userId && 
                                            a.AppointmentStart.Date == today);
                }

                // Сортировка: активные записи сначала, завершённые/отменённые внизу
                List<Appointment> appointments;
                
                if (_role == "Пациент" || _role == "Врач")
                {
                    // Для пациента и врача: активные записи сверху, завершённые/отменённые внизу
                    appointments = query
                        .OrderBy(a => (a.Status == "Отменено" || a.Status == "Выполнено") ? 1 : 0)
                        .ThenBy(a => a.AppointmentStart)
                        .ToList();
                }
                else
                {
                    appointments = query.OrderBy(a => a.AppointmentStart).ToList();
                }
                
                DisplayAppointments(appointments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке записей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Отображает карточки записей на приём.
        /// </summary>
        /// <param name="appointments">Список записей для отображения</param>
        private void DisplayAppointments(List<Appointment> appointments)
        {
            AppointmentsWrapPanel.Children.Clear();

            if (appointments.Count == 0)
            {
                AppointmentsWrapPanel.Children.Add(CreateEmptyMessage("Нет записей на приём"));
                return;
            }

            foreach (var appointment in appointments)
            {
                var card = CreateAppointmentCard(appointment);
                AppointmentsWrapPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// Создаёт визуальную карточку записи на приём.
        /// </summary>
        /// <param name="appointment">Данные записи</param>
        /// <returns>Border элемент с карточкой</returns>
        private Border CreateAppointmentCard(Appointment appointment)
        {
            double cardHeight = GetCardHeight();
            bool isCompletedOrCancelled = appointment.Status == "Отменено" || appointment.Status == "Выполнено";
            // Блеклые карточки для завершённых записей у пациента и врача
            bool shouldFade = isCompletedOrCancelled && (_role == "Пациент" || _role == "Врач");

            var border = new Border
            {
                Background = shouldFade 
                    ? new SolidColorBrush(Color.FromArgb(230, 245, 245, 245)) 
                    : Brushes.White,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(8),
                Padding = new Thickness(12),
                Width = 290,
                MinHeight = cardHeight,
                MaxWidth = 290,
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                BorderThickness = new Thickness(1),
                Opacity = shouldFade ? 0.75 : 1,
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

            // Заголовок с датой и временем
            sp.Children.Add(new TextBlock
            {
                Text = $"📅 {appointment.AppointmentStart:dd.MM.yyyy} в {appointment.AppointmentStart:HH:mm}",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Информация о враче/пациенте в зависимости от роли
            AddRoleSpecificInfo(sp, appointment);

            // Общая информация
            sp.Children.Add(CreateInfoTextBlock("🏥 Кабинет", appointment.Doctor?.CabinetNumber ?? "—"));
            sp.Children.Add(CreateInfoTextBlock("⏰ Время", $"{appointment.AppointmentStart:HH:mm} - {appointment.AppointmentEnd:HH:mm}"));
            sp.Children.Add(CreateInfoTextBlock("📝 Причина", appointment.Purpose ?? "—"));
            
            // Статус (не для пациентов)
            if (_role != "Пациент")
            {
                AddStatusInfo(sp, appointment);
            }

            // Кнопки управления
            var btnPanel = CreateAppointmentButtons(appointment, isCompletedOrCancelled);
            sp.Children.Add(btnPanel);

            border.Child = sp;
            return border;
        }

        /// <summary>
        /// Возвращает высоту карточки в зависимости от роли.
        /// </summary>
        private double GetCardHeight()
        {
            return _role switch
            {
                "Администратор" => 300,
                "Врач" => 310,
                "Регистратор" => 220,
                "Пациент" => 220,
                _ => 260
            };
        }

        /// <summary>
        /// Добавляет информацию о враче/пациенте в зависимости от роли.
        /// </summary>
        private void AddRoleSpecificInfo(StackPanel sp, Appointment appointment)
        {
            if (_role == "Пациент")
            {
                sp.Children.Add(CreateInfoTextBlock("👨‍⚕️ Врач", 
                    $"{appointment.Doctor?.User?.LastName} {appointment.Doctor?.User?.FirstName} {appointment.Doctor?.User?.MiddleName}"));
            }
            else if (_role == "Врач")
            {
                sp.Children.Add(CreateInfoTextBlock("👤 Пациент", 
                    $"{appointment.Patient?.User?.LastName} {appointment.Patient?.User?.FirstName} {appointment.Patient?.User?.MiddleName}"));
            }
            else // Регистратор или Администратор
            {
                sp.Children.Add(CreateInfoTextBlock("👨‍⚕️ Врач", 
                    $"{appointment.Doctor?.User?.LastName} {appointment.Doctor?.User?.FirstName}"));
                sp.Children.Add(CreateInfoTextBlock("👤 Пациент", 
                    $"{appointment.Patient?.User?.LastName} {appointment.Patient?.User?.FirstName}"));
            }
        }

        /// <summary>
        /// Добавляет информацию о статусе записи.
        /// </summary>
        private void AddStatusInfo(StackPanel sp, Appointment appointment)
        {
            var statusColor = GetStatusColor(appointment.Status ?? "Ожидает");
            sp.Children.Add(new TextBlock
            {
                Text = $"Статус: {appointment.Status ?? "Ожидает"}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = statusColor,
                Margin = new Thickness(0, 5, 0, 8)
            });
        }

        /// <summary>
        /// Создаёт панель кнопок для карточки записи.
        /// </summary>
        private WrapPanel CreateAppointmentButtons(Appointment appointment, bool isCompletedOrCancelled)
        {
            var btnPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            switch (_role)
            {
                case "Пациент":
                    AddPatientButtons(btnPanel, appointment, isCompletedOrCancelled);
                    break;
                case "Врач":
                    AddDoctorButtons(btnPanel, appointment);
                    break;
                case "Администратор":
                    AddAdminButtons(btnPanel, appointment);
                    break;
                // Регистратор - без кнопок
            }

            return btnPanel;
        }

        /// <summary>
        /// Добавляет кнопки для пациента.
        /// </summary>
        private void AddPatientButtons(WrapPanel panel, Appointment appointment, bool isCompletedOrCancelled)
        {
            if (!isCompletedOrCancelled)
            {
                var moveBtn = CreateButton("🔄 Перенести", "MoveButtonStyle", () =>
                {
                    var win = new MoveAppointmentWindow(appointment.AppointmentId);
                    if (win.ShowDialog() == true)
                        LoadAppointments();
                });

                var cancelBtn = CreateButton("❌ Отменить", "CancelButtonStyle", () =>
                {
                    if (MessageBox.Show("Вы уверены, что хотите отменить запись?", 
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        UpdateAppointmentStatus(appointment.AppointmentId, "Отменено");
                    }
                });

                panel.Children.Add(moveBtn);
                panel.Children.Add(cancelBtn);
            }
            else
            {
                // Информационный текст для завершённых записей
                panel.Children.Add(new TextBlock
                {
                    Text = appointment.Status == "Отменено" ? "❌ Запись отменена" : "✅ Запись выполнена",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = appointment.Status == "Отменено" 
                        ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                        : new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                    Margin = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
        }

        /// <summary>
        /// Добавляет кнопки для врача.
        /// </summary>
        private void AddDoctorButtons(WrapPanel panel, Appointment appointment)
        {
            bool isCompletedOrCancelled = appointment.Status == "Отменено" || appointment.Status == "Выполнено";

            if (isCompletedOrCancelled)
            {
                // Для завершённых записей показываем только документы и статус
                var docsBtn = CreateButton("📋 Документы", "ReportButtonStyle", () =>
                {
                    this.NavigationService?.Navigate(
                        new MedicalRecordsPage(_userId, _role, appointment.PatientId));
                });
                panel.Children.Add(docsBtn);

                // Показываем статус
                panel.Children.Add(new TextBlock
                {
                    Text = appointment.Status == "Отменено" ? "❌ Отменено" : "✅ Выполнено",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = appointment.Status == "Отменено" 
                        ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                        : new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                    Margin = new Thickness(8, 5, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else
            {
                // Для активных записей показываем все кнопки
                var reportBtn = CreateButton("📄 Отчёт", "ReportButtonStyle", () =>
                {
                    this.NavigationService?.Navigate(
                        new NewMedicalRecordPage(appointment.AppointmentId, appointment.PatientId, appointment.DoctorId));
                });

                var docsBtn = CreateButton("📋 Документы", "ReportButtonStyle", () =>
                {
                    this.NavigationService?.Navigate(
                        new MedicalRecordsPage(_userId, _role, appointment.PatientId));
                });

                var repeatBtn = CreateButton("🔁 Повторный", "EditButtonStyle", () =>
                {
                    using var db = new MedRegistryContext();
                    var doctor = db.Doctors.FirstOrDefault(d => d.UserId == _userId);
                    if (doctor != null)
                    {
                        var repeatWindow = new RepeatAppointmentWindow(appointment.PatientId, doctor.DoctorId);
                        if (repeatWindow.ShowDialog() == true)
                            LoadAppointments();
                    }
                });

                var doneBtn = CreateButton("✅ Выполнено", "DoneButtonStyle", () =>
                {
                    UpdateAppointmentStatus(appointment.AppointmentId, "Выполнено");
                });

                var cancelBtn = CreateButton("❌ Отменить", "CancelButtonStyle", () =>
                {
                    if (MessageBox.Show("Вы уверены, что хотите отменить приём?", 
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        UpdateAppointmentStatus(appointment.AppointmentId, "Отменено");
                    }
                });

                panel.Children.Add(reportBtn);
                panel.Children.Add(docsBtn);
                panel.Children.Add(repeatBtn);
                panel.Children.Add(doneBtn);
                panel.Children.Add(cancelBtn);
            }
        }

        /// <summary>
        /// Добавляет кнопки для администратора.
        /// </summary>
        private void AddAdminButtons(WrapPanel panel, Appointment appointment)
        {
            var moveBtn = CreateButton("🔄 Перенести", "MoveButtonStyle", () =>
            {
                var win = new MoveAppointmentWindow(appointment.AppointmentId);
                if (win.ShowDialog() == true)
                    LoadAppointments();
            });

            var editBtn = CreateButton("✏️ Изменить", "EditButtonStyle", () =>
            {
                var editWindow = new EditAppointmentWindow(appointment.AppointmentId);
                editWindow.ShowDialog();
                LoadAppointments();
            });

            var cancelBtn = CreateButton("❌ Отменить", "CancelButtonStyle", () =>
            {
                if (MessageBox.Show("Вы уверены, что хотите отменить запись?", 
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    UpdateAppointmentStatus(appointment.AppointmentId, "Отменено");
                }
            });

            var reportBtn = CreateButton("📄 Отчёт", "ReportButtonStyle", () =>
            {
                this.NavigationService?.Navigate(
                    new NewMedicalRecordPage(appointment.AppointmentId, appointment.PatientId, appointment.DoctorId));
            });

            var doneBtn = CreateButton("✅ Выполнено", "DoneButtonStyle", () =>
            {
                UpdateAppointmentStatus(appointment.AppointmentId, "Выполнено");
            });

            panel.Children.Add(moveBtn);
            panel.Children.Add(editBtn);
            panel.Children.Add(cancelBtn);
            panel.Children.Add(reportBtn);
            panel.Children.Add(doneBtn);
        }

        /// <summary>
        /// Создаёт кнопку с заданным стилем и обработчиком.
        /// </summary>
        private Button CreateButton(string content, string styleName, Action onClick)
        {
            var btn = new Button
            {
                Content = content,
                Style = (Style)Application.Current.Resources[styleName]
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        /// <summary>
        /// Создаёт текстовый блок с информацией.
        /// </summary>
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

        /// <summary>
        /// Возвращает цвет для статуса записи.
        /// </summary>
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

        /// <summary>
        /// Создаёт сообщение при отсутствии данных.
        /// </summary>
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

        /// <summary>
        /// Обновляет статус записи в базе данных.
        /// </summary>
        /// <param name="appointmentId">ID записи</param>
        /// <param name="status">Новый статус</param>
        private void UpdateAppointmentStatus(int appointmentId, string status)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Открывает окно создания новой записи на приём.
        /// </summary>
        private void AddAppointment_Click(object sender, RoutedEventArgs e)
        {
            var newWindow = new NewAppointmentWindow(_userId);
            if (newWindow.ShowDialog() == true)
            {
                LoadAppointments();
            }
        }

        /// <summary>
        /// Показывает напоминания о предстоящих приёмах (в течение часа).
        /// </summary>
        public void NotifyUpcomingAppointments()
        {
            try
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
            catch (Exception ex)
            {
                // Не показываем ошибку пользователю, просто логируем
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке напоминаний: {ex.Message}");
            }
        }
    }
}
