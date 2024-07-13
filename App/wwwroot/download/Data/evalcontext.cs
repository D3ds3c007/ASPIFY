

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
        modelBuilder.Entity<Member>()
            .HasOne(s => s.Privilege)
            .WithMany(st => st.Members)
            .HasForeignKey(t => t.PrivilegeId);
        modelBuilder.Entity<DevisWork>()
            .HasOne(s => s.Work)
            .WithMany(st => st.DevisWorks)
            .HasForeignKey(t => t.WorkId);
        modelBuilder.Entity<DevisWork>()
            .HasOne(s => s.HouseType)
            .WithMany(st => st.DevisWorks)
            .HasForeignKey(t => t.HouseTypeId);
        modelBuilder.Entity<Project>()
            .HasOne(s => s.Cutomer)
            .WithMany(st => st.Projects)
            .HasForeignKey(t => t.CutomerId);
        modelBuilder.Entity<Project>()
            .HasOne(s => s.Finition)
            .WithMany(st => st.Projects)
            .HasForeignKey(t => t.FinitionId);
        modelBuilder.Entity<Project>()
            .HasOne(s => s.HouseType)
            .WithMany(st => st.Projects)
            .HasForeignKey(t => t.HouseTypeId);
        modelBuilder.Entity<DevisProject>()
            .HasOne(s => s.Project)
            .WithMany(st => st.DevisProjects)
            .HasForeignKey(t => t.ProjectId);
        modelBuilder.Entity<DevisProject>()
            .HasOne(s => s.Work)
            .WithMany(st => st.DevisProjects)
            .HasForeignKey(t => t.WorkId);
        modelBuilder.Entity<Payment>()
            .HasOne(s => s.Cutomer)
            .WithMany(st => st.Payments)
            .HasForeignKey(t => t.CutomerId);
        modelBuilder.Entity<Payment>()
            .HasOne(s => s.Project)
            .WithMany(st => st.Payments)
            .HasForeignKey(t => t.ProjectId);

    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    //all dbset

    public virtual DbSet<Cutomer> Cutomers { get; set; }
    public virtual DbSet<Finition> Finitions { get; set; }
    public virtual DbSet<Privilege> Privileges { get; set; }
    public virtual DbSet<Member> Members { get; set; }
    public virtual DbSet<Work> Works { get; set; }
    public virtual DbSet<HouseType> HouseTypes { get; set; }
    public virtual DbSet<DevisWork> DevisWorks { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<DevisProject> DevisProjects { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    

}
