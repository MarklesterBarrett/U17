using Microsoft.Extensions.DependencyInjection;
using Site.DesignTokens.Css;
using Site.DesignTokens.Defaults;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Export;
using Site.DesignTokens.Import;
using Site.DesignTokens.Loading;
using Site.DesignTokens.Normalization;
using Site.DesignTokens.Parsing;
using Site.DesignTokens.Persistence;
using Site.DesignTokens.References;
using Site.DesignTokens.Serialization;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Tailwind;
using Site.DesignTokens.Usage;
using Site.DesignTokens.Validation;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenManagementComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton(new DesignTokenManagementOptions());
        builder.Services.AddSingleton<IDesignTokenStarterJsonProvider, DesignTokenStarterJsonProvider>();
        builder.Services.AddSingleton<IDesignTokenJsonParser, DesignTokenJsonParser>();
        builder.Services.AddSingleton<IDesignTokenSourceMerger, DesignTokenSourceMerger>();
        builder.Services.AddSingleton<IDesignTokenValueNormalizer, DesignTokenValueNormalizer>();
        builder.Services.AddSingleton<IDesignTokenReferenceResolver, DesignTokenReferenceResolver>();
        builder.Services.AddSingleton<IDesignTokenValidator, DesignTokenValidator>();
        builder.Services.AddSingleton<Site.DesignTokens.Css.IDesignTokenCssGenerator, Site.DesignTokens.Css.DesignTokenCssGenerator>();
        builder.Services.AddSingleton<IDesignTokenTailwindExporter, DesignTokenTailwindExporter>();
        builder.Services.AddSingleton<IDesignTokenJsonFormatter, DesignTokenJsonFormatter>();
        builder.Services.AddSingleton<DesignTokenCssVariablePathMapper>();
        builder.Services.AddSingleton<IDesignTokenUsageScanner>(provider =>
            new DesignTokenUsageScanner(
                new DesignTokenUsageScannerOptions
                {
                    Enabled = false,
                    RootPath = Directory.GetCurrentDirectory()
                },
                provider.GetRequiredService<DesignTokenCssVariablePathMapper>()));
        builder.Services.AddSingleton<IDesignTokenDocumentStore, DesignTokenDocumentStore>();
        builder.Services.AddSingleton<DesignTokenJsonSource>();
        builder.Services.AddSingleton<DesignTokenBuildPipeline>();
        builder.Services.AddSingleton<IDesignTokenCssWriter, DesignTokenCssWriter>();
        builder.Services.AddSingleton<IDesignTokenTailwindWriter, DesignTokenTailwindWriter>();
        builder.Services.AddSingleton<IDesignTokenDiagnosticsService>(provider =>
            new DesignTokenDiagnosticsService(
                provider.GetRequiredService<DesignTokenJsonSource>(),
                provider.GetRequiredService<IDesignTokenSourceMerger>(),
                provider.GetRequiredService<IDesignTokenValueNormalizer>(),
                provider.GetRequiredService<IDesignTokenReferenceResolver>(),
                provider.GetRequiredService<IDesignTokenValidator>(),
                provider.GetRequiredService<Site.DesignTokens.Css.IDesignTokenCssGenerator>(),
                provider.GetRequiredService<IDesignTokenTailwindExporter>(),
                provider.GetRequiredService<IDesignTokenJsonFormatter>(),
                null,
                provider.GetRequiredService<IDesignTokenCssWriter>(),
                provider.GetRequiredService<IDesignTokenTailwindWriter>(),
                provider.GetRequiredService<IDesignTokenUsageScanner>()));
        builder.Services.AddSingleton<IDesignTokenDocumentActivationService, DesignTokenDocumentActivationService>();
        builder.Services.AddSingleton<IDesignTokenImportService, DesignTokenImportService>();
        builder.Services.AddSingleton<IDesignTokenExportService, DesignTokenExportService>();
        builder.Services.AddSingleton<IDesignTokenBuildStatusStore>(provider =>
            new DesignTokenBuildStatusStore(provider.GetRequiredService<DesignTokenManagementOptions>().BuildStatusPath));
        builder.Services.AddSingleton<IDesignTokenOutputWriter, DesignTokenOutputWriter>();
        builder.Services.AddSingleton<IDesignTokenManagementService, DesignTokenManagementService>();
        builder.Services.AddSingleton<IDesignTokenBackofficeAccessService, DesignTokenBackofficeAccessService>();
        builder.Services.AddScoped<IFoundationTokensService, FoundationTokensService>();
    }
}
