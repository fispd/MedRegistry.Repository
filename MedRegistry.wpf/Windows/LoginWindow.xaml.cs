using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly MedRegistryContext _db;

        public LoginWindow()
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

            var main = new MainWindow(user.UserId, role);
            main.Show();
            this.Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow register = new RegisterWindow();
            register.ShowDialog();
        }
    }
}

