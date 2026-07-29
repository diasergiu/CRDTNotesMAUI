using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections;
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
        public async Task<List<NoteServer>> GetAllNotesFromUser(int IdUser)
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

        public async Task SyncData(List<SyncQueueServer> changesMade)
        {
            foreach (SyncQueueServer queue in changesMade)
            {
                SyncData(queue);
            }
            //await _dbContextServer.SaveChangesAsync();
        }
    

    public async Task SyncData(SyncQueueServer changesMade)
        {
            if (changesMade.Operation == "Update")
            {
                NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == changesMade.IdNote);
                if (existingNote != null)
                {
                    existingNote.Content = changesMade.ContentChanges;
                    existingNote.LastUpdate = changesMade.LastUpdate;
                }
            }
            else if (changesMade.Operation == "Delete")
            {
                NoteServer existingNote = _dbContextServer.Notes.FirstOrDefault(n => n.IdNote == changesMade.IdNote);
                if (existingNote != null)
                {
                    _dbContextServer.Notes.Remove(existingNote);
                }
            }
            else if (changesMade.Operation == "Create")
            {
                //CreateNote(changesMade);

            }
            await _dbContextServer.SaveChangesAsync(); // Now we make a save at every 
        }

        public List<SyncQueueServer> GetChangesFromNote(int idNote)
        {
            return _dbContextServer.Sync_Queues.Where(n => n.IdNote == idNote).ToList();
        }

        public async Task<NoteServer> CreateNote(NoteClient note, int idUser) 
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

        public async Task UpdateChanges(NoteServer note)
        {
            _dbContextServer.Notes.Update(note);
        }
    }
}
