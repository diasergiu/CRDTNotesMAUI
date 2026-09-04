using DatabaseLibrary.WrapperClasses;

namespace EndToEndTest.Mocks { 

    public class MockNotificationServices
    {
   
        public event EventHandler<CRDTChangePayload> NoteUpdated;

       
        public void SimulateRemoteUpdate(CRDTChangePayload payload)
        {
            // Invoke the event synchronously for test control
            NoteUpdated?.Invoke(this, payload);
        }

        public Task SimulateRemoteUpdateAsync(CRDTChangePayload payload)
        {
            SimulateRemoteUpdate(payload);
            return Task.CompletedTask;
        }

    
        public void ClearSubscribers()
        {
            NoteUpdated = null;
        }
    }
}
