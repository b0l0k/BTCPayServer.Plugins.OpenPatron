using BTCPayServer.Plugins.OpenPatron.Models;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class BlockRenderContext
{
    public required BlockDefinition Block { get; init; }
    public required OpenPatronPublicViewModel Page { get; init; }
}
