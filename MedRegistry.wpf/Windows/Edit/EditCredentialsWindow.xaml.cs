using DataLayer.Data;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для EditCredentialsWindow.xaml
    /// </summary>
    public partial class EditCredentialsWindow : Window
    {
        private int _userId;

        public EditCredentialsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadCredentials();
        }

        private void LoadCredentials()
        {
            using var db = new MedRegistryContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == _userId);
            if (user == null) return;

            UsernameBox.Text = user.Username;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == _userId);
            if (user == null) return;

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(PasswordBox.Password))
            {
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordBox.Password.Length < 6 || !PasswordBox.Password.Any(char.IsDigit) || !PasswordBox.Password.Any(char.IsLetter))
                {
                    MessageBox.Show("Пароль должен содержать минимум 6 символов, включая хотя бы одну букву и одну цифру.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.PasswordHash = PasswordBox.Password; // для простоты без хеша
            }

            user.Username = UsernameBox.Text;

            db.SaveChanges();
            MessageBox.Show("Данные для входа обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}

