//using Android.Content;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using MAUIClientUI.Services.HelperClasses;
using Microsoft.EntityFrameworkCore;

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
            return _dbContextUser.Notes.Where(b => b.DirtyFlagChangesMade == true).Include(c => c.CRDTCharacter)
                .Where(c => c.DirtyFlagChangesMade == true).AsNoTracking().ToList();
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

        public virtual void UpdateNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes.Update(note);
            _dbContextUser.SaveChanges();
        }

        public virtual void MarkNoteAsDirty(Guid IdNote) 
        {
            _dbContextUser.ChangeTracker.Clear();
            _dbContextUser.Notes
                .Where(n => n.IdNote == IdNote)
                .ExecuteUpdate(setters => setters.SetProperty(n => n.DirtyFlagChangesMade, true));

            _dbContextUser.SaveChanges();
        }
        public virtual void CreateNote(NoteClient note)
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

        public async void SoftDeleteNote(NoteClient note)
        {
            _dbContextUser.ChangeTracker.Clear();
            note.DirtyFlagChangesMade = true;
            note.isDeleted = true;
            _dbContextUser.Notes.Update(note);
            await _dbContextUser.SaveChangesAsync();
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

        public async Task<List<NoteClient>> GetAllNotes()
        {
            return _dbContextUser.Notes.Where(n => n.isDeleted == false).Include(b => b.CRDTCharacter).ToList();
        }

        public async Task ClearDirtyFlag(List<NoteClient> offlineChanges)
        {
            foreach (NoteClient note in offlineChanges)
            {
                foreach (var character in note.CRDTCharacter)
                {
                    character.IsDirtyFlag = false;
                }
                note.DirtyFlagChangesMade = false;
                _dbContextUser.ChangeTracker.Clear();
                _dbContextUser.CRDTCharacters.UpdateRange(note.CRDTCharacter);
                _dbContextUser.Notes.Update(note);
            }
            await _dbContextUser.SaveChangesAsync();
            _dbContextUser.ChangeTracker.Clear();

        }

        public virtual async Task SaveCRDTChanges(List<CRDTCharacterClient> changes)
        {
            if (changes == null || changes.Count == 0)
                return;

            _dbContextUser.ChangeTracker.Clear();

            // De-duplicate the input list by keeping only the last occurrence of each character
            var deduplicatedChanges = changes
                .GroupBy(c => (c.IdCharacter, c.IdNote))
                .Select(g => g.Last())
                .ToList();

            // Get all IDs from the list
            var changeIds = deduplicatedChanges.Select(c => c.IdCharacter).ToHashSet();
            var existingCharacters = await _dbContextUser.CRDTCharacters
                .AsNoTracking()
                .Where(c => changeIds.Contains(c.IdCharacter))
                .ToDictionaryAsync(c => (c.IdCharacter, c.IdNote));

            var toAdd = new List<CRDTCharacterClient>();
            var toUpdate = new List<CRDTCharacterClient>();

            foreach (var character in deduplicatedChanges)
            {
                var key = (character.IdCharacter, character.IdNote);
                if (existingCharacters.ContainsKey(key))
                {
                    toUpdate.Add(character);
                }
                else
                {
                    toAdd.Add(character);
                }
            }

            // Add new entities
            if (toAdd.Any())
            {
                _dbContextUser.CRDTCharacters.AddRange(toAdd);
            }

            // Update existing entities
            if (toUpdate.Any())
            {
                _dbContextUser.CRDTCharacters.UpdateRange(toUpdate);
            }

            if (toAdd.Any() || toUpdate.Any())
            {
                await _dbContextUser.SaveChangesAsync();
            }

            // Clear tracker after saving to avoid tracking issues in future operations
            _dbContextUser.ChangeTracker.Clear();
        }

        public void DeleteCharacterByNoteId(Guid noteId)
        {
            _dbContextUser.CRDTCharacters.Where(n => n.IdNote == noteId).ExecuteDeleteAsync();
        }

        public async Task DeleteNotesWithIdDeleted()
        {
            await _dbContextUser.Notes.Where(n => n.isDeleted).ExecuteDeleteAsync();
        }

        public async Task UpdateBasedOnNoteServer(List<NoteServer> data)
        {
            if (data == null || data.Count == 0)
                return;

            _dbContextUser.ChangeTracker.Clear();

            var listNotes = await _dbContextUser.Notes.AsNoTracking().ToDictionaryAsync(n => n.IdNote);
            var decodedChanges = new List<CRDTCharacterClient>();
            foreach (NoteServer note in data)
            {
                var noteProcess = EntityMapper.MapNoteServerToNoteClient(note);
                noteProcess.DirtyFlagChangesMade = false;
                if (!listNotes.ContainsKey(noteProcess.IdNote))
                {
                    if (note.isDeleted)
                    {
                        continue;
                    }
                    _dbContextUser.Add(noteProcess);
                }
                else
                {
                    _dbContextUser.Update(noteProcess);
                }
                if (note.CRDTCharacter != null)
                {
                    foreach (var change in note.CRDTCharacter)
                    {
                        var decodedCharacters = CharacterSerializer.Decode(change.Payload);

                        foreach (var character in decodedCharacters)
                        {

                            decodedChanges.Add(new CRDTCharacterClient()
                            {
                                IdCharacter = character.IdCharacter,
                                Character = character.Character,
                                Tombstone = character.Tombstone,
                                IdNote = note.IdNote,
                                IsDirtyFlag = false,
                            });                       
                        }
                    }
                    
                }
                
            }
            await _dbContextUser.SaveChangesAsync();
            if (decodedChanges.Any())
            {
                await SaveCRDTChanges(decodedChanges);
            }

            _dbContextUser.ChangeTracker.Clear();
        }
    }
}
