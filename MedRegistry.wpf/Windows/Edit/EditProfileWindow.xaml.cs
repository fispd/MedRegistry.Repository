using DataLayer.Data;
using System.Text.RegularExpressions;
using System.Windows;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для EditProfileWindow.xaml
    /// </summary>
    public partial class EditProfileWindow : Window
    {
        private int _userId;

        public EditProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUser();
        }

        private void LoadUser()
        {
            using var db = new MedRegistryContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == _userId);
            if (user == null) return;

            // Заполнение полей
            FirstNameBox.Text = user.FirstName;
            LastNameBox.Text = user.LastName;
            MiddleNameBox.Text = user.MiddleName ?? "";
            EmailBox.Text = user.Email ?? "";
            PhoneBox.Text = user.Phone ?? "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == _userId);
            if (user == null) return;

            // Проверки
            if (!string.IsNullOrEmpty(EmailBox.Text) && !Regex.IsMatch(EmailBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(PhoneBox.Text) && !Regex.IsMatch(PhoneBox.Text, @"^\d{10,15}$"))
            {
                MessageBox.Show("Телефон должен содержать только цифры (10–15 символов).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем изменения
            user.FirstName = FirstNameBox.Text;
            user.LastName = LastNameBox.Text;
            user.MiddleName = MiddleNameBox.Text;
            user.Email = EmailBox.Text;
            user.Phone = PhoneBox.Text;

            db.SaveChanges();
            MessageBox.Show("Профиль обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
