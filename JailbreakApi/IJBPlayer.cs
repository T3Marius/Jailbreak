namespace JailbreakApi
{
    public enum JBRole
    {
        Warden,
        Prisoner,
        Guardian,
        Rebel,
        Freeday,
        None
    }

    public interface IJBPlayer
    {
        string PlayerName { get; }
        JBRole Role { get; }
        bool IsWarden { get; }
        bool IsRebel { get; }
        bool IsFreeday { get; }
        bool IsValid { get; }

        void SetWarden(bool state);
        void SetRebel(bool state);
        void SetFreeday(bool state);
        void SetRole(JBRole role);

        void Print(string hud, string message, int duration = 0);
    }

}
