using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MedRegistry.wpf.Pages
{
    /// <summary>
    /// Страница просмотра медицинских записей (документов).
    /// Отображает историю посещений с диагнозами и назначениями.
    /// </summary>
    public partial class MedicalRecordsPage : Page
    {
        private int _userId;
        private string _role;
        private int? _filterPatientId;

        /// <summary>
        /// Конструктор страницы медицинских записей.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль пользователя</param>
        public MedicalRecordsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            LoadRecords();
        }

        /// <summary>
        /// Конструктор с фильтрацией по конкретному пациенту.
        /// </summary>
        /// <param name="userId">ID текущего пользователя</param>
        /// <param name="role">Роль пользователя</param>
        /// <param name="patientId">ID пациента для фильтрации</param>
        public MedicalRecordsPage(int userId, string role, int patientId)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            _filterPatientId = patientId;
            LoadRecords();
        }

        /// <summary>
        /// Загружает медицинские записи из базы данных.
        /// Фильтрует по роли пользователя или указанному пациенту.
        /// </summary>
        private void LoadRecords()
        {
            try
            {
                using var db = new MedRegistryContext();

                IQueryable<MedicalRecord> query = db.MedicalRecords
                    .Include(r => r.Patient).ThenInclude(p => p.User)
                    .Include(r => r.Doctor).ThenInclude(d => d.User)
                    .Include(r => r.Appointment);

                query = ApplyRoleFilter(query);

                var records = query
                    .OrderByDescending(r => r.RecordDate)
                    .ToList();

                DisplayRecords(records);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке записей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет фильтрацию записей в зависимости от роли пользователя.
        /// </summary>
        private IQueryable<MedicalRecord> ApplyRoleFilter(IQueryable<MedicalRecord> query)
        {
            // Фильтр по конкретному пациенту (например, при просмотре врачом)
            if (_filterPatientId.HasValue)
            {
                return query.Where(r => r.PatientId == _filterPatientId.Value);
            }

            // Фильтрация по роли
            return _role switch
            {
                "Администратор" => query, // Видит все записи
                "Врач" => query.Where(r => r.Doctor.UserId == _userId), // Только свои отчёты
                _ => query.Where(r => r.Patient.UserId == _userId) // Пациент видит свои записи
            };
        }

        /// <summary>
        /// Отображает карточки медицинских записей.
        /// </summary>
        /// <param name="records">Список записей для отображения</param>
        private void DisplayRecords(List<MedicalRecord> records)
        {
            RecordsWrapPanel.Children.Clear();

            if (records.Count == 0)
            {
                RecordsWrapPanel.Children.Add(CreateEmptyMessage(GetEmptyMessage()));
                return;
            }

            foreach (var record in records)
            {
                var card = CreateRecordCard(record);
                RecordsWrapPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// Возвращает сообщение при отсутствии записей.
        /// </summary>
        private string GetEmptyMessage()
        {
            if (_filterPatientId.HasValue)
                return "У пациента нет медицинских документов";
            
            return _role switch
            {
                "Администратор" => "Нет медицинских документов",
                "Врач" => "У вас пока нет созданных отчётов",
                _ => "У вас пока нет медицинских документов"
            };
        }

        /// <summary>
        /// Создаёт визуальную карточку медицинской записи.
        /// </summary>
        /// <param name="record">Данные медицинской записи</param>
        /// <returns>Border элемент с карточкой</returns>
        private Border CreateRecordCard(MedicalRecord record)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(8),
                Padding = new Thickness(15),
                Width = 380,
                MinHeight = 220,
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
            
            // Дата записи
            sp.Children.Add(new TextBlock
            {
                Text = $"📋 {record.RecordDate:dd.MM.yyyy}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Информация о пациенте
            sp.Children.Add(new TextBlock
            {
                Text = $"👤 Пациент: {record.Patient?.User?.LastName} {record.Patient?.User?.FirstName}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            // Информация о враче
            sp.Children.Add(new TextBlock 
            { 
                Text = $"👨‍⚕️ Врач: {record.Doctor?.User?.LastName} {record.Doctor?.User?.FirstName}",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Диагноз
            AddLabeledText(sp, "Диагноз:", record.Diagnosis ?? "—");
            
            // Лечение
            AddLabeledText(sp, "Лечение:", record.Treatment ?? "—");
            
            // Примечания (всегда отображаются для всех пользователей)
            AddLabeledText(sp, "Примечания:", !string.IsNullOrWhiteSpace(record.Notes) ? record.Notes : "—");

            border.Child = sp;
            return border;
        }

        /// <summary>
        /// Добавляет подписанный текстовый блок в контейнер.
        /// </summary>
        private void AddLabeledText(StackPanel sp, string label, string value)
        {
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Margin = new Thickness(0, 0, 0, 3)
            });
            sp.Children.Add(new TextBlock 
            { 
                Text = value,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });
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
    }
}
