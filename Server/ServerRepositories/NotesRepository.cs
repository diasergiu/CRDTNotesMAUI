using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.Migrations;
using DatabaseLibrary.WrapperClasses;
using Microsoft.EntityFrameworkCore;

namespace Server.ServeRepositories
{
    public class NotesRepository
    {
        private DbContextServer _dbContextServer;
        public NotesRepository(DbContextServer context)
        {
            _dbContextServer = context;
        }

        public bool DoseUserHaveAccessToNote(Guid userId, Guid noteId)
        {
            var noteUser = _dbContextServer.Note_Users.FirstOrDefault(nu => nu.IdNote == noteId && nu.IdUser == userId);
            return noteUser != null;
        }
        public async Task<List<NoteServer>> GetAllNotesFromUser(Guid IdUser)
        {
            var notes = await _dbContextServer.Notes
                .Join(_dbContextServer.Note_Users,
                    n => n.IdNote,
                    nu => nu.IdNote, (n, nu) => new { Note = n, NoteUser = nu }
                 )
                .Where(nu => nu.NoteUser.IdUser == IdUser)
                .Select(nu => nu.Note)
                .Include(n => n.CRDTCharacter) // added recently we need to return the CRDT from server
                .ToListAsync();
            return notes;
        }

        public async Task<NoteServer> CreateNote(NoteServer note, Guid idUser)
        {
            _dbContextServer.Notes.Add(note);
            await _dbContextServer.SaveChangesAsync();

            Note_UserServer newConnection = new Note_UserServer()
            {
                IdNote = note.IdNote,
                IdUser = idUser
            };
            _dbContextServer.Note_Users.Add(newConnection);
            await _dbContextServer.SaveChangesAsync();
            return note;
        }

        /// <summary>
        /// Updates note with optimistic concurrency control using version column.
        /// Only updates if client version matches server version.
        /// Automatically increments version on successful update.
        /// </summary>
        /// <param name="note">Note with updates and current version from client</param>
        /// <returns>Result with success/conflict status. On conflict, includes current server version</returns>
        public async Task<UpdateNoteWithVersionResult> UpdateChanges(NoteServer note, Guid idUser)
        {
            try
            {
                // Fetch current note from database
                var existingNote = await _dbContextServer.Notes
                    .FirstOrDefaultAsync(n => n.IdNote == note.IdNote);

                if (existingNote == null)
                    return UpdateNoteWithVersionResult.Error("Note not found");

                // CONCURRENCY CHECK: Compare client version with server version
                if (note.Version != existingNote.Version)
                {
                    // Version mismatch - conflict detected
                    // Return server's current version for client to see the conflict
                    return UpdateNoteWithVersionResult.VersionConflict(
                        $"Version conflict: Client sent v{note.Version}, but server has v{existingNote.Version}. " +
                        $"Note was modified by another user.",
                        existingNote);
                }

                // Versions match - safe to update
                existingNote.Title = note.Title;
                existingNote.LastUpdate = note.LastUpdate;
                existingNote.Version++;  // Increment version for next update

                // Save changes within transaction
                await using var transaction = await _dbContextServer.Database.BeginTransactionAsync();
                try
                {
                    _dbContextServer.SaveChanges();
                    await transaction.CommitAsync();

                    return UpdateNoteWithVersionResult.Success(existingNote);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return UpdateNoteWithVersionResult.Error($"Database error: {ex.Message}");
            }
        }

        public async Task DeleteNote(Guid noteId, Guid idUser)
        {
            // Verify the note belongs to the user
            var noteUser = await _dbContextServer.Note_Users
                .FirstOrDefaultAsync(nu => nu.IdNote == noteId && nu.IdUser == idUser);

            if (noteUser == null)
            {
                throw new Exception("Note not found or access denied.");
            }

            // Delete the note-user relationship
            _dbContextServer.Note_Users.Remove(noteUser);

            // Delete the note itself if there are no users connected to note
            List<Note_UserServer> remainingConnections = _dbContextServer.Note_Users.Where(n => n.IdNote == noteId).ToList();
            if (remainingConnections.Count == 1)
            {
                var note = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == noteId);
                if (note != null)
                {
                    _dbContextServer.Notes.Remove(note);
                }
                DeleteCharacters(note.IdNote);
            }

            await _dbContextServer.SaveChangesAsync();
        }

        public void DeleteCharacters(Guid noteId)
        {
            _dbContextServer.CRDTCharacters.Where(n => n.IdNote == noteId).ExecuteDelete();

        }
        public async Task SaveAllChangesFromClient(List<DToSendChanges> changes, Guid idUser)
        {
            if (changes == null || changes.Count == 0)
                return;

            // Clear change tracker to avoid conflicts with detached entities from client
            _dbContextServer.ChangeTracker.Clear();

            // Get all existing note IDs in one query (avoid N+1 query problem)
            var existingNoteIds = await _dbContextServer.Notes
                .Select(n => n.IdNote)
                .ToListAsync();

            foreach (var item in changes)
            {
                if (!existingNoteIds.Contains(item.NoteServer.IdNote))
                {
                    // New note
                    _dbContextServer.Notes.Add(item.NoteServer);
                    _dbContextServer.Note_Users.Add(new Note_UserServer()
                    {
                        IdNote = item.NoteServer.IdNote,
                        IdUser = idUser
                    });
                }
                else
                {
                    // Existing note - attach and update
                    _dbContextServer.Notes.Update(item.NoteServer);
                }

                // Add the CRDT payload
                _dbContextServer.CRDTCharacters.Add(new CRDTCharacterServer()
                {
                    Payload = item.Payload,
                    IdNote = item.NoteServer.IdNote
                });
            }

            await _dbContextServer.SaveChangesAsync();
        }

        public async Task SaveAllChangesFromClient(List<NoteServer> notesClient, Guid UserId)
        {
            if (notesClient == null || notesClient.Count == 0)
                return;
            var allNotes = _dbContextServer.Notes.ToList();
            foreach (NoteServer note in notesClient)
            {
                var exists = allNotes.FirstOrDefault(n => n.IdNote == note.IdNote);
                if (exists == null)
                {
                    await CreateNote(note, UserId);
                }
                else
                {
                    await UpdateChanges(note, UserId);
                }
            }
        }


        public async Task<NoteServer> GetNoteById(Guid IdNote, Guid idUser)
        {
            var noteAccess = _dbContextServer.Note_Users.FirstOrDefault(n => n.IdUser == idUser && n.IdNote == IdNote);
            if (noteAccess != null)
            {
                return _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == IdNote);
            }
            return null;
        }

        public async Task ManageCRDTCharacters(List<CRDTCharacterServer> crdtCharacters)
        {
            if (crdtCharacters == null || crdtCharacters.Count == 0)
                return;
            _dbContextServer.ChangeTracker.Clear(); // probably because of he entityMapper mapping, we need to clear the change tracker to avoid tracking issues
            _dbContextServer.CRDTCharacters.AddRange(crdtCharacters);
            await _dbContextServer.SaveChangesAsync();
        }
        public async Task saveCRDTChanges(CRDTChangePayload changes)
        {
            _dbContextServer.CRDTCharacters.Add(new CRDTCharacterServer()
            {
                IdNote = changes.IdNote,
                Payload = changes.Payload
            });
            await _dbContextServer.SaveChangesAsync();
        }

        public async Task<List<CRDTCharacterServer>> GetAllCRDTByUser(Guid userId) // dont forget to make the connecction of note with the client
        {
            return await _dbContextServer.CRDTCharacters
            .Include(c => c.NoteServer)
            .AsNoTracking()
            .Where(c => _dbContextServer.Note_Users
                .Any(nu => nu.IdUser == userId && nu.IdNote == c.IdNote))
            .ToListAsync();
        }

        public async Task<List<CRDTCharacterServer>> getCRDTCharactersbyIdNote(Guid noteId, Guid userId)
        {
            if (DoseUserHaveAccessToNote(userId, noteId))
            {
                return await _dbContextServer.CRDTCharacters
                    .Include(c => c.NoteServer)
                    .AsNoTracking()
                    .Where(n => n.IdNote == noteId).ToListAsync();
            }
            return new List<CRDTCharacterServer>();  // need a pathern where we verifie user access to Note and return error instead of empty list
        }

        public async Task SaveNoteUserConnection(Guid noteId, Guid userId)
        {
            _dbContextServer.Note_Users.Add(new Note_UserServer() { IdNote = noteId, IdUser = userId });
            await _dbContextServer.SaveChangesAsync(); // i dont think this is the best way to handle this 
        }
    }

    /// <summary>
    /// Result object for note updates with version checking (optimistic concurrency)
    /// </summary>
    public class UpdateNoteWithVersionResult
    {
        /// <summary>True if update was successful</summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Message describing result: success, conflict details, or error
        /// </summary>
        public string Message { get; set; }

        /// <summary>The updated note on success</summary>
        public NoteServer ServerNote { get; set; }

        /// <summary>True if version mismatch (concurrency conflict)</summary>
        public bool IsVersionConflict { get; set; }

        /// <summary>Create success result</summary>
        public static UpdateNoteWithVersionResult Success(NoteServer updatedNote)
        {
            return new UpdateNoteWithVersionResult
            {
                IsSuccess = true,
                Message = "Note updated successfully",
                ServerNote = updatedNote,
                IsVersionConflict = false,
            };
        }

        /// <summary>Create conflict result with server version</summary>
        public static UpdateNoteWithVersionResult VersionConflict(
            string message,
            NoteServer currentServerNote)
        {
            return new UpdateNoteWithVersionResult
            {
                IsSuccess = true,
                Message = message,
                ServerNote = currentServerNote,
                IsVersionConflict = true,
            };
        }

        /// <summary>Create error result</summary>
        /// <summary>Create error result</summary>
        public static UpdateNoteWithVersionResult Error(string errorMessage)
        {
            return new UpdateNoteWithVersionResult
            {
                IsSuccess = false,
                Message = errorMessage,
                ServerNote = null,
                IsVersionConflict = false,
            };
        }
    }
}

