using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PokeAPI
{
    public class PokemonGridBuilder
    {
        public Action<PokemonCompactData> onPokemonDisplay;
        public List<PokemonCompactData> pokemonsData { get => _pokemonsData; }

        List<PokemonCompactData> _pokemonsData = new List<PokemonCompactData>();

        const int maxElementsInGrid = 9;

        public async Task<List<PokemonCompactData>> CreatePokemonsGrid(int page)
        {
            _pokemonsData.Clear();
            int startIndex = (page - 1) * maxElementsInGrid + 1;
            int endIndex = startIndex + maxElementsInGrid;
            List<Pokemon> pokemons = await PokeAPIController.Instance.GetPokemonsData(startIndex, endIndex);

            List<Task<PokemonCompactData>> pokemonElementsTasks = new List<Task<PokemonCompactData>>();

            foreach (Pokemon pokemon in pokemons)
            {
                pokemonElementsTasks.Add(InitPokemonBaseData(pokemon));
            }

            await Task.WhenAll(pokemonElementsTasks);
            return _pokemonsData;
        }

        // Initialize basic information about pokemon displayed in the grid of the main panel
        private async Task<PokemonCompactData> InitPokemonBaseData(Pokemon pokemon)
        {
            string spriteURL = pokemon.Sprites.Other.OfficialArtwork.FrontDefault;
            PokemonCompactData pokemonData = new PokemonCompactData();
            pokemonData.InitPokemonBaseData(pokemon, spriteURL);
            _pokemonsData.Add(pokemonData);
            return pokemonData;
        }

        // Initialize extended information about pokemon (abilities, evolution chain)
        // displayed in the details panel
        public async Task<PokemonCompactData> InitPokemonExtendedData(PokemonCompactData pokemonData)
        {
            var pokemonCompactData = _pokemonsData.First(p => p == pokemonData);
            pokemonCompactData.pokemonExtendedData = new PokemonExtendedData();
            pokemonCompactData.pokemonExtendedData.abilities = new List<AbilityCompactData>();
            int pokemonId = pokemonData.pokemonBaseData.id;
            Pokemon pokemon = await PokeAPIController.Instance.GetPokemonData(pokemonId);
            foreach (var ability in pokemon.Abilities)
            {
                string descriptionURL = await PokeAPIController.Instance.GetAbilityDescription(ability.Ability.Name);
                pokemonCompactData.pokemonExtendedData.abilities.Add(new AbilityCompactData
                {
                    name = ability.Ability.Name,
                    description = descriptionURL
                });
            }
            var evolutionChain = await PokeAPIController.Instance.GetPokemonEvolutionChain(pokemon.Species.Url);
            pokemonData.InitPokemonExtendedData(evolutionChain.evolutionId, pokemonCompactData.pokemonExtendedData.abilities);
            return pokemonData;
        }
    }
}
