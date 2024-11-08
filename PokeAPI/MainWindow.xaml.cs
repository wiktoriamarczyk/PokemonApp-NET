using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace PokeAPI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PokemonGridBuilder pokemonGridBuilder;
        PokeAPIController pokeAPIController = PokeAPIController.Instance;

        const string searchBoxWatermarkName = "watermark";
        const int minPage = 1;

        static int _currentPage = minPage;
        int currentPage
        {
            get => _currentPage;
            set
            {
                if (value > 0)
                {
                    _currentPage = value;
                    UpdateButtonVisibilityBasedOnPage();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            pokemonGridBuilder = new PokemonGridBuilder();
            UpdateButtonVisibilityBasedOnPage();
            LoadPokemonGrid();
        }

        async void LoadPokemonGrid()
        {
            PokemonGridDisplay.Children.Clear();
            SetButtonsEnableState(false);

            var pokemons = await pokemonGridBuilder.CreatePokemonsGrid(currentPage);
            foreach (var pokemon in pokemons)
            {
                var pokemonElement = CreatePokemonElement(pokemon);

                PokemonGridDisplay.Children.Add(pokemonElement);
            }

            SetButtonsEnableState(true);
        }

        Button CreatePokemonElement(PokemonCompactData pokemonData)
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri(pokemonData.pokemonBaseData.spriteURL)),
                Width = 200,
                Height = 200,
                Margin = new Thickness(2)
            };

            var nameText = new TextBlock
            {
                Text = pokemonData.pokemonBaseData.name,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(nameText);

            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.Black,
                Child = stackPanel
            };

            var button = new Button
            {
                Content = border,
                Padding = new Thickness(0),
                Margin = new Thickness(5),
                BorderBrush = Brushes.CornflowerBlue,
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
            };

            button.Click += (s, e) => OnPokemonElementClick(pokemonData);

            return button;
        }

        async void OnPokemonElementClick(PokemonCompactData pokemonData)
        {
            StatisticPanel statisticPanel = new StatisticPanel();
            pokemonData = await pokemonGridBuilder.InitPokemonExtendedData(pokemonData);
            statisticPanel.Show();
            this.Close();
            statisticPanel.Init(pokemonData);
        }

        // TO FIX
        void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (string.IsNullOrEmpty(box.Text))
                    box.Background = (ImageBrush)FindResource(searchBoxWatermarkName);
                else
                    box.Background = Brushes.White;
            }

            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadPokemonGrid();
                return;
            }

            var filteredData = pokemonGridBuilder.pokemonsData.FindAll(p => p.pokemonBaseData.name.ToLower().Contains(searchText));
            PokemonGridDisplay.Children.Clear();
            foreach (var pokemonData in filteredData)
            {
                var pokemonElement = CreatePokemonElement(pokemonData);
                PokemonGridDisplay.Children.Add(pokemonElement);
            }
        }

        void UpdateButtonVisibilityBasedOnPage()
        {
            if (currentPage == minPage)
                PreviousPageButton.Visibility = Visibility.Hidden;
            else if (PreviousPageButton.Visibility == Visibility.Hidden)
                PreviousPageButton.Visibility = Visibility.Visible;
        }

        void SetButtonsEnableState(bool isEnabled)
        {
            PreviousPageButton.IsEnabled = isEnabled ? true : false;
            NextPageButton.IsEnabled = isEnabled ? true : false;
            PokeballListButton.IsEnabled = isEnabled ? true : false;
            LogoutButton.IsEnabled = isEnabled ? true : false;
        }

        void ButtonPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            currentPage--;
            LoadPokemonGrid();
        }

        void ButtonNextPage_Click(object sender, RoutedEventArgs e)
        {
            currentPage++;
            LoadPokemonGrid();
        }

        void PokeballListButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel loginPanel = new LoginPanel();
            this.Close();
            loginPanel.Show();
        }
    }
}