using DataLayer.Data;
using DataLayer.Models;
using System.Windows;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для NewDoctorWindow.xaml
    /// </summary>
    public partial class NewDoctorWindow : Window
    {
        public NewDoctorWindow()
        {
            InitializeComponent();
            LoadSpecializations();
        }

        private void LoadSpecializations()
        {
            using var db = new MedRegistryContext();
            SpecializationComboBox.ItemsSource = db.Specializations.ToList();
            SpecializationComboBox.DisplayMemberPath = "Name";
        }

        private void AddSpecialization_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите название специализации", "Новая специализация");

            if (string.IsNullOrWhiteSpace(input)) return;

            using var db = new MedRegistryContext();
            if (!db.Specializations.Any(s => s.Name == input))
            {
                db.Specializations.Add(new Specialization { Name = input });
                db.SaveChanges();
            }
            LoadSpecializations();
        }

        private string GenerateLogin()
        {
            var rnd = new Random();
            int code = rnd.Next(100, 999);

            return $"{LastNameBox.Text.ToLower()}.{FirstNameBox.Text.ToLower()}.{code}";
        }

        private string GeneratePassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new MedRegistryContext();

            if (string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameBox.Text))
            {
                ShowError("Введите имя и фамилию");
                return;
            }

            if (!(SpecializationComboBox.SelectedItem is Specialization spec))
            {
                ShowError("Выберите специализацию");
                return;
            }

            // Генерация логина и пароля
            string login = GenerateLogin();
            string password = GeneratePassword();

            // Создаём пользователя
            var user = new User
            {
                FirstName = FirstNameBox.Text,
                LastName = LastNameBox.Text,
                MiddleName = MiddleNameBox.Text,
                Phone = PhoneBox.Text,
                Email = EmailBox.Text,
                Address = AddressBox.Text,
                MedicalPolicy = PolicyBox.Text,

                Username = login,
                PasswordHash = password, // по-хорошему — хэшировать
                RoleId = db.Roles.First(r => r.RoleName == "Врач").RoleId
            };

            db.Users.Add(user);
            db.SaveChanges();

            // Создаём врача
            var doctor = new Doctor
            {
                UserId = user.UserId,
                SpecializationId = spec.SpecializationId,
                CabinetNumber = CabinetBox.Text
            };

            db.Doctors.Add(doctor);
            db.SaveChanges();

            MessageBox.Show(
                $"Врач успешно создан!\n\nЛогин: {login}\nПароль: {password}",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            this.Close();
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

