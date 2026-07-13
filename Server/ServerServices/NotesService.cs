using DatabaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.ServerServices
{
    public class NotesService
    {
        private DbContextServer _dbContextServer;
        public NotesService(DbContextServer context)
        {
            _dbContextServer = context;
        }

        public async Task<User> getUser(string username, string password)
        {
            User user = await _dbContextServer.Users
                .Include(u => u.NotesUsers)
                    .ThenInclude(snu => snu.Note)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
            return user;
        }

        public async Task<User> SaveOrUpdateNotes(User user, List<Note> notesUpdate)
        {
            List<Note> listNewNotes = new List<Note>();

            if (user == null) {
                foreach (var note in notesUpdate)
                {
                    note.DirtyFlagChangesMade = false; // Reset the dirty flag after saving
                    var existingNote = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
                    if (existingNote != null)
                    {
                        _dbContextServer.Entry(existingNote).CurrentValues.SetValues(note);
                    }
                    else
                    {
                        listNewNotes.Add(note);
                        _dbContextServer.Notes.Add(note);
                    }
                }

                await _dbContextServer.SaveChangesAsync();

                foreach (var note in listNewNotes)
                {
                    var noteExists = await _dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
                    if (noteExists != null)
                    {
                        //var existingRelationship = await dbContextServer.NoteUsers.FirstOrDefaultAsync(nu => nu.IdNote == note.IdNote && nu.IdUser == user.IdUser);

                        //if (existingRelationship == null)
                        //{
                        var noteUser = new Note_User
                        {
                            IdNote = note.IdNote,
                            IdUser = user.IdUser
                        };
                        _dbContextServer.Note_Users.Add(noteUser);
                        //}

                    }
                }

                await _dbContextServer.SaveChangesAsync();
            }
            return user;
        }

        public async Task<List<Note>> GetNotesToUpdateClient(User user)
        {
            var notesToUpdate = await _dbContextServer.Note_Users
                .Where(nu => nu.IdUser == user.IdUser)
                .Select(nu => nu.Note)
                .ToListAsync();
            return notesToUpdate;
        }
    }
}
