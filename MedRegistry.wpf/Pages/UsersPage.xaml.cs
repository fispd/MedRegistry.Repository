using DataLayer.Data;
using DataLayer.Models;
using MedRegistryApp.wpf.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Страница управления пользователями системы.
    /// Доступна только администраторам.
    /// Позволяет просматривать, редактировать и удалять пользователей.
    /// </summary>
    public partial class UsersPage : Page
    {
        private List<Role> _roles = new();
        private List<User> _allUsers = new();
        private string _role;

        /// <summary>
        /// Конструктор страницы пользователей.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль текущего пользователя</param>
        public UsersPage(int userId, string role)
        {
            InitializeComponent();
            _role = role;
            
            if (!CheckAccess())
                return;
            
            LoadRolesFilter();
            LoadData();
        }

        /// <summary>
        /// Проверяет доступ к странице. Только администраторы имеют доступ.
        /// </summary>
        /// <returns>True если доступ разрешён</returns>
        private new bool CheckAccess()
        {
            if (_role != "Администратор")
            {
                MessageBox.Show("Доступ запрещён. Эта страница доступна только администраторам.", 
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                Content = new TextBlock 
                { 
                    Text = "Доступ запрещён", 
                    FontSize = 18, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                return false;
            }
            return true;
        }

        /// <summary>
        /// Загружает список ролей в фильтр.
        /// </summary>
        private void LoadRolesFilter()
        {
            try
            {
                using var db = new MedRegistryContext();
                _roles = db.Roles.ToList();
                
                RoleFilter.Items.Clear();
                RoleFilter.Items.Add("Все роли");
                
                foreach (var r in _roles)
                {
                    RoleFilter.Items.Add(r.RoleName);
                }
                
                RoleFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает данные пользователей из базы данных.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using var db = new MedRegistryContext();

                _roles = db.Roles.ToList();
                _allUsers = db.Users.Include(u => u.Role).ToList();

                // Привязываем список ролей к ComboBox в колонке DataGrid
                var roleColumn = UsersGrid.Columns.OfType<DataGridComboBoxColumn>()
                    .FirstOrDefault(c => c.Header.ToString() == "Роль");
                if (roleColumn != null)
                {
                    roleColumn.ItemsSource = _roles;
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет фильтры поиска и роли к списку пользователей.
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = _allUsers.AsEnumerable();

            // Поиск по ФИО и логину
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var search = SearchBox.Text.ToLower();
                filtered = filtered.Where(u =>
                    (u.LastName?.ToLower().Contains(search) == true) ||
                    (u.FirstName?.ToLower().Contains(search) == true) ||
                    (u.MiddleName?.ToLower().Contains(search) == true) ||
                    (u.Username?.ToLower().Contains(search) == true));
            }

            // Фильтр по роли
            if (RoleFilter.SelectedIndex > 0)
            {
                var selectedRole = RoleFilter.SelectedItem?.ToString();
                filtered = filtered.Where(u => u.Role?.RoleName == selectedRole);
            }

            var users = filtered.ToList();
            
            UsersCountText.Text = $"Найдено пользователей: {users.Count}";
            UsersGrid.ItemsSource = users;
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        /// <summary>
        /// Сбрасывает все фильтры к значениям по умолчанию.
        /// </summary>
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            RoleFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        /// <summary>
        /// Сохраняет изменения, внесённые в DataGrid, в базу данных.
        /// </summary>
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UsersGrid.CommitEdit(DataGridEditingUnit.Row, true);
                UsersGrid.CommitEdit();

                using var db = new MedRegistryContext();

                var changedUsers = UsersGrid.Items.OfType<User>().ToList();

                foreach (var user in changedUsers)
                {
                    if (!ValidateUser(user))
                        continue;

                    UpdateUserInDatabase(db, user);
                }

                db.SaveChanges();
                MessageBox.Show("Изменения успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Валидирует обязательные поля пользователя.
        /// </summary>
        private bool ValidateUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                MessageBox.Show($"Пользователь с ID {user.UserId}: логин не может быть пустым", 
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.FirstName))
            {
                MessageBox.Show($"Пользователь с ID {user.UserId}: имя не может быть пустым", 
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.LastName))
            {
                MessageBox.Show($"Пользователь с ID {user.UserId}: фамилия не может быть пустой", 
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Обновляет данные пользователя в базе данных.
        /// </summary>
        private void UpdateUserInDatabase(MedRegistryContext db, User user)
        {
            var dbUser = db.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == user.UserId);

            if (dbUser == null) return;

            // Проверка уникальности логина
            if (user.Username != dbUser.Username)
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Username == user.Username && u.UserId != user.UserId);
                if (existingUser != null)
                {
                    MessageBox.Show($"Логин '{user.Username}' уже используется другим пользователем", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Обновляем поля
            dbUser.Username = user.Username;
            dbUser.FirstName = user.FirstName;
            dbUser.LastName = user.LastName;
            dbUser.MiddleName = user.MiddleName;
            dbUser.Phone = user.Phone;
            dbUser.Email = user.Email;
            dbUser.Address = user.Address;
            dbUser.MedicalPolicy = user.MedicalPolicy;
            
            // Обновляем роль
            if (user.RoleId != dbUser.RoleId)
            {
                var newRole = db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
                if (newRole != null)
                {
                    dbUser.RoleId = user.RoleId;
                }
            }

            db.Entry(dbUser).State = EntityState.Modified;
        }

        /// <summary>
        /// Удаляет выбранного пользователя.
        /// </summary>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not User selectedUser)
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя '{selectedUser.Username}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new MedRegistryContext();
                    var userToDelete = db.Users.FirstOrDefault(u => u.UserId == selectedUser.UserId);
                    if (userToDelete != null)
                    {
                        db.Users.Remove(userToDelete);
                        db.SaveChanges();
                        MessageBox.Show("Пользователь удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Открывает окно добавления нового пользователя.
        /// </summary>
        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewUserWindow();
            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }
    }
}
