using Microsoft.EntityFrameworkCore;
using PokeAPI.Data;

namespace PokeAPI
{
    public class PokeContext : DbContext
    {
        public DbSet<Pokemon> Pokemons { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PokemonUser> PokemonsUsers { get; set; }

        public User loggedInUser;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlite($"Data Source={Common.dbName}");
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names
            modelBuilder.Entity<Pokemon>().ToTable("Pokemon");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<PokemonUser>().ToTable("PokemonUser");

            // Primary keys
            modelBuilder.Entity<Pokemon>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<PokemonUser>()
                .Property(up => up.Id)
                .ValueGeneratedOnAdd();

            // Relations
            modelBuilder.Entity<PokemonUser>()
                .HasOne<Pokemon>()
                .WithMany()
                .HasForeignKey(up => up.PokemonId);

            modelBuilder.Entity<PokemonUser>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(up => up.UserId);
        }
    }
}
