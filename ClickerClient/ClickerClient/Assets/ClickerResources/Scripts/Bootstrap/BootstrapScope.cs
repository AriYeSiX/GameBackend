using System;
using System.IO;
using GameClient.Core;
using GameClient.Networking;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameClient.Bootstrap
{
    /// <summary>
    /// Root Lifetime Scope: services that persist for the entire duration of the game.
    /// A prefab containing this component is assigned in VContainerSettings → Root Lifetime Scope;
    /// VContainer then instantiates it, marks it with DontDestroyOnLoad, and sets it as the parent scope for all scenes.
    /// </summary>
    public class BootstrapScope : LifetimeScope
    {
        private const string ServerFileName = "server.txt";
        private const string ServerArgument = "-server";

        [Tooltip("Used when neither the -server argument nor StreamingAssets/server.txt provides an address.")]
        [SerializeField] private string _apiUrl = "http://localhost:5165/";

        protected override void Configure(IContainerBuilder builder)
        {
            var apiUrl = ResolveApiUrl();
            Debug.Log($"API server: {apiUrl}");

            builder.Register(_ => new ApiClient(apiUrl), Lifetime.Singleton);
            builder.Register<GameApi>(Lifetime.Singleton);
            builder.Register<GameHubClient>(Lifetime.Singleton);

            builder.Register<UserSession>(Lifetime.Singleton);
            builder.Register<LobbySession>(Lifetime.Singleton);
            builder.Register<MatchSession>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameFlow>().AsSelf();
        }

        /// <summary>
        /// Server address priority, so it can be changed without rebuilding the game:
        /// 1. Command-line argument: Game.exe -server http://26.12.34.56:5165
        /// 2. First non-empty line of StreamingAssets/server.txt (in a build: Game_Data/StreamingAssets/server.txt)
        /// 3. The serialized default value.
        /// </summary>
        private string ResolveApiUrl()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], ServerArgument, StringComparison.OrdinalIgnoreCase) && IsValidUrl(args[i + 1]))
                    return args[i + 1];
            }

            var path = Path.Combine(Application.streamingAssetsPath, ServerFileName);

            try
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var url = line.Trim();

                        if (url.Length > 0 && !url.StartsWith("#") && IsValidUrl(url))
                            return url;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to read {path}: {ex.Message}");
            }

            return _apiUrl;
        }

        private static bool IsValidUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
