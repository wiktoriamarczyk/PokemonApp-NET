﻿using PokeApiNet;
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

        const int gridElementDimension = 200;

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

        public MainWindow()
        {
            InitializeComponent();
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
            IsHitTestVisible = false;
            StatisticPanel statisticPanel = new StatisticPanel();
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

            IsHitTestVisible = false;
            PokemonGridDisplay.Children.Clear();
            SetPageButtonsVisibilityState(false);
            PokemonGridInfo.Visibility = Visibility.Visible;
            PokemonGridInfo.Text = "Searching...";

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
                PokemonGridInfo.Text = "No pokemons found :(";
                return;
            }

            PokemonGridInfo.Visibility = Visibility.Hidden;

            foreach (var pokemon in filteredData)
            {
                PokemonCompactData pokemonData = await pokemonGridBuilder.InitPokemonBaseData(pokemon);
                var pokemonElement = CreatePokemonElement(pokemonData);
                PokemonGridDisplay.Children.Add(pokemonElement);
            }

            PokemonGridScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            PokemonGridScrollViewer.UpdateLayout();
            IsHitTestVisible = true;
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

        void PokeballListButton_Click(object sender, RoutedEventArgs e)
        {
            // display pokemons added to fav list
        }

        void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel loginPanel = new LoginPanel();
            this.Close();
            loginPanel.Show();
        }
    }
}