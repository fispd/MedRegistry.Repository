using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MedRegistryApp.wpf.Pages
{
    /// <summary>
    /// Логика взаимодействия для UsersPage.xaml
    /// </summary>
    public partial class UsersPage : Page
    {
        private List<Role> _roles = new();
        private List<User> _allUsers = new();
        private string _role;

        public UsersPage(int userId, string role)
        {
            InitializeComponent();
            _role = role;
            
            // Проверка доступа - только для администратора
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
                return;
            }
            
            LoadRolesFilter();
            LoadData();
        }

        private void LoadRolesFilter()
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

        private void LoadData()
        {
            using var db = new MedRegistryContext();

            _roles = db.Roles.ToList();
            _allUsers = db.Users.Include(u => u.Role).ToList();

            // Привязываем список ролей к ComboBox в колонке
            var roleColumn = UsersGrid.Columns.OfType<DataGridComboBoxColumn>()
                                              .FirstOrDefault(c => c.Header.ToString() == "Роль");
            if (roleColumn != null)
            {
                roleColumn.ItemsSource = _roles;
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allUsers.AsEnumerable();

            // Поиск по ФИО
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
            
            // Обновляем счётчик
            UsersCountText.Text = $"Найдено пользователей: {users.Count}";
            
            UsersGrid.ItemsSource = users;
        }

        // ====== ОБРАБОТЧИКИ ФИЛЬТРОВ ======

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

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            RoleFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Принудительно завершаем редактирование в DataGrid
                UsersGrid.CommitEdit(DataGridEditingUnit.Row, true);
                UsersGrid.CommitEdit();

                using var db = new MedRegistryContext();

                // Получаем все измененные элементы из DataGrid
                var changedUsers = new List<User>();
                foreach (var item in UsersGrid.Items)
                {
                    if (item is User user)
                    {
                        changedUsers.Add(user);
                    }
                }

                // Обновляем каждый пользователь в БД
                foreach (var user in changedUsers)
                {
                    // Валидация обязательных полей
                    if (string.IsNullOrWhiteSpace(user.Username))
                    {
                        MessageBox.Show($"Пользователь с ID {user.UserId}: логин не может быть пустым", 
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(user.FirstName))
                    {
                        MessageBox.Show($"Пользователь с ID {user.UserId}: имя не может быть пустым", 
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(user.LastName))
                    {
                        MessageBox.Show($"Пользователь с ID {user.UserId}: фамилия не может быть пустой", 
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    var dbUser = db.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.UserId == user.UserId);

                    if (dbUser != null)
                    {
                        // Проверка уникальности логина (если изменился)
                        if (user.Username != dbUser.Username)
                        {
                            var existingUser = db.Users.FirstOrDefault(u => u.Username == user.Username && u.UserId != user.UserId);
                            if (existingUser != null)
                            {
                                MessageBox.Show($"Логин '{user.Username}' уже используется другим пользователем", 
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                continue;
                            }
                        }

                        // Обновляем только изменяемые поля
                        dbUser.Username = user.Username;
                        dbUser.FirstName = user.FirstName;
                        dbUser.LastName = user.LastName;
                        dbUser.MiddleName = user.MiddleName;
                        dbUser.Phone = user.Phone;
                        dbUser.Email = user.Email;
                        dbUser.Address = user.Address;
                        dbUser.MedicalPolicy = user.MedicalPolicy;
                        
                        // Обновляем роль если она изменилась
                        if (user.RoleId != dbUser.RoleId)
                        {
                            var newRole = db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
                            if (newRole != null)
                            {
                                dbUser.RoleId = user.RoleId;
                            }
                        }

                        // Помечаем запись как измененную
                        db.Entry(dbUser).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                }

                db.SaveChanges();
                MessageBox.Show("Изменения успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Перезагружаем данные
                LoadData();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\nДетали: {ex.InnerException?.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not User selectedUser)
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя '{selectedUser.Username}'?",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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
        }
    }
}
