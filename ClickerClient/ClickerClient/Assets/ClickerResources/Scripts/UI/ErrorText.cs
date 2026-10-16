using System;
using System.Net.Http;
using GameClient.Networking;
using Microsoft.AspNetCore.SignalR;

namespace GameClient.UI
{
    /// <summary>Error text to show to the player.</summary>
    public static class ErrorText
    {
        public static string From(Exception ex)
        {
            switch (ex)
            {
                case ApiException api:
                    return api.Message;

                case HubException hub:
                    // The SignalR client wraps the server text: "... HubException: The lobby is full"
                    const string marker = "HubException: ";
                    var index = hub.Message.IndexOf(marker, StringComparison.Ordinal);
                    return index >= 0 ? hub.Message.Substring(index + marker.Length) : hub.Message;

                case HttpRequestException _:
                    return "Server is unavailable";

                default:
                    return ex.Message;
            }
        }
    }
}
