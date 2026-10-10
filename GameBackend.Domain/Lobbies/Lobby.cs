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
            throw new DomainException("Название лобби должно быть от 3 до 40 символов");

        if (maxPlayers is < MinPlayers or > MaxPlayersLimit)
            throw new DomainException($"Игроков в лобби может быть от {MinPlayers} до {MaxPlayersLimit}");

        return new Lobby(Guid.NewGuid(), name, hostId, maxPlayers,
            LobbyState.Waiting, now, [new LobbyMember(hostId, hostName)]);
    }

    public static Lobby Restore(Guid id, string name, Guid hostId, int maxPlayers,
        LobbyState state, DateTime createdAt, IEnumerable<LobbyMember> members) =>
        new(id, name, hostId, maxPlayers, state, createdAt, members);

    public void Join(Guid playerId, string username)
    {
        if (State != LobbyState.Waiting)
            throw new DomainException("Матч в этом лобби уже начинается");

        if (_members.Any(m => m.PlayerId == playerId))
            return;

        if (IsFull)
            throw new DomainException("Лобби заполнено");

        _members.Add(new LobbyMember(playerId, username));
    }

    public void Leave(Guid playerId)
    {
        _members.RemoveAll(m => m.PlayerId == playerId);

        if (HostId == playerId && _members.Count > 0)
            HostId = _members[0].PlayerId;
    }

    /// <returns>true, если все готовы и матч должен стартовать</returns>
    public bool SetReady(Guid playerId, bool isReady)
    {
        if (State != LobbyState.Waiting)
            throw new DomainException("Матч уже начинается");

        var member = _members.FirstOrDefault(m => m.PlayerId == playerId)
                     ?? throw new DomainException("Вы не состоите в этом лобби");

        member.IsReady = isReady;

        if (_members.Count < MinPlayers || !_members.All(m => m.IsReady))
            return false;

        State = LobbyState.Starting;
        return true;
    }
}