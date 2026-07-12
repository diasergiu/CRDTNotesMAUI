using DatabaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.ServerServices
{
    public class NotesService
    {
        private DbContextServer dbContextServer;
        public NotesService(DbContextServer context)
        {
            dbContextServer = context;
        }

        public async Task<User> getUser(string username, string password)
        {
            User user = await dbContextServer.Users
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
                    var existingNote = await dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
                    if (existingNote != null)
                    {
                        dbContextServer.Entry(existingNote).CurrentValues.SetValues(note);
                    }
                    else
                    {
                        listNewNotes.Add(note);
                        dbContextServer.Notes.Add(note);
                    }
                }

                await dbContextServer.SaveChangesAsync();

                foreach (var note in listNewNotes)
                {
                    var noteExists = await dbContextServer.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);
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
                        dbContextServer.Note_Users.Add(noteUser);
                        //}
                        
                    }
                }

                await dbContextServer.SaveChangesAsync();
            }
            return user;
        }

        public async Task<List<Note>> GetNotesToUpdateClient(User user)
        {
            var notesToUpdate = await dbContextServer.Note_Users
                .Where(nu => nu.IdUser == user.IdUser)
                .Select(nu => nu.Note)
                .ToListAsync();
            return notesToUpdate;
        }
    }
}
