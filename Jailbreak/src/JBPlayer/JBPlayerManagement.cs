using CounterStrikeSharp.API.Core;

namespace Jailbreak;

public static class JBPlayerManagement
{
    private static Dictionary<int, JBPlayer> JBPlayers = new();
    public static JBPlayer GetOrCreate(CCSPlayerController controller)
    {
        if (controller == null || controller.PlayerPawn.Value == null)
            throw new ArgumentException("Invalid player controller");

        if (!JBPlayers.TryGetValue(controller.Slot, out JBPlayer? jbPlayer))
        {
            jbPlayer = new JBPlayer(controller, controller.PlayerPawn.Value);
            JBPlayers[controller.Slot] = jbPlayer;

            jbPlayer.OnPlayerRoleChanged += Events.OnPlayerRoleChanged;
        }

        return jbPlayer;
    }
    public static JBPlayer? GetWarden()
    {
        return JBPlayers.Values.FirstOrDefault(p => p.IsWarden && p.IsValid);
    }
    public static List<JBPlayer> GetAllRebels()
    {
        return JBPlayers.Values.Where(p => p.IsRebel && p.IsValid).ToList();
    }
    public static List<JBPlayer> GetAllFreedays()
    {
        return JBPlayers.Values.Where(p => p.IsFreeday && p.IsValid).ToList();
    }
    public static List<JBPlayer> GetAllPrisoners(bool excludeRebels = true, bool exlcudeFreedays = true)
    {
        return JBPlayers.Values.Where(p => p.IsValid && p.Role == JBRole.Prisoner && excludeRebels ? !p.IsRebel : p.IsRebel && exlcudeFreedays ? !p.IsFreeday : p.IsFreeday).ToList();
    }
    public static List<JBPlayer> GetAllGuardians()
    {
        return JBPlayers.Values.Where(p => p.IsValid && p.Role == JBRole.Guardian).ToList();
    }
    public static List<JBPlayer> GetAllPlayers()
    {
        return JBPlayers.Values.Where(p => p.IsValid).ToList();
    }
    public static void Remove(CCSPlayerController controller)
    {
        if (JBPlayers.TryGetValue(controller.Slot, out JBPlayer? jbPlayer))
        {
            jbPlayer.Dispose();
            JBPlayers.Remove(controller.Slot);
        }
    }
}