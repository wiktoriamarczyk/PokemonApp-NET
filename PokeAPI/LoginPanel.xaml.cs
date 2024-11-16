using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PokeAPI
{
    /// <summary>
    /// Interaction logic for LoginPanel.xaml
    /// </summary>
    public partial class LoginPanel : Window
    {
        StackPanel currentPanel;

        public LoginPanel()
        {
            InitializeComponent();
            currentPanel = LoginStackPanel;
            // initialize PokeAPIController to fetch data in the background
            PokeAPIController pokeAPIController = PokeAPIController.Instance;
        }

        void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (LoginStackPanel.Visibility == Visibility.Visible)
            {
                LoginStackPanel.Visibility = Visibility.Collapsed;
                RegisterStackPanel.Visibility = Visibility.Visible;
                currentPanel = RegisterStackPanel;
                TogglePanelButton.Text = "Back to Login";
            }
            else
            {
                LoginStackPanel.Visibility = Visibility.Visible;
                RegisterStackPanel.Visibility = Visibility.Collapsed;
                currentPanel = LoginStackPanel;
                TogglePanelButton.Text = "Register";
            }
        }

        void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            ShowMainPanel();
        }

        void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string email = RegisterEmailBox.Text;
            string password = RegisterPasswordBox.Password;
            string confirmPassword = RegisterConfirmPasswordBox.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match!");
                return;
            }

            ShowMainPanel();
        }

        async void ShowMainPanel()
        {
            await PokeAPIController.Instance.Initialize();
            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }
    }
}
