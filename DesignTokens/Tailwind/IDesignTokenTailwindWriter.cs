namespace Site.DesignTokens.Tailwind;

public interface IDesignTokenTailwindWriter
{
    string OutputPath { get; }

    void Write(string json);
}
