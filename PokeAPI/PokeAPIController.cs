﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using PokeApiNet;


namespace PokeAPI
{
    public class PokeAPIController
    {
        private static PokeAPIController _instance;
        public static PokeAPIController Instance => _instance ??= new PokeAPIController();

        public HashSet<Pokemon> pokemons { get; private set; } = new HashSet<Pokemon>();
        public List<EvolutionChainCompactData> evolutionChains { get; private set; } = new List<EvolutionChainCompactData>();

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancelToken;
        //PokeAPIBackend pokeAPIBackend = new PokeAPIBackend();
        PokeApiClient pokeApiNet = new PokeApiClient();

        int maxPokemonCount;
        const string language = "en";

        public PokeAPIController()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancelToken = cancellationTokenSource.Token;
        }

        public void CancelTasks()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        public async Task<int> GetPokemonCount()
        {
            //var data = await pokeAPIBackend.GetApiResourcePageAsync<PokemonApiResource>(1, 0, cancellationTokenSource.Token);
            //return data.Count;
            var data = await pokeApiNet.GetNamedResourcePageAsync<Pokemon>(1, 0, cancellationTokenSource.Token);
            return data.Count;
        }

        public async Task<List<Pokemon>> GetPokemonsData(int startIndex, int endIndex)
        {
            bool isRangeValid = await ValidateRange(startIndex, endIndex);
            if (!isRangeValid)
            {
                return null;
            }

            List<Pokemon> pokemons = new List<Pokemon>();
            for (int id = startIndex; id < endIndex; ++id)
            {
                Pokemon pokemon = await GetPokemonData(id);
                if (pokemon != null)
                {
                    pokemons.Add(pokemon);
                    Console.WriteLine($"Pokemon [{pokemon.Id}]: {pokemon.Name}");
                }
            }
            return pokemons;
        }

        public async Task<Pokemon> GetPokemonData(int id)
        {
            Pokemon pokemon = pokemons.FirstOrDefault(p => p.Id == id);
            if (pokemon != null)
            {
                return pokemon;
            }
            // pokemon = await pokeAPIBackend.GetResourceAsync<Pokemon>(id, cancelToken);
            pokemon = await pokeApiNet.GetResourceAsync<Pokemon>(id);
            if (pokemon != null)
            {
                pokemons.Add(pokemon);
            }
            return pokemon;
        }

        public async Task<Pokemon> GetPokemonData(string name)
        {
            Pokemon pokemon = pokemons.FirstOrDefault(p => p.Name == name);
            if (pokemon != null)
            {
                return pokemon;
            }
            // pokemon = await pokeAPIBackend.GetResourceAsync<Pokemon>(name, cancelToken);
            pokemon = await pokeApiNet.GetResourceAsync<Pokemon>(name);
            if (pokemon != null)
            {
                pokemons.Add(pokemon);
            }
            return pokemon;
        }

        public async Task<string> GetAbilityDescription(string abilityName)
        {
            string result = string.Empty;
            // Ability ability = await pokeAPIBackend.GetResourceAsync<Ability>(abilityName, cancellationTokenSource.Token);
            Ability ability = await pokeApiNet.GetResourceAsync<Ability>(abilityName);
            return ability?.EffectEntries.FirstOrDefault(e => e.Language.Name == language)?.Effect ?? string.Empty;
        }

        public async Task<EvolutionChainCompactData> GetPokemonEvolutionChain(string speciesUrl)
        {
            //PokemonSpecies species = await pokeAPIBackend.GetResourceByUrlAsync<PokemonSpecies>(speciesUrl, cancellationTokenSource.Token);
            Uri uri = new Uri(speciesUrl);
            string speciesId = uri.Segments[^1].TrimEnd('/');

            PokemonSpecies species = await pokeApiNet.GetResourceAsync<PokemonSpecies>(int.Parse(speciesId));
            if (species == null)
            {
                return null;
            }

            //EvolutionChain evolutionChain = await pokeAPIBackend.GetResourceByUrlAsync<EvolutionChain>(species.EvolutionChain.Url, cancellationTokenSource.Token);
            uri = new Uri(species.EvolutionChain.Url);
            string evolutionChainId = uri.Segments[^1].TrimEnd('/');

            EvolutionChain evolutionChain = await pokeApiNet.GetResourceAsync<EvolutionChain>(int.Parse(evolutionChainId));
            if (evolutionChain == null)
            {
                return null;
            }

            int evolutionId = evolutionChain.Id;
            EvolutionChainCompactData evolutionChainData = evolutionChains.FirstOrDefault(e => e.evolutionId == evolutionId);
            if (evolutionChainData != null)
            {
                return evolutionChainData;
            }

            evolutionChainData = new EvolutionChainCompactData
            {
                evolutionElementsIds = new List<int>(),
                evolutionId = evolutionId
            };

            ChainLink chain = evolutionChain.Chain;
            string pokemonName = chain.Species.Name;
            Pokemon pokemon = await GetPokemonData(pokemonName);
            if (pokemon == null)
            {
                return null;
            }
            evolutionChainData.evolutionElementsIds.Add(pokemon.Id);

            if (chain.EvolvesTo == null || chain.EvolvesTo.Count == 0)
            {
                evolutionChains.Add(evolutionChainData);
                return evolutionChainData;
            }

            do
            {
                pokemonName = chain.EvolvesTo[0].Species.Name;
                pokemon = await GetPokemonData(pokemonName);
                if (pokemon != null)
                {
                    evolutionChainData.evolutionElementsIds.Add(pokemon.Id);
                }
                chain = chain.EvolvesTo[0];

            } while (chain.EvolvesTo.Count > 0);

            evolutionChains.Add(evolutionChainData);

            return evolutionChainData;
        }

        private async Task<bool> ValidateRange(int startIndex, int endIndex)
        {
            if (maxPokemonCount == default)
            {
                maxPokemonCount = await GetPokemonCount();
            }

            if (startIndex < 1 || endIndex > maxPokemonCount)
            {
                Console.WriteLine("Invalid range");
                return false;
            }
            return true;
        }
    }
}