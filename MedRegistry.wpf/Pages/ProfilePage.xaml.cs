using DataLayer.Data;
using MedRegistryApp.wpf.Windows.Edit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Страница профиля пользователя.
    /// Отображает личные данные, ближайший приём и последний медицинский отчёт.
    /// </summary>
    public partial class ProfilePage : Page
    {
        private int _userId;
        private string? _role;

        /// <summary>
        /// Конструктор страницы профиля.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="role">Роль пользователя (опционально)</param>
        public ProfilePage(int userId, string? role = null)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            LoadProfile();
        }

        /// <summary>
        /// Загружает и отображает данные профиля пользователя.
        /// Включает информацию о ближайшем приёме и последнем медицинском отчёте.
        /// </summary>
        public void LoadProfile()
        {
            try
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

                InitializeRole(user);
                DisplayPersonalInfo(user);
                DisplayNextAppointment(user);
                DisplayLastMedicalRecord(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке профиля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Инициализирует роль пользователя и обновляет заголовок профиля.
        /// </summary>
        private void InitializeRole(DataLayer.Models.User user)
        {
            if (string.IsNullOrEmpty(_role) && user.Role != null)
            {
                _role = user.Role.RoleName;
            }

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
        }

        /// <summary>
        /// Отображает личную информацию пользователя.
        /// </summary>
        private void DisplayPersonalInfo(DataLayer.Models.User user)
        {
            UsernameRun.Text = user.Username;
            FirstNameRun.Text = user.FirstName;
            LastNameRun.Text = user.LastName;
            MiddleNameRun.Text = user.MiddleName ?? "—";
            EmailRun.Text = user.Email ?? "—";
            PhoneRun.Text = user.Phone ?? "—";
            AddressRun.Text = user.Address ?? "—";
            PolicyRun.Text = user.MedicalPolicy ?? "—";
        }

        /// <summary>
        /// Отображает информацию о ближайшем приёме.
        /// Для пациента - показывает врача, для врача - показывает пациента.
        /// </summary>
        private void DisplayNextAppointment(DataLayer.Models.User user)
        {
            var now = DateTime.Now;
            
            if (_role == "Врач" && user.Doctor != null)
            {
                DisplayDoctorNextAppointment(user, now);
            }
            else if (_role == "Пациент" && user.Patient != null)
            {
                DisplayPatientNextAppointment(user, now);
            }
            else
            {
                NextAppointmentBorder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Отображает ближайший приём для врача (показывает пациента).
        /// </summary>
        private void DisplayDoctorNextAppointment(DataLayer.Models.User user, DateTime now)
        {
            NextAppointmentBorder.Visibility = Visibility.Visible;
            NextApptPersonLabel.Text = "Пациент:";
            NextApptPurposeLabel.Visibility = Visibility.Visible;
            NextApptPurposeText.Visibility = Visibility.Visible;

            var nextAppt = user.Doctor.Appointments
                .Where(a => a.AppointmentStart > now && a.Status != "Отменено")
                .OrderBy(a => a.AppointmentStart)
                .FirstOrDefault();

            if (nextAppt != null)
            {
                NextAppointmentGrid.Visibility = Visibility.Visible;
                NoAppointmentText.Visibility = Visibility.Collapsed;

                NextApptDateText.Text = nextAppt.AppointmentStart.ToString("dd MMMM yyyy (dddd)");
                NextApptTimeText.Text = $"{nextAppt.AppointmentStart:HH:mm} - {nextAppt.AppointmentEnd:HH:mm}";
                NextApptPersonText.Text = $"{nextAppt.Patient?.User?.LastName} {nextAppt.Patient?.User?.FirstName} {nextAppt.Patient?.User?.MiddleName ?? ""}".Trim();
                NextApptPurposeText.Text = nextAppt.Purpose ?? "—";
                NextApptCabinetText.Text = user.Doctor.CabinetNumber ?? "—";
            }
            else
            {
                NextAppointmentGrid.Visibility = Visibility.Collapsed;
                NoAppointmentText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Отображает ближайший приём для пациента (показывает врача).
        /// </summary>
        private void DisplayPatientNextAppointment(DataLayer.Models.User user, DateTime now)
        {
            NextAppointmentBorder.Visibility = Visibility.Visible;
            NextApptPersonLabel.Text = "Врач:";
            NextApptPurposeLabel.Visibility = Visibility.Collapsed;
            NextApptPurposeText.Visibility = Visibility.Collapsed;

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
                NextApptPersonText.Text = $"{nextAppt.Doctor?.User?.LastName} {nextAppt.Doctor?.User?.FirstName}";
                NextApptCabinetText.Text = nextAppt.Doctor?.CabinetNumber ?? "—";
            }
            else
            {
                NextAppointmentGrid.Visibility = Visibility.Collapsed;
                NoAppointmentText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Отображает информацию о последнем медицинском отчёте (только для пациентов).
        /// </summary>
        private void DisplayLastMedicalRecord(DataLayer.Models.User user)
        {
            var lastRecordBorder = this.FindName("LastRecordBorder") as Border;
            if (lastRecordBorder == null) return;

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

        /// <summary>
        /// Открывает окно редактирования профиля.
        /// </summary>
        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditProfileWindow(_userId);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            LoadProfile();
        }

        /// <summary>
        /// Открывает окно изменения учётных данных (логин/пароль).
        /// </summary>
        private void EditCredentials_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditCredentialsWindow(_userId);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            LoadProfile();
        }

        /// <summary>
        /// Переключает видимость панели личных данных.
        /// </summary>
        private void ToggleProfileData_Click(object sender, MouseButtonEventArgs e)
        {
            if (ProfileDataBorder.Visibility == Visibility.Collapsed)
            {
                ProfileDataBorder.Visibility = Visibility.Visible;
                ToggleIcon.Text = "▲";
                ToggleText.Text = "Скрыть личные данные";
            }
            else
            {
                ProfileDataBorder.Visibility = Visibility.Collapsed;
                ToggleIcon.Text = "▼";
                ToggleText.Text = "Показать личные данные";
            }
        }

        /// <summary>
        /// Скрывает панель личных данных.
        /// </summary>
        private void HideProfileData_Click(object sender, RoutedEventArgs e)
        {
            ProfileDataBorder.Visibility = Visibility.Collapsed;
            ToggleIcon.Text = "▼";
            ToggleText.Text = "Показать личные данные";
        }
    }
}
