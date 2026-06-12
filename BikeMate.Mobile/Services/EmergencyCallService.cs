namespace BikeMate.Services;

internal interface IEmergencyCallService
{
    Task StartCallAsync(int requestId, CancellationToken cancellationToken = default);
    Task EndCallAsync(int requestId, CancellationToken cancellationToken = default);
    Task<bool> ToggleMuteAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleCameraAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleSpeakerAsync(CancellationToken cancellationToken = default);
}

internal sealed class EmergencyCallService : IEmergencyCallService
{
    private bool _isMuted;
    private bool _isCameraEnabled = true;
    private bool _isSpeakerEnabled = true;

    public Task StartCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate a real provider such as WebRTC, Agora, or Twilio for emergency audio/video.
        return Task.CompletedTask;
    }

    public Task EndCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ToggleMuteAsync(CancellationToken cancellationToken = default)
    {
        _isMuted = !_isMuted;
        return Task.FromResult(_isMuted);
    }

    public Task<bool> ToggleCameraAsync(CancellationToken cancellationToken = default)
    {
        _isCameraEnabled = !_isCameraEnabled;
        return Task.FromResult(_isCameraEnabled);
    }

    public Task<bool> ToggleSpeakerAsync(CancellationToken cancellationToken = default)
    {
        _isSpeakerEnabled = !_isSpeakerEnabled;
        return Task.FromResult(_isSpeakerEnabled);
    }
}
