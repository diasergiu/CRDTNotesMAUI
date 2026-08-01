using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Repositories
{
    public class NoteRepository
    {
        private DbContextClient _dbContextUser;
        public NoteRepository(DbContextClient dbContextUser)
        {
            _dbContextUser = dbContextUser;
        }

        public List<NoteClient> GetNoteFromUser(Guid idUser)
        {
            //return _dbContextUser.Notes.Where(n => n.NoteUser.Any(nu => nu.IdUser == idUser)).ToList();
            return _dbContextUser.Notes.ToList();
        }

        public List<SyncQueueClient> getAllChanges(UserClient user)
        {
            List<SyncQueueClient> changesMade =
                new List<SyncQueueClient>(); // this line if because the method needs to return a list, but if the query fails, it will return an empty list instead of null
            try
            {
                //changesMade = _dbContextUser.SyncQueues.Where(n => n.UserDevice.IdUser == user.IdUser).ToList();
                changesMade = _dbContextUser.SyncQueues.ToList(); // get all changes now
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving flagged notes: {ex.Message}");
            }
            return changesMade;
        }

        public void UpdateListNotes(List<NoteClient> noteClients)
        {
            if (noteClients == null || noteClients.Count == 0)
                return;

            _dbContextUser.ChangeTracker.Clear();

            // Get all existing note IDs from the database
            var existingNoteIds = _dbContextUser.Notes
                .AsNoTracking()  // Add this to prevent tracking
                .Select(n => n.IdNote)
                .ToList();

            // Separate notes into new and existing_
            var notesToAdd = noteClients
                .Where(n => !existingNoteIds.Contains(n.IdNote))
                .ToList();

            var notesToUpdate = noteClients
                .Where(n => existingNoteIds.Contains(n.IdNote))
                .ToList();

            // Add new notes
            if (notesToAdd.Count > 0)
            {
                _dbContextUser.Notes.AddRange(notesToAdd);
            }

            // Update existing notes
            if (notesToUpdate.Count > 0)
            {
                _dbContextUser.Notes.UpdateRange(notesToUpdate);
            }

            // Save all changes at once
            if (notesToAdd.Count > 0 || notesToUpdate.Count > 0)
            {
                _dbContextUser.SaveChanges();
            }
        }
        //untested if it saves the changes to the database
        // kind of duplicated but i might need to use the device ID
        public void UpdateListNotes(List<ISyncQueue> flaggedNotes)
        {
            if (flaggedNotes == null) return;
            foreach (SyncQueueClient queue in flaggedNotes)
            {
                if (queue.Operation == "Update")
                {
                    NoteClient existingNote = _dbContextUser.Notes.FirstOrDefault(n => n.IdNote == queue.IdNote); // probably SyncData dosent needto have IdUser
                    if (existingNote != null)
                    {
                        existingNote.Content = queue.ContentChanges;
                        existingNote.LastUpdate = queue.LastUpdate;
                    }
                }
                _dbContextUser.SaveChangesAsync();
            }
        }

        public void SaveNewUser(UserClient newUser)
        {
            _dbContextUser.Users.Add(newUser);
        }

        public void updateNote(NoteClient note) 
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Update(note);
            _dbContextUser.SaveChanges();


        }
        public void createNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Add(note);
            _dbContextUser.SaveChanges();
        }

        public void deleteNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Remove(note);
            _dbContextUser.SaveChanges();
        }

    }
}
