using PokeApiNet;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PokeAPI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PokemonGridBuilder pokemonGridBuilder;
        PokeAPIController pokeAPIController = PokeAPIController.Instance;

        readonly PokeContext context;

        static int _currentPage = Common.minPage;
        int currentPage
        {
            get => _currentPage;
            set
            {
                if (value <= 0)
                {
                    return;
                }

                _currentPage = value;
                UpdatePrevPageButtonVisibility();

                // Fetch next batch of pokemons if the user is close to the end of the current batch
                if (value % (Common.maxPagesToFetchOnOneRequest / 2) == 0)
                {
                    pokeAPIController.FetchPokemonsOnAnotherThread(value);
                }
            }
        }

        bool favListOpened = false;

        const string searchBoxWatermarkName = "watermark";
        const string noPokemonsFoundMessage = "No pokemons found :(";
        const string searchingMessage = "Searching...";
        const string noFavPokemonsMessage = "You don't have any favorite pokemons yet!";
        const int gridElementDimension = 200;

        public MainWindow(PokeContext context)
        {
            InitializeComponent();
            this.context = context;
            pokemonGridBuilder = new PokemonGridBuilder();
            UpdatePrevPageButtonVisibility();
            LoadPokemonGrid();
        }

        async void LoadPokemonGrid()
        {
            PokemonGridDisplay.Children.Clear();
            IsHitTestVisible = false;

            var pokemons = await pokemonGridBuilder.CreatePokemonsGrid(currentPage);
            foreach (var pokemon in pokemons)
            {
                var pokemonElement = CreatePokemonElement(pokemon);

                PokemonGridDisplay.Children.Add(pokemonElement);
            }
            IsHitTestVisible = true;
        }

        // Create pokemon element based on data fetched from the API and converted to compact data
        Button CreatePokemonElement(PokemonCompactData pokemonData)
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri(pokemonData.pokemonBaseData.spriteURL)),
                Width = gridElementDimension,
                Height = gridElementDimension,
                Margin = new Thickness(2)
            };

            var nameText = new TextBlock
            {
                Text = pokemonData.pokemonBaseData.name,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var button = InitializePokemonElement(image, nameText);
            button.Click += (s, e) => OnPokemonElementClick(pokemonData);
            return button;
        }

        Button InitializePokemonElement(Image image, TextBlock nameText)
        {
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

            return button;
        }

        // Create pokemon element based on data stored in the db
        Button CreatePokemonElement(Data.Pokemon pokemon)
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri(pokemon.ImageUrl)),
                Width = gridElementDimension,
                Height = gridElementDimension,
                Margin = new Thickness(2)
            };

            var nameText = new TextBlock
            {
                Text = pokemon.Name,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var button = InitializePokemonElement(image, nameText);
            button.Click += (s, e) => OnPokemonElementClick(pokemon);
            return button;
        }

        // Display detailed information about the pokemon based on the data stored in the db
        async void OnPokemonElementClick(Data.Pokemon pokemon)
        {
            IsHitTestVisible = false;
            StatisticPanel statisticPanel = new StatisticPanel(context);

            Pokemon pokemonApiData = await pokeAPIController.GetPokemon(pokemon.PokeApiId);
            PokemonCompactData pokemonData = await pokemonGridBuilder.InitPokemonBaseData(pokemonApiData);
            pokemonData = await pokemonGridBuilder.InitPokemonExtendedData(pokemonData);

            statisticPanel.Show();
            this.Close();
            statisticPanel.Init(pokemonData);
        }

        // Display detailed information about the pokemon based on the data fetched from the API and converted to compact data
        async void OnPokemonElementClick(PokemonCompactData pokemonData)
        {
            IsHitTestVisible = false;
            StatisticPanel statisticPanel = new StatisticPanel(context);
            pokemonData = await pokemonGridBuilder.InitPokemonExtendedData(pokemonData);
            statisticPanel.Show();
            this.Close();
            statisticPanel.Init(pokemonData);
        }

        void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (string.IsNullOrEmpty(box.Text))
                    box.Background = (ImageBrush)FindResource(searchBoxWatermarkName);
                else
                    box.Background = Brushes.White;
            }

            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                currentPage = Common.minPage;
                LoadPokemonGrid();
                SetPageButtonsVisibilityState(true);
                PokemonGridScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                PokemonGridInfo.Visibility = Visibility.Hidden;
                return;
            }
        }

        // TODO - display pokemons in batches
        async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrEmpty(searchText))
                return;

            // Disable user input while searching
            IsHitTestVisible = false;
            PokemonGridDisplay.Children.Clear();
            SetPageButtonsVisibilityState(false);
            PokemonGridInfo.Visibility = Visibility.Visible;
            PokemonGridInfo.Text = searchingMessage;

            List<Pokemon> filteredData = new List<Pokemon>();

            try
            {
                filteredData = await pokeAPIController.FindPokemonsStartingWith(searchText);
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Search operation was cancelled");
            }

            if (filteredData == null || filteredData.Count == 0)
            {
                PokemonGridInfo.Text = noPokemonsFoundMessage;
                // Enable user input
                IsHitTestVisible = true;
                return;
            }

            PokemonGridInfo.Visibility = Visibility.Hidden;

            foreach (var pokemon in filteredData)
            {
                if (pokemon.IsDefault == false)
                    continue;
                PokemonCompactData pokemonData = await pokemonGridBuilder.InitPokemonBaseData(pokemon);
                var pokemonElement = CreatePokemonElement(pokemonData);
                PokemonGridDisplay.Children.Add(pokemonElement);
            }

            PokemonGridScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            PokemonGridScrollViewer.UpdateLayout();
            // Enable user input
            IsHitTestVisible = true;
        }

        async void FavPokemonList_Click(object sender, RoutedEventArgs e)
        {
            SetPageButtonsVisibilityState(true);
            UpdatePrevPageButtonVisibility();

            PokemonGridInfo.Visibility = Visibility.Hidden;
            // Toggle between favorite pokemons and all pokemons
            favListOpened = !favListOpened;
            if (!favListOpened)
            {
                SearchButton.Visibility = Visibility.Visible;
                SearchTextBox.Visibility = Visibility.Visible;

                LoadPokemonGrid();
                return;
            }

            PokemonGridDisplay.Children.Clear();
            SearchButton.Visibility = Visibility.Hidden;
            SearchTextBox.Visibility = Visibility.Hidden;
            SetPageButtonsVisibilityState(false);

            Data.User loggedInUser = context.loggedInUser;
            List<Data.PokemonUser> pokemonUsers = context.PokemonsUsers.Where(pu => pu.UserId == loggedInUser.Id).ToList();

            // If user has no favorite pokemons, display a message
            if (pokemonUsers == null || pokemonUsers.Count == 0)
            {
                PokemonGridInfo.Visibility = Visibility.Visible;
                PokemonGridInfo.Text = noFavPokemonsMessage;
                return;
            }

            // Fetch favorite pokemons from the db
            var pokemonsDB = context.Pokemons
                .Where(p => context.PokemonsUsers.Any(pu => pu.PokemonId == p.Id && pu.UserId == loggedInUser.Id))
                .ToList();

            // Display favorite pokemons in the grid
            foreach (var pokemonDB in pokemonsDB)
            {
                var pokemonElement = CreatePokemonElement(pokemonDB);
                PokemonGridDisplay.Children.Add(pokemonElement);
            }

            //// Convert Data.Pokemon stored in the db to Pokemon from the API
            //foreach (var pokemonDB in pokemonsDB)
            //{
            //    var pokemon = await pokeAPIController.GetPokemon(pokemonDB.PokeApiId);
            //    PokemonCompactData pokemonData = await pokemonGridBuilder.InitPokemonBaseData(pokemon);
            //    var pokemonElement = CreatePokemonElement(pokemonData);
            //    PokemonGridDisplay.Children.Add(pokemonElement);
            //}

            PokemonGridScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            PokemonGridScrollViewer.UpdateLayout();
        }

        void UpdatePrevPageButtonVisibility()
        {
            if (currentPage == Common.minPage)
                PreviousPageButton.Visibility = Visibility.Hidden;
            else if (PreviousPageButton.Visibility == Visibility.Hidden)
                PreviousPageButton.Visibility = Visibility.Visible;
        }

        void SetPageButtonsVisibilityState(bool isVisible)
        {
            PreviousPageButton.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            NextPageButton.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
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

        void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel loginPanel = new LoginPanel(context);
            this.Close();
            loginPanel.Show();
        }
    }
}