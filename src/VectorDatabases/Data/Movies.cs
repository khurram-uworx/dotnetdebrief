using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VectorDatabases.Data;

class User
{
    [Key]
    public int UserId { get; set; }
    public string Gender { get; set; }
    public int Age { get; set; }
    public string Occupation { get; set; }
    public string Zip { get; set; }
    public virtual ICollection<Rating> Ratings { get; set; }
}

class Movie
{
    [Key]
    public int MovieId { get; set; }
    public string Title { get; set; }
    public string Genres { get; set; }
    public virtual ICollection<Rating> Ratings { get; set; }
}

class Rating
{
    [Key]
    public int RatingId { get; set; }
    public int UserId { get; set; }
    public int MovieId { get; set; }
    public float RatingValue { get; set; }
    public long Timestamp { get; set; }

    public virtual User User { get; set; }
    public virtual Movie Movie { get; set; }
}

class MovieContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("MoviesDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Movie)
            .WithMany(m => m.Ratings)
            .HasForeignKey(r => r.MovieId);
    }
}
