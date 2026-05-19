namespace Site.DesignTokens.Management;

public interface IDesignTokenBuildStatusStore
{
    DesignTokenBuildStatusRecord? Get();

    void Save(DesignTokenBuildStatusRecord status);
}
