using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for StatisticPanel.xaml
    /// </summary>
    ///
    public partial class StatisticPanel : Window
    {
        PokeAPIController pokeAPIController = PokeAPIController.Instance;
        ObservableCollection<PokemonChainDisplay> pokemons;

        const string heightUnit = "cm";
        const string weightUnit = "kg";

        public class PokemonChainDisplay
        {
            public string ImageUrl;
            public string Name;
        }

        public StatisticPanel()
        {
            InitializeComponent();
        }

        public async void Init(PokemonCompactData pokemonData)
        {
            pokemons = new ObservableCollection<PokemonChainDisplay>();
            var evolutionChain = pokeAPIController.GetPokemonEvolutionChain(pokemonData.pokemonExtendedData.evolutionChainId);

            foreach (var pokemonId in evolutionChain.evolutionElementsIds)
            {
                var pokemon = await pokeAPIController.GetPokemonData(pokemonId);
                PokemonChainDisplay pokemonChainDisplay = new PokemonChainDisplay
                {
                    ImageUrl = pokemon.Sprites.Other.OfficialArtwork.FrontDefault,
                    Name = pokemon.Name
                };

                pokemons.Add(pokemonChainDisplay);
            }

            DisplayPokemonEvolutionChain(pokemons);
            DisplayFormattedBasicData(pokemonData);
            DisplayStatistics(pokemonData);
            DisplayAbilities(pokemonData);
        }

        void DisplayAbilities(PokemonCompactData pokemonData)
        {
            PokemonAbilities.Children.Clear();
            PokemonAbilities.RowDefinitions.Clear();

            int row = 0;
            foreach (var ability in pokemonData.pokemonExtendedData.abilities)
            {
                var abilityName = ability.name;
                var abilityDescription = ability.description;

                // Create a new row for each ability
                PokemonAbilities.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var abilityNameTextBlock = new TextBlock
                {
                    Text = abilityName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 20,
                    Margin = new Thickness(10),
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Name in the first column
                Grid.SetRow(abilityNameTextBlock, row);
                Grid.SetColumn(abilityNameTextBlock, 0);
                PokemonAbilities.Children.Add(abilityNameTextBlock);

                var abilityDescriptionTextBlock = new TextBlock
                {
                    Text = abilityDescription.Replace("\n", " "),
                    FontSize = 16,
                    Margin = new Thickness(10),
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                // Description in the second column
                Grid.SetRow(abilityDescriptionTextBlock, row);
                Grid.SetColumn(abilityDescriptionTextBlock, 1);
                PokemonAbilities.Children.Add(abilityDescriptionTextBlock);
                row++;

                // Add separator in the next row
                PokemonAbilities.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var separator = new Rectangle
                {
                    Height = 1,
                    Fill = new SolidColorBrush(Colors.White),
                    Opacity = 0.5,
                    Margin = new Thickness(5, 10, 5, 10)
                };

                Grid.SetRow(separator, row);
                Grid.SetColumnSpan(separator, 2);
                PokemonAbilities.Children.Add(separator);
                row++;
            }
        }

        void DisplayFormattedBasicData(PokemonCompactData pokemonData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pokemonData.pokemonBaseData.height).Append("0 ").Append(heightUnit);
            PokemonHeight.Text = sb.ToString();

            int weight = int.Parse(pokemonData.pokemonBaseData.weight);
            weight /= 10;

            sb.Clear();
            sb.Append(weight).Append(" ").Append(weightUnit);

            PokemonWeight.Text = sb.ToString();
            PokemonTypes.Text = string.Join(", ", pokemonData.pokemonBaseData.types);
            PokemonXP.Text = pokemonData.pokemonBaseData.xp.ToString();
        }

        void DisplayPokemonEvolutionChain(ObservableCollection<PokemonChainDisplay> pokemons)
        {
            PokemonEvolutionChain.Children.Clear();
            PokemonEvolutionChain.ColumnDefinitions.Clear();

            int column = 0;

            foreach (var pokemon in pokemons)
            {
                if (column >= PokemonEvolutionChain.ColumnDefinitions.Count)
                {
                    PokemonEvolutionChain.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                }

                var pokemonImage = new Image
                {
                    Source = new BitmapImage(new Uri(pokemon.ImageUrl)),
                    Width = 200,
                    Height = 200
                };

                var pokemonName = new TextBlock
                {
                    Text = pokemon.Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5)
                };

                // Image is displayed in the first row
                Grid.SetRow(pokemonImage, 0);
                Grid.SetColumn(pokemonImage, column);
                // Name is displayed in the second row
                Grid.SetRow(pokemonName, 1);
                Grid.SetColumn(pokemonName, column);

                PokemonEvolutionChain.Children.Add(pokemonImage);
                PokemonEvolutionChain.Children.Add(pokemonName);

                column++;
            }
        }

        void DisplayStatistics(PokemonCompactData pokemonData)
        {
            StatsGrid.Children.Clear();

            int row = 0;

            foreach (var stat in pokemonData.pokemonBaseData.statistics)
            {
                if (stat.value > 0)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = stat.name,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 17,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    Grid.SetRow(textBlock, row);
                    Grid.SetColumn(textBlock, 0);
                    StatsGrid.Children.Add(textBlock);

                    ProgressBar progressBar = new ProgressBar
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Minimum = 0,
                        Maximum = 100,
                        Value = stat.value,
                        Width = 250,
                        Height = 20,
                        Margin = new Thickness(20, 10, 0, 5),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Direction = 320, ShadowDepth = 5, Opacity = 0.5, BlurRadius = 5 },
                        ToolTip = $"{stat.value} / 100"
                    };
                    Grid.SetRow(progressBar, row);
                    Grid.SetColumn(progressBar, 1);
                    StatsGrid.Children.Add(progressBar);

                    row++;
                }
            }
        }

        void ReturnToMainPanelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }
    }
}
