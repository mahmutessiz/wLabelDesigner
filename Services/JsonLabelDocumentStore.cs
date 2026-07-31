using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public sealed class JsonLabelDocumentStore : ILabelDocumentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<LabelDocument> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var document = await JsonSerializer.DeserializeAsync<LabelDocument>(
            stream,
            SerializerOptions,
            cancellationToken);

        return document ?? throw new InvalidDataException("The label template is empty or invalid.");
    }

    public async Task SaveAsync(
        string path,
        LabelDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous);

        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
    }
}
