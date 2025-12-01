using DataLayer.Data;
using DataLayer.Models;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для RegisterLoginWindow.xaml
    /// </summary>
    public partial class RegisterLoginWindow : Window
    {
        private RegisterData _registerData;

        public RegisterLoginWindow(RegisterData data)
        {
            InitializeComponent();
            _registerData = data ?? throw new ArgumentNullException(nameof(data));
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

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

            MessageBox.Show("Пользователь успешно зарегистрирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();

            registerWindow.LastNameBox.Text = _registerData.LastName;
            registerWindow.FirstNameBox.Text = _registerData.FirstName;
            registerWindow.MiddleNameBox.Text = _registerData.MiddleName;
            registerWindow.PhoneBox.Text = _registerData.Phone;
            registerWindow.EmailBox.Text = _registerData.Email;
            registerWindow.AddressBox.Text = _registerData.Address;
            registerWindow.PolicyBox.Text = _registerData.Policy;

            registerWindow.Show();
            this.Close();
        }
    }
}
