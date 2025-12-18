using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace ToxicWasteOfTime.Services;

/// <summary>
/// Service to manage the Xbox 360 controller lifecycle.
/// </summary>
public class XboxControllerService : IDisposable
{
    private readonly ViGEmClient _client;
    private readonly IXbox360Controller _controller;
    private readonly Xbox360ControllerAPI _api;
    private bool _disposed = false;

    public XboxControllerService()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.Connect();
        _api = new Xbox360ControllerAPI(_controller);
    }

    public Xbox360ControllerAPI GetAPI() => _api;

    public IXbox360Controller GetController() => _controller;

    public void Dispose()
    {
        if (!_disposed)
        {
            _controller?.Disconnect();
            _client?.Dispose();
            _disposed = true;
        }
    }
}
