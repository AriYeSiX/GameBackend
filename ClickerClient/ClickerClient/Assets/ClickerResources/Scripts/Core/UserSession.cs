using System;
using GameClient.Networking;

namespace GameClient.Core
{
    /// <summary>The signed-in player. Lives for the whole application lifetime.</summary>
    public sealed class UserSession
    {
        public Guid PlayerId { get; private set; }
        public string Username { get; private set; }
        public bool IsSignedIn => PlayerId != Guid.Empty;

        public void SignIn(PlayerProfileDto profile)
        {
            PlayerId = profile.Id;
            Username = profile.Username;
        }
    }
}
