using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Timers;
using JailbreakApi;
using CounterStrikeSharp.API.Core;
using System.Drawing;
using CounterStrikeSharp.API;

namespace Jailbreak;

public static class LastRequestManagement
{
    private static readonly List<ILastRequest> Requests = new();
    private static ILastRequest? ActiveRequest;
    private static CSTimer? PrepTimer;

    public static IReadOnlyList<ILastRequest> GetRequests() => Requests;
    public static ILastRequest? GetActiveRequest() => ActiveRequest;

    private static bool IsPrepTimeActive = false;

    public static void RegisterRequest(ILastRequest request)
    {
        Requests.Add(request);
    }

    public static void SelectRequest(ILastRequest request, CCSPlayerController prisoner, CCSPlayerController guardian, string weaponName, string weaponId)
    {
        if (ActiveRequest != null)
        {
            prisoner.PrintToChat(Instance.Localizer["prefix"] + Instance.Localizer["last_request_aleardy_active"]);
            return;
        }

        ActiveRequest = request;
        ActiveRequest.Prisoner = prisoner;
        ActiveRequest.Guardian = guardian;
        ActiveRequest.SelectedWeaponName = weaponName;
        ActiveRequest.SelectedWeaponID = weaponId;

        int prepDelay = 8;

        PrepTimer = Instance.AddTimer(1.0f, () =>
        {
            prepDelay--;

            if (prepDelay <= 0)
            {
                StartRequest();
                request.IsPrepTimerActive = false;
                IsPrepTimeActive = false;

                Beams.StopAllPersistentBeams();
            }
            else
            {
                IsPrepTimeActive = true;
                request.IsPrepTimerActive = true;
                Beams.DrawBeaconOnPlayer(prisoner);
                Beams.DrawBeaconOnPlayer(guardian);

                prisoner.PrintToHtml(Instance.Localizer["last_request_starting_html", request.Name, prepDelay, guardian.PlayerName], 1);
                guardian.PrintToHtml(Instance.Localizer["last_request_starting_html", request.Name, prepDelay, prisoner.PlayerName], 1);

                Beams.DrawLaserBetween(
                    prisoner.PlayerPawn.Value?.AbsOrigin!,
                    guardian.PlayerPawn.Value?.AbsOrigin!,
                    Color.Red,
                    0f,
                    2f,
                    onTick: true,
                    player1: prisoner,
                    player2: guardian);

            }

        }, TimerFlags.REPEAT);
    }
    private static void StartRequest()
    {
        JBPlayer? activeWarden = JBPlayerManagement.GetWarden();
        if (activeWarden != null)
            activeWarden.SetWarden(false); // we remove warrden when a lr starts

        PrepTimer?.Kill();
        PrepTimer = null;


        ActiveRequest?.Start();
        if (ActiveRequest != null)
        {
            Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["last_request_started", ActiveRequest.Name, ActiveRequest.SelectedType!]);
        }
    }
    public static void EndRequest(CCSPlayerController? winner = null, CCSPlayerController? loser = null)
    {
        if (ActiveRequest != null)
        {
            ActiveRequest.End(winner, loser);
            ActiveRequest.Prisoner = null;
            ActiveRequest.Guardian = null;
            ActiveRequest = null;
        }

        PrepTimer?.Kill();
        PrepTimer = null;
    }
    public static void OnPlayerDeath(CCSPlayerController player)
    {
        if (ActiveRequest == null)
            return;

        if (player == ActiveRequest.Prisoner)
        {
            EndRequest(ActiveRequest.Guardian, ActiveRequest.Prisoner);
        }
        else if (player == ActiveRequest.Guardian)
        {
            EndRequest(ActiveRequest.Prisoner, ActiveRequest.Guardian);
        }
    }
    public static HookResult OnTakeDamage(CTakeDamageInfo info, CCSPlayerController attacker, CCSPlayerController victim)
    {
        if (ActiveRequest == null)
            return HookResult.Continue;

        if ((attacker != ActiveRequest.Prisoner && attacker != ActiveRequest.Guardian) ||
            (victim != ActiveRequest.Prisoner && victim != ActiveRequest.Guardian))
        {
            info.Damage = 0;
            return HookResult.Handled;
        }

        if (IsPrepTimeActive)
        {
            info.Damage = 0;
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

}