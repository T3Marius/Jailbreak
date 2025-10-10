using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API.Core.Translations;
using JailbreakApi;

namespace Jailbreak;

public class JBPlayer : IDisposable, IJBPlayer
{
    public CCSPlayerController Controller { get; private set; }
    public CCSPlayerPawn PlayerPawn { get; private set; }
    public string PlayerName => Controller.PlayerName ?? "";
    public JBRole Role { get; private set; } = JBRole.None;
    public bool IsWarden => Role == JBRole.Warden;
    public bool IsRebel => Role == JBRole.Rebel;
    public bool IsFreeday => Role == JBRole.Freeday;
    public bool IsValid => Controller.IsValid && Controller.PlayerPawn.Value?.IsValid == true;
    public string WardenModel => Instance.Config.Models.WardenModel;
    public string GuardianModel => Instance.Config.Models.GuardianModel;
    public string PrisonerModel => Instance.Config.Models.PrisonerModel;
    private Color DefaultColor => Color.FromArgb(255, 255, 255, 255);
    public JBPlayer(CCSPlayerController controller, CCSPlayerPawn playerPawn)
    {
        Controller = controller;
        PlayerPawn = playerPawn;
    }
    public void SetWarden(bool state)
    {
        if (state && !IsWarden)
        {
            SetRole(JBRole.Warden);
            ConfigureWarden();

            if (!Controller.IsBot && Instance.Config.Warden.ShowMenuOnSet)
                WardenMenu.Display(this);
        }
        else
        {

            ClearWarden();

            if (Controller.Team == CsTeam.CounterTerrorist)
            {
                SetRole(JBRole.Guardian);
            }
            else if (Controller.Team == CsTeam.Terrorist)
            {
                SetRole(JBRole.Prisoner);
            }
            else
            {
                SetRole(JBRole.None);
            }
        }
    }
    public void SetRebel(bool state)
    {
        if (state && Role == JBRole.Prisoner)
        {
            SetColor(Color.Red);
            SetRole(JBRole.Rebel);
        }
        else
        {
            SetColor(DefaultColor);

            if (Controller.Team == CsTeam.Terrorist)
                SetRole(JBRole.Prisoner);
            else if (Controller.Team == CsTeam.CounterTerrorist)
                SetRole(JBRole.Guardian);
            else
                SetRole(JBRole.None);
        }
    }
    public void SetFreeday(bool state)
    {
        if (state && Role == JBRole.Prisoner)
        {
            SetColor(Color.Green);
            SetRole(JBRole.Freeday);
        }
        else
        {
            SetColor(DefaultColor);

            if (Controller.Team == CsTeam.Terrorist)
                SetRole(JBRole.Prisoner);
            else if (Controller.Team == CsTeam.CounterTerrorist)
                SetRole(JBRole.Guardian);
            else
                SetRole(JBRole.None);
        }
    }
    public void SetRole(JBRole role)
    {
        Role = role;
    }
    public void OnPlayerSpawn()
    {
        Server.NextFrame(() =>
        {
            if (Role == JBRole.Prisoner)
            {
                if (!string.IsNullOrEmpty(PrisonerModel))
                    PlayerPawn.SetModel(PrisonerModel);
            }
            else if (Role == JBRole.Guardian)
            {
                // it isn't possible for an active Warden to just get spawned, so we can safetly only set Guardian model.
                if (!string.IsNullOrEmpty(GuardianModel))
                    PlayerPawn.SetModel(GuardianModel);
            }
        });
    }
    public void OnChangeTeam(CsTeam team)
    {
        if (team == CsTeam.Terrorist)
        {
            SetRole(JBRole.Prisoner);
            if (IsWarden)
                SetWarden(false);
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            if (Role != JBRole.Warden)
            {
                SetRole(JBRole.Guardian);
            }
        }
        else
        {
            SetRole(JBRole.None);
            if (IsWarden)
                SetWarden(false);
        }
    }
    public void Print(string hud, string message, int duration = 0)
    {
        switch (hud)
        {
            case "chat":
                Controller.PrintToChat(message);
                break;
            case "center":
                Controller.PrintToCenter(message);
                break;
            case "alert":
                Controller.PrintToCenterAlert(message);
                break;
            case "html":
                Controller.PrintToHtml(message, duration);
                break;
        }
    }
    private void ConfigureWarden()
    {
        // we call everyting on NextFrame for safety
        Server.NextFrame(() =>
        {
            SetColor(Color.FromName(Instance.Config.Warden.WardenColor));

            if (!string.IsNullOrEmpty(WardenModel))
                PlayerPawn.SetModel(WardenModel);
        });
    }
    private void ClearWarden()
    {
        Server.NextFrame(() =>
        {
            SetColor(DefaultColor);

            if (Controller.Team == CsTeam.CounterTerrorist)
            {
                if (!string.IsNullOrEmpty(GuardianModel))
                    PlayerPawn.SetModel(GuardianModel);
            }
            else if (Controller.Team == CsTeam.Terrorist)
            {
                if (!string.IsNullOrEmpty(PrisonerModel))
                    PlayerPawn.SetModel(PrisonerModel);
            }
        });
    }
    public void SetColor(Color color)
    {
        Server.NextFrame(() =>
        {
            PlayerPawn.RenderMode = RenderMode_t.kRenderTransColor;
            PlayerPawn.Render = color;
            Utilities.SetStateChanged(PlayerPawn, "CBaseModelEntity", "m_clrRender");
        });
    }
    public void Dispose()
    {
        SetRole(JBRole.None);
    }
}