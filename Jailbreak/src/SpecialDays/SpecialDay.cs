using JailbreakApi;

namespace Jailbreak;

public class SpecialDay : IJailbreakApi
{
    public void RegisterDay(ISpecialDay day) => SpecialDayManagement.RegisterDay(day);
    public ISpecialDay? GetActiveDay() => SpecialDayManagement.GetActiveDay();
    public IReadOnlyList<ISpecialDay> GetAllDays() => SpecialDayManagement.GetDays();
    public void EndDay() => SpecialDayManagement.EndDay();
}
