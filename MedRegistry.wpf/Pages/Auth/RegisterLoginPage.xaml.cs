using DataLayer.Data;
using DataLayer.Models;
using MedRegistryApp.wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Pages.Auth
{
    /// <summary>
    /// Логика взаимодействия для RegisterLoginPage.xaml
    /// </summary>
    public partial class RegisterLoginPage : Page
    {
        private RegisterData _registerData;

        public RegisterLoginPage(RegisterData data)
        {
            InitializeComponent();
            _registerData = data ?? throw new ArgumentNullException(nameof(data));
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$"))
            {
                MessageBox.Show("Логин должен содержать только латинские буквы, цифры и подчеркивания, от 3 до 20 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new MedRegistryContext();

            if (db.Users.Any(u => u.Username == username))
            {
                MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = new User
            {
                FirstName = _registerData.FirstName,
                LastName = _registerData.LastName,
                MiddleName = _registerData.MiddleName,
                Phone = _registerData.Phone,
                Email = _registerData.Email,
                Address = _registerData.Address,
                MedicalPolicy = _registerData.Policy,
                Username = username,
                PasswordHash = password,
                RoleId = 4
            };

            db.Users.Add(user);
            db.SaveChanges();

            if (user.RoleId == 4)
            {
                var patient = new Patient
                {
                    UserId = user.UserId
                };

                db.Patients.Add(patient);
                db.SaveChanges();
            }

            MessageBox.Show("Регистрация успешно завершена! Теперь вы можете войти в систему.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Возвращаемся на страницу входа
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.NavigateToLogin();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на предыдущий шаг с сохранёнными данными
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.AuthFrame.Navigate(new RegisterPage(_registerData));
            }
        }
    }
}


