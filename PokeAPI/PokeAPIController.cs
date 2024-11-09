using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using PokeApiNet;


namespace PokeAPI
{
    public class PokeAPIController
    {
        public static PokeAPIController Instance => _instance ??= new PokeAPIController();
        static PokeAPIController _instance;

        HashSet<Pokemon> pokemons { get; set; } = new HashSet<Pokemon>();
        List<EvolutionChainCompactData> evolutionChains { get; set; } = new List<EvolutionChainCompactData>();

        PokeApiClient pokeApiNet = new PokeApiClient();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancelToken;

        int maxPokemonCount;
        const string language = "en";

        public PokeAPIController()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancelToken = cancellationTokenSource.Token;
            FetchPokemonsOnAnotherThread(1);
        }

        ~PokeAPIController()
        {
            CancelTasks();
        }

        public void CancelTasks()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        // Fetch Common.maxPagesToFetchOnOneRequest * Common.maxPokemonsInGrid pokemons starting from the given page
        // with batches of 9 pokemons each
        public async Task FetchPokemonsOnAnotherThread(int page)
        {
            maxPokemonCount = await GetPokemonCount();

            await Task.Run(async () =>
            {
                try
                {
                    const int maxPokemonsInGrid = Common.maxPokemonsInGrid;
                    const int maxPokemonsToFetch = maxPokemonsInGrid * Common.maxPagesToFetchOnOneRequest;

                    int startIndex = (page - 1) * maxPokemonsInGrid + 1;
                    int endIndex = Math.Min(startIndex + maxPokemonsInGrid, maxPokemonCount);

                    int pokemonsLeft = Math.Min(maxPokemonsToFetch, endIndex - startIndex);

                    while (pokemonsLeft > 0)
                    {
                        Trace.WriteLine($"Fetching pokemons {startIndex}-{endIndex}");
                        List<Pokemon> pokemonsBatch = await PokeAPIController.Instance.GetPokemons(startIndex, endIndex);
                        if (pokemonsBatch != null && pokemonsBatch.Any())
                        {
                            foreach (var pokemon in pokemonsBatch)
                            {
                               // Trace.WriteLine($"Pokemon [{pokemon.Id}]: {pokemon.Name}");
                            }
                        }

                        page++;
                        startIndex = (page - 1) * maxPokemonsInGrid + 1;
                        endIndex = Math.Min(startIndex + maxPokemonsInGrid, maxPokemonCount);
                        pokemonsLeft--;

                        if (cancelToken.IsCancellationRequested)
                        {
                            Trace.WriteLine("Task was cancelled.");
                            break;
                        }
                    }

                    Trace.WriteLine("All pokemons fetched.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"An error occurred while fetching pokemons: {ex.Message}");
                }
            });
        }

        public async Task<List<Pokemon>> GetPokemons(int startIndex, int endIndex)
        {
            bool isRangeValid = await ValidateRange(startIndex, endIndex);
            if (!isRangeValid)
            {
                return null;
            }

            List<Pokemon> pokemons = new List<Pokemon>();
            for (int id = startIndex; id < endIndex; ++id)
            {
                Pokemon pokemon = await GetPokemon(id);
                if (pokemon != null)
                {
                    pokemons.Add(pokemon);
                    Console.WriteLine($"Pokemon [{pokemon.Id}]: {pokemon.Name}");
                }
            }
            return pokemons;
        }

        public async Task<Pokemon> GetPokemon(int id)
        {
            Pokemon pokemon = pokemons.FirstOrDefault(p => p.Id == id);
            if (pokemon != null)
            {
                return pokemon;
            }
            pokemon = await pokeApiNet.GetResourceAsync<Pokemon>(id);
            if (pokemon != null)
            {
                pokemons.Add(pokemon);
            }
            return pokemon;
        }

        public async Task<Pokemon> GetPokemon(string name)
        {
            Pokemon pokemon = pokemons.FirstOrDefault(p => p.Name == name);
            if (pokemon != null)
            {
                return pokemon;
            }
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
            Ability ability = await pokeApiNet.GetResourceAsync<Ability>(abilityName);
            return ability?.EffectEntries.FirstOrDefault(e => e.Language.Name == language)?.Effect ?? string.Empty;
        }

        public EvolutionChainCompactData? GetPokemonEvolutionChain(int evolutionId)
        {
            return evolutionChains.FirstOrDefault(e => e.evolutionId == evolutionId);
        }

        public async Task<EvolutionChainCompactData> GetPokemonEvolutionChain(string speciesUrl)
        {
            Uri uri = new Uri(speciesUrl);
            string speciesId = uri.Segments[^1].TrimEnd('/');

            PokemonSpecies species = await pokeApiNet.GetResourceAsync<PokemonSpecies>(int.Parse(speciesId));
            if (species == null)
            {
                return null;
            }

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
            Pokemon pokemon = await GetPokemon(pokemonName);
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
                pokemon = await GetPokemon(pokemonName);
                if (pokemon != null)
                {
                    evolutionChainData.evolutionElementsIds.Add(pokemon.Id);
                }
                chain = chain.EvolvesTo[0];

            } while (chain.EvolvesTo.Count > 0);

            evolutionChains.Add(evolutionChainData);

            return evolutionChainData;
        }

        async Task<int> GetPokemonCount()
        {
            var data = await pokeApiNet.GetNamedResourcePageAsync<Pokemon>(1, 0, cancellationTokenSource.Token);
            return data.Count;
        }

        async Task<bool> ValidateRange(int startIndex, int endIndex)
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
