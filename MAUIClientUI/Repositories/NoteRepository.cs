//using Android.Content;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            //            return _dbContextUser.Notes.Where(n => n.NoteUser.Any(nu => nu.IdUser == idUser)).ToList();
            return _dbContextUser.Notes.AsNoTracking().ToList();
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

        public void SaveNewUser(UserClient newUser)
        {
            _dbContextUser.Users.Add(newUser);
        }

        public void UpdateNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Update(note);
            _dbContextUser.SaveChanges();


        }
        public void CreateNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Add(note);
            _dbContextUser.SaveChanges();
        }

        public void DeleteNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Remove(note);
            _dbContextUser.SaveChanges();
        }

        public void TextToCRDTCharacter(List<CRDTCharacterClient> characters)
        {
            _dbContextUser.CRDTCharacters.AddRange(characters);
            _dbContextUser.SaveChanges();
        }

        public async Task<List<CRDTCharacterClient>> GetAllCRDTCharacters()
        {
            _dbContextUser.ChangeTracker.Clear();
            return _dbContextUser.CRDTCharacters
                .Where(n => n.IsDirtyFlag == true).AsNoTracking().ToList();
        }

        internal async Task<List<NoteClient>> GetAllNotes()
        {
            return _dbContextUser.Notes.Include(b => b.CRDTCharacter).ToList();
        }

        public async Task ClearDirtyFlag(List<CRDTCharacterClient> offlineChanges)
        {
            foreach (var character in offlineChanges)
            {
                character.IsDirtyFlag = false;
            }
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.CRDTCharacters.UpdateRange(offlineChanges);
            _dbContextUser.SaveChangesAsync();
            _dbContextUser.ChangeTracker.Clear();

        }

        public async Task SaveCRDTChanges(List<CRDTCharacterClient> changes)
        {
            // Get all IDs from the list
            var changeIds = changes.Select(c => c.IdCharacter).ToList();
            var existingCharacters = await _dbContextUser.CRDTCharacters
                .AsNoTracking()
                .Where(c => changeIds.Contains(c.IdCharacter))
                .ToListAsync();


            // Create lookup for O(1) access
            var existingLookup = existingCharacters
                .ToDictionary(c => (c.IdCharacter, c.IdNote));

            foreach (var character in changes)
            {
                if (existingLookup.TryGetValue((character.IdCharacter, character.IdNote), out var existing))
                {
                    // Update existing - mark as modified
                    //  _dbContextServer.Entry(character).State = EntityState.Modified;
                    _dbContextUser.CRDTCharacters.Update(character);
                }
                else
                {
                    // New entity - mark as added
                    //_dbContextServer.Entry(character).State = EntityState.Added;
                    _dbContextUser.CRDTCharacters.Add(character);
                }
            }

            await _dbContextUser.SaveChangesAsync();
        }

        public async Task DeleteCharacterByNoteId(Guid noteId)
        {
            _dbContextUser.CRDTCharacters.Where(n => n.IdNote == noteId).ExecuteDeleteAsync();
        }
    }
}
