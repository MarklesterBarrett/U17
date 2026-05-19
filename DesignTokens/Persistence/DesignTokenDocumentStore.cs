using System.Text.Json;

namespace Site.DesignTokens.Persistence;

public sealed class DesignTokenDocumentStore : IDesignTokenDocumentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public DesignTokenDocumentStore(string? storePath = null)
    {
        StorePath = string.IsNullOrWhiteSpace(storePath)
            ? Path.Combine("App_Data", "design-token-documents.json")
            : storePath;
    }

    public string StorePath { get; }

    public DesignTokenDocument? GetActive() =>
        LoadDocuments()
            .Where(x => x.Status == DesignTokenDocumentStatus.Active)
            .OrderByDescending(x => x.UpdatedDateUtc)
            .FirstOrDefault();

    public DesignTokenDocument? GetById(Guid id) =>
        LoadDocuments().FirstOrDefault(x => x.Id == id);

    public DesignTokenDocument SaveDraft(DesignTokenDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var documents = LoadDocuments();
        var now = DateTime.UtcNow;
        var existingIndex = documents.FindIndex(x => x.Id == document.Id && document.Id != Guid.Empty);
        var existing = existingIndex >= 0 ? documents[existingIndex] : null;

        var savedDocument = new DesignTokenDocument
        {
            Id = existing?.Id ?? (document.Id == Guid.Empty ? Guid.NewGuid() : document.Id),
            Name = document.Name,
            Json = document.Json,
            Status = document.Status,
            CreatedDateUtc = existing?.CreatedDateUtc ?? (document.CreatedDateUtc == default ? now : document.CreatedDateUtc),
            UpdatedDateUtc = now,
            CreatedBy = existing?.CreatedBy ?? document.CreatedBy,
            UpdatedBy = document.UpdatedBy,
            Hash = document.Hash,
            Version = (existing?.Version ?? 0) + 1,
            ValidationSummary = document.ValidationSummary
        };

        if (existingIndex >= 0)
        {
            documents[existingIndex] = savedDocument;
        }
        else
        {
            documents.Add(savedDocument);
        }

        SaveDocuments(documents);
        return savedDocument;
    }

    public DesignTokenDocument Activate(Guid id, string? updatedBy = null)
    {
        var documents = LoadDocuments();
        var targetIndex = documents.FindIndex(x => x.Id == id);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException($"Design token document '{id}' was not found.");
        }

        var now = DateTime.UtcNow;
        for (var index = 0; index < documents.Count; index++)
        {
            var document = documents[index];
            if (document.Status == DesignTokenDocumentStatus.Active)
            {
                documents[index] = document with { Status = DesignTokenDocumentStatus.Archived, UpdatedDateUtc = now, UpdatedBy = updatedBy ?? document.UpdatedBy, Version = document.Version + 1 };
            }
        }

        var target = documents[targetIndex];
        var activated = target with { Status = DesignTokenDocumentStatus.Active, UpdatedDateUtc = now, UpdatedBy = updatedBy ?? target.UpdatedBy, Version = target.Version + 1 };
        documents[targetIndex] = activated;
        SaveDocuments(documents);
        return activated;
    }

    public DesignTokenDocument Archive(Guid id, string? updatedBy = null)
    {
        var documents = LoadDocuments();
        var targetIndex = documents.FindIndex(x => x.Id == id);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException($"Design token document '{id}' was not found.");
        }

        var document = documents[targetIndex];
        var archived = document with { Status = DesignTokenDocumentStatus.Archived, UpdatedDateUtc = DateTime.UtcNow, UpdatedBy = updatedBy ?? document.UpdatedBy, Version = document.Version + 1 };
        documents[targetIndex] = archived;
        SaveDocuments(documents);
        return archived;
    }

    public IReadOnlyList<DesignTokenDocument> List() =>
        LoadDocuments()
            .OrderByDescending(x => x.UpdatedDateUtc)
            .ThenByDescending(x => x.CreatedDateUtc)
            .ToArray();

    private List<DesignTokenDocument> LoadDocuments()
    {
        if (!File.Exists(StorePath))
        {
            return [];
        }

        var json = File.ReadAllText(StorePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<DesignTokenDocument>>(json, SerializerOptions) ?? [];
    }

    private void SaveDocuments(List<DesignTokenDocument> documents)
    {
        var directory = Path.GetDirectoryName(StorePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(documents, SerializerOptions);
        var tempPath = $"{StorePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(StorePath))
        {
            File.Replace(tempPath, StorePath, null);
            return;
        }

        File.Move(tempPath, StorePath);
    }
}
