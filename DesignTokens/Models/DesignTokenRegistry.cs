namespace Site.DesignTokens.Models;

public sealed class DesignTokenRegistry
{
    private readonly Dictionary<DesignTokenPath, DesignToken> _tokens = [];

    public IReadOnlyCollection<DesignToken> All => _tokens.Values;

    public void Add(DesignToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (!_tokens.TryAdd(token.Path, token))
        {
            throw new InvalidOperationException(
                $"A design token with path '{token.Path}' already exists.");
        }
    }

    public bool TryGet(DesignTokenPath path, out DesignToken? token)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _tokens.TryGetValue(path, out token);
    }

    public bool TryGet(string path, out DesignToken? token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _tokens.TryGetValue(new DesignTokenPath(path), out token);
    }
}
