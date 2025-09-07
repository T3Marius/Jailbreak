using JailbreakApi;

namespace Jailbreak;

public class JailbreakApi : IJailbreakApi
{
    public void RegisterDay(ISpecialDay day) => SpecialDayManagement.RegisterDay(day);
    public ISpecialDay? GetActiveDay() => SpecialDayManagement.GetActiveDay();
    public IReadOnlyList<ISpecialDay> GetAllDays() => SpecialDayManagement.GetDays();
    public void EndDay() => SpecialDayManagement.EndDay();

    public void RegisterRequest(ILastRequest request) => LastRequestManagement.RegisterRequest(request);
    public ILastRequest? GetActiveRequest() => LastRequestManagement.GetActiveRequest();
    public IReadOnlyList<ILastRequest> GetAllRequests() => LastRequestManagement.GetRequests();
    public void EndRequest() => LastRequestManagement.EndRequest();


}
