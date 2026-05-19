namespace Site.DesignTokens.Css;

public interface IDesignTokenCssWriter
{
    string OutputPath { get; }

    void Write(string css);
}
