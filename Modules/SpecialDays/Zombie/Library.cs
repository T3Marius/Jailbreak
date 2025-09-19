using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static SpecialDays.Zombie;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Jailbreak;

public static class Library
{
    public static CSTimer StartTimer(int seconds, Action<int> onTick, Action onFinished)
    {
        int remaining = seconds;

        CSTimer? timer = null;

        timer = Instance.AddTimer(1.0f, () =>
        {
            remaining--;

            if (remaining > 0)
                onTick?.Invoke(remaining);
            else
            {
                onFinished?.Invoke();
                timer?.Kill();
            }
        }, TimerFlags.REPEAT);

        return timer;
    }
    public static void PrintToCenterAll(string message)
    {
        VirtualFunctions.ClientPrintAll(HudDestination.Center, message, 0, 0, 0, 0);
    }
    public static void PrintToAlertAll(string message)
    {
        VirtualFunctions.ClientPrintAll(HudDestination.Alert, message, 0, 0, 0, 0);
    }
    public static void SetGravity(this CCSPlayerController player, float value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.ActualGravityScale = value;
        //Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flActualGravityScale");
    }
    public static void SetSpeed(this CCSPlayerController player, float value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.VelocityModifier = value;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }
    public static void SetHealth(this CCSPlayerController player, int value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.MaxHealth = value;
        pawn.Health = value;

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
    public static void SetAmmo(this CCSPlayerController player, int ammo)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        CBasePlayerWeapon? weapon = pawn.GetActiveWeapon();
        if (weapon == null)
            return;

        weapon.Clip1 = ammo;
    }
    public static void SetReserve(this CCSPlayerController player, int reserve)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        CBasePlayerWeapon? weapon = pawn.GetActiveWeapon();
        if (weapon == null)
            return;

        weapon.ReserveAmmo[0] = reserve;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }
    public static CBasePlayerWeapon? GetActiveWeapon(this CCSPlayerPawn pawn)
    {
        CBasePlayerWeapon? weapon = pawn.WeaponServices?.ActiveWeapon.Value;
        return weapon;
    }
    public static Vector GetEyePosition(this CCSPlayerPawn pawn)
    {
        if (pawn == null)
            return new Vector(0, 0, 0);

        var origin = pawn.AbsOrigin ?? new Vector(0, 0, 0);
        return new Vector(origin.X, origin.Y, origin.Z + 64.0f);
    }
    public static Vector GetForwardVector(QAngle angles)
    {
        double pitch = angles.X * Math.PI / 180.0;
        double yaw = angles.Y * Math.PI / 180.0;

        float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
        float y = (float)(Math.Cos(pitch) * Math.Sin(yaw));
        float z = (float)(-Math.Sin(pitch));

        return new Vector(x, y, z);
    }
}