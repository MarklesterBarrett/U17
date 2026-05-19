using Site.DesignTokens.Css;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Tailwind;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenOutputWriter : IDesignTokenOutputWriter
{
    private static readonly object WriteSync = new();
    private readonly IDesignTokenCssWriter _cssWriter;
    private readonly IDesignTokenTailwindWriter _tailwindWriter;
    private readonly DesignTokenManagementOptions _options;

    public DesignTokenOutputWriter(
        IDesignTokenCssWriter cssWriter,
        IDesignTokenTailwindWriter tailwindWriter,
        DesignTokenManagementOptions? options = null)
    {
        _cssWriter = cssWriter;
        _tailwindWriter = tailwindWriter;
        _options = options ?? new DesignTokenManagementOptions();
    }

    public IReadOnlyList<DesignTokenDiagnostic> Write(string css, string tailwindJson)
    {
        lock (WriteSync)
        {
            var cssSnapshot = FileSnapshot.Create(_cssWriter.OutputPath);
            var tailwindSnapshot = FileSnapshot.Create(_tailwindWriter.OutputPath);

            try
            {
                _cssWriter.Write(css);
            }
            catch (Exception exception)
            {
                cssSnapshot.Restore();
                return [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.CssWrite, exception.Message)];
            }

            if (!_options.EnableTailwindOutput)
            {
                return [];
            }

            try
            {
                _tailwindWriter.Write(tailwindJson);
            }
            catch (Exception exception)
            {
                cssSnapshot.Restore();
                tailwindSnapshot.Restore();
                return [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.TailwindWrite, exception.Message)];
            }

            return [];
        }
    }

    private sealed class FileSnapshot
    {
        private FileSnapshot(string path, bool existed, string? contents)
        {
            _path = path;
            _existed = existed;
            _contents = contents;
        }

        private readonly string _path;
        private readonly bool _existed;
        private readonly string? _contents;

        public static FileSnapshot Create(string path) =>
            File.Exists(path)
                ? new FileSnapshot(path, true, File.ReadAllText(path))
                : new FileSnapshot(path, false, null);

        public void Restore()
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (_existed)
            {
                File.WriteAllText(_path, _contents ?? string.Empty);
                return;
            }

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
