using MedRegistryApp.wpf.Pages.Auth;
using System.Windows;

namespace MedRegistryApp.wpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            // Открываем страницу логина по умолчанию
            AuthFrame.Navigate(new LoginPage());
        }

        public void NavigateToLogin()
        {
            AuthFrame.Navigate(new LoginPage());
        }

        public void NavigateToRegister()
        {
            AuthFrame.Navigate(new RegisterPage());
        }

        public void NavigateToRegisterLogin(RegisterData data)
        {
            AuthFrame.Navigate(new RegisterLoginPage(data));
        }

        public void NavigateToForgotCredentials()
        {
            AuthFrame.Navigate(new ForgotCredentialsPage());
        }

        public void OpenMainWindow(int userId, string role)
        {
            var main = new MainWindow(userId, role);
            main.Show();
            this.Close();
        }
    }
}

