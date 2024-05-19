using System;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models;

public class CPULevelSettings
{
    public bool EnableAltCPULevel { get; set; }
    public int CPULevel { get; set; }
    public int CPULevelAlt { get; set; }
    public TimeSpan? AltTimeStart { get; set; }
    public TimeSpan? AltTimeEnd { get; set; }

    /// <summary>
    ///     Determines which CPU level to use based on the current time
    /// </summary>
    public int CurrentCPULimit
    {
        get
        {
            var useAlternateLevel = false;

            if (EnableAltCPULevel && AltTimeStart.HasValue && AltTimeEnd.HasValue)
            {
                var now = DateTime.UtcNow.TimeOfDay;

                if (AltTimeStart < AltTimeEnd)
                    useAlternateLevel = AltTimeStart < now && now < AltTimeEnd;
                else
                    useAlternateLevel = AltTimeStart < now || now < AltTimeEnd;
            }

            return useAlternateLevel ? CPULevelAlt : CPULevel;
        }
    }

    public void Save(IConfigService configService)
    {
        // CPU Usage settings
        configService.Set(ConfigSettings.AltCPULimitEnabled, EnableAltCPULevel.ToString());
        configService.Set(ConfigSettings.CPULimit, CPULevel.ToString());
        configService.Set(ConfigSettings.AltCPULimit, CPULevelAlt.ToString());

        string? cpuTimeRangeSettings = null;

        if (EnableAltCPULevel)
            cpuTimeRangeSettings = $"{AltTimeStart.LocalTimeSpanToUTC()}-{AltTimeEnd.LocalTimeSpanToUTC()}";

        configService.Set(ConfigSettings.AltCPULimitTimes, cpuTimeRangeSettings);
    }

    /// <summary>
    ///     Load the settings, or set defaults
    /// </summary>
    /// <param name="configService"></param>
    public void Load(IConfigService configService)
    {
        EnableAltCPULevel = configService.GetBool(ConfigSettings.AltCPULimitEnabled);
        CPULevel = configService.GetInt(ConfigSettings.CPULimit, 25);
        CPULevelAlt = configService.GetInt(ConfigSettings.AltCPULimit, 75);
        var timeRangeStr = configService.Get(ConfigSettings.AltCPULimitTimes, "23:00-04:30");

        if (!string.IsNullOrEmpty(timeRangeStr))
        {
            var settings = timeRangeStr.Split("-");

            if (settings.Length == 2)
            {
                TimeSpan? start = TimeSpan.Parse(settings[0]);
                TimeSpan? end = TimeSpan.Parse(settings[1]);

                AltTimeStart = start.UTCTimeSpanToLocal();
                AltTimeEnd = end.UTCTimeSpanToLocal();
            }
        }
    }

    public override string ToString()
    {
        var result = $"CPULevel={CPULevel}%";

        if (EnableAltCPULevel) result += $", AltLevel={CPULevelAlt}% [{AltTimeStart} - {AltTimeEnd}]";

        return result;
    }
}
