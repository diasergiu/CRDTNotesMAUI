using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.ServeRepositories
{
    public class NotesRepository
    {
        private DbContextServer _dbContextServer;
        public NotesRepository(DbContextServer context)
        {
            _dbContextServer = context;
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

        public async Task<List<SyncQueueServer>> GetServerSyncChanges(IUser user, int deviceId)
        {
            var notesToUpdate = await _dbContextServer.Sync_Queues
                .Where(nu => nu.IdUser == user.IdUser && nu.IdDevice == deviceId)
                .Select(nu => nu)
                .ToListAsync();
            return notesToUpdate;
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

        public async Task SyncData(List<SyncQueueServer> changesMade)
        {
            foreach (SyncQueueServer queue in changesMade)
            {
                if (queue.Operation == "Update")
                {
                    NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == queue.IdNote);
                    if (existingNote != null)
                    {
                        existingNote.Content = queue.ContentChanges;
                        existingNote.LastUpdate = queue.LastUpdate;
                    }
                }
                else if (queue.Operation == "Delete")
                {
                    NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == queue.IdNote);
                    if (existingNote != null)
                    {
                        _dbContextServer.Notes.Remove(existingNote);
                    }
                }
                else if(queue.Operation == "Create")
                {
                    NoteServer newNote = new NoteServer
                    {
                        IdNote = queue.IdNote,
                        Content = queue.ContentChanges,
                        LastUpdate = queue.LastUpdate
                    };
                    _dbContextServer.Notes.Add(newNote);
                    Note_UserServer newConnection = new Note_UserServer
                    {
                        IdNote = queue.IdNote,
                        IdUser = queue.IdUser
                    };
                    _dbContextServer.Note_Users.Add(newConnection);
                    
                }
            }
            await _dbContextServer.SaveChangesAsync();
        }
    }
}
