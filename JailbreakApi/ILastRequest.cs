using CounterStrikeSharp.API.Core;

namespace JailbreakApi
{
    public interface ILastRequest
    {
        string Name { get; }
        string Description { get; }

        CCSPlayerController? Prisoner { get; set; }
        CCSPlayerController? Guardian { get; set; }

        string? SelectedWeapon { get; set; }
        IReadOnlyList<string> GetAvalibleWeapons();

        string? SelectedType { get; set; }
        IReadOnlyList<string> GetAvalibleTypes();
        


        bool IsPrepTimerActive { get; set; }

        void Start();
        void End(CCSPlayerController? winner, CCSPlayerController? loser);
    }
}
