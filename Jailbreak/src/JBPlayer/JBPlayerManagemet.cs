
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace Jailbreak;

public static class JBPlayerManagement
{
    private static readonly Dictionary<ulong, JBPlayer> _players = new();
    private static ILogger _logger = null!;

    public static void Initialize(ILogger logger)
    {
        _logger = logger;
    }
    public static JBPlayer GetOrCreate(CCSPlayerController controller)
    {
        if (!_players.TryGetValue(controller.SteamID, out var jbPlayer))
        {
            jbPlayer = new JBPlayer(controller, _logger, Jailbreak.Instance);
            _players[controller.SteamID] = jbPlayer;
        }
        return jbPlayer;
    }
    public static void Remove(ulong steamId)
    {
        if (_players.TryGetValue(steamId, out var player))
        {
            player.Dispose();
            _players.Remove(steamId);
        }
    }
    public static JBPlayer? GetWarden()
    {
        return _players.Values.FirstOrDefault(p => p.IsWarden);
    }
    public static List<JBPlayer> GetAllPlayers()
    {
        return _players.Values.Where(p => p.IsValid).ToList();
    }
    public static List<JBPlayer> GetPrisoners()
    {
        return _players.Values.Where(p => p.IsPrisoner && p.IsValid).ToList();
    }
    public static List<JBPlayer> GetGuards()
    {
        return _players.Values.Where(p => p.IsGuard && p.IsValid).ToList();
    }
}