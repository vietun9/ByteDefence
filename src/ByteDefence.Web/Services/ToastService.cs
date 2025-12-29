using System.Timers;

namespace ByteDefence.Web.Services;

public record ToastItem(string Id, string Message, string Level);

public class ToastService
{
    private readonly List<ToastItem> _toasts = new();
    public IReadOnlyList<ToastItem> Toasts => _toasts;

    public event Action? OnToastsChanged;

    public void ShowSuccess(string message, int durationMs = 3000)
        => Show(message, "success", durationMs);

    public void ShowInfo(string message, int durationMs = 3000)
        => Show(message, "info", durationMs);

    private void Show(string message, string level, int durationMs)
    {
        var id = Guid.NewGuid().ToString();
        var toast = new ToastItem(id, message, level);
        _toasts.Add(toast);
        OnToastsChanged?.Invoke();

        var timer = new System.Timers.Timer(durationMs) { AutoReset = false };
        timer.Elapsed += (s, e) =>
        {
            Remove(id);
            timer.Dispose();
        };
        timer.Start();
    }

    public void Remove(string id)
    {
        var idx = _toasts.FindIndex(t => t.Id == id);
        if (idx >= 0)
        {
            _toasts.RemoveAt(idx);
            OnToastsChanged?.Invoke();
        }
    }
}
