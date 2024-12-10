using System.Windows;
using System.Windows.Controls;
using PokeAPI.Data;

namespace PokeAPI
{
    /// <summary>
    /// Interaction logic for LoginPanel.xaml
    /// </summary>
    public partial class LoginPanel : Window
    {
        PokeContext context;
        StackPanel currentPanel;

        // Error messages
        const string userNameAlreadyExistsMessage = "Username already exists!";
        const string loginErrorMessage = "Invalid username or password!";
        const string passwordsDoNotMatchMessage = "Passwords do not match!";
        const string fillInAllFieldsMessage = "Please fill in all fields!";

        // Buttons text
        const string loginButtonText = "Back to Login";
        const string registerButtonText = "Register";

        public LoginPanel(PokeContext context)
        {
            InitializeComponent();
            this.context = context;
            // Start with the login panel
            currentPanel = LoginStackPanel;
            // Initialize PokeAPIController to fetch data in the background
            PokeAPIController pokeAPIController = PokeAPIController.Instance;

        }

        void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (LoginStackPanel.Visibility == Visibility.Visible)
            {
                LoginStackPanel.Visibility = Visibility.Hidden;
                RegisterStackPanel.Visibility = Visibility.Visible;
                currentPanel = RegisterStackPanel;
                TogglePanelButton.Text = loginButtonText;
            }
            else
            {
                LoginStackPanel.Visibility = Visibility.Visible;
                RegisterStackPanel.Visibility = Visibility.Hidden;
                currentPanel = LoginStackPanel;
                TogglePanelButton.Text = registerButtonText;
            }
        }

        void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginErrorBlock.Visibility = Visibility.Hidden;

            string userName = UserNameBox.Text;
            string password = PasswordBox.Password;

            // Find user in the database
            User user = context.Users.FirstOrDefault(u => u.Username == userName);

            // If user does not exist, show an error message
            if (user == null)
            {
                LoginErrorBlock.Text = loginErrorMessage;
                LoginErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            // Retrieve the salt and hashed password
            byte[] storedSaltBytes = Convert.FromBase64String(user.Salt);
            // Hash the entered password with the stored salt
            string enteredPasswordHash = Common.HashPassword(password, storedSaltBytes);

            if (enteredPasswordHash != user.Password)
            {
                LoginErrorBlock.Text = loginErrorMessage;
                LoginErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            context.loggedInUser = user;
            ShowMainPanel();
        }

        async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterErrorBlock.Visibility = Visibility.Hidden;

            string userName = RegisterUserNameBox.Text;
            string password = RegisterPasswordBox.Password;
            string confirmPassword = RegisterConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                RegisterErrorBlock.Text = fillInAllFieldsMessage;
                RegisterErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            if (password != confirmPassword)
            {
                RegisterErrorBlock.Text = passwordsDoNotMatchMessage;
                RegisterErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            // Hash the password with the salt
            byte[] saltBytes = Common.GenerateSalt();
            string hashedPassword = Common.HashPassword(password, saltBytes);
            string base64Salt = Convert.ToBase64String(saltBytes);

            // Create user
            User user = new User
            {
                Username = userName,
                Password = hashedPassword,
                Salt = base64Salt
            };

            // Find user in the database
            User existingUser = context.Users.FirstOrDefault(u => u.Username == userName);
            // If user already exists, show an error message
            if (existingUser != null)
            {
                RegisterErrorBlock.Text = userNameAlreadyExistsMessage;
                RegisterErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            context.loggedInUser = user;
            ShowMainPanel();
        }

        async void ShowMainPanel()
        {
            await PokeAPIController.Instance.Initialize();
            MainWindow mainWindow = new MainWindow(context);
            this.Close();
            mainWindow.Show();
        }
    }
}
