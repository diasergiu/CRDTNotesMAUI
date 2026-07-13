using DatabaseLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Repositories
{
    public class NoteRepository
    {
        private DbContextUser _dbContextUser;
        public NoteRepository(DbContextUser dbContextUser)
        {
            _dbContextUser = dbContextUser;
        }

        public List<Note> getAllFlagedNotes()
        {
            List<Note> flaggedNotes = new List<Note>();
            try
            {
                flaggedNotes = _dbContextUser.Notes.Where(n => n.DirtyFlagChangesMade == true).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving flagged notes: {ex.Message}");
            }
            return flaggedNotes;
        }
    }
}
