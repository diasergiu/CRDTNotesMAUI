using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities.Client
{
    public class DbContextClient : DbContext
    {
        public DbSet<NoteClient> Notes { get; set; }
        public DbSet<Note_UserClient> NoteUsers { get; set; }
        public DbSet<UserClient> Users { get; set; }
        public DbSet<SyncQueueClient> SyncQueues { get; set; }

        public string DbPath { get; }

        public DbContextClient()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "NotesDatabase.db");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
    }
}
