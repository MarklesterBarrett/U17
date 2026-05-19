namespace Site.DesignTokens.Parsing;

public interface IDesignTokenJsonParser
{
    DesignTokenParseResult Parse(string json);
}
