using DataLayer.Data;
using DataLayer.Models;
using MedRegistryApp.wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Pages.Auth
{
    /// <summary>
    /// Логика взаимодействия для ForgotCredentialsPage.xaml
    /// </summary>
    public partial class ForgotCredentialsPage : Page
    {
        private User? _foundUser;

        public ForgotCredentialsPage()
        {
            InitializeComponent();
            ChangeLoginRadio.Checked += ChangeType_Changed;
            ChangePasswordRadio.Checked += ChangeType_Changed;
        }

        private void ChangeType_Changed(object sender, RoutedEventArgs e)
        {
            if (ChangeLoginRadio.IsChecked == true)
            {
                NewLoginPanel.Visibility = Visibility.Visible;
                NewPasswordPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NewLoginPanel.Visibility = Visibility.Collapsed;
                NewPasswordPanel.Visibility = Visibility.Visible;
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Введите Email или номер телефона", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new MedRegistryContext();

            // Ищем пользователя по email или телефону
            _foundUser = db.Users.FirstOrDefault(u => 
                u.Email.ToLower() == searchText.ToLower() || 
                u.Phone == searchText ||
                u.Phone == searchText.Replace("+", "").Replace("-", "").Replace(" ", ""));

            if (_foundUser == null)
            {
                MessageBox.Show("Пользователь с таким Email или телефоном не найден.", 
                    "Не найдено", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Показываем информацию о найденном пользователе
            string maskedLogin = MaskString(_foundUser.Username);
            FoundUserInfo.Text = $"{_foundUser.LastName} {_foundUser.FirstName}\nЛогин: {maskedLogin}";

            // Переключаем панели
            SearchPanel.Visibility = Visibility.Collapsed;
            ChangePanel.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_foundUser == null) return;

            using var db = new MedRegistryContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == _foundUser.UserId);
            
            if (user == null)
            {
                MessageBox.Show("Ошибка: пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ChangeLoginRadio.IsChecked == true)
            {
                // Изменение логина
                string newLogin = NewLoginBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(newLogin))
                {
                    MessageBox.Show("Введите новый логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newLogin.Length < 3)
                {
                    MessageBox.Show("Логин должен содержать минимум 3 символа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка уникальности
                if (db.Users.Any(u => u.Username == newLogin && u.UserId != user.UserId))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.Username = newLogin;
                db.SaveChanges();

                MessageBox.Show($"Логин успешно изменён!\n\nВаш новый логин: {newLogin}", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Изменение пароля
                string newPassword = NewPasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("Введите новый пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newPassword.Length < 6)
                {
                    MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.PasswordHash = newPassword;
                db.SaveChanges();

                MessageBox.Show("Пароль успешно изменён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Возвращаемся на страницу входа
            var authWindow = Window.GetWindow(this) as AuthWindow;
            authWindow?.NavigateToLogin();
        }

        private void SearchAgain_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем состояние
            _foundUser = null;
            SearchBox.Text = "";
            NewLoginBox.Text = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            ChangeLoginRadio.IsChecked = true;

            SearchPanel.Visibility = Visibility.Visible;
            ChangePanel.Visibility = Visibility.Collapsed;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = Window.GetWindow(this) as AuthWindow;
            authWindow?.NavigateToLogin();
        }

        private string MaskString(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= 2)
                return "***";

            return input[0] + new string('*', input.Length - 2) + input[^1];
        }
    }
}


