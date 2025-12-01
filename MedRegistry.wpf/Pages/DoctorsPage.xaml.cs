using DataLayer.Data;
using MedRegistryApp.wpf.Windows;
using MedRegistryApp.wpf.Windows.Edit;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для DoctorsPage.xaml
    /// </summary>
    public partial class DoctorsPage : Page
    {
        private int _userId;
        private string _role;

        public DoctorsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            // Скрываем кнопку добавления для неавторизованных ролей
            var addButton = this.FindName("AddDoctorButton") as Button;
            if (addButton != null && _role != "Администратор" && _role != "Регистратор")
            {
                addButton.Visibility = Visibility.Collapsed;
            }
            
            LoadSpecializations();
            LoadDoctors();
        }

        private void LoadSpecializations()
        {
            using var db = new MedRegistryContext();

            var specializations = db.Specializations
                .OrderBy(s => s.Name)
                .ToList();

            SpecializationFilter.Items.Clear();
            SpecializationFilter.Items.Add("Все");

            foreach (var s in specializations)
                SpecializationFilter.Items.Add(s.Name);

            SpecializationFilter.SelectedIndex = 0;
        }

        private void LoadDoctors()
        {
            using var db = new MedRegistryContext();

            var doctorsQuery = db.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var search = SearchBox.Text.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(search)
                );
            }


            if (SpecializationFilter.SelectedItem != null &&
                SpecializationFilter.SelectedItem.ToString() != "Все")
            {
                var selectedSpec = SpecializationFilter.SelectedItem.ToString();
                doctorsQuery = doctorsQuery.Where(d => d.Specialization.Name == selectedSpec);
            }

            var doctors = doctorsQuery.ToList();


            DoctorsWrapPanel.Children.Clear();


            // Обновляем счётчик
            DoctorsCountText.Text = $"Найдено врачей: {doctors.Count}";

            foreach (var d in doctors)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(8),
                    Padding = new Thickness(15),
                    Width = 380,
                    Height = 220,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 315,
                        ShadowDepth = 3,
                        Opacity = 0.3,
                        BlurRadius = 5
                    }
                };

                var sp = new StackPanel();

                sp.Children.Add(new TextBlock
                {
                    Text = $"Имя: {d.User?.FirstName} {d.User?.LastName}",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"Специализация: {d.Specialization?.Name ?? ""}",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 6)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"Опыт: {d.WorkExperienceYears ?? 0} лет",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 6)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"Кабинет: {d.CabinetNumber ?? "—"}",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12)
                });


                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                // Кнопки редактирования/удаления только для админа и регистратора
                if (_role == "Администратор" || _role == "Регистратор")
                {
                    var editBtn = new Button
                    {
                        Content = "Изменить",
                        Tag = d.DoctorId
                    };
                    editBtn.Click += EditDoctor_Click;
                    editBtn.Style = (Style)FindResource("EditButtonStyle");

                    var deleteBtn = new Button
                    {
                        Content = "Удалить",
                        Tag = d.DoctorId
                    };
                    deleteBtn.Click += DeleteDoctor_Click;
                    deleteBtn.Style = (Style)FindResource("DeleteButtonStyle");

                    btnPanel.Children.Add(editBtn);
                    btnPanel.Children.Add(deleteBtn);
                }

                sp.Children.Add(btnPanel);

                border.Child = sp;

                DoctorsWrapPanel.Children.Add(border);
            }
        }

        private void AddDoctor_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewDoctorWindow();
            if (window.ShowDialog() == true)
            {
                LoadDoctors();
            }
        }

        private void EditDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int doctorId)
            {
                var window = new EditDoctorWindow(doctorId);
                if (window.ShowDialog() == true)
                {
                    LoadDoctors();
                }
            }
        }

        private void DeleteDoctor_Click(object sender, RoutedEventArgs e)
        {
            int doctorId = (int)((Button)sender).Tag;

            using var db = new MedRegistryContext();

            var doctor = db.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefault(d => d.DoctorId == doctorId);

            if (doctor == null)
                return;

            // ✅ Проверка: есть ли приёмы
            if (doctor.Appointments != null && doctor.Appointments.Any())
            {
                MessageBox.Show(
                    "Нельзя удалить врача, так как у него есть связанные приёмы.",
                    "Удаление невозможно",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // ✅ Если приёмов нет — удаляем
            db.Doctors.Remove(doctor);
            db.SaveChanges();

            MessageBox.Show("Врач успешно удалён.");

            LoadDoctors();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDoctors();
        }

        private void SpecializationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDoctors();
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            SpecializationFilter.SelectedIndex = 0;
            LoadDoctors();
        }

        private void ToggleFilter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FilterPanel.Visibility == Visibility.Collapsed)
            {
                FilterPanel.Visibility = Visibility.Visible;
                FilterToggleIcon.Text = "▲";
                FilterToggleText.Text = "Скрыть фильтры";
            }
            else
            {
                FilterPanel.Visibility = Visibility.Collapsed;
                FilterToggleIcon.Text = "▼";
                FilterToggleText.Text = "Показать фильтры";
            }
        }
    }
}

