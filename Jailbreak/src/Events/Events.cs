using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API;

namespace Jailbreak;

public static class Events
{
    public static void RegisterEventsHandlers()
    {
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    }
    public static void RegisterListeners()
    {
        Instance.RegisterListener<OnTick>(OnTick);
    }
    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var jbPlayer = JBPlayerManagement.GetOrCreate(player);

            jbPlayer.OnRoundStart();
        }

        return HookResult.Continue;
    }
    private static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var jbPlayer = JBPlayerManagement.GetOrCreate(player);

            jbPlayer.OnRoundEnd();
        }

        return HookResult.Continue;
    }


    private static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            var jbPlayer = JBPlayerManagement.GetOrCreate(player);
            jbPlayer.UpdateRole();
        });

        return HookResult.Continue;
    }
    private static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? userId = @event.Userid;
        if (userId == null)
            return HookResult.Continue;

        var jbPlayer = JBPlayerManagement.GetOrCreate(userId);

        // events for jailbreak
        jbPlayer.OnWardenStatusChanged += OnWardenStatusChanged;
        jbPlayer.OnRebelStatusChanges += OnRebelStatusChanged;

        return HookResult.Continue;
    }
    private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? userId = @event.Userid;
        if (userId == null)
            return HookResult.Continue;

        var player = JBPlayerManagement.GetOrCreate(userId);

        player.OnDisconnect();
        JBPlayerManagement.Remove(userId.SteamID);

        return HookResult.Continue;
    }
    private static void OnWardenStatusChanged(JBPlayer player, bool isWarden)
    {

    }
    private static void OnRebelStatusChanged(JBPlayer player, bool isRebel)
    {

    }
    private static void OnTick()
    {
        foreach (var userId in Utilities.GetPlayers())
        {
            var player = JBPlayerManagement.GetOrCreate(userId);

            if (player.HtmlMessages.TryGetValue(userId, out string? message))
            {
                userId.PrintToCenterHtml(message);
            }
            else
            {
                // do nothing
            }
        }
    }
}