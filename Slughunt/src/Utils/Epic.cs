using System;

namespace Slughunt.Utils;

public static class Epic {
    public static string FormatTime(TimeSpan time, string neg = "-", string pos = "") {
        int seconds = Math.Abs((int)Math.Floor(time.TotalSeconds));

        int minutes = seconds / 60;
        seconds %= 60;

        int hours = minutes / 60;
        minutes %= 60;

        return hours == 0 ?
            $"{(time.Ticks < 0 ? neg : pos)}{minutes}:{seconds:D2}" :
            $"{(time.Ticks < 0 ? neg : pos)}{hours}:{minutes:D2}:{seconds:D2}";
    }
}
