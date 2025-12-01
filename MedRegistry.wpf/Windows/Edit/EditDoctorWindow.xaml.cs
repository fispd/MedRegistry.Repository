using DataLayer.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace MedRegistryApp.wpf.Windows.Edit
{
    /// <summary>
    /// Логика взаимодействия для EditDoctorWindow.xaml
    /// </summary>
    public partial class EditDoctorWindow : Window
    {
        private readonly int _doctorId;

        public EditDoctorWindow(int doctorId)
        {
            InitializeComponent();
            _doctorId = doctorId;

            LoadSpecializations();
            LoadDoctorData();
        }

        private void LoadSpecializations()
        {
            using var db = new MedRegistryContext();

            var specs = db.Specializations.OrderBy(s => s.Name).ToList();

            SpecializationBox.Items.Clear();

            foreach (var s in specs)
                SpecializationBox.Items.Add(new ComboBoxItem
                {
                    Content = s.Name,
                    Tag = s.SpecializationId
                });
        }

        private void LoadDoctorData()
        {
            using var db = new MedRegistryContext();

            var doctor = db.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .FirstOrDefault(d => d.DoctorId == _doctorId);

            if (doctor == null)
            {
                MessageBox.Show("Ошибка: врач не найден");
                Close();
                return;
            }

            FirstNameBox.Text = doctor.User?.FirstName;
            LastNameBox.Text = doctor.User?.LastName;
            ExperienceBox.Text = doctor.WorkExperienceYears?.ToString() ?? "";

            // Выбрана текущая специализация
            foreach (ComboBoxItem item in SpecializationBox.Items)
            {
                if ((int)item.Tag == doctor.SpecializationId)
                {
                    SpecializationBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameBox.Text) ||
                SpecializationBox.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            if (!int.TryParse(ExperienceBox.Text, out int experience))
            {
                MessageBox.Show("Опыт должен быть числом!");
                return;
            }

            using var db = new MedRegistryContext();

            var doctor = db.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == _doctorId);

            if (doctor == null)
            {
                MessageBox.Show("Ошибка: врач не найден");
                return;
            }

            // Обновляем данные
            doctor.User.FirstName = FirstNameBox.Text.Trim();
            doctor.User.LastName = LastNameBox.Text.Trim();
            doctor.WorkExperienceYears = experience;

            var selectedItem = (ComboBoxItem)SpecializationBox.SelectedItem;
            doctor.SpecializationId = (int)selectedItem.Tag;

            db.SaveChanges();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

