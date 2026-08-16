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
        public DbSet<CRDTCharacter> CRDTCharacters { get; set; }

        public string DbPath { get; }

        public DbContextClient(string instanceId = "")
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            string instanceSuffix = string.IsNullOrEmpty(instanceId) ? "" : instanceId;
            DbPath = System.IO.Path.Join(path, $"NotesDatabase{instanceSuffix}.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
    }
}
