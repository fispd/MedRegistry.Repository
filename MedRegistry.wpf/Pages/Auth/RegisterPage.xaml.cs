using MedRegistryApp.wpf.Windows;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Pages.Auth
{
    /// <summary>
    /// Логика взаимодействия для RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        public RegisterPage(RegisterData data) : this()
        {
            if (data != null)
            {
                LastNameBox.Text = data.LastName;
                FirstNameBox.Text = data.FirstName;
                MiddleNameBox.Text = data.MiddleName;
                PhoneBox.Text = data.Phone;
                EmailBox.Text = data.Email;
                AddressBox.Text = data.Address;
                PolicyBox.Text = data.Policy;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FormatName(FirstNameBox.Text.Trim());
            string lastName = FormatName(LastNameBox.Text.Trim());
            string middleName = FormatName(MiddleNameBox.Text.Trim());
            string phone = PhoneBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string address = AddressBox.Text.Trim();
            string policy = PolicyBox.Text.Trim();

            if (string.IsNullOrEmpty(firstName) ||
                string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(address) ||
                string.IsNullOrEmpty(policy))
            {
                MessageBox.Show("Заполните все обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidName(lastName) || !IsValidName(firstName) ||
                (!string.IsNullOrEmpty(middleName) && !IsValidName(middleName)))
            {
                MessageBox.Show("Поля Фамилия, Имя и Отчество должны содержать только русские буквы и начинаться с заглавной.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var checkEmail = new MailAddress(email);
            }
            catch
            {
                MessageBox.Show("Некорректный Email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(phone, @"^\+?\d{10,15}$"))
            {
                MessageBox.Show("Телефон введен некорректно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new DataLayer.Data.MedRegistryContext();

            if (db.Users.Any(u => u.Phone == phone))
            {
                MessageBox.Show("Пользователь с таким номером телефона уже зарегистрирован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (db.Users.Any(u => u.Email == email))
            {
                MessageBox.Show("Пользователь с таким email уже зарегистрирован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Убираем пробелы из номера полиса
            string policyClean = policy.Replace(" ", "").Replace("-", "");
            
            if (!Regex.IsMatch(policyClean, @"^\d{16}$"))
            {
                MessageBox.Show("Номер полиса ОМС должен состоять из 16 цифр.\n\n" +
                    "Пример: 1234567890123456\n\n" +
                    "Номер полиса указан на лицевой стороне вашего полиса ОМС.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            policy = policyClean;

            if (!IsValidAddress(address))
            {
                MessageBox.Show("Адрес введён некорректно. Укажите улицу и номер дома (например: 'ул. Ломоносова 12').",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var registerData = new RegisterData
            {
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
                Phone = phone,
                Email = email,
                Address = address,
                Policy = policy
            };

            // Переходим на следующий шаг
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.NavigateToRegisterLogin(registerData);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = Window.GetWindow(this) as AuthWindow;
            if (authWindow != null)
            {
                authWindow.NavigateToLogin();
            }
        }

        private bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return Regex.IsMatch(name, @"^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)?$");
        }

        private string FormatName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            name = name.ToLower();

            string[] parts = name.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Join("-", parts);
        }

        private bool IsValidAddress(string address)
        {
            return Regex.IsMatch(address, @"[А-Яа-яЁё]+") && Regex.IsMatch(address, @"\d+");
        }
    }
}

