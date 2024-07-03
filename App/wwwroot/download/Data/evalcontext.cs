

using System;
using System.Collections.Generic;
using donet_backend_mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace evaluation2.Data;

public partial class evalContext : DbContext
{
    public evalContext()
    {
    }

    public evalContext(DbContextOptions<evalContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
                optionsBuilder.UseNpgsql("Host=localhost;Database=evaluation;Username=postgres;Password=root;");
                optionsBuilder.UseLazyLoadingProxies();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);

    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    //all dbset

    public virtual DbSet<Locations> Locationss { get; set; }
    public virtual DbSet<Car> Cars { get; set; }
    

}
