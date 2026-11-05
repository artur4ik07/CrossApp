using Core.Domain;
using Core.Dto;

namespace Core.Abstractions;

public interface IStreamOrderImporter
{
    IAsyncEnumerable<Order> LoadStreamAsync(
        string path,
        IProgress<ImportProgressInfo>? progress = null,
        CancellationToken ct = default);
}
