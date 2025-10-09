namespace JailbreakApi
{
    /// <summary>
    /// Roles used by Jailbreak to describe player state.
    /// </summary>
    public enum JBRole
    {
        Warden,
        Prisoner,
        Guardian,
        Rebel,
        Freeday,
        None
    }

    /// <summary>
    /// Public interface representing a Jailbreak player. Implementations expose
    /// basic role/state information and convenience methods to change role or
    /// send messages to the player.
    /// </summary>
    public interface IJBPlayer
    {
        /// <summary>
        /// Player's display name.
        /// </summary>
        string PlayerName { get; }

        /// <summary>
        /// Current role of the player.
        /// </summary>
        JBRole Role { get; }

        /// <summary>
        /// True if the player is currently the Warden.
        /// </summary>
        bool IsWarden { get; }

        /// <summary>
        /// True if the player is currently a Rebel.
        /// </summary>
        bool IsRebel { get; }

        /// <summary>
        /// True if the player currently has Freeday privileges.
        /// </summary>
        bool IsFreeday { get; }

        /// <summary>
        /// True if the underlying controller and pawn are valid.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Make the player a warden (or remove the warden status).
        /// </summary>
        void SetWarden(bool state);

        /// <summary>
        /// Make the player a rebel (or remove rebel status).
        /// </summary>
        void SetRebel(bool state);

        /// <summary>
        /// Give or remove freeday privileges.
        /// </summary>
        void SetFreeday(bool state);

        /// <summary>
        /// Forcefully set the player's role to the provided value.
        /// </summary>
        void SetRole(JBRole role);

        /// <summary>
        /// Send a message to the player. `hud` accepts values like "chat",
        /// "center", "alert" or "html". Duration is used for html messages.
        /// </summary>
        void Print(string hud, string message, int duration = 0);
    }

}
