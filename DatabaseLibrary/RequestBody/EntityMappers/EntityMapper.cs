using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.RequestBody.EntityMappers
{
    public static class EntityMapper
    {
        public static NoteServer MapNoteClientToNoteServer(NoteClient noteClient)
        {

            return new NoteServer
            {
                IdNote = noteClient.IdNote,
                Title = noteClient.Title,
                Content = noteClient.Content,
                LastUpdate = noteClient.LastUpdate,
                CreationDate = noteClient.CreationDate,
                Version = noteClient.Version,
                DirtyFlagChangesMade = noteClient.DirtyFlagChangesMade
            };
        }

        public static NoteClient MapNoteServerToNoteClient(NoteServer noteServer)
        {
            return new NoteClient
            {
                //IdNote = clientIdNote,
                IdNote = noteServer.IdNote,
                Title = noteServer.Title,
                CreationDate = noteServer.CreationDate,
                Content = noteServer.Content,
                LastUpdate = noteServer.LastUpdate,
                Version = noteServer.Version,
                DirtyFlagChangesMade = noteServer.DirtyFlagChangesMade
            };
        }

        public static UserServer MapUserClientToUserServer(UserClient userClient)
        {
            return new UserServer
            {
                IdUser = userClient.IdUser,
                Name = userClient.Name,
                Username = userClient.Username,
                Password = userClient.Password
            };
        }

        public static UserClient MapUserServerToUserClient(UserServer userServer)
        {
            return new UserClient
            {
                IdUser = userServer.IdUser,
                Name = userServer.Name,
                Username = userServer.Username,
                Password = userServer.Password
            };
        }


        public static SyncQueueServer MapSyncQueueClientToSyncQueueServer(SyncQueueClient syncQueueClient, Guid IdDevice)
        {
            return new SyncQueueServer
            {
                IdSync = syncQueueClient.IdSync,
             //   IdUser = syncQueueClient.IdUser,
                IdDevice = IdDevice,
                IdNote = syncQueueClient.IdNote,
                Operation = syncQueueClient.Operation,
                ContentChanges = syncQueueClient.ContentChanges,
                LastUpdate = syncQueueClient.LastUpdate,
            };
        }


        public static SyncQueueClient MapSyncQueueServerToSyncQueueClient(SyncQueueServer syncQueueServer)
        {
            return new SyncQueueClient
            {
                IdSync = syncQueueServer.IdSync,
              //  IdUser = syncQueueServer.IdUser,
                IdNote = syncQueueServer.IdNote,
                Operation = syncQueueServer.Operation,
                ContentChanges = syncQueueServer.ContentChanges,
                LastUpdate = syncQueueServer.LastUpdate,
            };
        }
    }
}
