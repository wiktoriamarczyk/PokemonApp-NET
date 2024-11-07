using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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

        int _currentPage = 1;
        int currentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            pokemonGridBuilder = new PokemonGridBuilder();
            LoadPokemonGrid();
        }

        async void LoadPokemonGrid()
        {
            PokemonGridDisplay.Children.Clear();
            var pokemons = await pokemonGridBuilder.CreatePokemonsGrid(currentPage);

            foreach (var pokemon in pokemons)
            {
                var pokemonElement = CreatePokemonElement(pokemon);
                PokemonGridDisplay.Children.Add(pokemonElement);
            }
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
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                Child = stackPanel
            };

            var button = new Button
            {
                Content = border,
                Padding = new Thickness(0),
                Margin = new Thickness(5),
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(3),
                Background = Brushes.Transparent
            };

            button.Click += (s, e) => OnPokemonElementClick(pokemonData);

            return button;
        }

        async void OnPokemonElementClick(PokemonCompactData pokemonData)
        {
            var evol = pokeAPIController.GetPokemonEvolutionChain(pokemonData.pokemonBaseData.speciesURL);
            await evol;
            var x = evol.Result.evolutionElementsIds;
        }

        void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
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

        private void ButtonPreviousPage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonNextPage_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}