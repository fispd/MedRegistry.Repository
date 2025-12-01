using DataLayer.Data;
using MedRegistryApp.wpf.Windows.Edit;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        private int _userId;
        private string _role;

        public ProfilePage(int userId, string role = null)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            LoadProfile();
        }

        public void LoadProfile()
        {
            using var db = new MedRegistryContext();

            var user = db.Users
                .Include(u => u.Role)
                .Include(u => u.Patient)
                    .ThenInclude(p => p.Appointments)
                        .ThenInclude(a => a.Doctor)
                            .ThenInclude(d => d.User)
                .Include(u => u.Doctor)
                    .ThenInclude(d => d.Appointments)
                        .ThenInclude(a => a.Patient)
                            .ThenInclude(p => p.User)
                .Include(u => u.Doctor)
                    .ThenInclude(d => d.Specialization)
                .Include(u => u.Patient)
                    .ThenInclude(p => p.MedicalRecords)
                .FirstOrDefault(u => u.UserId == _userId);

            if (user == null) return;

            // Определяем роль если не передана
            if (string.IsNullOrEmpty(_role) && user.Role != null)
            {
                _role = user.Role.RoleName;
            }

            // Обновляем заголовок профиля в зависимости от роли
            var profileTitle = this.FindName("ProfileTitle") as TextBlock;
            if (profileTitle != null)
            {
                profileTitle.Text = _role switch
                {
                    "Пациент" => "Профиль пациента",
                    "Врач" => "Профиль врача",
                    "Регистратор" => "Профиль регистратора",
                    "Администратор" => "Профиль администратора",
                    _ => "Профиль пользователя"
                };
            }

            UsernameRun.Text = user.Username;
            FirstNameRun.Text = user.FirstName;
            LastNameRun.Text = user.LastName;
            MiddleNameRun.Text = user.MiddleName ?? "—";
            EmailRun.Text = user.Email ?? "—";
            PhoneRun.Text = user.Phone ?? "—";
            AddressRun.Text = user.Address ?? "—";
            PolicyRun.Text = user.MedicalPolicy ?? "—";

            // Ближайший приём (только для пациентов)
            if (user.Patient != null)
            {
                NextAppointmentBorder.Visibility = Visibility.Visible;

                var now = DateTime.Now;
                var nextAppt = user.Patient.Appointments
                    .Where(a => a.AppointmentStart > now && a.Status != "Отменено")
                    .OrderBy(a => a.AppointmentStart)
                    .FirstOrDefault();

                if (nextAppt != null)
                {
                    NextAppointmentGrid.Visibility = Visibility.Visible;
                    NoAppointmentText.Visibility = Visibility.Collapsed;

                    NextApptDateText.Text = nextAppt.AppointmentStart.ToString("dd MMMM yyyy (dddd)");
                    NextApptTimeText.Text = $"{nextAppt.AppointmentStart:HH:mm} - {nextAppt.AppointmentEnd:HH:mm}";
                    NextApptDoctorText.Text = $"{nextAppt.Doctor?.User?.LastName} {nextAppt.Doctor?.User?.FirstName}";
                    NextApptCabinetText.Text = nextAppt.Doctor?.CabinetNumber ?? "—";
                }
                else
                {
                    NextAppointmentGrid.Visibility = Visibility.Collapsed;
                    NoAppointmentText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                NextAppointmentBorder.Visibility = Visibility.Collapsed;
            }

            // Последний отчёт (только для пациентов)
            var lastRecordBorder = this.FindName("LastRecordBorder") as Border;
            if (lastRecordBorder != null)
            {
                if (user.Patient != null)
                {
                    lastRecordBorder.Visibility = Visibility.Visible;
                    LastDiagnosisText.Text = "Диагноз: —";
                    LastTreatmentText.Text = "Лечение: —";

                    var lastRecord = user.Patient.MedicalRecords
                        .OrderByDescending(r => r.RecordDate)
                        .FirstOrDefault();

                    if (lastRecord != null)
                    {
                        LastDiagnosisText.Text = $"Диагноз: {lastRecord.Diagnosis ?? "-"}";
                        LastTreatmentText.Text = $"Лечение: {lastRecord.Treatment ?? "-"}";
                    }
                }
                else
                {
                    lastRecordBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditProfileWindow(_userId);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            LoadProfile();
        }

        private void EditCredentials_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditCredentialsWindow(_userId);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            LoadProfile();
        }

        private void ToggleProfileData_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProfileDataBorder.Visibility == Visibility.Collapsed)
            {
                // Показываем данные
                ProfileDataBorder.Visibility = Visibility.Visible;
                ToggleIcon.Text = "▲";
                ToggleText.Text = "Скрыть личные данные";
            }
            else
            {
                // Скрываем данные
                ProfileDataBorder.Visibility = Visibility.Collapsed;
                ToggleIcon.Text = "▼";
                ToggleText.Text = "Показать личные данные";
            }
        }

        private void HideProfileData_Click(object sender, RoutedEventArgs e)
        {
            ProfileDataBorder.Visibility = Visibility.Collapsed;
            ToggleIcon.Text = "▼";
            ToggleText.Text = "Показать личные данные";
        }

    }
}

