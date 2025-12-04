using DataLayer.Data;
using DataLayer.Models;
using System.Linq;
using System.Windows;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для NewUserWindow.xaml
    /// </summary>
    public partial class NewUserWindow : Window
    {
        public NewUserWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            using var db = new MedRegistryContext();
            RoleComboBox.ItemsSource = db.Roles.ToList();
            RoleComboBox.DisplayMemberPath = "RoleName";
            RoleComboBox.SelectedValuePath = "RoleId";
        }

        /// <summary>
        /// Проверяет логин при потере фокуса полем ввода.
        /// Показывает предупреждение только если логин некорректен или занят.
        /// </summary>
        private void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim();
            
            // Пустой логин - просто выходим без сообщений
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            // Проверка длины
            if (username.Length < 3)
            {
                MessageBox.Show("Логин должен содержать не менее 3 символов", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (username.Length > 50)
            {
                MessageBox.Show("Логин не должен превышать 50 символов", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка на допустимые символы (буквы, цифры, подчёркивание, точка, дефис)
            if (!IsValidUsername(username))
            {
                MessageBox.Show("Логин может содержать только буквы (латиница), цифры, точку, дефис и подчёркивание", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка уникальности логина
            using var db = new MedRegistryContext();
            bool exists = db.Users.Any(u => u.Username.ToLower() == username.ToLower());
            
            if (exists)
            {
                MessageBox.Show("Пользователь с таким логином уже существует.\nПожалуйста, выберите другой логин.", 
                    "Логин занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.SelectAll();
            }
            // Если логин свободен - ничего не показываем, пользователь продолжает заполнение
        }

        /// <summary>
        /// Проверяет корректность логина (только латиница, цифры, точка, дефис, подчёркивание).
        /// </summary>
        private bool IsValidUsername(string username)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._\-]+$");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            var username = UsernameBox.Text.Trim();
            if (username.Length < 3)
            {
                MessageBox.Show("Логин должен содержать не менее 3 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            if (username.Length > 50)
            {
                MessageBox.Show("Логин не должен превышать 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            if (!IsValidUsername(username))
            {
                MessageBox.Show("Логин может содержать только буквы (латиница), цифры, точку, дефис и подчёркивание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (PasswordBox.Password.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать не менее 4 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameBox.Focus();
                return;
            }

            var firstName = FirstNameBox.Text.Trim();
            if (firstName.Length < 2)
            {
                MessageBox.Show("Имя должно содержать не менее 2 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameBox.Focus();
                return;
            }

            if (firstName.Length > 50)
            {
                MessageBox.Show("Имя не должно превышать 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameBox.Focus();
                return;
            }

            if (!IsValidName(firstName))
            {
                MessageBox.Show("Имя должно содержать только буквы (кириллица или латиница)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameBox.Focus();
                return;
            }

            var lastName = LastNameBox.Text.Trim();
            if (lastName.Length < 2)
            {
                MessageBox.Show("Фамилия должна содержать не менее 2 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameBox.Focus();
                return;
            }

            if (lastName.Length > 50)
            {
                MessageBox.Show("Фамилия не должна превышать 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameBox.Focus();
                return;
            }

            if (!IsValidName(lastName))
            {
                MessageBox.Show("Фамилия должна содержать только буквы (кириллица или латиница)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameBox.Focus();
                return;
            }

            // Валидация отчества (если указано)
            var middleName = MiddleNameBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(middleName))
            {
                if (middleName.Length < 2)
                {
                    MessageBox.Show("Отчество должно содержать не менее 2 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MiddleNameBox.Focus();
                    return;
                }

                if (middleName.Length > 50)
                {
                    MessageBox.Show("Отчество не должно превышать 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MiddleNameBox.Focus();
                    return;
                }

                if (!IsValidName(middleName))
                {
                    MessageBox.Show("Отчество должно содержать только буквы (кириллица или латиница)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MiddleNameBox.Focus();
                    return;
                }
            }

            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return;
            }

            // Валидация email (если указан)
            var email = EmailBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (email.Length > 100)
                {
                    MessageBox.Show("Email не должен превышать 100 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailBox.Focus();
                    return;
                }

                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Введите корректный email адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailBox.Focus();
                    return;
                }
            }

            // Валидация телефона (если указан) - российский формат
            var phone = PhoneBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!IsValidRussianPhone(phone))
                {
                    MessageBox.Show("Введите корректный российский номер телефона.\n\nФорматы:\n+7 (XXX) XXX-XX-XX\n8 (XXX) XXX-XX-XX\n+7XXXXXXXXXX\n8XXXXXXXXXX", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneBox.Focus();
                    return;
                }
            }

            using var db = new MedRegistryContext();

            // Проверка уникальности логина (регистронезависимая)
            if (db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            {
                MessageBox.Show("Пользователь с таким логином уже существует.\nПожалуйста, выберите другой логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                UsernameBox.SelectAll();
                return;
            }

            // Проверка уникальности email (если указан)
            if (!string.IsNullOrWhiteSpace(email) && db.Users.Any(u => u.Email == email))
            {
                MessageBox.Show("Пользователь с таким email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Создаём пользователя
            var user = new User
            {
                Username = username,
                PasswordHash = PasswordBox.Password, // В реальном приложении нужно хэшировать пароль
                FirstName = firstName,
                LastName = lastName,
                MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                RoleId = ((Role)RoleComboBox.SelectedItem).RoleId
            };

            db.Users.Add(user);
            db.SaveChanges();

            MessageBox.Show(
                $"Пользователь успешно создан!\n\nЛогин: {user.Username}",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            this.Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidName(string name)
        {
            // Проверка что имя содержит только буквы (кириллица, латиница) и дефисы
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[а-яА-ЯёЁa-zA-Z\-]+$");
        }

        private bool IsValidRussianPhone(string phone)
        {
            // Удаляем все пробелы, скобки, дефисы и плюсы для проверки
            string cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"[\s\(\)\-\+]", "");
            
            // Проверяем что остались только цифры
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanPhone, @"^\d+$"))
            {
                return false;
            }

            // Российский номер должен начинаться с 7 или 8 и иметь 11 цифр (7XXXXXXXXXX или 8XXXXXXXXXX)
            // или 10 цифр без кода страны (XXXXXXXXXX)
            if (cleanPhone.Length == 11)
            {
                // Формат: 7XXXXXXXXXX или 8XXXXXXXXXX
                return cleanPhone.StartsWith("7") || cleanPhone.StartsWith("8");
            }
            else if (cleanPhone.Length == 10)
            {
                // Формат: XXXXXXXXXX (10 цифр без кода страны)
                return true;
            }

            return false;
        }
    }
}

