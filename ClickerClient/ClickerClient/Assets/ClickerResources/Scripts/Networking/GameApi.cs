using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace GameClient.Networking
{
    /// <summary>Typed REST calls on top of <see cref="ApiClient"/>.</summary>
    public sealed class GameApi
    {
        private readonly ApiClient _api;

        public GameApi(ApiClient api) => _api = api;

        public Task<PlayerProfileDto> GetMeAsync() =>
            _api.GetAsync<PlayerProfileDto>("api/players/me");

        public Task<List<LeaderboardEntry>> GetTopAsync(string board, int count = 10) =>
            _api.GetAsync<List<LeaderboardEntry>>($"api/leaderboards/{board}/top?count={count}", authorize: false);

        /// <returns>null if the player has not played any matches yet.</returns>
        public async Task<PlayerRankResponse> TryGetMyRankAsync(string board)
        {
            try
            {
                return await _api.GetAsync<PlayerRankResponse>($"api/leaderboards/{board}/me");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public Task<List<LobbyDto>> GetOpenLobbiesAsync() =>
            _api.GetAsync<List<LobbyDto>>("api/lobbies");
    }
}
