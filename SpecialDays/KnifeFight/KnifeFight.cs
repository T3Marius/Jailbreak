using CounterStrikeSharp.API.Core;
using JailbreakApi;

namespace SpecialDays;

public class Knife_Fight : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[SD] Knife Fight";
    public override string ModuleVersion => "1.0.0";

    public IJailbreakApi Api = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("Jailbreak Api not found!");
    }
}
public class KnifeFight : ILastRequest
{

}