using GameClient.Networking;

namespace GameClient.Core
{
    /// <summary>The state of the lobby, which must survive a scene reload.</summary>
    public sealed class LobbySession
    {
        public LobbyDto CurrentLobby { get; set; }

        /// <summary>A message to display upon entering the lobby (for example, "Opponents have left the match").</summary>
        public string PendingNotice { get; set; }

        public string TakeNotice()
        {
            var notice = PendingNotice;
            PendingNotice = null;
            return notice;
        }
    }
}
