using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Miscellaneous;
using MAUIClientUI.Test.HelperClasses;

namespace MAUIClientUI.Test.Concurrency
{ 
    public class NoteOrchestratorConcurrencyTest
    {
        private const int OperationCount = 50;

        public static IEnumerable<object[]> Iterations =>
            Enumerable.Range(0, 25).Select(i => new object[] { i });

        [Theory]
        [MemberData(nameof(Iterations))]
        public async Task LocalTypingDuringRemoteMerge_LosesNoCharacters(int iteration)
        {
            var noteId = Guid.NewGuid();
            var services = new RecordingNoteServices();

            var localOrchestrator = CreateOrchestrator(noteId, services);
            var remoteOrchestrator = CreateOrchestrator(noteId, services);


            var remotePayloads = new List<CRDTChangePayload>();
            services.OnCRDTChangeSent = payload =>
            {
                remotePayloads.Add(payload);
                return Task.CompletedTask;
            };

            for (int i = 0; i < OperationCount; i++)
                await remoteOrchestrator.InsertCharacter(i, 'b');

            Assert.Equal(OperationCount, remotePayloads.Count);

  
            services.OnCRDTChangeSent = null;

            var failures = new List<Exception>();

            var typing = Task.Run(async () =>
            {
                for (int i = 0; i < OperationCount; i++)
                {
                    try
                    {
                        // Position 0 is always valid regardless of how the document grows underneath us.
                        await localOrchestrator.InsertCharacter(0, 'a');
                    }
                    catch (Exception ex)
                    {
                        lock (failures) { failures.Add(ex); }
                    }
                }
            });

            var merging = Task.Run(async () =>
            {
                foreach (var payload in remotePayloads)
                {
                    try
                    {
                        await localOrchestrator.ApplyRemoteChangesAsync(payload);
                    }
                    catch (Exception ex)
                    {
                        lock (failures) { failures.Add(ex); }
                    }
                }
            });

            await Task.WhenAll(typing, merging);

            Assert.True(failures.Count == 0,
                $"Iteration {iteration}: {failures.Count} operation(s) threw. First: {failures.FirstOrDefault()}");

            var text = localOrchestrator.GetText();

            Assert.Equal(OperationCount, text.Count(c => c == 'a'));
            Assert.Equal(OperationCount, text.Count(c => c == 'b'));
        }


        [Theory]
        [MemberData(nameof(Iterations))]
        public async Task OverlappingRemoteMerges_LoseNoCharacters(int iteration)
        {
            var noteId = Guid.NewGuid();
            var services = new RecordingNoteServices();

            var localOrchestrator = CreateOrchestrator(noteId, services);

            var firstBatch = await CapturePayloadsAsync(noteId, services, 'x');
            var secondBatch = await CapturePayloadsAsync(noteId, services, 'y');

            services.OnCRDTChangeSent = null;

            var failures = new List<Exception>();

            async Task ApplyAll(IEnumerable<CRDTChangePayload> payloads)
            {
                foreach (var payload in payloads)
                {
                    try
                    {
                        await localOrchestrator.ApplyRemoteChangesAsync(payload);
                    }
                    catch (Exception ex)
                    {
                        lock (failures) { failures.Add(ex); }
                    }
                }
            }

            await Task.WhenAll(
                Task.Run(() => ApplyAll(firstBatch)),
                Task.Run(() => ApplyAll(secondBatch)));

            Assert.True(failures.Count == 0,
                $"Iteration {iteration}: {failures.Count} merge(s) threw. First: {failures.FirstOrDefault()}");

            var text = localOrchestrator.GetText();

            Assert.Equal(OperationCount, text.Count(c => c == 'x'));
            Assert.Equal(OperationCount, text.Count(c => c == 'y'));
        }

        private static async Task<List<CRDTChangePayload>> CapturePayloadsAsync(
            Guid noteId, RecordingNoteServices services, char character)
        {
            var captured = new List<CRDTChangePayload>();
            services.OnCRDTChangeSent = payload =>
            {
                captured.Add(payload);
                return Task.CompletedTask;
            };

            var producer = CreateOrchestrator(noteId, services);
            for (int i = 0; i < OperationCount; i++)
                await producer.InsertCharacter(i, character);

            return captured;
        }

        private static NoteOrchestrator CreateOrchestrator(Guid noteId, RecordingNoteServices services)
        {
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = "Concurrency Test Note",
                CRDTCharacter = new List<CRDTCharacterClient>()
            };

            return new NoteOrchestrator(
                note,
                new InMemoryNoteRepository(),
                services,
                new InMemoryCRDTCharacterRepository());
        }
    }
}
