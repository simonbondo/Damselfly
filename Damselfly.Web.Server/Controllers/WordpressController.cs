using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/wordpress")]
public class WordpressController : ControllerBase
{
    private readonly ILogger<WordpressController> _logger;
    private readonly WordpressService _service;

    public WordpressController(WordpressService service, ILogger<WordpressController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("/api/wordpress/")]
    public async Task Put(List<Image> images)
    {
        await _service.UploadImagesToWordpress(images);
    }
}
