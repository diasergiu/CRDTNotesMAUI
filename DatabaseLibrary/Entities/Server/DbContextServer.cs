using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    public class DbContextServer : DbContext
    {
        public DbSet<NoteServer> Notes { get; set; }
        //public DbSet<Device> Devices { get; set; }
        public DbSet<UserServer> Users { get; set; }
        public DbSet<Note_UserServer> Note_Users { get; set; }
        //public DbSet<User_Device> User_Devices { get; set; }
        public DbSet<CRDTCharacterServer> CRDTCharacters { get; set; }
        public string DbPath { get; }
        //Probably Delete
        public DbContextServer()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "NotesDatabase.mdf");
        }
        public DbContextServer(DbContextOptions<DbContextServer> options)
           : base(options)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "NotesDatabase.mdf");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Only configure if not already configured by dependency injection
            if (!options.IsConfigured)
            {
                string connectionString = "server=localhost;database=NotesDatabase;Trusted_Connection=True;TrustServerCertificate=True;";
                options.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite key for ServerNotesUser
            modelBuilder.Entity<Note_UserServer>()
                .HasKey(snu => new { snu.IdUser, snu.IdNote });

            // Relationships
            modelBuilder.Entity<Note_UserServer>()
                .HasOne(snu => snu.User)
                .WithMany(u => u.NotesUser)
                .HasForeignKey(snu => snu.IdUser);

            modelBuilder.Entity<Note_UserServer>()
                .HasOne(snu => snu.Note)
                .WithMany(n => n.NoteUser)
                .HasForeignKey(snu => snu.IdNote);

            //// Device relationships
            //modelBuilder.Entity<User_Device>()
            //    .HasKey(ud => new { ud.IdUser, ud.IdDevice });

            //modelBuilder.Entity<User_Device>()
            //    .HasOne(ud => ud.Device)
            //    .WithMany(d => d.UserDevices)
            //    .HasForeignKey(ud => ud.IdDevice);

            //modelBuilder.Entity<User_Device>()
            //    .HasOne(ud => ud.User)
            //    .WithMany(u => u.DevicesUser)
            //    .HasForeignKey(ud => ud.IdUser);


            modelBuilder.Entity<NoteServer>()
                .Property(n => n.CreationDate)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<NoteServer>()
                .Property(n => n.LastUpdate)
                .HasDefaultValueSql("datetime('now')");

            base.OnModelCreating(modelBuilder);
        }
    }
}
