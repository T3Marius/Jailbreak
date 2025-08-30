using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;

namespace Jailbreak;

public class JBPlayer : IDisposable
{
    // +--------------------+
    // | CORE PROPERTIES    |
    // +--------------------+
    public readonly CCSPlayerController? Controller;
    public readonly ulong SteamID;
    public readonly string Name;
    private readonly ILogger _logger;
    private readonly BasePlugin _plugin;

    // +--------------------+
    // | Jailbreak Roles    |
    // +--------------------+
    public JBRole Role { get; private set; } = JBRole.None;
    public bool IsWarden { get; private set; } = false;
    public bool IsPrisoner => Role == JBRole.Prisoner;
    public bool IsGuard => Role == JBRole.Guard;

    // +--------------------+
    // | Jailbreak Status   |
    // +--------------------+
    public bool IsMuted { get; private set; } = false;
    public bool IsRebel { get; private set; } = false;
    public bool IsInLastRequest { get; private set; } = false;
    public bool IsFreeday { get; private set; } = false;

    // +--------------------+
    // | Jailbreak Events   |
    // +--------------------+
    public event Action<JBPlayer>? OnRoleChanged;
    public event Action<JBPlayer, bool>? OnWardenStatusChanged;
    public event Action<JBPlayer, bool>? OnRebelStatusChanges;

    // +--------------------+
    // | Jailbreak Timers   |
    // +--------------------+
    public CSTimer? RoundStartMute = null;

    // +--------------------+
    // | Constructor        |
    // +--------------------+
    public JBPlayer(CCSPlayerController controller, ILogger logger, BasePlugin plugin)
    {
        Controller = controller;
        SteamID = controller.SteamID;
        Name = controller.PlayerName ?? "Unknown";
        _logger = logger;
        _plugin = plugin;

        UpdateRole();
    }

    // +--------------------+
    // | Validation         |
    // +--------------------+
    public bool IsValid => Controller?.IsValid == true && Controller.PlayerPawn?.IsValid == true;
    public bool IsAlive => Controller?.PlayerPawn.Value?.Health > 0;
    public bool IsConnected => Controller?.Connected == PlayerConnectedState.PlayerConnected;

    // +--------------------+
    // | HTML Management    |
    // +--------------------+
    public readonly Dictionary<CCSPlayerController, string> HtmlMessages = new();

    // +--------------------+
    // | Role Management    |
    // +--------------------+
    public void UpdateRole()
    {
        if (!IsValid) return;

        var oldRole = Role;
        Role = Controller!.Team switch
        {
            CsTeam.CounterTerrorist => JBRole.Guard,
            CsTeam.Terrorist => JBRole.Prisoner,
            _ => JBRole.None
        };

        if (oldRole != Role)
        {
            if (Role != JBRole.Guard)
            {
                SetWarden(false);
            }

            if (Role != JBRole.Prisoner)
            {
                SetRebel(false);
                SetLastRequest(false);
            }

            OnRoleChanged?.Invoke(this);
        }
    }
    public bool CanBecomeWarden()
    {
        return IsValid && IsAlive && IsConnected && IsGuard && !IsWarden;
    }
    public void SetWarden(bool isWarden)
    {
        if (!IsGuard && isWarden) return;

        var oldStatus = IsWarden;
        IsWarden = isWarden;

        if (isWarden)
        {
            _logger.LogInformation($"Set warden function called on {Name}");
        }

        if (oldStatus != IsWarden)
        {
            OnWardenStatusChanged?.Invoke(this, IsWarden);
        }
    }
    public void SetRebel(bool isRebel, string reason = "")
    {
        if (!IsPrisoner) return;

        var oldStatus = IsRebel;
        IsRebel = isRebel;

        if (isRebel)
        {
            _logger.LogInformation($"Set rebel function called on {Name}");
        }

        if (oldStatus != isRebel)
        {
            OnRebelStatusChanges?.Invoke(this, isRebel);
        }
    }
    public void SetLastRequest(bool inLR)
    {
        if (!IsPrisoner) return;
        IsInLastRequest = inLR;
    }
    public void SetFreeday(bool freeDay)
    {
        if (!IsPrisoner) return;
        IsFreeday = freeDay;
    }
    public void SetMute(bool mute)
    {
        IsMuted = mute;

        if (mute)
        {
            if (!Controller!.VoiceFlags.HasFlag(VoiceFlags.Muted))
                Controller!.VoiceFlags = VoiceFlags.Muted;
        }
        else
        {
            if (Controller!.VoiceFlags.HasFlag(VoiceFlags.Muted))
                Controller!.VoiceFlags = VoiceFlags.Normal;
        }
    }

    // +--------------------+
    // | Messaging          |
    // +--------------------+
    public void PrinToChat(string message)
    {
        if (!IsValid) return;
        Controller!.PrintToChat(message);
    }
    public void PrintToCenter(string message)
    {
        if (!IsValid) return;
        Controller!.PrintToCenter(message);
    }
    public void PrintToAlert(string message)
    {
        if (!IsValid) return;
        Controller!.PrintToCenterAlert(message);
    }
    public void PrintToHtml(string message, float duration)
    {
        if (!IsValid) return;

        HtmlMessages.Add(Controller!, message);
        _plugin.AddTimer(duration, () =>
        {
            if (HtmlMessages.ContainsKey(Controller!))
                HtmlMessages.Remove(Controller!);
        });
    }

    // +--------------------+
    // | Round Management   |
    // +--------------------+
    public void OnRoundStart()
    {
        IsRebel = false;
        IsInLastRequest = false;
        IsFreeday = false;
        UpdateRole();

        if (IsPrisoner && IsValid)
        {
            if (Instance.Config.Prisoner.RoundStartMuteDuration > 0)
            {
                bool skip = Instance.Config.Prisoner.SkipMuteFlags.Count > 0
                    && Instance.Config.Prisoner.SkipMuteFlags.Any(flag => AdminManager.PlayerHasPermissions(Controller, flag));

                if (!IsMuted && !skip)
                {
                    SetMute(true);

                    int muteDuration = Instance.Config.Prisoner.RoundStartMuteDuration;

                    RoundStartMute = _plugin.AddTimer(1.0f, () =>
                    {
                        muteDuration--;

                        if (muteDuration <= 0)
                        {
                            if (IsMuted)
                                SetMute(false);


                            RoundStartMute?.Kill();
                            RoundStartMute = null;
                        }

                        if (IsValid && IsPrisoner)
                        {
                            PrintToHtml(Instance.Localizer["round_start_mute", muteDuration], 1.0f);
                        }
                    }, TimerFlags.REPEAT);

                    PrintToHtml(Instance.Localizer["round_start_mute", muteDuration], 1.0f);
                }
            }
        }
    }
    public void OnRoundEnd()
    {
        SetWarden(false);

        if (IsPrisoner && IsValid)
        {
            if (Instance.Config.Prisoner.UnmutePrisonerOnRoundEnd)
            {
                if (IsMuted)
                    SetMute(false);
            }
        }
    }
    public void OnDisconnect()
    {
        if (HtmlMessages.ContainsKey(Controller!))
            HtmlMessages.Remove(Controller!);
    }

    // +--------------------+
    // | Dispose            |
    // +--------------------+
    public void Dispose()
    {

    }


}
public enum JBRole
{
    Prisoner,
    Guard,
    None
}