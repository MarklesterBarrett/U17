using System.Diagnostics;
using System.Text.Json;
using Site.DesignTokens.Css;
using Site.DesignTokens.Loading;
using Site.DesignTokens.Models;
using Site.DesignTokens.Normalization;
using Site.DesignTokens.Parsing;
using Site.DesignTokens.References;
using Site.DesignTokens.Serialization;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Themes;
using Site.DesignTokens.Tailwind;
using Site.DesignTokens.Usage;
using Site.DesignTokens.Validation;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnosticsService : IDesignTokenDiagnosticsService
{
    private readonly DesignTokenJsonSource _jsonSource;
    private readonly IDesignTokenSourceMerger _sourceMerger;
    private readonly IDesignTokenValueNormalizer _valueNormalizer;
    private readonly IDesignTokenReferenceResolver _referenceResolver;
    private readonly IDesignTokenValidator _validator;
    private readonly Site.DesignTokens.Css.IDesignTokenCssGenerator _cssGenerator;
    private readonly IDesignTokenTailwindExporter _tailwindExporter;
    private readonly IDesignTokenJsonFormatter _jsonFormatter;
    private readonly DesignTokenDiagnosticsOptions _options;
    private readonly IDesignTokenCssWriter? _cssWriter;
    private readonly IDesignTokenTailwindWriter? _tailwindWriter;
    private readonly IDesignTokenUsageScanner? _usageScanner;

    public DesignTokenDiagnosticsService(
        DesignTokenJsonSource jsonSource,
        IDesignTokenSourceMerger sourceMerger,
        IDesignTokenValueNormalizer valueNormalizer,
        IDesignTokenReferenceResolver referenceResolver,
        IDesignTokenValidator validator,
        Site.DesignTokens.Css.IDesignTokenCssGenerator cssGenerator,
        IDesignTokenTailwindExporter tailwindExporter,
        IDesignTokenJsonFormatter jsonFormatter,
        DesignTokenDiagnosticsOptions? options = null,
        IDesignTokenCssWriter? cssWriter = null,
        IDesignTokenTailwindWriter? tailwindWriter = null,
        IDesignTokenUsageScanner? usageScanner = null)
    {
        _jsonSource = jsonSource;
        _sourceMerger = sourceMerger;
        _valueNormalizer = valueNormalizer;
        _referenceResolver = referenceResolver;
        _validator = validator;
        _cssGenerator = cssGenerator;
        _tailwindExporter = tailwindExporter;
        _jsonFormatter = jsonFormatter;
        _options = options ?? new DesignTokenDiagnosticsOptions();
        _cssWriter = cssWriter;
        _tailwindWriter = tailwindWriter;
        _usageScanner = usageScanner;
    }

    public DesignTokenDiagnosticsResult Inspect(string? importedJson = null)
    {
        var variants = _jsonSource.GetThemeVariants(importedJson);
        var unsupportedThemeVariants = _jsonSource.GetUnsupportedThemeVariantAliases(importedJson);
        var defaultVariant = variants.FirstOrDefault(x => x.IsDefault) ?? variants.First();
        var stageResults = new List<DesignTokenPipelineStageResult>();
        var infos = new List<DesignTokenDiagnostic>();
        var warnings = new List<DesignTokenDiagnostic>();
        var errors = new List<DesignTokenDiagnostic>();
        var debugExports = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var themeSummaries = BuildThemeSummaries(variants);
        infos.AddRange(BuildThemeVariantDiagnostics(themeSummaries, unsupportedThemeVariants));
        var totalTokenCount = 0;
        var combinedCss = string.Empty;

        var sources = defaultVariant.Sources;
        var mergeStopwatch = Stopwatch.StartNew();
        var mergeResult = _sourceMerger.Merge(sources);
        mergeStopwatch.Stop();

        var parseErrors = mergeResult.ParseErrors
            .Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Parse, x.Message, x.Path))
            .ToArray();
        errors.AddRange(parseErrors);
        stageResults.Add(new DesignTokenPipelineStageResult
        {
            Stage = DesignTokenDiagnosticStage.Parse,
            Success = parseErrors.Length == 0,
            Errors = parseErrors,
            DurationMs = mergeStopwatch.ElapsedMilliseconds
        });

        var mergeErrors = mergeResult.Errors
            .Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, x.Message, x.Path))
            .ToArray();
        errors.AddRange(mergeErrors);
        stageResults.Add(new DesignTokenPipelineStageResult
        {
            Stage = DesignTokenDiagnosticStage.SourceMerge,
            Success = mergeErrors.Length == 0,
            Errors = mergeErrors,
            DurationMs = mergeStopwatch.ElapsedMilliseconds
        });

        if (!mergeResult.Success)
        {
            AddSkippedStages(stageResults, warnings, DesignTokenDiagnosticStage.Normalise);
            return CreateResult(mergeResult.Registry, mergeResult.SourceTraces, [], [], mergeResult.SourceSummaries, mergeResult.Totals, mergeResult.MergeEvents, debugExports, stageResults, infos, warnings, errors, string.Empty, string.Empty, themeSummaries, totalTokenCount, null);
        }

        var normalizeResult = MeasureStage(
            DesignTokenDiagnosticStage.Normalise,
            () => _valueNormalizer.Normalize(mergeResult.Registry),
            stageResults,
            warnings,
            errors);

        if (!normalizeResult.Success)
        {
            AddSkippedStages(stageResults, warnings, DesignTokenDiagnosticStage.Resolve);
            return CreateResult(normalizeResult.Registry, mergeResult.SourceTraces, [], [], mergeResult.SourceSummaries, mergeResult.Totals, mergeResult.MergeEvents, debugExports, stageResults, infos, warnings, errors, string.Empty, string.Empty, themeSummaries, totalTokenCount, null);
        }

        var resolutionResult = MeasureStage(
            DesignTokenDiagnosticStage.Resolve,
            () => _referenceResolver.Resolve(normalizeResult.Registry),
            stageResults,
            warnings,
            errors);

        if (!resolutionResult.Success)
        {
            AddSkippedStages(stageResults, warnings, DesignTokenDiagnosticStage.Validate);
            var dependenciesFailed = BuildDependencyGraph(resolutionResult.Registry);
            return CreateResult(resolutionResult.Registry, mergeResult.SourceTraces, dependenciesFailed, [], mergeResult.SourceSummaries, mergeResult.Totals, mergeResult.MergeEvents, debugExports, stageResults, infos, warnings, errors, string.Empty, string.Empty, themeSummaries, totalTokenCount, null);
        }

        var validationResult = MeasureStage(
            DesignTokenDiagnosticStage.Validate,
            () => _validator.Validate(resolutionResult.Registry),
            stageResults,
            warnings,
            errors);

        var dependencies = BuildDependencyGraph(validationResult.Registry);
        var advisoryDiagnostics = BuildAdvisoryDiagnostics(validationResult.Registry, mergeResult.SourceTraces, dependencies);
        infos.AddRange(advisoryDiagnostics.Infos);
        warnings.AddRange(advisoryDiagnostics.Warnings);

        if (!validationResult.Success)
        {
            AddSkippedStages(stageResults, warnings, DesignTokenDiagnosticStage.CssGenerate);
            return CreateResult(validationResult.Registry, mergeResult.SourceTraces, dependencies, [], mergeResult.SourceSummaries, mergeResult.Totals, mergeResult.MergeEvents, debugExports, stageResults, infos, warnings, errors, string.Empty, string.Empty, themeSummaries, totalTokenCount, null);
        }

        var cssGenerationResult = MeasureStage(
            DesignTokenDiagnosticStage.CssGenerate,
            () => _cssGenerator.Generate(validationResult.Registry, defaultVariant.Selector, includeHeader: true),
            stageResults,
            warnings,
            errors);

        var tailwindResult = MeasureStage(
            DesignTokenDiagnosticStage.TailwindGenerate,
            () => _tailwindExporter.Export(validationResult.Registry),
            stageResults,
            warnings,
            errors);

        stageResults.Add(new DesignTokenPipelineStageResult
        {
            Stage = DesignTokenDiagnosticStage.CssWrite,
            Success = true,
            DurationMs = 0,
            Warnings = [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.CssWrite, "CSS write not executed by diagnostics service.")]
        });

        stageResults.Add(new DesignTokenPipelineStageResult
        {
            Stage = DesignTokenDiagnosticStage.TailwindWrite,
            Success = true,
            DurationMs = 0,
            Warnings = [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.TailwindWrite, "Tailwind write not executed by diagnostics service.")]
        });

        if (_options.EnableDebugExports)
        {
            WriteDebugExports(
                mergeResult.Registry,
                validationResult.Registry,
                dependencies,
                cssGenerationResult.Css,
                tailwindResult.Json,
                debugExports);
        }

        totalTokenCount = validationResult.Registry.All.Count;
        combinedCss = cssGenerationResult.Css;

        foreach (var variant in variants.Where(x => x.Enabled && !x.IsDefault).OrderBy(x => x.Alias, StringComparer.Ordinal))
        {
            var variantResult = BuildAdditionalVariant(variant);
            totalTokenCount += variantResult.TokenCount;
            errors.AddRange(variantResult.Errors);
            warnings.AddRange(variantResult.Warnings);
            infos.AddRange(variantResult.Infos);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.Parse, variantResult.ParseErrors, []);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.SourceMerge, variantResult.MergeErrors, []);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.Normalise, variantResult.NormalisationErrors, []);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.Resolve, variantResult.ResolutionErrors, variantResult.ResolveWarnings);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.Validate, variantResult.ValidationErrors, variantResult.ValidateWarnings);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.CssGenerate, variantResult.CssGenerationErrors, []);
            MergeStageResult(stageResults, DesignTokenDiagnosticStage.TailwindGenerate, [], variantResult.TailwindWarnings);

            if (!string.IsNullOrWhiteSpace(variantResult.Css))
            {
                combinedCss = string.IsNullOrWhiteSpace(combinedCss)
                    ? variantResult.Css
                    : $"{combinedCss.TrimEnd()}{Environment.NewLine}{Environment.NewLine}{variantResult.Css.TrimEnd()}";
            }
        }

        var aggregateBuild = new DesignTokenBuildPipeline(
            _jsonSource,
            _sourceMerger,
            new DesignTokenJsonParser(),
            _valueNormalizer,
            _referenceResolver,
            _validator,
            _cssGenerator).Build(variants);

        if (!string.IsNullOrWhiteSpace(aggregateBuild.Css))
        {
            combinedCss = aggregateBuild.Css;
        }

        DesignTokenUsageScanResult? usageScan = null;
        if (_options.EnableUsageScanning && _usageScanner is not null)
        {
            usageScan = _usageScanner.Scan(validationResult.Registry);
            warnings.AddRange(ToUsageDiagnostics(usageScan));
        }

        var tokenViews = BuildTokenViews(validationResult.Registry, mergeResult.SourceTraces, validationResult.Errors, infos, warnings, errors, dependencies, usageScan);
        return CreateResult(validationResult.Registry, mergeResult.SourceTraces, dependencies, tokenViews, mergeResult.SourceSummaries, mergeResult.Totals, mergeResult.MergeEvents, debugExports, stageResults, infos, warnings, errors, combinedCss, tailwindResult.Json, themeSummaries, totalTokenCount, usageScan);
    }

    public IReadOnlyList<DesignTokenDiagnosticViewModel> GetTokens(string? importedJson = null) =>
        Inspect(importedJson).Tokens;

    public DesignTokenDiagnosticViewModel? GetToken(string path, string? importedJson = null) =>
        Inspect(importedJson).Tokens.FirstOrDefault(x => string.Equals(x.Path, path, StringComparison.Ordinal));

    public IReadOnlyList<DesignTokenSourceTrace> GetSources(string? importedJson = null) =>
        Inspect(importedJson).SourceTraces;

    public IReadOnlyList<DesignTokenDependencyNode> GetDependencies(string? importedJson = null) =>
        Inspect(importedJson).Dependencies;

    public IReadOnlyList<DesignTokenDiagnostic> GetErrors(string? importedJson = null) =>
        Inspect(importedJson).BuildReport.Errors;

    private T MeasureStage<T>(
        DesignTokenDiagnosticStage stage,
        Func<T> action,
        List<DesignTokenPipelineStageResult> stages,
        List<DesignTokenDiagnostic> warnings,
        List<DesignTokenDiagnostic> errors)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();

        var stageErrors = ExtractErrors(stage, result);
        var stageWarnings = ExtractWarnings(stage, result);
        errors.AddRange(stageErrors);
        warnings.AddRange(stageWarnings);

        stages.Add(new DesignTokenPipelineStageResult
        {
            Stage = stage,
            Success = stageErrors.Count == 0,
            Errors = stageErrors,
            Warnings = stageWarnings,
            DurationMs = stopwatch.ElapsedMilliseconds
        });

        return result;
    }

    private static IReadOnlyList<DesignTokenDiagnostic> ExtractErrors<T>(DesignTokenDiagnosticStage stage, T result)
    {
        return result switch
        {
            DesignTokenNormalizationResult normalizeResult => normalizeResult.Errors.Select(x => new DesignTokenDiagnostic(stage, x.Message, x.Path)).ToArray(),
            DesignTokenReferenceResolutionResult resolutionResult => resolutionResult.Errors.Select(x => new DesignTokenDiagnostic(stage, x.Message, x.SourcePath)).ToArray(),
            DesignTokenValidationResult validationResult => validationResult.Errors.Select(x => new DesignTokenDiagnostic(stage, x.Message, x.Path, x.Field)).ToArray(),
            DesignTokenCssGenerationResult cssResult => cssResult.Errors.Select(x => new DesignTokenDiagnostic(stage, x.Message, x.Path)).ToArray(),
            DesignTokenTailwindExportResult tailwindResult => tailwindResult.Errors.Select(x => new DesignTokenDiagnostic(stage, x.Message, x.Path)).ToArray(),
            _ => []
        };
    }

    private static IReadOnlyList<DesignTokenDiagnostic> ExtractWarnings<T>(DesignTokenDiagnosticStage stage, T result)
    {
        return result switch
        {
            _ => []
        };
    }

    private static void AddSkippedStages(
        List<DesignTokenPipelineStageResult> stages,
        List<DesignTokenDiagnostic> warnings,
        DesignTokenDiagnosticStage firstSkippedStage)
    {
        foreach (var stage in Enum.GetValues<DesignTokenDiagnosticStage>().Where(x => x >= firstSkippedStage))
        {
            var warning = new DesignTokenDiagnostic(stage, "Stage skipped because an earlier stage failed.");
            warnings.Add(warning);
            stages.Add(new DesignTokenPipelineStageResult
            {
                Stage = stage,
                Success = false,
                DurationMs = 0,
                Warnings = [warning]
            });
        }
    }

    private VariantInspectionResult BuildAdditionalVariant(DesignTokenThemeVariant variant)
    {
        var mergeResult = _sourceMerger.Merge(variant.Sources);
        var parseErrors = PrefixVariantDiagnostics(
            variant,
            mergeResult.ParseErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Parse, x.Message, x.Path)).ToArray());
        var mergeErrors = PrefixVariantDiagnostics(
            variant,
            mergeResult.Errors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, x.Message, x.Path)).ToArray());

        if (!mergeResult.Success)
        {
            return new VariantInspectionResult(
                mergeResult.Registry.All.Count,
                string.Empty,
                parseErrors,
                mergeErrors,
                [],
                [],
                [],
                [],
                [],
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.Normalise),
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.Validate),
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.TailwindGenerate));
        }

        var normalizeResult = _valueNormalizer.Normalize(mergeResult.Registry);
        var normalisationErrors = PrefixVariantDiagnostics(
            variant,
            normalizeResult.Errors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Normalise, x.Message, x.Path)).ToArray());
        if (!normalizeResult.Success)
        {
            return new VariantInspectionResult(
                normalizeResult.Registry.All.Count,
                string.Empty,
                parseErrors,
                mergeErrors,
                normalisationErrors,
                [],
                [],
                [],
                [],
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.Resolve),
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.Validate),
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.TailwindGenerate));
        }

        var resolutionResult = _referenceResolver.Resolve(normalizeResult.Registry);
        var resolutionErrors = PrefixVariantDiagnostics(
            variant,
            resolutionResult.Errors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Resolve, x.Message, x.SourcePath)).ToArray());
        var dependencies = BuildDependencyGraph(resolutionResult.Registry);
        var advisoryDiagnostics = BuildAdvisoryDiagnostics(resolutionResult.Registry, mergeResult.SourceTraces, dependencies);
        var variantInfos = PrefixVariantDiagnostics(variant, advisoryDiagnostics.Infos.ToArray());
        var resolveWarnings = PrefixVariantDiagnostics(variant, advisoryDiagnostics.Warnings.Where(x => x.Stage == DesignTokenDiagnosticStage.Resolve).ToArray());
        if (!resolutionResult.Success)
        {
            return new VariantInspectionResult(
                resolutionResult.Registry.All.Count,
                string.Empty,
                parseErrors,
                mergeErrors,
                normalisationErrors,
                resolutionErrors,
                [],
                [],
                variantInfos,
                resolveWarnings,
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.Validate),
                PrefixSkippedStageWarnings(variant, DesignTokenDiagnosticStage.TailwindGenerate));
        }

        var validationResult = _validator.Validate(resolutionResult.Registry);
        var validationErrors = PrefixVariantDiagnostics(
            variant,
            validationResult.Errors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Validate, x.Message, x.Path, x.Field)).ToArray());
        var validateWarnings = PrefixVariantDiagnostics(variant, advisoryDiagnostics.Warnings.Where(x => x.Stage == DesignTokenDiagnosticStage.Validate).ToArray());
        var tailwindWarnings = PrefixVariantDiagnostics(variant, advisoryDiagnostics.Warnings.Where(x => x.Stage == DesignTokenDiagnosticStage.TailwindGenerate).ToArray());
        if (!validationResult.Success)
        {
            return new VariantInspectionResult(
                validationResult.Registry.All.Count,
                string.Empty,
                parseErrors,
                mergeErrors,
                normalisationErrors,
                resolutionErrors,
                validationErrors,
                [],
                variantInfos,
                resolveWarnings,
                validateWarnings,
                tailwindWarnings);
        }

        var cssResult = _cssGenerator.Generate(validationResult.Registry, variant.Selector, includeHeader: false);
        var cssGenerationErrors = PrefixVariantDiagnostics(
            variant,
            cssResult.Errors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.CssGenerate, x.Message, x.Path)).ToArray());

        return new VariantInspectionResult(
            validationResult.Registry.All.Count,
            cssResult.Success ? cssResult.Css : string.Empty,
            parseErrors,
            mergeErrors,
            normalisationErrors,
            resolutionErrors,
            validationErrors,
            cssGenerationErrors,
            variantInfos,
            resolveWarnings,
            validateWarnings,
            tailwindWarnings);
    }

    private static IReadOnlyList<DesignTokenDiagnostic> PrefixVariantDiagnostics(
        DesignTokenThemeVariant variant,
        IReadOnlyList<DesignTokenDiagnostic> diagnostics)
    {
        return diagnostics
            .Select(x => new DesignTokenDiagnostic(
                x.Stage,
                $"Theme '{variant.Alias}': {x.Message}",
                x.TokenPath,
                x.Field))
            .ToArray();
    }

    private static IReadOnlyList<DesignTokenDiagnostic> PrefixSkippedStageWarnings(
        DesignTokenThemeVariant variant,
        DesignTokenDiagnosticStage firstSkippedStage)
    {
        return Enum.GetValues<DesignTokenDiagnosticStage>()
            .Where(x => x >= firstSkippedStage)
            .Select(x => new DesignTokenDiagnostic(x, $"Theme '{variant.Alias}': Stage skipped because an earlier stage failed."))
            .ToArray();
    }

    private static void MergeStageResult(
        List<DesignTokenPipelineStageResult> stages,
        DesignTokenDiagnosticStage stage,
        IReadOnlyList<DesignTokenDiagnostic> errors,
        IReadOnlyList<DesignTokenDiagnostic> warnings)
    {
        var existing = stages.FirstOrDefault(x => x.Stage == stage);
        if (existing is null || (errors.Count == 0 && warnings.Count == 0))
        {
            return;
        }

        stages.Remove(existing);
        stages.Add(new DesignTokenPipelineStageResult
        {
            Stage = stage,
            Success = existing.Success && errors.Count == 0,
            DurationMs = existing.DurationMs,
            Errors = existing.Errors.Concat(errors).ToArray(),
            Warnings = existing.Warnings.Concat(warnings).ToArray()
        });
    }

    private static IReadOnlyList<DesignTokenThemeVariantSummary> BuildThemeSummaries(IReadOnlyList<DesignTokenThemeVariant> variants) =>
        variants.Select(x => new DesignTokenThemeVariantSummary
        {
            Id = x.Id,
            Name = x.Name,
            Alias = x.Alias,
            Selector = x.Selector,
            IsDefault = x.IsDefault,
            VariantType = x.VariantType.ToString(),
            Enabled = x.Enabled
        }).ToArray();

    private static IReadOnlyList<DesignTokenDiagnostic> BuildThemeVariantDiagnostics(
        IReadOnlyList<DesignTokenThemeVariantSummary> variants,
        IReadOnlyList<string> unsupportedThemeVariants)
    {
        var diagnostics = new List<DesignTokenDiagnostic>();
        var enabledVariants = variants.Where(x => x.Enabled).Select(x => x.Alias).ToArray();

        diagnostics.Add(new DesignTokenDiagnostic(
            DesignTokenDiagnosticStage.Parse,
            $"Enabled theme variants: {string.Join(", ", enabledVariants)}."));

        foreach (var variant in variants)
        {
            diagnostics.Add(new DesignTokenDiagnostic(
                DesignTokenDiagnosticStage.Parse,
                variant.Enabled
                    ? $"Theme variant '{variant.Alias}': enabled, selector '{variant.Selector}', generated."
                    : $"Theme variant '{variant.Alias}': disabled, skipped."));
        }

        if (unsupportedThemeVariants.Count > 0)
        {
            diagnostics.Add(new DesignTokenDiagnostic(
                DesignTokenDiagnosticStage.Parse,
                $"Unsupported theme variants ignored: {string.Join(", ", unsupportedThemeVariants)}."));
        }

        return diagnostics;
    }

    private List<DesignTokenDependencyNode> BuildDependencyGraph(DesignTokenRegistry registry)
    {
        var outgoing = registry.All.ToDictionary(
            x => x.Path.Value,
            x => ExtractReferences(x).ToArray(),
            StringComparer.Ordinal);

        var incoming = registry.All.ToDictionary(
            x => x.Path.Value,
            _ => new List<string>(),
            StringComparer.Ordinal);

        foreach (var pair in outgoing)
        {
            foreach (var target in pair.Value.Where(incoming.ContainsKey))
            {
                incoming[target].Add(pair.Key);
            }
        }

        return registry.All
            .OrderBy(x => x.Path.Value, StringComparer.Ordinal)
            .Select(token =>
            {
                var refs = outgoing[token.Path.Value];
                return new DesignTokenDependencyNode
                {
                    TokenPath = token.Path.Value,
                    IncomingReferences = incoming[token.Path.Value].OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    OutgoingReferences = refs.Where(incoming.ContainsKey).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    UnresolvedReferences = refs.Where(x => !incoming.ContainsKey(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    SourceType = token.SourceType,
                    SourceName = token.SourceName
                };
            })
            .ToList();
    }

    private AdvisoryDiagnostics BuildAdvisoryDiagnostics(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenSourceTrace> sourceTraces,
        IReadOnlyList<DesignTokenDependencyNode> dependencies)
    {
        var warnings = new List<DesignTokenDiagnostic>();
        var infos = new List<DesignTokenDiagnostic>();
        var dependencyMap = dependencies.ToDictionary(x => x.TokenPath, StringComparer.Ordinal);

        foreach (var trace in sourceTraces.Where(x => x.OverriddenSources.Count > 0))
        {
            infos.Add(new DesignTokenDiagnostic(
                DesignTokenDiagnosticStage.SourceMerge,
                $"Token '{trace.TokenPath}' overrides {trace.OverriddenSources.Count} lower-priority source value(s).",
                trace.TokenPath));
        }

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            var dependency = dependencyMap[token.Path.Value];
            if (dependency.IncomingReferences.Count == 0)
            {
                infos.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Resolve,
                    "Token is not referenced by any other token.",
                    token.Path.Value));
            }

            if (token.SourceType == DesignTokenSourceType.Imported && dependency.IncomingReferences.Count == 0)
            {
                infos.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Resolve,
                    "Imported token is never referenced by another token.",
                    token.Path.Value));
            }

            if (token.SourceType == DesignTokenSourceType.CmsSemantic && dependency.OutgoingReferences.Count == 0)
            {
                infos.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Resolve,
                    "Semantic token uses a direct raw value instead of referencing another token.",
                    token.Path.Value));
            }

            if (token.SourceType == DesignTokenSourceType.Component && dependency.OutgoingReferences.Count == 0)
            {
                infos.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Resolve,
                    "Component token uses a direct raw value instead of referencing another token.",
                    token.Path.Value));
            }

            if (token.Type == DesignTokenType.Typography &&
                token.ResolvedValue is TypographyTokenValue typography &&
                typography.LetterSpacing is null)
            {
                warnings.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Validate,
                    "Typography token is missing optional field 'letterSpacing'.",
                    token.Path.Value,
                    "letterSpacing"));
            }

            if (!CanMapToTailwind(token))
            {
                infos.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.TailwindGenerate,
                    "Token does not have a supported Tailwind mapping.",
                    token.Path.Value));
            }
        }

        var duplicatePrimitiveValues = registry.All
            .Where(IsPrimitiveToken)
            .GroupBy(GetResolvedValueKey, StringComparer.Ordinal)
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1);

        foreach (var group in duplicatePrimitiveValues)
        {
            foreach (var token in group)
            {
                warnings.Add(new DesignTokenDiagnostic(
                    DesignTokenDiagnosticStage.Validate,
                    $"Primitive token shares duplicate resolved value '{group.Key}' with other token(s).",
                    token.Path.Value));
            }
        }

        return new AdvisoryDiagnostics(infos, warnings);
    }

    private List<DesignTokenDiagnosticViewModel> BuildTokenViews(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenSourceTrace> sourceTraces,
        IReadOnlyList<DesignTokenValidationError> validationErrors,
        IReadOnlyList<DesignTokenDiagnostic> infos,
        IReadOnlyList<DesignTokenDiagnostic> warnings,
        IReadOnlyList<DesignTokenDiagnostic> errors,
        IReadOnlyList<DesignTokenDependencyNode> dependencies,
        DesignTokenUsageScanResult? usageScan)
    {
        var validationLookup = validationErrors
            .GroupBy(x => x.Path, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Message).ToArray(), StringComparer.Ordinal);

        var errorLookup = errors
            .Where(x => !string.IsNullOrWhiteSpace(x.TokenPath))
            .GroupBy(x => x.TokenPath!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Message).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        var infoLookup = infos
            .Where(x => !string.IsNullOrWhiteSpace(x.TokenPath))
            .GroupBy(x => x.TokenPath!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Message).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        var warningLookup = warnings
            .Where(x => !string.IsNullOrWhiteSpace(x.TokenPath))
            .GroupBy(x => x.TokenPath!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Message).ToArray(), StringComparer.Ordinal);

        var dependencyLookup = dependencies.ToDictionary(x => x.TokenPath, StringComparer.Ordinal);
        var sourceTraceLookup = sourceTraces.ToDictionary(x => x.TokenPath, StringComparer.Ordinal);
        var unusedLookup = new HashSet<string>(usageScan?.UnusedTokenPaths ?? [], StringComparer.Ordinal);

        return registry.All
            .OrderBy(x => x.Path.Value, StringComparer.Ordinal)
            .Select(token =>
            {
                var cssInfo = GetCssInspection(token);
                var dependency = dependencyLookup[token.Path.Value];
                var sourceTrace = sourceTraceLookup[token.Path.Value];
                var resolutionTrace = BuildResolutionTrace(token.Path.Value, dependencyLookup);
                var tailwindMapped = CanMapToTailwind(token);

                return new DesignTokenDiagnosticViewModel
                {
                    Path = token.Path.Value,
                    Type = token.Type.ToString(),
                    SourceType = token.SourceType,
                    SourceName = token.SourceName,
                    SourcePriority = token.SourcePriority,
                    RawValue = SerializeValue(token.RawValue),
                    NormalizedValue = SerializeValue(token.NormalizedValue),
                    ResolvedValue = SerializeValue(token.ResolvedValue),
                    GeneratedCssVariableName = cssInfo.VariableName,
                    GeneratedCssValue = cssInfo.CssValue,
                    References = dependency.OutgoingReferences,
                    ReferencedBy = dependency.IncomingReferences,
                    Errors = errorLookup.GetValueOrDefault(token.Path.Value, []),
                    ValidationErrors = validationLookup.GetValueOrDefault(token.Path.Value, []),
                    Warnings = warningLookup.GetValueOrDefault(token.Path.Value, []),
                    Infos = infoLookup.GetValueOrDefault(token.Path.Value, []),
                    OverriddenSources = sourceTrace.OverriddenSources,
                    ResolutionTrace = resolutionTrace,
                    IsAdded = sourceTrace.WinningSource.SourceType != DesignTokenSourceType.Starter,
                    IsOverridden = sourceTrace.OverriddenSources.Count > 0,
                    IsUnused = unusedLookup.Contains(token.Path.Value),
                    HasGeneratedCss = !string.IsNullOrWhiteSpace(cssInfo.VariableName),
                    IsTailwindMapped = tailwindMapped,
                    IsTailwindSkipped = !tailwindMapped
                };
            })
            .ToList();
    }

    private (string? VariableName, string? CssValue) GetCssInspection(DesignToken token)
    {
        if (!DesignTokenCssVariableName.TryCreate(token, out var variableName, out _))
        {
            return (null, null);
        }

        var registry = new DesignTokenRegistry();
        registry.Add(token);
        var result = _cssGenerator.Generate(registry);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Css))
        {
            return (variableName, null);
        }

        var lines = result.Css
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.TrimStart().StartsWith("--", StringComparison.Ordinal))
            .Select(x => x.Trim().TrimEnd(';'))
            .ToArray();

        var cssValue = lines.Length == 1
            ? ExtractDeclarationValue(lines[0])
            : string.Join(Environment.NewLine, lines);

        return (variableName, cssValue);
    }

    private static IReadOnlyList<string> BuildResolutionTrace(
        string tokenPath,
        IReadOnlyDictionary<string, DesignTokenDependencyNode> dependencies)
    {
        var trace = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = tokenPath;

        while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
        {
            trace.Add(current);
            if (!dependencies.TryGetValue(current, out var dependency) || dependency.OutgoingReferences.Count == 0)
            {
                break;
            }

            current = dependency.OutgoingReferences[0];
        }

        return trace;
    }

    private void WriteDebugExports(
        DesignTokenRegistry mergedRegistry,
        DesignTokenRegistry resolvedRegistry,
        IReadOnlyList<DesignTokenDependencyNode> dependencies,
        string css,
        string tailwindJson,
        Dictionary<string, string> debugExports)
    {
        Directory.CreateDirectory(_options.OutputDirectory);

        WriteDebugFile("merged-token-snapshot.json", _jsonFormatter.FormatRegistry(mergedRegistry, useResolvedValues: false), debugExports);
        WriteDebugFile("resolved-token-snapshot.json", _jsonFormatter.FormatRegistry(resolvedRegistry, useResolvedValues: true), debugExports);
        WriteDebugFile("dependency-graph.json", JsonSerializer.Serialize(dependencies, new JsonSerializerOptions { WriteIndented = true }), debugExports);

        if (!string.IsNullOrWhiteSpace(css))
        {
            WriteDebugFile("generated-css-snapshot.css", css, debugExports);
        }

        if (!string.IsNullOrWhiteSpace(tailwindJson))
        {
            WriteDebugFile("generated-tailwind-snapshot.json", tailwindJson, debugExports);
        }
    }

    private void WriteDebugFile(string fileName, string contents, Dictionary<string, string> debugExports)
    {
        var path = Path.Combine(_options.OutputDirectory, fileName);
        File.WriteAllText(path, contents);
        debugExports[fileName] = path;
    }

    private static IEnumerable<string> ExtractReferences(DesignToken token)
    {
        var references = new List<string>();
        AddReference(references, token.RawValue as string);

        switch (token.NormalizedValue)
        {
            case DimensionTokenValue dimension:
                AddReference(references, dimension.Reference);
                break;
            case ResponsiveDimensionTokenValue responsive:
                AddReference(references, responsive.MobileReference);
                AddReference(references, responsive.TabletReference);
                AddReference(references, responsive.DesktopReference);
                break;
            case FontWeightTokenValue fontWeight:
                AddReference(references, fontWeight.Reference);
                break;
            case NumberTokenValue number:
                AddReference(references, number.Reference);
                break;
            case DurationTokenValue duration:
                AddReference(references, duration.Reference);
                break;
            case FontFamilyTokenValue fontFamily:
                AddReference(references, fontFamily.Value);
                break;
            case ColorTokenValue color:
                AddReference(references, color.Value);
                break;
            case TypographyTokenValue typography:
                AddReference(references, typography.FontFamily);
                AddReference(references, typography.FontWeightReference);
                AddReference(references, typography.FontSizeReference);
                AddReference(references, typography.LineHeightReference);
                AddReference(references, typography.LetterSpacingReference);
                break;
            case ShadowTokenValue shadow:
                AddReference(references, shadow.Color);
                AddReference(references, shadow.OffsetXReference);
                AddReference(references, shadow.OffsetYReference);
                AddReference(references, shadow.BlurReference);
                AddReference(references, shadow.SpreadReference);
                break;
            case BorderTokenValue border:
                AddReference(references, border.WidthReference);
                AddReference(references, border.Color);
                AddReference(references, border.Style);
                break;
        }

        return references.Distinct(StringComparer.Ordinal);
    }

    private static void AddReference(List<string> references, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (TryExtractReferencePath(value, out var path))
        {
            references.Add(path);
        }
    }

    private static bool TryExtractReferencePath(string value, out string path)
    {
        path = string.Empty;
        if (value.Length < 3 || value[0] != '{' || value[^1] != '}')
        {
            return false;
        }

        path = value[1..^1];
        return !string.IsNullOrWhiteSpace(path);
    }

    private static bool IsPrimitiveToken(DesignToken token) =>
        token.Type is DesignTokenType.Color or DesignTokenType.Dimension or DesignTokenType.FontFamily or DesignTokenType.FontWeight or DesignTokenType.Duration or DesignTokenType.Number;

    private static string GetResolvedValueKey(DesignToken token) =>
        SerializeValue(token.ResolvedValue) ?? string.Empty;

    private static bool CanMapToTailwind(DesignToken token)
    {
        var segments = token.Path.Segments;
        return token.Type switch
        {
            DesignTokenType.Color when segments.Count >= 2 => true,
            DesignTokenType.Dimension when segments.Count >= 2 &&
                                             (string.Equals(segments[0], "space", StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(segments[0], "spacing", StringComparison.OrdinalIgnoreCase)) => true,
            DesignTokenType.FontFamily when segments.Count >= 3 &&
                                              string.Equals(segments[0], "font", StringComparison.OrdinalIgnoreCase) &&
                                              string.Equals(segments[1], "family", StringComparison.OrdinalIgnoreCase) => true,
            DesignTokenType.FontWeight when segments.Count >= 3 &&
                                              string.Equals(segments[0], "font", StringComparison.OrdinalIgnoreCase) &&
                                              string.Equals(segments[1], "weight", StringComparison.OrdinalIgnoreCase) => true,
            DesignTokenType.Duration when segments.Count >= 2 => true,
            DesignTokenType.Number when segments.Count >= 2 &&
                                         string.Equals(segments[0], "opacity", StringComparison.OrdinalIgnoreCase) => true,
            DesignTokenType.Shadow when segments.Count >= 2 => true,
            DesignTokenType.Typography when segments.Count >= 2 => true,
            _ => false
        };
    }

    private static string? SerializeValue(object? value)
    {
        return value switch
        {
            null => null,
            string stringValue => stringValue,
            JsonElement jsonElement => jsonElement.GetRawText(),
            ColorTokenValue color => color.Value,
            FontFamilyTokenValue fontFamily => fontFamily.Value,
            FontWeightTokenValue fontWeight => fontWeight.Value?.ToString(),
            NumberTokenValue number => number.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DurationTokenValue duration when duration.Value is not null => $"{duration.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}{duration.Unit}",
            DimensionTokenValue dimension when dimension.Value is not null => dimension.Value == 0 ? "0" : $"{dimension.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}{dimension.Unit}",
            _ => JsonSerializer.Serialize(value)
        };
    }

    private static string ExtractDeclarationValue(string declaration)
    {
        var separatorIndex = declaration.IndexOf(": ", StringComparison.Ordinal);
        return separatorIndex >= 0
            ? declaration[(separatorIndex + 2)..]
            : declaration;
    }

    private DesignTokenDiagnosticsResult CreateResult(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenSourceTrace> sourceTraces,
        IReadOnlyList<DesignTokenDependencyNode> dependencies,
        IReadOnlyList<DesignTokenDiagnosticViewModel> tokens,
        IReadOnlyList<DesignTokenSourceSummary> sourceSummaries,
        DesignTokenSourceSummaryTotals sourceSummaryTotals,
        IReadOnlyList<DesignTokenMergeEvent> mergeEvents,
        IReadOnlyDictionary<string, string> debugExports,
        IReadOnlyList<DesignTokenPipelineStageResult> stageResults,
        IReadOnlyList<DesignTokenDiagnostic> infos,
        IReadOnlyList<DesignTokenDiagnostic> warnings,
        IReadOnlyList<DesignTokenDiagnostic> errors,
        string css,
        string tailwindJson,
        IReadOnlyList<DesignTokenThemeVariantSummary> themeVariants,
        int tokenCount,
        DesignTokenUsageScanResult? usageScan)
    {
        var enrichedSourceSummaries = EnrichSourceSummaries(sourceSummaries, tokens, infos, warnings, errors);
        var enrichedTotals = new DesignTokenSourceSummaryTotals
        {
            TotalTokensBeforeMerge = sourceSummaryTotals.TotalTokensBeforeMerge,
            TotalTokensAfterMerge = sourceSummaryTotals.TotalTokensAfterMerge,
            TokensAdded = sourceSummaryTotals.TokensAdded,
            TokensOverridden = sourceSummaryTotals.TokensOverridden,
            SamePriorityDuplicates = sourceSummaryTotals.SamePriorityDuplicates,
            DisabledSources = themeVariants.Count(x => !x.Enabled)
        };
        var mergedEvents = mergeEvents
            .Concat(themeVariants.Select(x => new DesignTokenMergeEvent
            {
                EventType = "ThemeVariant",
                Message = x.Enabled
                    ? $"Generated selector {x.Selector} for {x.Alias}."
                    : $"Skipped {x.Alias} theme because disabled.",
                SourceType = "ThemeVariant",
                SourceName = x.Alias
            }))
            .ToArray();

        return new DesignTokenDiagnosticsResult
        {
            Registry = registry,
            SourceTraces = sourceTraces,
            Dependencies = dependencies,
            Tokens = tokens,
            DebugExports = debugExports,
            CircularReferenceChains = ExtractCircularChains(errors),
            Css = css,
            TailwindJson = tailwindJson,
            ThemeVariants = themeVariants,
            UsageScan = usageScan,
            SourceSummaries = enrichedSourceSummaries,
            SourceSummaryTotals = enrichedTotals,
            MergeEvents = mergedEvents,
            DiagnosticsExport = CreateDiagnosticsExport(themeVariants, enrichedSourceSummaries, tokens, mergedEvents, infos, warnings, errors),
            BuildReport = new DesignTokenBuildReport
            {
                Success = errors.Count == 0,
                GeneratedCssPath = _cssWriter?.OutputPath,
                GeneratedTailwindPath = _tailwindWriter?.OutputPath,
                TokenCount = tokenCount <= 0 ? registry.All.Count : tokenCount,
                InfoCount = infos.Count,
                WarningCount = warnings.Count,
                ErrorCount = errors.Count,
                PipelineStages = stageResults,
                Infos = infos,
                Warnings = warnings,
                Errors = errors
            }
        };
    }

    private static IReadOnlyList<DesignTokenSourceSummary> EnrichSourceSummaries(
        IReadOnlyList<DesignTokenSourceSummary> sourceSummaries,
        IReadOnlyList<DesignTokenDiagnosticViewModel> tokens,
        IReadOnlyList<DesignTokenDiagnostic> infos,
        IReadOnlyList<DesignTokenDiagnostic> warnings,
        IReadOnlyList<DesignTokenDiagnostic> errors)
    {
        return sourceSummaries.Select(summary =>
        {
            var winningPaths = tokens
                .Where(token =>
                    token.SourceType == summary.SourceType &&
                    string.Equals(token.SourceName, summary.SourceName, StringComparison.Ordinal) &&
                    token.SourcePriority == summary.Priority)
                .Select(token => token.Path)
                .ToHashSet(StringComparer.Ordinal);

            return new DesignTokenSourceSummary
            {
                SourceType = summary.SourceType,
                SourceName = summary.SourceName,
                Priority = summary.Priority,
                Enabled = summary.Enabled,
                TokenCountBeforeMerge = summary.TokenCountBeforeMerge,
                TokenCountAfterMerge = summary.TokenCountAfterMerge,
                TokensOverriddenByHigherSource = summary.TokensOverriddenByHigherSource,
                ErrorCount = errors.Count(x => !string.IsNullOrWhiteSpace(x.TokenPath) && winningPaths.Contains(x.TokenPath!)),
                WarningCount = warnings.Count(x => !string.IsNullOrWhiteSpace(x.TokenPath) && winningPaths.Contains(x.TokenPath!)),
                InfoCount = infos.Count(x => !string.IsNullOrWhiteSpace(x.TokenPath) && winningPaths.Contains(x.TokenPath!))
            };
        }).ToArray();
    }

    private static DesignTokenDiagnosticsExport CreateDiagnosticsExport(
        IReadOnlyList<DesignTokenThemeVariantSummary> themeVariants,
        IReadOnlyList<DesignTokenSourceSummary> sourceSummaries,
        IReadOnlyList<DesignTokenDiagnosticViewModel> tokens,
        IReadOnlyList<DesignTokenMergeEvent> mergeEvents,
        IReadOnlyList<DesignTokenDiagnostic> infos,
        IReadOnlyList<DesignTokenDiagnostic> warnings,
        IReadOnlyList<DesignTokenDiagnostic> errors)
    {
        return new DesignTokenDiagnosticsExport
        {
            Themes = themeVariants
                .Select(x => (object)new
                {
                    x.Alias,
                    x.Name,
                    x.Selector,
                    x.Enabled,
                    x.IsDefault,
                    x.VariantType
                })
                .ToArray(),
            Sources = sourceSummaries
                .Select(x => (object)new
                {
                    SourceType = x.SourceType.ToString(),
                    x.SourceName,
                    x.Priority,
                    x.Enabled,
                    x.TokenCountBeforeMerge,
                    x.TokenCountAfterMerge,
                    x.TokensOverriddenByHigherSource,
                    x.ErrorCount,
                    x.WarningCount,
                    x.InfoCount
                })
                .ToArray(),
            Tokens = tokens
                .Select(x => (object)new
                {
                    x.Path,
                    x.Type,
                    Source = x.SourceType.ToString(),
                    x.SourceName,
                    x.SourcePriority,
                    x.RawValue,
                    x.NormalizedValue,
                    x.ResolvedValue,
                    CssVariable = x.GeneratedCssVariableName,
                    x.References,
                    x.ReferencedBy,
                    OverriddenSources = x.OverriddenSources.Select(source => new
                    {
                        SourceType = source.SourceType.ToString(),
                        source.SourceName,
                        source.SourcePriority,
                        source.TokenType,
                        source.RawValue
                    }).ToArray()
                })
                .ToArray(),
            MergeEvents = mergeEvents.Cast<object>().ToArray(),
            Infos = infos.Select(x => (object)new { Stage = x.Stage.ToString(), x.TokenPath, x.Field, x.Message }).ToArray(),
            Warnings = warnings.Select(x => (object)new { Stage = x.Stage.ToString(), x.TokenPath, x.Field, x.Message }).ToArray(),
            Errors = errors.Select(x => (object)new { Stage = x.Stage.ToString(), x.TokenPath, x.Field, x.Message }).ToArray()
        };
    }

    private static IReadOnlyList<DesignTokenDiagnostic> ToUsageDiagnostics(DesignTokenUsageScanResult usageScan)
    {
        return usageScan.Items.Select(item =>
        {
            var location = string.IsNullOrWhiteSpace(item.FilePath)
                ? string.Empty
                : $" [{item.FilePath}:{item.LineNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?"}]";
            return new DesignTokenDiagnostic(
                DesignTokenDiagnosticStage.Validate,
                $"{item.Message}{location}",
                item.TokenPath,
                item.Kind.ToString());
        }).ToArray();
    }

    private sealed record VariantInspectionResult(
        int TokenCount,
        string Css,
        IReadOnlyList<DesignTokenDiagnostic> ParseErrors,
        IReadOnlyList<DesignTokenDiagnostic> MergeErrors,
        IReadOnlyList<DesignTokenDiagnostic> NormalisationErrors,
        IReadOnlyList<DesignTokenDiagnostic> ResolutionErrors,
        IReadOnlyList<DesignTokenDiagnostic> ValidationErrors,
        IReadOnlyList<DesignTokenDiagnostic> CssGenerationErrors,
        IReadOnlyList<DesignTokenDiagnostic> Infos,
        IReadOnlyList<DesignTokenDiagnostic> ResolveWarnings,
        IReadOnlyList<DesignTokenDiagnostic> ValidateWarnings,
        IReadOnlyList<DesignTokenDiagnostic> TailwindWarnings)
    {
        public IReadOnlyList<DesignTokenDiagnostic> Errors =>
            ParseErrors
                .Concat(MergeErrors)
                .Concat(NormalisationErrors)
                .Concat(ResolutionErrors)
                .Concat(ValidationErrors)
                .Concat(CssGenerationErrors)
                .ToArray();

        public IReadOnlyList<DesignTokenDiagnostic> Warnings =>
            ResolveWarnings
                .Concat(ValidateWarnings)
                .Concat(TailwindWarnings)
                .ToArray();
    }

    private sealed record AdvisoryDiagnostics(
        IReadOnlyList<DesignTokenDiagnostic> Infos,
        IReadOnlyList<DesignTokenDiagnostic> Warnings);

    private static IReadOnlyList<IReadOnlyList<string>> ExtractCircularChains(IReadOnlyList<DesignTokenDiagnostic> errors)
    {
        return errors
            .Where(x => x.Message.Contains("Circular reference detected:", StringComparison.Ordinal))
            .Select(x =>
            {
                var chainText = x.Message.Split(':', 2)[1].Trim();
                return (IReadOnlyList<string>)chainText.Split(" -> ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            })
            .ToArray();
    }
}
