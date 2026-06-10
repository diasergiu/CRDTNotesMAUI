using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class DbContextServer : DbContext
    {
        public DbSet<ServerNote> ServerNotes { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ServerNoteUser> ServerNoteUsers { get; set; }
        public string DbPath { get; }
        public DbContextServer()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "ServerDatabase.db");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite key for ServerNotesUser
            modelBuilder.Entity<ServerNoteUser>()
                .HasKey(snu => new { snu.IdUser, snu.IdServerNote });

            // Relationships
            modelBuilder.Entity<ServerNoteUser>()
                .HasOne(snu => snu.User)
                .WithMany(u => u.ServerNotesUsers)
                .HasForeignKey(snu => snu.IdUser);

            modelBuilder.Entity<ServerNoteUser>()
                .HasOne(snu => snu.ServerNotes)
                .WithMany(sn => sn.ServerNoteUser)
                .HasForeignKey(snu => snu.IdServerNote);

            // Device relationships
            modelBuilder.Entity<Device>()
                .HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.IdUser);

            base.OnModelCreating(modelBuilder);
        }
    }
}
