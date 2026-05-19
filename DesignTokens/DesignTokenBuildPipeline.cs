using Site.DesignTokens.Css;
using Site.DesignTokens.Loading;
using Site.DesignTokens.Models;
using Site.DesignTokens.Normalization;
using Site.DesignTokens.Parsing;
using Site.DesignTokens.References;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Themes;
using Site.DesignTokens.Validation;
using System.Text;

namespace Site.DesignTokens;

public sealed class DesignTokenBuildPipeline
{
    private readonly DesignTokenJsonSource _jsonSource;
    private readonly IDesignTokenJsonParser _jsonParser;
    private readonly IDesignTokenValueNormalizer _valueNormalizer;
    private readonly IDesignTokenReferenceResolver _referenceResolver;
    private readonly IDesignTokenValidator _validator;
    private readonly Site.DesignTokens.Css.IDesignTokenCssGenerator _cssGenerator;
    private readonly IDesignTokenSourceMerger _sourceMerger;

    public DesignTokenBuildPipeline(
        DesignTokenJsonSource jsonSource,
        IDesignTokenSourceMerger sourceMerger,
        IDesignTokenJsonParser jsonParser,
        IDesignTokenValueNormalizer valueNormalizer,
        IDesignTokenReferenceResolver referenceResolver,
        IDesignTokenValidator validator,
        Site.DesignTokens.Css.IDesignTokenCssGenerator cssGenerator)
    {
        _jsonSource = jsonSource;
        _sourceMerger = sourceMerger;
        _jsonParser = jsonParser;
        _valueNormalizer = valueNormalizer;
        _referenceResolver = referenceResolver;
        _validator = validator;
        _cssGenerator = cssGenerator;
    }

    public DesignTokenBuildPipelineResult Build(string? importedJson)
    {
        return Build(_jsonSource.GetThemeVariants(importedJson));
    }

    public DesignTokenBuildPipelineResult Build(IEnumerable<DesignTokenSource> sources)
    {
        return BuildSources(sources, ":root", includeHeader: true);
    }

    public DesignTokenBuildPipelineResult Build(IReadOnlyList<DesignTokenThemeVariant> variants)
    {
        if (variants.Count == 0)
        {
            return BuildSources([], ":root", includeHeader: true);
        }

        var aggregateRegistry = new DesignTokenRegistry();
        var cssBuilder = new StringBuilder();
        var sourceMergeErrors = new List<DesignTokenSourceMergeError>();
        var parseErrors = new List<DesignTokenParseError>();
        var normalisationErrors = new List<DesignTokenNormalizationError>();
        var resolutionErrors = new List<DesignTokenReferenceResolutionError>();
        var validationErrors = new List<DesignTokenValidationError>();
        var cssGenerationErrors = new List<DesignTokenCssGenerationError>();

        var orderedVariants = variants
            .Where(x => x.Enabled || x.IsDefault)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Alias, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < orderedVariants.Length; index++)
        {
            var variant = orderedVariants[index];
            var variantResult = BuildSources(variant.Sources, variant.Selector, includeHeader: index == 0);

            if (variant.IsDefault)
            {
                aggregateRegistry = variantResult.Registry;
            }

            if (variantResult.Success && !string.IsNullOrWhiteSpace(variantResult.Css))
            {
                if (cssBuilder.Length > 0 && !cssBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                {
                    cssBuilder.AppendLine();
                }

                if (cssBuilder.Length > 0)
                {
                    cssBuilder.AppendLine();
                }

                cssBuilder.Append(variantResult.Css.TrimEnd());
            }

            sourceMergeErrors.AddRange(PrefixVariantErrors(variantResult.SourceMergeErrors, variant, (error, message) => new DesignTokenSourceMergeError(error.SourceType, error.SourceName, error.Path, message)));
            parseErrors.AddRange(PrefixVariantErrors(variantResult.ParseErrors, variant, (error, message) => new DesignTokenParseError(error.Path, message)));
            normalisationErrors.AddRange(PrefixVariantErrors(variantResult.NormalisationErrors, variant, (error, message) => new DesignTokenNormalizationError(error.Path, message)));
            resolutionErrors.AddRange(PrefixVariantErrors(variantResult.ResolutionErrors, variant, (error, message) => new DesignTokenReferenceResolutionError(error.SourcePath, message)));
            validationErrors.AddRange(PrefixVariantErrors(variantResult.ValidationErrors, variant, (error, message) => new DesignTokenValidationError(error.Path, error.Type, error.Field, message)));
            cssGenerationErrors.AddRange(PrefixVariantErrors(variantResult.CssGenerationErrors, variant, (error, message) => new DesignTokenCssGenerationError(error.Path, message)));
        }

        return new DesignTokenBuildPipelineResult(
            aggregateRegistry,
            cssBuilder.ToString(),
            sourceMergeErrors,
            parseErrors,
            normalisationErrors,
            resolutionErrors,
            validationErrors,
            cssGenerationErrors);
    }

    private DesignTokenBuildPipelineResult BuildSources(IEnumerable<DesignTokenSource> sources, string selector, bool includeHeader)
    {
        var mergeResult = _sourceMerger.Merge(sources);

        if (!mergeResult.Success)
        {
            return new DesignTokenBuildPipelineResult(
                mergeResult.Registry,
                string.Empty,
                mergeResult.Errors,
                mergeResult.ParseErrors,
                [],
                [],
                [],
                []);
        }

        var normalizeResult = _valueNormalizer.Normalize(mergeResult.Registry);
        if (!normalizeResult.Success)
        {
            return new DesignTokenBuildPipelineResult(
                normalizeResult.Registry,
                string.Empty,
                mergeResult.Errors,
                mergeResult.ParseErrors,
                normalizeResult.Errors,
                [],
                [],
                []);
        }

        var resolutionResult = _referenceResolver.Resolve(normalizeResult.Registry);
        if (!resolutionResult.Success)
        {
            return new DesignTokenBuildPipelineResult(
                resolutionResult.Registry,
                string.Empty,
                mergeResult.Errors,
                mergeResult.ParseErrors,
                normalizeResult.Errors,
                resolutionResult.Errors,
                [],
                []);
        }

        var validationResult = _validator.Validate(resolutionResult.Registry);
        if (!validationResult.Success)
        {
            return new DesignTokenBuildPipelineResult(
                validationResult.Registry,
                string.Empty,
                mergeResult.Errors,
                mergeResult.ParseErrors,
                normalizeResult.Errors,
                resolutionResult.Errors,
                validationResult.Errors,
                []);
        }

        var cssGenerationResult = _cssGenerator.Generate(validationResult.Registry, selector, includeHeader);

        return new DesignTokenBuildPipelineResult(
            validationResult.Registry,
            cssGenerationResult.Css,
            mergeResult.Errors,
            mergeResult.ParseErrors,
            normalizeResult.Errors,
            resolutionResult.Errors,
            validationResult.Errors,
            cssGenerationResult.Errors);
    }

    private static IEnumerable<TError> PrefixVariantErrors<TError>(
        IReadOnlyList<TError> errors,
        DesignTokenThemeVariant variant,
        Func<TError, string, TError> factory)
    {
        foreach (var error in errors)
        {
            var message = variant.IsDefault
                ? GetMessage(error)
                : $"Theme '{variant.Alias}': {GetMessage(error)}";
            yield return factory(error, message);
        }
    }

    private static string GetMessage<TError>(TError error)
    {
        return error switch
        {
            DesignTokenSourceMergeError sourceMergeError => sourceMergeError.Message,
            DesignTokenParseError parseError => parseError.Message,
            DesignTokenNormalizationError normalizationError => normalizationError.Message,
            DesignTokenReferenceResolutionError resolutionError => resolutionError.Message,
            DesignTokenValidationError validationError => validationError.Message,
            DesignTokenCssGenerationError cssGenerationError => cssGenerationError.Message,
            _ => string.Empty
        };
    }
}

public sealed class DesignTokenBuildPipelineResult
{
    public DesignTokenBuildPipelineResult(
        DesignTokenRegistry registry,
        string css,
        IReadOnlyList<DesignTokenSourceMergeError> sourceMergeErrors,
        IReadOnlyList<DesignTokenParseError> parseErrors,
        IReadOnlyList<DesignTokenNormalizationError> normalisationErrors,
        IReadOnlyList<DesignTokenReferenceResolutionError> resolutionErrors,
        IReadOnlyList<DesignTokenValidationError> validationErrors,
        IReadOnlyList<DesignTokenCssGenerationError> cssGenerationErrors)
    {
        Registry = registry;
        Css = css;
        SourceMergeErrors = sourceMergeErrors;
        ParseErrors = parseErrors;
        NormalisationErrors = normalisationErrors;
        ResolutionErrors = resolutionErrors;
        ValidationErrors = validationErrors;
        CssGenerationErrors = cssGenerationErrors;
    }

    public DesignTokenRegistry Registry { get; }

    public string Css { get; }

    public IReadOnlyList<DesignTokenSourceMergeError> SourceMergeErrors { get; }

    public IReadOnlyList<DesignTokenParseError> ParseErrors { get; }

    public IReadOnlyList<DesignTokenNormalizationError> NormalisationErrors { get; }

    public IReadOnlyList<DesignTokenReferenceResolutionError> ResolutionErrors { get; }

    public IReadOnlyList<DesignTokenValidationError> ValidationErrors { get; }

    public IReadOnlyList<DesignTokenCssGenerationError> CssGenerationErrors { get; }

    public bool Success =>
        SourceMergeErrors.Count == 0 &&
        ParseErrors.Count == 0 &&
        NormalisationErrors.Count == 0 &&
        ResolutionErrors.Count == 0 &&
        ValidationErrors.Count == 0 &&
        CssGenerationErrors.Count == 0;
}
