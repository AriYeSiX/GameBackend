namespace GameBackend.Domain.Lobbies;

public enum LobbyState
{
    Waiting,
    Starting
}

public class LobbyMember(Guid playerId, string username, bool isReady = false)
{
    public Guid PlayerId { get; } = playerId;
    public string Username { get; } = username;
    public bool IsReady { get; internal set; } = isReady;
}

public class Lobby
{
    public const int MinPlayers = 2;
    public const int MaxPlayersLimit = 16;

    private readonly List<LobbyMember> _members;

    private Lobby(Guid id, string name, Guid hostId, int maxPlayers,
        LobbyState state, DateTime createdAt, IEnumerable<LobbyMember> members)
    {
        Id = id;
        Name = name;
        HostId = hostId;
        MaxPlayers = maxPlayers;
        State = state;
        CreatedAt = createdAt;
        _members = members.ToList();
    }

    public Guid Id { get; }
    public string Name { get; }
    public Guid HostId { get; private set; }
    public int MaxPlayers { get; }
    public LobbyState State { get; private set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<LobbyMember> Members => _members;

    public bool IsFull => _members.Count >= MaxPlayers;
    public bool IsEmpty => _members.Count == 0;

    public static Lobby Create(string name, int maxPlayers, Guid hostId, string hostName, DateTime now)
    {
        name = name.Trim();

        if (name.Length is < 3 or > 40)
            throw new DomainException("The lobby name must be between 3 and 40 characters long");

        if (maxPlayers is < MinPlayers or > MaxPlayersLimit)
            throw new DomainException($"A lobby must have between {MinPlayers} and {MaxPlayersLimit} players");

        return new Lobby(Guid.NewGuid(), name, hostId, maxPlayers,
            LobbyState.Waiting, now, [new LobbyMember(hostId, hostName)]);
    }

    public static Lobby Restore(Guid id, string name, Guid hostId, int maxPlayers,
        LobbyState state, DateTime createdAt, IEnumerable<LobbyMember> members) =>
        new(id, name, hostId, maxPlayers, state, createdAt, members);

    public void Join(Guid playerId, string username)
    {
        if (State != LobbyState.Waiting)
            throw new DomainException("The match in this lobby is already starting");

        if (_members.Any(m => m.PlayerId == playerId))
            return;

        if (IsFull)
            throw new DomainException("The lobby is full");

        _members.Add(new LobbyMember(playerId, username));
    }

    public void Leave(Guid playerId)
    {
        _members.RemoveAll(m => m.PlayerId == playerId);

        if (HostId == playerId && _members.Count > 0)
            HostId = _members[0].PlayerId;
    }

    /// <returns>true, if everyone is ready and the match should start</returns>
    public bool SetReady(Guid playerId, bool isReady)
    {
        if (State != LobbyState.Waiting)
            throw new DomainException("The match is already starting");

        var member = _members.FirstOrDefault(m => m.PlayerId == playerId)
                     ?? throw new DomainException("You are not part of this lobby");

        member.IsReady = isReady;

        if (_members.Count < MinPlayers || !_members.All(m => m.IsReady))
            return false;

        State = LobbyState.Starting;
        return true;
    }
}