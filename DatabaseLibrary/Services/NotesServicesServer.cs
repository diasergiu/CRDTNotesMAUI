//using DatabaseLibrary.Entities;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace DatabaseLibrary.Services
//{
//    public class NotesServicesServer
//    {
//        private DbContextServer serverContext;

//        public NotesServicesServer(DbContextServer context)
//        {
//            serverContext = context;
//        }

//        public async Task SaveNoteRequest(List<Note> noteRequests)
//        {
//            foreach (NoteRequest noteRequest in noteRequests)
//            {
//                Note note = new Note()
//                {
//                    Title = noteRequest.Title,
//                    Content = noteRequest.Content,
//                    LastUpdate = noteRequest.DateChanges,

//                };

//                if (noteRequest.isNewNote)
//                {

//                }
//            }
//        }
//    }
//}