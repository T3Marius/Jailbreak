using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Localization;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace JailbreakApi
{
    public interface IJailbreakApi
    {
        public static readonly PluginCapability<IJailbreakApi> Capability = new("jailbreak:api");

        void RegisterDay(ISpecialDay day);
        ISpecialDay? GetActiveDay();
        IReadOnlyList<ISpecialDay> GetAllDays();
        void EndDay();

        void RegisterRequest(ILastRequest day);
        ILastRequest? GetActiveRequest();
        IReadOnlyList<ILastRequest> GetAllRequests();
        void EndRequest();

        IJBPlayer? GetJBPlayer(CCSPlayerController controller);
        IJBPlayer? GetWarden();

        CSTimer StartTimer(int seconds, Action<int> onTick, Action onFinished);
        void PrintToHtml(CCSPlayerController controller, string message, int duration);

        T GetConfigValue<T>(string key, T defaultValue = default!);
        LocalizedString GetLocalizer(string message, params object[] args);

        void FreezePlayer(CCSPlayerController controller);
        void UnfreezePlayer(CCSPlayerController controller);
    }

}