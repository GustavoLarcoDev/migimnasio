namespace Gimnasio.Helpers;

public static class DateHelper
{
    public static DateTime StartOfWeek(DateTime date)
        => date.Date.AddDays(-(int)date.DayOfWeek);

    public static DateTime StartOfMonth(DateTime date)
        => new DateTime(date.Year, date.Month, 1);

    public static DateTime GetDesde(string periodo)
    {
        var ahora = TimeHelper.Now;
        return periodo switch
        {
            "dia" => ahora.Date,
            "semana" => StartOfWeek(ahora),
            _ => StartOfMonth(ahora)
        };
    }
}
