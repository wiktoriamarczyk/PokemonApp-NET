using PokeApiNet;
using System.Collections.Generic;
using System.Security.Policy;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PokeAPI
{
    public class PokemonCompactData
    {
        public PokemonBaseData pokemonBaseData;
        public PokemonExtendedData? pokemonExtendedData;

        public void InitPokemonBaseData(Pokemon pokemon, string spriteURL)
        {
            pokemonBaseData = new PokemonBaseData();
            pokemonBaseData.id = pokemon.Id;
            pokemonBaseData.name = pokemon.Name;
            pokemonBaseData.height = pokemon.Height.ToString();
            pokemonBaseData.weight = pokemon.Weight.ToString();
            pokemonBaseData.xp = pokemon.BaseExperience;
            pokemonBaseData.spriteURL = spriteURL;
            pokemonBaseData.speciesURL = pokemon.Species.Url;
            pokemonBaseData.types = new List<string>();
            foreach (var type in pokemon.Types)
            {
                pokemonBaseData.types.Add(type.Type.Name);
            }
            pokemonBaseData.statistics = new List<StatisticsCompactData>();
            foreach (var stat in pokemon.Stats)
            {
                StatisticsCompactData statData = new StatisticsCompactData();
                statData.name = stat.Stat.Name;
                statData.value = stat.BaseStat;
                pokemonBaseData.statistics.Add(statData);
            }
        }

        public void InitPokemonExtendedData(int evolutionChainId, List<AbilityCompactData> abilitiesDescriptions)
        {
            pokemonExtendedData = new PokemonExtendedData();
            pokemonExtendedData.evolutionChainId = evolutionChainId;
            pokemonExtendedData.abilities = abilitiesDescriptions;
        }
    }


    public class PokemonBaseData
    {
        public int id;
        public string name;
        public string height;
        public string weight;
        public int? xp;
        public string spriteURL;
        public string speciesURL;
        public List<string> types;
        public List<StatisticsCompactData> statistics;
    }

    public class PokemonExtendedData
    {
        public int evolutionChainId;
        public List<AbilityCompactData> abilities;
    }

    public class EvolutionChainCompactData
    {
        public int evolutionId;
        public List<int> evolutionElementsIds;
    }

    public struct StatisticsCompactData
    {
        public string name;
        public int value;
    }

    public struct AbilityCompactData
    {
        public string name;
        public string description;
    }
}