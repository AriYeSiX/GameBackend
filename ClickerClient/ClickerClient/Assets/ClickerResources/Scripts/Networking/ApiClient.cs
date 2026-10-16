using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameClient.Networking
{
    /// <summary>
    /// HTTP client for the REST API: stores tokens and refreshes the access token transparently.
    /// </summary>
    public sealed class ApiClient : IDisposable
    {
        internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        // The access token lives for 15 minutes. For long-lived connections (the hub) we refresh it in advance.
        private static readonly TimeSpan AccessTokenRefreshAge = TimeSpan.FromMinutes(10);

        private readonly HttpClient _http;
        private readonly SemaphoreSlim _refreshGate = new(1, 1);

        private string _refreshToken;
        private DateTime _accessTokenReceivedAt;

        public ApiClient(string baseUrl)
        {
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public Uri BaseAddress => _http.BaseAddress;
        public string AccessToken { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

        public async Task RegisterAsync(string username, string email, string password)
        {
            var request = new RegisterRequest { Username = username, Email = email, Password = password };
            SetTokens(await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request, authorize: false));
        }

        public async Task LoginAsync(string email, string password)
        {
            var request = new LoginRequest { Email = email, Password = password };
            SetTokens(await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, authorize: false));
        }

        /// <summary>The access token, refreshed if needed. Used when connecting to the hub.</summary>
        public async Task<string> GetFreshAccessTokenAsync()
        {
            if (IsAuthenticated && DateTime.UtcNow - _accessTokenReceivedAt > AccessTokenRefreshAge)
                await RefreshTokensAsync(AccessToken);

            return AccessToken;
        }

        public Task<T> GetAsync<T>(string path, bool authorize = true) =>
            SendAsync<T>(HttpMethod.Get, path, null, authorize);

        public Task<T> PostAsync<T>(string path, object body, bool authorize = true) =>
            SendAsync<T>(HttpMethod.Post, path, body, authorize);

        private async Task<T> SendAsync<T>(HttpMethod method, string path, object body, bool authorize, bool isRetry = false)
        {
            using var request = new HttpRequestMessage(method, path);
            var sentToken = AccessToken;

            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

            if (authorize && IsAuthenticated)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sentToken);

            using var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized && authorize && !isRetry && !string.IsNullOrEmpty(_refreshToken))
            {
                await RefreshTokensAsync(sentToken);
                return await SendAsync<T>(method, path, body, authorize, isRetry: true);
            }

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw ApiException.From(response.StatusCode, content, Json);

            return string.IsNullOrEmpty(content) ? default : JsonSerializer.Deserialize<T>(content, Json);
        }

        /// <param name="staleAccessToken">The token that got a 401 response.</param>
        private async Task RefreshTokensAsync(string staleAccessToken)
        {
            // If two requests get a 401 at the same time, refresh the token only once.
            // Otherwise the second exchange would use an already revoked refresh token (tokens are rotated).
            await _refreshGate.WaitAsync();

            try
            {
                if (AccessToken != staleAccessToken)
                    return; // another request refreshed the token while we were waiting

                var request = new RefreshRequest { RefreshToken = _refreshToken };
                SetTokens(await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/refresh", request, authorize: false));
            }
            catch
            {
                SetTokens(null); // the refresh token is invalid too, so the player must sign in again
                throw;
            }
            finally
            {
                _refreshGate.Release();
            }
        }

        private void SetTokens(AuthResponse tokens)
        {
            AccessToken = tokens?.AccessToken;
            _refreshToken = tokens?.RefreshToken;
            _accessTokenReceivedAt = DateTime.UtcNow;
        }

        public void Dispose()
        {
            _http.Dispose();
            _refreshGate.Dispose();
        }
    }
}
