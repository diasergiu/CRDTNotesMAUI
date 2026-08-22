using DatabaseLibrary.Entities;
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
                DirtyFlagChangesMade = noteClient.DirtyFlagChangesMade,
              //  CRDTCharacter = MapCharacterClientToServer(noteClient.CRDTCharacter)

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
                DirtyFlagChangesMade = noteServer.DirtyFlagChangesMade,
                //CRDTCharacter = MapCharacterServerToClient(noteServer.CRDTCharacter)
            };
        }

        public static List<CRDTCharacter> MapCharacterClientToServer(List<CRDTCharacterClient> client)
        {
            List<CRDTCharacter> map = new List<CRDTCharacter>();
            foreach(var character in client)
            {
                map.Add(MapCharacterClientToServer(character));
            }
            return map;
        }

        public static List<CRDTCharacterClient> MapCharacterServerToClient(List<CRDTCharacter> client)
        {
            List<CRDTCharacterClient> map = new List<CRDTCharacterClient>();
            foreach (var character in client)
            {
                map.Add(MapCharacterServerToClient(character));
            }
            return map;
        }
        public static CRDTCharacter MapCharacterClientToServer(CRDTCharacterClient client) // how do those dont create conflicts with already existing data from the server
        {
            return new CRDTCharacter()
            {
                Character = client.Character,
                IdCharacter = client.IdCharacter,
                IdNote = client.IdNote,
                ClockDateTime = client.ClockDateTime,
                Tombstone = client.Tombstone,
            };
        }
        public static CRDTCharacterClient MapCharacterServerToClient(CRDTCharacter Server) // how do those dont create conflicts with already existing data from the server
        {
            return new CRDTCharacterClient()
            {
                Character = Server.Character,
                IdCharacter = Server.IdCharacter,
                IdNote = Server.IdNote,
                ClockDateTime = Server.ClockDateTime,
                Tombstone = Server.Tombstone,
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
    }
}
