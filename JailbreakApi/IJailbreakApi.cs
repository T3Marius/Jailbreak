using System.Numerics;
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

        /// <summary>
        /// Register a special day
        /// </summary>
        /// <param name="day"></param>
        void RegisterDay(ISpecialDay day);

        /// <summary>
        /// Get the active special day.
        /// </summary>
        /// <returns></returns>
        ISpecialDay? GetActiveDay();

        /// <summary>
        /// Get all special days avaliable.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<ISpecialDay> GetAllDays();

        /// <summary>
        /// Force end an active day.
        /// </summary>
        void EndDay();

        /// <summary>
        /// Register a last request
        /// </summary>
        /// <param name="request"></param>
        void RegisterRequest(ILastRequest request);

        /// <summary>
        /// Get the active last request
        /// </summary>
        /// <returns></returns>
        ILastRequest? GetActiveRequest();

        /// <summary>
        /// Get all last requests avaliable
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<ILastRequest> GetAllRequests();

        /// <summary>
        /// Force end a last request.
        /// </summary>
        void EndRequest();

        /// <summary>
        /// Get the JBPlayer class from a controller.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        IJBPlayer? GetJBPlayer(CCSPlayerController controller);

        /// <summary>
        /// Get active Warden.
        /// </summary>
        /// <returns></returns>
        IJBPlayer? GetWarden();

        /// <summary>
        /// Starts a repeating timer for x seconds.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="onTick"></param>
        /// <param name="onFinished"></param>
        /// <returns></returns>
        CSTimer StartTimer(int seconds, Action<int> onTick, Action onFinished);

        /// <summary>
        /// Print to html
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        void PrintToHtml(CCSPlayerController controller, string message, int duration);

        /// <summary>
        /// Get a value from jailbreak core config.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T GetConfigValue<T>(string key, T defaultValue = default!);

        /// <summary>
        /// Get localizer from jailbreak core lang folder.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        LocalizedString GetLocalizer(string message, params object[] args);

        void FreezePlayer(CCSPlayerController controller);
        void UnfreezePlayer(CCSPlayerController controller);

        void SetGravity(CCSPlayerController controller, float gravity);
        void SetSpeed(CCSPlayerController controller, float speed);

        void SetHealth(CCSPlayerController controller, int health);

        void SetAmmo(CCSPlayerController controller, int ammo);
        void SetReserve(CCSPlayerController controller, int reserve);

        void PrintToCenterAll(string message);
        void PrintToAlertAll(string message);
        void PrintToChatAll(string message);

        /// <summary>
        /// Get player active weapon.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        CBasePlayerWeapon? GetActiveWeapon(CCSPlayerController controller);
    }

}