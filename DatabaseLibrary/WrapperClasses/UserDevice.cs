using System;

namespace DatabaseLibrary.WrapperClasses
{
    /// <summary>
    /// Interface for user context management. Allows different clients to maintain separate user identities.
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Gets or sets the ID of the local user.
        /// </summary>
        Guid LocalUser { get; set; }

        /// <summary>
        /// Gets or sets the SignalR connection ID for this application instance.
        /// Sent with every update so the server can exclude only this connection,
        /// not every connection belonging to the same user.
        /// </summary>
        string? HubConnectionId { get; set; }
    }

    /// <summary>
    /// Default implementation of IUserContext for managing user identity in a client instance.
    /// </summary>
    public class UserContext : IUserContext
    {
        public Guid LocalUser { get; set; }
        public string? HubConnectionId { get; set; }
    }
}
