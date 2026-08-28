//using DatabaseLibrary.Entities.Client;
//using DatabaseLibrary.WrapperClasses;
//using MAUIClientUI.MVVM;
//using MAUIClientUI.Repositories;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace MAUIClientUI.Test.EndToEndServiceTest
//{
//    internal class MVMMTests
//    {


//        // ==================== MVVM TO SERVER CRDT PIPELINE TESTS ====================

//        [Fact]
//        public async Task E2E_CRDT_NotesViewModel_Initialization_WithEmptyContent_Succeeds()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var emptyNote = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "ViewModel Test Note",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            // Act
//            var createResult = await _noteService.CreateNewNote(emptyNote);
//            Assert.True(createResult.IsSuccess, "Note creation failed");

//            // Create ViewModel
//            var viewModel = new NotesViewModel(emptyNote, _noteService, isNewNote: true);

//            // Assert
//            Assert.NotNull(viewModel);
//            Assert.NotNull(viewModel.NoteController);
//            var noteText = viewModel.NoteController.GetText();
//            Assert.Equal("", noteText);
//        }

//        [Fact]
//        public async Task E2E_CRDT_NoteController_InsertCharacter_CreatesCharacterAndSyncsToServer()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var note = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "Character Insert Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(note);
//            Assert.True(createResult.IsSuccess, "Failed to create note");

//            var viewModel = new NotesViewModel(note, _noteService, isNewNote: true);

//            // Act - Insert a single character
//            viewModel.NoteController.InsertCharacter(0, 'H');
//            var noteTextAfterInsert = viewModel.NoteController.GetText();

//            // Assert
//            Assert.Equal("H", noteTextAfterInsert);

//            // Verify CRDT character was created
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var characters = crdtRepo.GetCRDTCharacterFromNote(note.IdNote);
//            Assert.NotEmpty(characters);
//            Assert.Single(characters);
//            Assert.Equal('H', characters.First().Character);
//        }

//        [Fact]
//        public async Task E2E_CRDT_NoteController_InsertString_CreatesMultipleCharactersAndSyncs()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var note = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "String Insert Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(note);
//            Assert.True(createResult.IsSuccess, "Failed to create note");

//            var viewModel = new NotesViewModel(note, _noteService, isNewNote: true);

//            // Act - Insert a string (simulating paste operation)
//            viewModel.NoteController.InsertString(0, "Hello");
//            var noteText = viewModel.NoteController.GetText();

//            // Assert
//            Assert.Equal("Hello", noteText);

//            // Verify all CRDT characters were created
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var characters = crdtRepo.GetCRDTCharacterFromNote(note.IdNote);
//            Assert.NotEmpty(characters);
//            Assert.Equal(5, characters.Count);  // "Hello" = 5 characters

//            var charList = characters.OrderBy(c => c.IdCharacter).ToList();
//            var reconstructed = new string(charList.Where(c => !c.Tombstone).Select(c => c.Character).ToArray());
//            Assert.Equal("Hello", reconstructed);
//        }

//        [Fact]
//        public async Task E2E_CRDT_NoteController_DeleteCharacter_MarksTombstoneAndSyncs()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var note = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "Delete Character Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(note);
//            Assert.True(createResult.IsSuccess, "Failed to create note");

//            var viewModel = new NotesViewModel(note, _noteService, isNewNote: true);

//            // Act - Insert string then delete middle character
//            viewModel.NoteController.InsertString(0, "Hello");
//            string textAfterInsert = viewModel.NoteController.GetText();
//            Assert.Equal("Hello", textAfterInsert);

//            // Delete the 'l' at position 3 (cursor at position 3, delete to the left)
//            viewModel.NoteController.DeleteCharacter(3);
//            string textAfterDelete = viewModel.NoteController.GetText();

//            // Assert
//            Assert.Equal("Helo", textAfterDelete);

//            // Verify CRDT character has tombstone flag
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var characters = crdtRepo.GetCRDTCharacterFromNote(note.IdNote);
//            Assert.Equal(5, characters.Count);  // Still 5 (1 marked as tombstone)
//            Assert.Single(characters.Where(c => c.Tombstone));  // One should be marked as deleted
//        }

//        [Fact]
//        public async Task E2E_CRDT_NoteController_DeleteRange_MarksMutipleCharactersAsTombstone()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var note = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "Delete Range Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(note);
//            Assert.True(createResult.IsSuccess, "Failed to create note");

//            var viewModel = new NotesViewModel(note, _noteService, isNewNote: true);

//            // Act
//            viewModel.NoteController.InsertString(0, "HelloWorld");
//            Assert.Equal("HelloWorld", viewModel.NoteController.GetText());

//            // Delete "loWo" (positions 3-7)
//            viewModel.NoteController.DeleteCharacterRange(3, 7);
//            string textAfterDelete = viewModel.NoteController.GetText();

//            // Assert
//            Assert.Equal("Helrld", textAfterDelete);

//            // Verify 4 characters are marked as tombstone
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var characters = crdtRepo.GetCRDTCharacterFromNote(note.IdNote);
//            Assert.Equal(10, characters.Count);  // Still 10 total
//            Assert.Equal(4, characters.Count(c => c.Tombstone));  // 4 deleted
//        }

//        [Fact]
//        public async Task E2E_CRDT_MultiUserConcurrentEdits_ProperlyConverges()
//        {
//            // Arrange - Create a shared note
//            UserDevice.SetLocalUser(_user1.UserId);
//            var sharedNote = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "Multi-User CRDT Convergence Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(sharedNote);
//            Assert.True(createResult.IsSuccess, "Failed to create shared note");

//            // User 1 creates and edits a ViewModel
//            var user1ViewModel = new NotesViewModel(sharedNote, _noteService);
//            UserDevice.SetLocalUser(_user1.UserId);
//            user1ViewModel.NoteController.InsertString(0, "Alice");
//            string user1Text = user1ViewModel.NoteController.GetText();
//            Assert.Equal("Alice", user1Text);

//            // User 2 creates ViewModel for same note
//            UserDevice.SetLocalUser(_user2.UserId);
//            var user2ViewModel = new NotesViewModel(sharedNote, _noteService);
//            user2ViewModel.NoteController.InsertString(0, "Bob");
//            string user2Text = user2ViewModel.NoteController.GetText();
//            Assert.Equal("Bob", user2Text);

//            // User 1 adds more content
//            UserDevice.SetLocalUser(_user1.UserId);
//            user1ViewModel.NoteController.InsertCharacter(5, ' ');
//            user1ViewModel.NoteController.InsertString(6, "and Charlie");
//            string user1FinalText = user1ViewModel.NoteController.GetText();
//            Assert.Equal("Alice and Charlie", user1FinalText);

//            // User 2 adds more content
//            UserDevice.SetLocalUser(_user2.UserId);
//            user2ViewModel.NoteController.InsertCharacter(3, ' ');
//            user2ViewModel.NoteController.InsertString(4, "the Builder");
//            string user2FinalText = user2ViewModel.NoteController.GetText();
//            Assert.Equal("Bob the Builder", user2FinalText);

//            // Assert - Both users have their own persistent edits
//            Assert.Equal("Alice and Charlie", user1FinalText);
//            Assert.Equal("Bob the Builder", user2FinalText);

//            // Verify CRDT characters from both users exist
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var allCharacters = crdtRepo.GetCRDTCharacterFromNote(sharedNote.IdNote);
//            Assert.NotEmpty(allCharacters);
//        }

//        [Fact]
//        public async Task E2E_CRDT_ViewModelPersistence_ReloadedNotePreservesContent()
//        {
//            // Arrange
//            UserDevice.SetLocalUser(_user1.UserId);
//            var note = new NoteClient
//            {
//                IdNote = Guid.NewGuid(),
//                Title = "Persistence Test",
//                Content = "",
//                CreationDate = DateTime.UtcNow,
//                LastUpdate = DateTime.UtcNow,
//                DirtyFlagChangesMade = false,
//                Version = 1,
//                CRDTCharacter = new List<CRDTCharacterClient>()
//            };

//            var createResult = await _noteService.CreateNewNote(note);
//            Assert.True(createResult.IsSuccess, "Failed to create note");

//            // Act - Create ViewModel 1 and insert text
//            var viewModel1 = new NotesViewModel(note, _noteService);
//            viewModel1.NoteController.InsertString(0, "Persisted Content");
//            string text1 = viewModel1.NoteController.GetText();
//            Assert.Equal("Persisted Content", text1);

//            // Get CRDT characters from database
//            var crdtRepo = new CRDTCharacterRepository(new DbContextClient());
//            var savedCharacters = crdtRepo.GetCRDTCharacterFromNote(note.IdNote);
//            Assert.Equal(17, savedCharacters.Count);  // "Persisted Content"

//            // Create a new ViewModel with same note (simulating reload)
//            note.CRDTCharacter = savedCharacters;
//            var viewModel2 = new NotesViewModel(note, _noteService);

//            // Assert
//            string text2 = viewModel2.NoteController.GetText();
//            Assert.Equal("Persisted Content", text2);
//        }
//    }
//}
