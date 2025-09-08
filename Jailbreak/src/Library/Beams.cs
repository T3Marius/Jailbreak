using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using System.Drawing;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class Beams
{
    private static readonly Dictionary<CBeam, Timer> PersistentBeams = new();
    private static List<CBeam> CurrentPingBeams = new List<CBeam>();

    private static Vector AngleOnCircle(float angle, float radius, Vector mid)
    {
        return new Vector(
            (float)(mid.X + (radius * Math.Cos(angle))),
            (float)(mid.Y + (radius * Math.Sin(angle))),
            mid.Z + 6.0f
        );
    }

    public static void TeleportLaser(CBeam? laser, Vector start, Vector end)
    {
        if (laser == null || !laser.IsValid) return;

        laser.Teleport(start, RotationZero, VectorZero);
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;

        Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
    }
    public static void DrawPingBeacon(Vector pos)
    {
        if (pos == null) return;

        // Remove old beacon if exists
        if (CurrentPingBeams.Count > 0)
        {
            foreach (var beam in CurrentPingBeams)
            {
                if (beam != null && beam.IsValid)
                    beam.Remove();
            }
            CurrentPingBeams.Clear();
        }

        Vector mid = new Vector(pos.X, pos.Y, pos.Z);

        int lines = 32;
        float step = (float)(2.0f * Math.PI) / lines;
        float radius = 60.0f;

        float angle_old = 0.0f;
        float angle_cur = step;

        for (int i = 0; i < lines; i++)
        {
            Vector start = AngleOnCircle(angle_old, radius, mid);
            Vector end = AngleOnCircle(angle_cur, radius, mid);

            var result = DrawLaserBetween(
                start,
                end,
                Color.Blue,
                60f,
                4.0f
            );

            if (result.Item2 != null)
            {
                CurrentPingBeams.Add(result.Item2);
            }

            angle_old = angle_cur;
            angle_cur += step;
        }

    }

    public static void ClearPingBeacon()
    {
        if (CurrentPingBeams.Count > 0)
        {
            foreach (var beam in CurrentPingBeams)
            {
                if (beam != null && beam.IsValid)
                    beam.Remove();
            }
            CurrentPingBeams.Clear();
        }
    }
    public static void DrawBeaconOnPlayer(CCSPlayerController? player)
    {
        if (player?.Pawn?.Value == null || player.PlayerPawn?.Value == null) return;

        var absOrigin = player.PlayerPawn?.Value?.AbsOrigin;
        if (absOrigin == null) return;

        Vector mid = new Vector(
            absOrigin.X,
            absOrigin.Y,
            absOrigin.Z
        );

        int lines = 20;
        int[] ent = new int[lines];
        CBeam[] beam_ent = new CBeam[lines];

        float step = (float)(2.0f * Math.PI) / lines;
        float radius = 20.0f;

        float angle_old = 0.0f;
        float angle_cur = step;

        for (int i = 0; i < lines; i++)
        {
            Vector start = AngleOnCircle(angle_old, radius, mid);
            Vector end = AngleOnCircle(angle_cur, radius, mid);

            var result = DrawLaserBetween(
                start,
                end,
                player.TeamNum == 2 ? Color.Red : Color.Blue,
                1.0f,
                2.0f
            );

            if (result.Item2 == null) return;

            ent[i] = result.Item1;
            beam_ent[i] = result.Item2;
            angle_old = angle_cur;
            angle_cur += step;
        }

        Instance.AddTimer(0.1f, () =>
        {
            for (int i = 0; i < lines; i++)
            {
                Vector start = AngleOnCircle(angle_old, radius, mid);
                Vector end = AngleOnCircle(angle_cur, radius, mid);

                TeleportLaser(beam_ent[i], start, end);

                angle_old = angle_cur;
                angle_cur += step;
            }
            radius += 10;
        }, TimerFlags.REPEAT);

        PlaySoundOnPlayer(player, "sounds/tools/sfm/beep.vsnd_c");
    }

    private static void PlaySoundOnPlayer(CCSPlayerController? player, string sound)
    {
        if (player == null || !player.IsValid) return;
        player.ExecuteClientCommand($"play {sound}");
    }

    private static readonly Vector VectorZero = new Vector(0, 0, 0);
    private static readonly QAngle RotationZero = new QAngle(0, 0, 0);

    public static (int, CBeam?) DrawLaserBetween(
         Vector startPos,
         Vector endPos,
         Color color,
         float life,
         float width,
         bool onTick = false,
         CCSPlayerController? player1 = null,
         CCSPlayerController? player2 = null)
    {
        if (startPos == null || endPos == null)
            return (-1, null);

        CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null)
            return (-1, null);

        beam.Render = color;
        beam.Width = width;
        beam.Teleport(startPos, RotationZero, VectorZero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        if (onTick && player1 != null && player2 != null)
        {
            // Use OnTick style timer to constantly track players
            Timer tickTimer = Instance.AddTimer(0.05f, () =>
            {
                if (!beam.IsValid || !player1.IsValid || !player2.IsValid)
                {
                    StopPersistentBeam(beam);
                    return;
                }

                Vector? start = player1.PlayerPawn.Value?.AbsOrigin;
                Vector? end = player2.PlayerPawn.Value?.AbsOrigin;

                if (start == null || end == null)
                    return;


                TeleportLaser(beam, start, end);

            }, TimerFlags.REPEAT);

            PersistentBeams[beam] = tickTimer;
        }
        else
        {
            // Normal life timer
            Instance.AddTimer(life, () =>
            {
                if (beam?.IsValid == true)
                    beam.Remove();
            });
        }

        return ((int)beam.Index, beam);
    }

    public static void StopPersistentBeam(CBeam beam)
    {
        if (PersistentBeams.TryGetValue(beam, out Timer? timer))
        {
            timer.Kill();
            PersistentBeams.Remove(beam);
        }

        if (beam != null && beam.IsValid)
            beam.Remove();
    }

    public static void StopAllPersistentBeams()
    {
        foreach (var kv in PersistentBeams)
        {
            kv.Value.Kill();
            if (kv.Key.IsValid)
                kv.Key.Remove();
        }
        PersistentBeams.Clear();
    }
}