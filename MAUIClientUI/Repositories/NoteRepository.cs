using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
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

        public List<SyncQueueClient> getAllChanges(UserClient user)
        {
            List<SyncQueueClient> changesMade =
                new List<SyncQueueClient>(); // this line if because the method needs to return a list, but if the query fails, it will return an empty list instead of null
            try
            {
                changesMade = _dbContextUser.SyncQueues.Where(n => n.IdUser == user.IdUser).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving flagged notes: {ex.Message}");
            }
            return changesMade;
        }
        //untested if it saves the changes to the database
        // kind of duplicated but i might need to use the device ID
        public void UpdateListNotes(List<ISyncQueue> flaggedNotes)
        {
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
                //else if (queue.Operation == "Delete")
                //{
                //    NoteServer existingNote = _dbContextUser.Notes.FirstOrDefault(n => n.IdNote == queue.IdNote);
                //    if (existingNote != null)
                //    {
                //        _dbContextUser.Notes.Remove(existingNote);
                //    }
                //}
                //    else if (queue.Operation == "Create")
                //    {
                //        NoteServer newNote = new NoteServer
                //        {
                //            IdNote = queue.IdNote,
                //            Content = queue.ContentChanges,
                //            LastUpdate = queue.LastUpdate
                //        };
                //        _dbContextUser.Notes.Add(newNote);
                //        Note_UserServer newConnection = new Note_UserServer
                //        {
                //            IdNote = queue.IdNote,
                //            IdUser = queue.IdUser
                //        };
                //        _dbContextUser.Note_Users.Add(newConnection);

                //    }
                //}
                _dbContextUser.SaveChangesAsync();
            }
        }
    }
}
