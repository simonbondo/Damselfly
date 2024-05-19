using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Hosting;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     Service to generate download files for exporting images from the system. Zip files
///     are built from the basket or other selection sets, and then created on disk in the
///     wwwroot folder. We can then pass them back to the browser as a URL to trigger a
///     download. The service can also perform transforms on the images before they're
///     zipped for download, such as resizing, rotations, watermarking etc.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IDictionary<string, ThemeConfig> _themeConfigs =
        new Dictionary<string, ThemeConfig>(StringComparer.OrdinalIgnoreCase);

    public ThemeConfig DarkTheme = new()
    {
        Name = "Light Theme",

        Primary = "#444444",
        PrimaryDarken = "#222222",
        PrimaryLighten = "#777777",
        Black = "#A7A7A7",
        Background = "#f2f2f2",
        BackgroundGrey = "#cfcfcf",
        Surface = "#f7f7f7",
        DrawerBackground = "#9f9f9f",
        DrawerText = "rgba(255,255,255, 0.50)",
        DrawerIcon = "rgba(255,255,255, 0.50)",
        AppbarBackground = "#9f9f9f",
        AppbarText = "rgba(255,255,255, 0.70)",
        TextPrimary = "rgba(40,40,40, 0.80)",
        TextSecondary = "rgba(100,100,100, 0.80)",
        ActionDefault = "#2d2d2d",
        ActionDisabled = "rgba(255,255,255, 0.26)",
        ActionDisabledBackground = "rgba(255,255,255, 0.12)",
        Divider = "rgba(100,100,100, 0.12)",
        DividerLight = "rgba(150,150,150, 0.06)",
        TableLines = "rgba(100,100,100, 0.12)",
        LinesDefault = "rgba(200,200,200, 0.12)",
        LinesInputs = "rgba(255,255,255, 0.3)",
        TextDisabled = "rgba(100,100,100, 0.2)",
        Warning = "#666600",
        Tertiary = "#2f2f2f" // Unknown
    };

    public ThemeConfig LightTheme = new()
    {
        Name = "Dark Theme",

        Primary = "#dddddd",
        PrimaryDarken = "#aaaaaa",
        PrimaryLighten = "#FFFFFF",
        Black = "#222222",
        Background = "#000000",
        BackgroundGrey = "#232323",
        Surface = "#272727",
        DrawerBackground = "#2f2f2f",
        DrawerText = "rgba(255,255,255, 0.50)",
        DrawerIcon = "rgba(255,255,255, 0.50)",
        AppbarBackground = "#2f2f2f",
        AppbarText = "rgba(255,255,255, 0.70)",
        TextPrimary = "rgba(255,255,255, 0.50)",
        TextSecondary = "rgba(255,255,255, 0.70)",
        ActionDefault = "#adadad",
        ActionDisabled = "rgba(255,255,255, 0.26)",
        ActionDisabledBackground = "rgba(255,255,255, 0.12)",
        Divider = "rgba(255,255,255, 0.12)",
        DividerLight = "rgba(255,255,255, 0.06)",
        TableLines = "rgba(255,255,255, 0.12)",
        LinesDefault = "rgba(255,255,255, 0.12)",
        LinesInputs = "rgba(255,255,255, 0.3)",
        TextDisabled = "rgba(255,255,255, 0.2)",
        Tertiary = "#2f2f2f" // Unknown
    };

    public ThemeService(IWebHostEnvironment env)
    {
        SetContentPath(env.WebRootPath);
    }

    public event Action<ThemeConfig> OnChangeTheme;

    public Task ApplyTheme(ThemeConfig newTheme)
    {
        OnChangeTheme?.Invoke(newTheme);
        return Task.CompletedTask;
    }

    public async Task<List<ThemeConfig>> GetAllThemes()
    {
        return await Task.FromResult(_themeConfigs.Values
            .OrderBy(x => x.Name)
            .ToList());
    }

    public async Task<ThemeConfig> GetDefaultTheme()
    {
        return await GetThemeConfig("Green");
    }

    public Task<ThemeConfig> GetThemeConfig(string name)
    {
        if (_themeConfigs.TryGetValue(name, out var config))
            return Task.FromResult(config);

        return null;
    }

    public async Task ApplyTheme(string themeName)
    {
        var themeConfig = await GetThemeConfig(themeName);
        if (themeConfig != null)
            await ApplyTheme(themeConfig);
    }

    /// <summary>
    ///     Initialise the service with the download file path - which will usually
    ///     be a subfolder of the wwwroot content folder.
    /// </summary>
    /// <param name="contentRootPath"></param>
    public void SetContentPath(string contentRootPath)
    {
        var themesFolder = new DirectoryInfo(Path.Combine(contentRootPath, "themes"));

        if (themesFolder.Exists)
        {
            Logging.Log($"Scanning for themes in {themesFolder}...");

            var themes = themesFolder.GetFiles("*.css")
                .Select(x => x.Name)
                .ToList();
            foreach (var themeFile in themes)
            {
                var name = Path.GetFileNameWithoutExtension(themeFile);
                var themeFullPath = Path.Combine(themesFolder.FullName, themeFile);

                // Do the mapping to create a matching MudTheme from the theme
                var config = CreateThemeConfigFromCSS(themeFullPath, $"themes/{themeFile}", name);

                Logging.Log($"Configured theme '{name}'.");
                _themeConfigs.Add(name, config);
            }
        }
        else
        {
            Logging.LogError($"Themes folder {themesFolder} was not found.");
        }
    }

    private string Color(IDictionary<string, string> pairs, string ID)
    {
        var value = string.Empty;
        try
        {
            if (pairs.TryGetValue(ID, out value))
                return value;
        }
        catch (Exception ex)
        {
            Logging.Log($"Invalid colour value {value} for {ID}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    ///     Given the new theme, parse its colours and variable names out, and
    ///     then map them to Mud colours.
    /// </summary>
    /// <param name="newTheme"></param>
    /// <returns></returns>
    private ThemeConfig CreateThemeConfigFromCSS(string themeFullPath, string configPath, string themeName)
    {
        try
        {
            var lines = File.ReadAllLines(themeFullPath);

            // Pull out the variables
            var pairs = lines.Select(x => x.Trim())
                .Where(x => x.StartsWith("--"))
                .Select(x => x.Substring(2, x.Length - 3))
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x.First().Trim(), y => y.Last().Trim());

            var themeConfig = new ThemeConfig
            {
                Name = themeName,
                Path = configPath,
                Black = Color(pairs, "main-background"),
                Primary = Color(pairs, "body-text"), // Primary highlighted text, such as selected tab text
                Surface = Color(pairs, "statusbar-gradend"), // Tab backgrounds, dropdown control backgrounds
                TextPrimary = Color(pairs, "body-text"), // Main text on controls
                TextSecondary = Color(pairs, "tool-window-shadow"), // Help text on controls
                ActionDefault = Color(pairs, "statusbar-text"), // Checkboxes 
                TableLines = Color(pairs, "keyword-border"), // Table row separator lines
                LinesInputs = Color(pairs, "keyword-border"), // Underline for enabled controls
                TextDisabled = Color(pairs, "tag-editor-border"), // Disabled controls and labels
                Tertiary = Color(pairs, "statusbar-gradstart"), // Date Picker header background
                ActionDisabledBackground = "rgba(255,255,255, 0.4)", // Unknown
                //PrimaryDarken = Color( dict, "keyword-bg" ), // Unknown
                //PrimaryLighten = Color( dict,"keyword-text" ), // Unknown
                ActionDisabled = "rgba(255,255,255, 0.5)", // Unknown (disabled checkboxes?)
                Background = Color(pairs, "main-background"), // Unknown
                BackgroundGrey = Color(pairs, "tool-window-bg"), // Unknown
                DrawerBackground = "#2f2f2f", // Unknown
                DrawerText = "rgba(255,255,255, 0.50)", // Unknown
                DrawerIcon = "rgba(255,255,255, 0.50)", // Unknown
                AppbarBackground = Color(pairs, "statusbar-text"), // Unknown
                AppbarText = "rgba(255,255,255, 0.70)", // Unknown
                Divider = Color(pairs, "tool-window-title"), // Unknown
                DividerLight = Color(pairs, "tool-window-title-bg"), // Unknown
                LinesDefault = Color(pairs, "keyword-border") // Unknown
            };

            return themeConfig;
        }
        catch (Exception ex)
        {
            Logging.LogWarning($"Unable to parse theme CSS: {ex.Message}");
            return null;
        }
    }
}
