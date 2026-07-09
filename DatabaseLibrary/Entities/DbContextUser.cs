using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class DbContextUser : DbContext
    {
        public DbSet<Note> Notes { get; set; }
        public DbSet<Note_User> NoteUsers { get; set; }
        public DbSet<UserClient> Users { get; set; }

        public string DbPath { get; }

        public DbContextUser()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "NotesDatabase.db");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
    }
}
