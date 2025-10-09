using CounterStrikeSharp.API.Core;

namespace JailbreakApi
{
    /// <summary>
    /// Represents a "Last Request" interaction — a limited prisoner request
    /// where a (typically last remaining) prisoner can request a specific
    /// duel, weapon, or action handled by the module.
    /// </summary>
    public interface ILastRequest
    {
        /// <summary>
        /// Human-friendly name of the last-request type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Brief description explaining the last-request behaviour.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The prisoner controller who initiated the last request.
        /// </summary>
        CCSPlayerController? Prisoner { get; set; }

        /// <summary>
        /// The guardian/controller who is selected to handle the request.
        /// </summary>
        CCSPlayerController? Guardian { get; set; }

        /// <summary>
        /// Selected weapon identifier (internal/class name) for the request.
        /// </summary>
        string SelectedWeaponID { get; set; }

        /// <summary>
        /// Display name of the selected weapon.
        /// </summary>
        string SelectedWeaponName { get; set; }

        /// <summary>
        /// Returns the available weapons for the last request as tuples of
        /// (display name, class name).
        /// </summary>
        public IReadOnlyList<(string DisplayName, string ClassName)> GetAvailableWeapons();

        /// <summary>
        /// Optional selected type/category for the request (if applicable).
        /// </summary>
        string? SelectedType { get; set; }

        /// <summary>
        /// Returns a list of available last-request types (string identifiers).
        /// </summary>
        IReadOnlyList<string> GetAvalibleTypes();

        /// <summary>
        /// If true, the prep timer (countdown before the duel/request) is active.
        /// </summary>
        bool IsPrepTimerActive { get; set; }

        /// <summary>
        /// Start the last-request flow (show menus, start timers, etc.).
        /// </summary>
        void Start();

        /// <summary>
        /// End the last-request flow and resolve the outcome. `winner`/`loser`
        /// can be null if there is no clear result.
        /// </summary>
        void End(CCSPlayerController? winner, CCSPlayerController? loser);
    }
}
