using System;
using System.Linq;
using MetadataExtractor;

namespace Damselfly.Core.Utils;

/// <summary>
///     Utilities for extracting data from Metadata
/// </summary>
public static class ExifUtils
{
    public static string SafeExifGetString(this Directory dir, int tagType)
    {
        try
        {
            return dir?.GetString(tagType);
        }
        catch
        {
            Logging.LogVerbose("Error reading string metadata!");
            return string.Empty;
        }
    }

    public static string SafeExifGetString(this Directory dir, string tagName)
    {
        try
        {
            var tag = dir?.Tags.FirstOrDefault(x => x.Name == tagName);

            return tag?.Description;
        }
        catch
        {
            Logging.LogVerbose("Error reading string metadata!");
            return string.Empty;
        }
    }

    public static int SafeGetExifInt(this Directory dir, int tagType)
    {
        var retVal = 0;
        try
        {
            var val = dir?.GetInt32(tagType);
            if (val.HasValue)
                retVal = val.Value;
        }
        catch
        {
            Logging.LogVerbose("Error reading int metadata!");
        }

        return retVal;
    }

    public static DateTime SafeGetExifDateTime(this Directory dir, int tagType)
    {
        var retVal = DateTime.MinValue;
        try
        {
            var val = dir?.GetDateTime(tagType);
            if (val.HasValue)
                retVal = val.Value;
        }
        catch (Exception ex)
        {
            Logging.LogVerbose($"Error reading date/time metadata! {ex}");
        }

        return retVal;
    }
}
