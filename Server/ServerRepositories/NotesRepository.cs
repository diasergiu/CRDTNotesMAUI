using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.Migrations;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.WrapperClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Server.ServerHub;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Server.ServeRepositories
{
    public class NotesRepository
    {
        private DbContextServer _dbContextServer;
        private IHubContext<NotesHub> _notesHubContext;
        public NotesRepository(DbContextServer context, IHubContext<NotesHub> notesHubContext)
        {
            _dbContextServer = context;
            _notesHubContext = notesHubContext;
        }

        public async Task<UserServer> getUser(string username, string password)
        {
            UserServer user = await _dbContextServer.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
            return user;
        }

        //public async Task<UserServer> SaveOrUpdateNotes(UserServer user, List<Note> notesUpdate)
        //{
        //    List<Note> listNewNotes = new List<Note>();

        //    if (user != null) {
        //        foreach (var note in notesUpdate)
        //        {
        //            note.DirtyFlagChangesMade = false; // Reset the dirty flag after saving
        //            var existingNote = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
        //            if (existingNote != null)
        //            {
        //                _dbContextServer.Entry(existingNote).CurrentValues.SetValues(note);
        //            }
        //            else
        //            {
        //                listNewNotes.Add(note);
        //                _dbContextServer.Notes.Add(note);
        //            }
        //        }

        //        await _dbContextServer.SaveChangesAsync();

        //        foreach (var note in listNewNotes)
        //        {
        //            var noteExists = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
        //            if (noteExists != null)
        //            {
        //                //var existingRelationship = await dbContextServer.NoteUsers.FirstOrDefaultAsync(nu => nu.IdNote == note.IdNote && nu.IdUser == user.IdUser);

        //                //if (existingRelationship == null)
        //                //{
        //                var noteUser = new Note_UserServer
        //                {
        //                    IdNote = note.IdNote,
        //                    IdUser = user.IdUser
        //                };
        //                _dbContextServer.Note_Users.Add(noteUser);
        //                //}

        //            }
        //        }

        //        await _dbContextServer.SaveChangesAsync();
        //    }
        //    return user;
        //}

        public async Task<List<SyncQueueServer>> GetServerSyncChanges(IUser user, Guid deviceId)
        {
            var notesToUpdate = await _dbContextServer.Sync_Queues
                .Where(nu => nu.IdUser == user.IdUser && nu.IdDevice == deviceId)
                .Select(nu => nu)
                .ToListAsync();
            return notesToUpdate;
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
                .ToListAsync();
            return notes;
        }


        //public async Task saveOrUpdateNewNote(UserServer user, Note note)
        //{
        //    Note existingNote = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
        //    if (existingNote != null)
        //    {
        //        _dbContextServer.Entry(existingNote).CurrentValues.SetValues(note);
        //    }
        //    else
        //    {
        //        _dbContextServer.Add(note);
        //        Note_UserClient connection = new Note_UserClient();
        //        connection.IdNote = note.IdNote;
        //        connection.IdUser = user.IdUser;
        //        connection.Note = note;
        //        //connection.User = user;
        //        _dbContextServer.Add(connection);
        //    }
        //    _dbContextServer.SaveChangesAsync();
        //}

        //public async Task SyncData(List<SyncQueueServer> changesMade)
        //{
        //    foreach (SyncQueueServer queue in changesMade)
        //    {
        //        SyncData(queue);
        //    }
        //    //await _dbContextServer.SaveChangesAsync();
        //}


        //public async Task SyncData(SyncQueueServer changesMade)
        //{
        //    if (changesMade.Operation == "Update")
        //    {
        //        NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == changesMade.IdNote);
        //        if (existingNote != null)
        //        {
        //            existingNote.Content = changesMade.ContentChanges;
        //            existingNote.LastUpdate = changesMade.LastUpdate;
        //        }
        //    }
        //    else if (changesMade.Operation == "Delete")
        //    {
        //        NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == changesMade.IdNote);
        //        if (existingNote != null)
        //        {
        //            _dbContextServer.Notes.Remove(existingNote);
        //        }
        //    }
        //    else if (changesMade.Operation == "Create")
        //    {
        //        //CreateNote(changesMade);

        //    }
        //    await _dbContextServer.SaveChangesAsync(); // Now we make a save at every 
        //}

        public List<SyncQueueServer> GetChangesFromNote(Guid idNote)
        {
            return _dbContextServer.Sync_Queues.Where(n => n.IdNote == idNote).ToList();
        }

        public async Task<NoteServer> CreateNote(NoteClient note, Guid idUser)
        {
            //NoteServer newNote = new NoteServer
            //{
            //    Content = changesMade.ContentChanges,
            //    LastUpdate = changesMade.LastUpdate
            //};
            //_dbContextServer.Notes.Add(newNote);
            //Note_UserServer newConnection = new Note_UserServer
            //{
            //    IdNote = changesMade.IdNote,
            //    IdUser = changesMade.IdUser
            //};
            //_dbContextServer.Note_Users.Add(newConnection);

            NoteServer newNote = EntityMapper.MapNoteClientToNoteServer(note);
            _dbContextServer.Notes.Add(newNote);
            await _dbContextServer.SaveChangesAsync();

            Note_UserServer newConnection = new Note_UserServer()
            {
                IdNote = newNote.IdNote,
                IdUser = idUser
            };
            _dbContextServer.Note_Users.Add(newConnection);
            await _dbContextServer.SaveChangesAsync();
            return newNote;
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
                existingNote.Content = note.Content;
                existingNote.LastUpdate = note.LastUpdate;
                existingNote.Version++;  // Increment version for next update

                // Save changes within transaction
                await using var transaction = await _dbContextServer.Database.BeginTransactionAsync();
                try
                {
                    _dbContextServer.SaveChanges();
                    await transaction.CommitAsync();

                    // *** NEW: Notify all other users viewing this note ***
                    if (_notesHubContext != null)
                    {
                        var groupName = $"note-{note.IdNote}";
                        var senderConnectionId = NotesHub.GetConnectionId(idUser.ToString());

                        var sendTask = senderConnectionId != null
                            ? _notesHubContext.Clients.GroupExcept(groupName, senderConnectionId)
                            : _notesHubContext.Clients.Group(groupName);

                        await sendTask.SendAsync("NoteUpdated", new
                        {
                            noteId = existingNote.IdNote,
                            title = existingNote.Title,
                            content = existingNote.Content,
                            lastUpdate = existingNote.LastUpdate,
                            version = existingNote.Version,
                            updatedAt = DateTime.UtcNow
                        });
                    }

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
            if(remainingConnections.Count == 0)
            {
                var note = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == noteId);
                if (note != null)
                {
                    _dbContextServer.Notes.Remove(note);
                }
            }

            await _dbContextServer.SaveChangesAsync();
        }

        public async Task SaveAllChangesFromClient(List<NoteServer> notesClient, Guid UserId)
        {
            if (notesClient == null || notesClient.Count == 0)
                return;

            _dbContextServer.ChangeTracker.Clear();

            // Get all existing note IDs from the database
            var existingNoteIds = _dbContextServer.Notes
                .AsNoTracking()  // Add this to prevent tracking
                .Select(n => n.IdNote)
                .ToList();

            // Separate notes into new and existing_
            var notesToAdd = notesClient
                .Where(n => !existingNoteIds.Contains(n.IdNote))
                .ToList();

            var notesToUpdate = notesClient
                .Where(n => existingNoteIds.Contains(n.IdNote))
                .ToList();
            List<Note_UserServer> connections = new List<Note_UserServer>();

            foreach (NoteServer newNotes in notesToAdd)
            {
                connections.Add(new Note_UserServer()
                {
                    IdNote = newNotes.IdNote,
                    IdUser = UserId
                });
            }
            // Add new notes
            if (notesToAdd.Count > 0)
            {
                _dbContextServer.Notes.AddRange(notesToAdd);
                _dbContextServer.Note_Users.AddRange(connections);

            }

            // Update existing notes
            if (notesToUpdate.Count > 0)
            {
                _dbContextServer.Notes.UpdateRange(notesToUpdate);
            }

            // Save all changes at once
            if (notesToAdd.Count > 0 || notesToUpdate.Count > 0)
            {
                _dbContextServer.SaveChanges();
            }
        }


        public async Task<NoteServer> GetNoteById(Guid IdNote, Guid idUser)
        {
            var noteAccess = _dbContextServer.Note_Users.FirstOrDefault(n => n.IdUser == idUser && n.IdNote == IdNote);
            if(noteAccess != null)
            {
                return _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == IdNote);
            }
            return null;
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

