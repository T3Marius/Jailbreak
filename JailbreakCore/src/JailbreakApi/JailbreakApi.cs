using CounterStrikeSharp.API.Core;
using Jailbreak.Config;
using JailbreakApi;
using Microsoft.Extensions.Localization;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public class JailbreakApi : IJailbreakApi
{
    private readonly ConfigManager _configManager;

    public JailbreakApi(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    public void RegisterDay(ISpecialDay day) => SpecialDayManagement.RegisterDay(day);
    public ISpecialDay? GetActiveDay() => SpecialDayManagement.GetActiveDay();
    public IReadOnlyList<ISpecialDay> GetAllDays() => SpecialDayManagement.GetDays();
    public void EndDay() => SpecialDayManagement.EndDay();

    public void RegisterRequest(ILastRequest request) => LastRequestManagement.RegisterRequest(request);
    public ILastRequest? GetActiveRequest() => LastRequestManagement.GetActiveRequest();
    public IReadOnlyList<ILastRequest> GetAllRequests() => LastRequestManagement.GetRequests();
    public void EndRequest() => LastRequestManagement.EndRequest();

    public IJBPlayer? GetJBPlayer(CCSPlayerController controller)
    {
        return JBPlayerManagement.GetOrCreate(controller);
    }
    public IJBPlayer? GetWarden()
    {
        return JBPlayerManagement.GetWarden();
    }
    public CSTimer StartTimer(int seconds, Action<int> onTick, Action onFinished)
    {
        return Library.StartTimer(seconds, onTick, onFinished);
    }
    public void PrintToHtml(CCSPlayerController controller, string message, int duration)
    {
        controller.PrintToHtml(message, duration);
    }
    public T GetConfigValue<T>(string key, T defaultValue = default!)
    {
        return _configManager.GetConfigValue(key, defaultValue);
    }
    public LocalizedString GetLocalizer(string message, params object[] args)
    {
        return Instance.Localizer[message, args];
    }
    public void FreezePlayer(CCSPlayerController controller)
    {
        controller.Freeze();
    }
    public void UnfreezePlayer(CCSPlayerController controller)
    {
        controller.Unfreeze();
    }
}
