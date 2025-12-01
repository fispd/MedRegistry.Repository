using DataLayer.Data;
using MedRegistryApp.wpf.Windows;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Pages.Auth
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private readonly MedRegistryContext _db;

        public LoginPage()
        {
            InitializeComponent();
            _db = new MedRegistryContext();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text?.Trim();
            string password = PasswordBox.Password ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = _db.Users
              .Include(u => u.Role)
              .FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

            if (user == null)
            {
                MessageBox.Show("Неправильно введён логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string role = user.Role.RoleName;

            // Получаем родительское окно AuthWindow и открываем MainWindow
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.OpenMainWindow(user.UserId, role);
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу регистрации
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.NavigateToRegister();
            }
        }

        private void ForgotCredentials_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Переход на страницу восстановления
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.NavigateToForgotCredentials();
            }
        }

        private void GuestLogin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Вход как гость (userId = 0, role = "Гость")
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.OpenMainWindow(0, "Гость");
            }
        }
    }
}

