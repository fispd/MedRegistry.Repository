using MedRegistry.wpf.Pages;
using MedRegistryApp.wpf.Pages;
using MedRegistryApp.wpf.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MedRegistryApp.wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _userId;
        private string _role;

        public MainWindow(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;

            if (_role == "Гость")
            {
                ProfileButton.Visibility = Visibility.Collapsed;
                NewAppointment.Visibility = Visibility.Collapsed;
                AppointmentsButton.Visibility = Visibility.Collapsed;
                ReportsButton.Visibility = Visibility.Collapsed;
                UserButton.Visibility = Visibility.Collapsed;

                MainFrame.Content = new DoctorsPage(_userId, _role);
                SetActiveButton(DoctorsButton);
                return;
            }

            if (_role != "Администратор")
            {
                UserButton.Visibility = Visibility.Collapsed;
            }

            if (_role == "Врач" || _role == "Регистратор" || _role == "Администратор")
            {
                NewAppointment.Visibility = Visibility.Collapsed;
            }

            MainFrame.Content = new ProfilePage(_userId, _role);
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new ProfilePage(_userId, _role); SetActiveButton(ProfileButton); }

        private void DoctorsButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new DoctorsPage(_userId, _role); SetActiveButton(DoctorsButton); }
        private void ScheduleButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new SchedulePage(_userId, _role); SetActiveButton(ScheduleButton); }
        private void AppointmentsButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new AppointmentsPage(_userId, _role); SetActiveButton(AppointmentsButton); }
        private void ReportsButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new MedicalRecordsPage(_userId, _role); SetActiveButton(ReportsButton); }
        private void UsersButton_Click(object sender, RoutedEventArgs e) { MainFrame.Content = new UsersPage(_userId, _role); SetActiveButton(UserButton); }


        private void NewAppointment_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewAppointmentWindow(_userId);
            window.ShowDialog();

        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow auth = new AuthWindow();
            auth.Show();
            this.Close();
        }
        private void SetActiveButton(Button active)
        {
            DoctorsButton.Background = Brushes.Gray;
            ScheduleButton.Background = Brushes.Gray;
            AppointmentsButton.Background = Brushes.Gray;
            ProfileButton.Background = Brushes.Gray;
            ReportsButton.Background = Brushes.Gray;
            NewAppointment.Background = Brushes.Gray;
            UserButton.Background = Brushes.Gray;

            active.Background = Brushes.DodgerBlue;
        }
    }
}




