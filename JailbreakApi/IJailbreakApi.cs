
using CounterStrikeSharp.API.Core.Capabilities;

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
    }

}