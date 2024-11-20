namespace PokeAPI.Data
{
    public class PokemonUser
    {
        public int Id { get; set; }
        public int PokemonId { get; set; } // Foreign Key
        public int UserId { get; set; } // Foreign Key
    }
}
