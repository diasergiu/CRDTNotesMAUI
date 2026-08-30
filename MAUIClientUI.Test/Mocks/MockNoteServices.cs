using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MAUIClientUI.Test.Mocks
{

    public class MockNoteServices : INoteServices
    {
        private readonly List<CRDTChangePayload> _sentChanges = new List<CRDTChangePayload>();
        private List<NoteClient> _mockNotes = new List<NoteClient>();
        private List<DToSendChanges> _mockServerChanges = new List<DToSendChanges>();

        public IReadOnlyList<CRDTChangePayload> SentChanges => _sentChanges.AsReadOnly();

        public Func<CRDTChangePayload, Task> OnCRDTChangeSent { get; set; }


        public void SetMockNotes(List<NoteClient> notes)
        {
            _mockNotes = notes ?? new List<NoteClient>();
        }

        public void SetMockServerChanges(List<DToSendChanges> changes)
        {
            _mockServerChanges = changes ?? new List<DToSendChanges>();
        }

        public Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser)
        {
            var result = new ApiResultData<List<NoteClient>>
            {
                IsSuccess = true,
                Data = _mockNotes
            };
            return Task.FromResult(result);
        }

        public Task<ApiResultData<List<DToSendChanges>>> GetServerChanges()
        {
            var result = new ApiResultData<List<DToSendChanges>>
            {
                IsSuccess = true,
                Data = _mockServerChanges
            };
            return Task.FromResult(result);
        }

        public Task<ApiResult> CreateNewNote(NoteClient currentNote)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResult> DeleteNote(Guid noteId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResult> SendChangesToServer(List<DToSendChanges> noteClient)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload)
        {
            // Capture the change
            _sentChanges.Add(payload);

            // Simulate server broadcast if callback is set
            if (OnCRDTChangeSent != null)
            {
                await OnCRDTChangeSent(payload);
            }

            return new ApiResult { IsSuccess = true };
        }

        public Task<ApiResultData<List<DToSendChanges>>> GetServerChangesByNote(Guid noteId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResult> GiveNoteAccessToUser(Guid noteId, Guid userId)
        {
            throw new NotImplementedException();
        }
        public void Clear()
        {
            _sentChanges.Clear();
            _mockNotes.Clear();
            _mockServerChanges.Clear();
        }
    }
}
