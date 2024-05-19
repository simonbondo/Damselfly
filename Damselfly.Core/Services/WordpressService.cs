using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using WordPressPCL;
using WordPressPCL.Models;

namespace Damselfly.Core.Services;

/// <summary>
///     Service for accessing Wordpress and uploading media.
/// </summary>
public class WordpressService : IWordpressService
{
    private readonly IConfigService _configService;
    private readonly ImageProcessService _imageProcessService;
    private readonly IStatusService _statusService;
    private WordPressClient _client;

    public WordpressService(ImageProcessService imageService,
        ConfigService configService,
        IStatusService statusService)
    {
        _configService = configService;
        _statusService = statusService;
        _imageProcessService = imageService;

        ResetClient();
    }

    /// <summary>
    ///     Upload the basket imagesconfigured Wordpress  site's media library
    ///     TODO: Add option to watermark and resize images when uploading
    /// </summary>
    /// <returns></returns>
    public async Task UploadImagesToWordpress(List<Image> images)
    {
        try
        {
            _statusService.UpdateStatus($"Uploading {images.Count()} to Wordpress...");

            Logging.LogVerbose("Checking token validity...");

            var validToken = await CheckTokenValidity();

            if (validToken)
            {
                foreach (var image in images)
                {
                    using var memoryStream = new MemoryStream();

                    // We shrink the images a bit before upload to Wordpress.
                    // TODO: Support watermarks for WP Upload in future.
                    var wpConfig = new ExportConfig { Size = ExportSize.Large, WatermarkText = null };

                    // This saves to the memoryStream with encoder
                    await _imageProcessService.TransformDownloadImage(image.FullPath, memoryStream, wpConfig);

                    // The position needs to be reset, before we push it to Wordpress
                    memoryStream.Position = 0;

                    _statusService.UpdateStatus($"Uploading {image.FileName} to Wordpress.");

                    await _client.Media.CreateAsync(memoryStream, image.FileName);

                    Logging.LogVerbose($"Image uploaded: {image.FullPath} successfully.");
                }

                _statusService.UpdateStatus($"{images.Count()} images uploaded to Wordpress.");
            }
            else
            {
                Logging.LogError("Token was invalid.");
                _statusService.UpdateStatus("Authentication error uploading to Wordpress.");
            }
        }
        catch (Exception e)
        {
            Logging.LogError($"Error uploading to Wordpress: {e.Message}");
            _statusService.UpdateStatus("Error uploading images to Wordpress. Please check the logs.");
        }
    }

    /// <summary>
    ///     See if the token we have is valid. If it is, return true.
    ///     If it's invalid (either we never had one, or it's expired)
    ///     request a new one.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> CheckTokenValidity()
    {
        var gotToken = false;

        if (_client == null)
        {
            // Create the one-time client.
            // TODO: Destroy this if the settings are updated.
            _client = GetClient();

            if (_client == null)
                return false;
        }

        // Now check if we have a valid token (they expire after
        // 24 hours) and if not, obtain one
        gotToken = await _client.Auth.IsValidJWTokenAsync();

        if (!gotToken)
        {
            var user = _configService.Get(ConfigSettings.WordpressUser);
            var pass = _configService.Get(ConfigSettings.WordpressPassword);

            Logging.LogVerbose("No valid JWT token. Requesting a new one.");

            await _client.Auth.RequestJWTokenAsync(user, pass);

            gotToken = await _client.Auth.IsValidJWTokenAsync();
        }

        var state = gotToken ? "valid" : "invalid";
        Logging.LogVerbose($"JWT token is {state}.");

        return gotToken;
    }

    /// <summary>
    ///     Reset the client - use this if the settings are updated.
    /// </summary>
    public void ResetClient()
    {
        _client = GetClient();

        if (_client != null)
            Logging.Log("Wordpress API client reset.");
    }

    /// <summary>
    ///     Create the Wordpress PCL client.
    /// </summary>
    /// <returns></returns>
    private WordPressClient GetClient()
    {
        WordPressClient client = null;

        try
        {
            var wpUrl = _configService.Get(ConfigSettings.WordpressURL);

            if (!string.IsNullOrEmpty(wpUrl))
            {
                var baseUrl = new Uri(wpUrl);
                var url = new Uri(baseUrl, "/wp-json/");

                Logging.LogVerbose($"Initialising Wordpress Client for {url}...");

                // JWT authentication
                client = new WordPressClient(url.ToString());
                client.Auth.UseBearerAuth(JWTPlugin.JWTAuthByEnriqueChavez);

                Logging.Log("JWT Auth token generated successfully.");
            }
            else
            {
                Logging.LogVerbose("Wordpress integration was not configured.");
            }
        }
        catch (Exception ex)
        {
            Logging.LogError($"Unable to create Wordpress Client: {ex.Message}");
            client = null;
        }

        return client;
    }
}
