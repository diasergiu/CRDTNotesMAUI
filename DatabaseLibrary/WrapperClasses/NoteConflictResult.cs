using DatabaseLibrary.Entities.Server;
using System;

namespace DatabaseLibrary.WrapperClasses
{
    /// <summary>
    /// Result object for note update operations that includes conflict information.
    /// Used when server detects version mismatch (409 Conflict).
    /// </summary>
    public class NoteConflictResult : ApiResult
    {
        /// <summary>
        /// True if version mismatch conflict occurred
        /// </summary>
        public bool IsVersionConflict { get; set; }

        /// <summary>
        /// Server's current version of the note when conflict occurs
        /// Client should use this to understand what changed on server
        /// </summary>
        public NoteServer ServerNote { get; set; }

        /// <summary>
        /// Client's version that was rejected
        /// Useful for debugging/logging
        /// </summary>
        public int ClientVersionAttempted { get; set; }

        /// <summary>
        /// Server's version at time of conflict
        /// </summary>
        public int ServerVersionAtConflict { get; set; }

        /// <summary>
        /// Creates a successful update result (no conflict)
        /// </summary>
        public static NoteConflictResult Success(NoteServer note)
        {
            return new NoteConflictResult
            {
                IsSuccess = true,
                ErrorMessage = null,
                IsVersionConflict = false,
                ServerNote = note,
                ClientVersionAttempted = 0,
                ServerVersionAtConflict = 0
            };
        }

        /// <summary>
        /// Creates a conflict result with server version information
        /// </summary>
        public static NoteConflictResult Conflict(NoteServer serverNote, int clientVersion)
        {
            return new NoteConflictResult
            {
                IsSuccess = false,
                ErrorMessage = $"Version conflict: Client sent v{clientVersion}, but server has v{serverNote.Version}. " +
                               $"Note was modified by another user at {serverNote.LastUpdate}",
                IsVersionConflict = true,
                ServerNote = serverNote,
                ClientVersionAttempted = clientVersion,
                ServerVersionAtConflict = serverNote.Version
            };
        }

        /// <summary>
        /// Creates an error result (not a conflict, but another error)
        /// </summary>
        public static NoteConflictResult Error(string message, ApiErrorType errorType = ApiErrorType.Unknown)
        {
            return new NoteConflictResult
            {
                IsSuccess = false,
                ErrorMessage = message,
                IsVersionConflict = false,
                ServerNote = null,
                ErrorType = errorType
            };
        }
    }
}
