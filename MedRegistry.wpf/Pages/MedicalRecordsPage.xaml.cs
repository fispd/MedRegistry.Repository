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
    /// Логика взаимодействия для MedicalRecordsPage.xaml
    /// </summary>
    public partial class MedicalRecordsPage : Page
    {
        private int _userId;
        private string _role;
        private int? _filterPatientId;

        public MedicalRecordsPage(int userId, string role)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            LoadRecords();
        }

        public MedicalRecordsPage(int userId, string role, int patientId)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            _filterPatientId = patientId;
            LoadRecords();
        }

        private void LoadRecords()
        {
            using var db = new MedRegistryContext();

            IQueryable<MedicalRecord> query = db.MedicalRecords
                .Include(r => r.Patient).ThenInclude(p => p.User)
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.Appointment);

            if (_filterPatientId.HasValue)
            {
                query = query.Where(r => r.PatientId == _filterPatientId.Value);
            }
            else if (_role == "Администратор")
            {
            }
            else if (_role == "Врач")
            {
                query = query.Where(r => r.Doctor.UserId == _userId);
            }
            else
            {
                query = query.Where(r => r.Patient.UserId == _userId);
            }

            var records = query
                .OrderByDescending(r => r.RecordDate)
                .ToList();

            DisplayRecords(records);
        }

        private void DisplayRecords(List<MedicalRecord> records)
        {
            RecordsWrapPanel.Children.Clear();

            if (records.Count == 0)
            {
                string emptyMessage;
                if (_filterPatientId.HasValue)
                {
                    emptyMessage = "У пациента нет медицинских документов";
                }
                else if (_role == "Администратор")
                {
                    emptyMessage = "Нет медицинских документов";
                }
                else if (_role == "Врач")
                {
                    emptyMessage = "У вас пока нет созданных отчётов";
                }
                else
                {
                    emptyMessage = "У вас пока нет медицинских документов";
                }
                RecordsWrapPanel.Children.Add(CreateEmptyMessage(emptyMessage));
                return;
            }

            foreach (var r in records)
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
                
                sp.Children.Add(new TextBlock
                {
                    Text = $"📋 {r.RecordDate:dd.MM.yyyy}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(11, 127, 199)),
                    Margin = new Thickness(0, 0, 0, 10)
                });
                
                sp.Children.Add(new TextBlock
                {
                    Text = $"👤 Пациент: {r.Patient?.User?.LastName} {r.Patient?.User?.FirstName}",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Margin = new Thickness(0, 0, 0, 5)
                });
                
                sp.Children.Add(new TextBlock 
                { 
                    Text = $"👨‍⚕️ Врач: {r.Doctor?.User?.LastName} {r.Doctor?.User?.FirstName}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    Margin = new Thickness(0, 0, 0, 10)
                });
                
                sp.Children.Add(new TextBlock
                {
                    Text = "Диагноз:",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(0, 0, 0, 3)
                });
                sp.Children.Add(new TextBlock 
                { 
                    Text = r.Diagnosis ?? "—",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                });
                
                sp.Children.Add(new TextBlock
                {
                    Text = "Лечение:",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(0, 0, 0, 3)
                });
                sp.Children.Add(new TextBlock 
                { 
                    Text = r.Treatment ?? "—",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                });
                
                if (!string.IsNullOrWhiteSpace(r.Notes))
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = "Примечания:",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        Margin = new Thickness(0, 0, 0, 3)
                    });
                    sp.Children.Add(new TextBlock 
                    { 
                        Text = r.Notes,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                border.Child = sp;
                RecordsWrapPanel.Children.Add(border);
            }
        }

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
