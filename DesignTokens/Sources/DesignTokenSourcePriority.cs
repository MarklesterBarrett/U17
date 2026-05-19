namespace Site.DesignTokens.Sources;

public static class DesignTokenSourcePriority
{
    public const int Starter = 1;
    public const int Imported = 2;
    public const int CmsPrimitive = 3;
    public const int CmsSemantic = 4;
    public const int Component = 5;

    public static int GetDefault(DesignTokenSourceType sourceType)
    {
        return sourceType switch
        {
            DesignTokenSourceType.Starter => Starter,
            DesignTokenSourceType.Imported => Imported,
            DesignTokenSourceType.CmsPrimitive => CmsPrimitive,
            DesignTokenSourceType.CmsSemantic => CmsSemantic,
            DesignTokenSourceType.Component => Component,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null)
        };
    }
}
