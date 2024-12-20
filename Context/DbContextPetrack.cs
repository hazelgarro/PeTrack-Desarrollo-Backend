﻿using APIPetrack.Models.Adoptions;
using APIPetrack.Models.Notificacions;
using APIPetrack.Models.Pets;
using APIPetrack.Models.Transfer;
using APIPetrack.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Context
{
    public class DbContextPetrack : DbContext
    {
        public DbContextPetrack(DbContextOptions<DbContextPetrack> options) : base(options) { }

        public DbSet<AppUser> AppUser { get; set; }
        public DbSet<PetOwner> PetOwner { get; set; }
        public DbSet<PetStoreShelter> PetStoreShelter { get; set; }
        public DbSet<Pet> Pet { get; set; }
        public DbSet<AdoptionRequest> AdoptionRequest { get; set; }
        public DbSet<Notification> Notification { get; set; }

        public DbSet<TransferRequest> TransferRequest {  get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PetOwner>()
                .HasOne(p => p.AppUser)
                .WithOne(u => u.PetOwner)
                .HasForeignKey<PetOwner>(p => p.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PetStoreShelter>()
                .HasOne(s => s.AppUser)
                .WithOne(u => u.PetStoreShelter)
                .HasForeignKey<PetStoreShelter>(s => s.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pet>()
               .HasOne(p => p.PetOwner)
               .WithMany(po => po.Pets)
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false); // Relación opcional

            modelBuilder.Entity<Pet>()
                .HasOne(p => p.PetStoreShelter)
                .WithMany(s => s.Pets)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // Relación opcional

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Pet>()
                .HasIndex(p => new { p.OwnerId, p.OwnerTypeId })
                .IsUnique();
        }
    }   

}
