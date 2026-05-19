using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace Site.SubsidiarySites;

[VersionedApiBackOfficeRoute("subsidiary-site-generator")]
public sealed class SubsidiarySiteGeneratorController : ManagementApiControllerBase
{
    private readonly ISubsidiarySiteGeneratorService _generatorService;
    private readonly ISubsidiarySiteThemeService _themeService;

    public SubsidiarySiteGeneratorController(
        ISubsidiarySiteGeneratorService generatorService,
        ISubsidiarySiteThemeService themeService)
    {
        _generatorService = generatorService;
        _themeService = themeService;
    }

    [HttpGet("default-theme")]
    [ProducesResponseType(typeof(ThemeValidationResponse), StatusCodes.Status200OK)]
    public ActionResult<ThemeValidationResponse> GetDefaultTheme()
    {
        var json = _themeService.CreateDefaultThemeJson();
        return Ok(new ThemeValidationResponse(true, json, [], []));
    }

    [HttpPost("validate-theme")]
    [ProducesResponseType(typeof(ThemeValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ThemeValidationResponse> ValidateTheme([FromBody] ValidateThemeEnvelope request)
    {
        if (request.Request is null)
        {
            var details = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] =
                [
                    "Expected JSON payload shaped like {\"request\":{\"designTokenJson\":\"...\"}}."
                ]
            });

            return BadRequest(details);
        }

        var validation = _themeService.Validate(request.Request.DesignTokenJson, useDefaultsWhenEmpty: false);

        return Ok(new ThemeValidationResponse(
            validation.IsValid,
            request.Request.DesignTokenJson ?? string.Empty,
            validation.Errors,
            validation.Warnings));
    }

    [HttpPost("generate")]
    [RequestSizeLimit(50_000_000)]
    [ProducesResponseType(typeof(SiteGenerationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteGenerationResponse>> Generate()
    {
        string siteName;
        string? designTokenJson;
        List<UploadedLogoAsset> assets;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            siteName = form["siteName"].ToString();
            designTokenJson = form["designTokenJson"].ToString();
            assets = new List<UploadedLogoAsset>();

            foreach (var file in form.Files)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);

                assets.Add(new UploadedLogoAsset(
                    file.FileName,
                    file.ContentType ?? "application/octet-stream",
                    buffer.ToArray()));
            }
        }
        else
        {
            var request = await Request.ReadFromJsonAsync<GenerateSiteEnvelope>();
            if (request?.Request is null)
            {
                var details = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["request"] =
                    [
                        "Expected JSON payload shaped like {\"request\":{\"siteName\":\"...\",\"designTokenJson\":\"...\",\"logoFiles\":[]}}."
                    ]
                });

                return BadRequest(details);
            }

            siteName = request.Request.SiteName ?? string.Empty;
            designTokenJson = request.Request.DesignTokenJson;
            assets = new List<UploadedLogoAsset>();

            foreach (var file in request.Request.LogoFiles ?? [])
            {
                if (string.IsNullOrWhiteSpace(file.Base64))
                {
                    continue;
                }

                assets.Add(new UploadedLogoAsset(
                    file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    Convert.FromBase64String(file.Base64)));
            }
        }

        var response = await _generatorService.GenerateAsync(new SiteGenerationRequest(
            siteName,
            string.IsNullOrWhiteSpace(designTokenJson) ? null : designTokenJson,
            assets));

        return Ok(response);
    }

    public sealed record ValidateThemeEnvelope(ValidateThemeRequest? Request);

    public sealed record ValidateThemeRequest(string? DesignTokenJson);

    public sealed record GenerateSiteEnvelope(GenerateSiteRequest? Request);

    public sealed record GenerateSiteRequest(
        string? SiteName,
        string? DesignTokenJson,
        IReadOnlyList<UploadedLogoAssetRequest>? LogoFiles);
}
