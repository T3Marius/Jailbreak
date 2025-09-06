
namespace JailbreakApi
{
    public interface ISpecialDay
    {
        string Name { get; }
        string Description { get; }

        void Start();
        void End();
    }
}
