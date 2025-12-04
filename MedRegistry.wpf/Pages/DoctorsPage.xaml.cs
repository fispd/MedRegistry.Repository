using DataLayer.Data;
using DataLayer.Models;
using MedRegistryApp.wpf.Windows;
using MedRegistryApp.wpf.Windows.Edit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Страница просмотра и управления списком врачей.
    /// Поддерживает фильтрацию по специализации и поиск по имени.
    /// </summary>
    public partial class DoctorsPage : Page
    {
        private int _userId;
        private string _role;

        /// <summary>
        /// Конструктор страницы врачей.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль пользователя</param>
        public DoctorsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            
            ConfigureButtonVisibility();
            LoadSpecializations();
            LoadDoctors();
        }

        /// <summary>
        /// Настраивает видимость кнопки добавления врача.
        /// Доступна только администраторам.
        /// </summary>
        private void ConfigureButtonVisibility()
        {
            var addButton = this.FindName("AddDoctorButton") as Button;
            if (addButton != null && _role != "Администратор")
            {
                addButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Загружает список специализаций в фильтр.
        /// </summary>
        private void LoadSpecializations()
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке специализаций: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает и отображает список врачей с учётом фильтров.
        /// </summary>
        private void LoadDoctors()
        {
            try
            {
                using var db = new MedRegistryContext();

                var query = db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .AsQueryable();

                // Применяем поиск по имени
                query = ApplySearchFilter(query);

                // Применяем фильтр по специализации
                query = ApplySpecializationFilter(query);

                var doctors = query.ToList();

                DisplayDoctors(doctors);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке врачей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет фильтр поиска по имени.
        /// </summary>
        private IQueryable<Doctor> ApplySearchFilter(IQueryable<Doctor> query)
        {
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var search = SearchBox.Text.Trim().ToLower();
                query = query.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(search) ||
                    (d.User.LastName + " " + d.User.FirstName).ToLower().Contains(search)
                );
            }
            return query;
        }

        /// <summary>
        /// Применяет фильтр по специализации.
        /// </summary>
        private IQueryable<Doctor> ApplySpecializationFilter(IQueryable<Doctor> query)
        {
            if (SpecializationFilter.SelectedItem != null &&
                SpecializationFilter.SelectedItem.ToString() != "Все")
            {
                var selectedSpec = SpecializationFilter.SelectedItem.ToString();
                query = query.Where(d => d.Specialization.Name == selectedSpec);
            }
            return query;
        }

        /// <summary>
        /// Отображает карточки врачей.
        /// </summary>
        /// <param name="doctors">Список врачей</param>
        private void DisplayDoctors(List<Doctor> doctors)
        {
            DoctorsWrapPanel.Children.Clear();
            DoctorsCountText.Text = $"Найдено врачей: {doctors.Count}";

            if (doctors.Count == 0)
            {
                DoctorsWrapPanel.Children.Add(CreateEmptyMessage("Врачи не найдены"));
                return;
            }

            foreach (var doctor in doctors)
            {
                var card = CreateDoctorCard(doctor);
                DoctorsWrapPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// Создаёт визуальную карточку врача.
        /// </summary>
        /// <param name="doctor">Данные врача</param>
        /// <returns>Border элемент с карточкой</returns>
        private Border CreateDoctorCard(Doctor doctor)
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

            // ФИО врача
            sp.Children.Add(new TextBlock
            {
                Text = $"👨‍⚕️ {doctor.User?.LastName} {doctor.User?.FirstName} {doctor.User?.MiddleName}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Специализация
            sp.Children.Add(new TextBlock
            {
                Text = $"🏥 Специализация: {doctor.Specialization?.Name ?? "—"}",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Опыт работы
            sp.Children.Add(new TextBlock
            {
                Text = $"📅 Опыт: {doctor.WorkExperienceYears ?? 0} лет",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Кабинет
            sp.Children.Add(new TextBlock
            {
                Text = $"🚪 Кабинет: {doctor.CabinetNumber ?? "—"}",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12)
            });

            // Кнопки управления (только для администратора)
            var btnPanel = CreateDoctorButtons(doctor);
            sp.Children.Add(btnPanel);

            border.Child = sp;
            return border;
        }

        /// <summary>
        /// Создаёт панель кнопок для карточки врача.
        /// </summary>
        /// <param name="doctor">Данные врача</param>
        /// <returns>Панель с кнопками</returns>
        private StackPanel CreateDoctorButtons(Doctor doctor)
        {
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            // Кнопки редактирования/удаления только для администратора
            if (_role == "Администратор")
            {
                var editBtn = new Button
                {
                    Content = "✏️ Изменить",
                    Tag = doctor.DoctorId,
                    Style = (Style)FindResource("EditButtonStyle")
                };
                editBtn.Click += EditDoctor_Click;

                var deleteBtn = new Button
                {
                    Content = "🗑️ Удалить",
                    Tag = doctor.DoctorId,
                    Style = (Style)FindResource("DeleteButtonStyle")
                };
                deleteBtn.Click += DeleteDoctor_Click;

                btnPanel.Children.Add(editBtn);
                btnPanel.Children.Add(deleteBtn);
            }

            return btnPanel;
        }

        /// <summary>
        /// Создаёт сообщение при отсутствии данных.
        /// </summary>
        private UIElement CreateEmptyMessage(string text)
        {
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(20),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        /// <summary>
        /// Открывает окно добавления нового врача.
        /// </summary>
        private void AddDoctor_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewDoctorWindow();
            if (window.ShowDialog() == true)
            {
                LoadDoctors();
            }
        }

        /// <summary>
        /// Открывает окно редактирования врача.
        /// </summary>
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

        /// <summary>
        /// Удаляет врача после проверки связанных записей.
        /// </summary>
        private void DeleteDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int doctorId = (int)((Button)sender).Tag;

                using var db = new MedRegistryContext();

                var doctor = db.Doctors
                    .Include(d => d.Appointments)
                    .Include(d => d.User)
                    .FirstOrDefault(d => d.DoctorId == doctorId);

                if (doctor == null)
                    return;

                // Проверка наличия связанных приёмов
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

                // Подтверждение удаления
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить врача {doctor.User?.LastName} {doctor.User?.FirstName}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                    return;

                db.Doctors.Remove(doctor);
                db.SaveChanges();

                MessageBox.Show("Врач успешно удалён.", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDoctors();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении врача: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик изменения текста поиска.
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDoctors();
        }

        /// <summary>
        /// Обработчик изменения фильтра специализации.
        /// </summary>
        private void SpecializationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                LoadDoctors();
        }

        /// <summary>
        /// Сбрасывает все фильтры к значениям по умолчанию.
        /// </summary>
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            SpecializationFilter.SelectedIndex = 0;
            LoadDoctors();
        }

        /// <summary>
        /// Переключает видимость панели фильтров.
        /// </summary>
        private void ToggleFilter_Click(object sender, MouseButtonEventArgs e)
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
