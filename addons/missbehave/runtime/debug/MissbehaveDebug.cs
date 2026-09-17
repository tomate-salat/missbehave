using Godot;

namespace Missbehave;

/// <summary>
/// Runtime half of the live-debug channel. No-ops outside an editor-launched game, so shipped
/// builds pay nothing but a couple of branch predictions.
/// <para>
/// Only runners whose tree resource is the one currently open in the editor stream anything, and a
/// tick sends one batched status array rather than a message per node — the difference matters at
/// 60 Hz with a few dozen enemies. The bookkeeping itself lives in <see cref="DebugStream"/>.
/// </para>
/// </summary>
public static class MissbehaveDebug {
    public const string Prefix = "missbehave";

    static DebugStream _stream;
    static Callable _capture;

    static bool Active => !Engine.IsEditorHint() && OS.HasFeature("editor") && EngineDebugger.IsActive();

    public static void Register(BehaviorTreeRunner runner) {
        if (!Active) return;
        Stream.Register(runner);
    }

    public static void Unregister(BehaviorTreeRunner runner) {
        if (!Active) return;
        Stream.Unregister(runner);
    }

    public static void SendFrame(BehaviorTreeRunner runner) {
        if (!Active) return;
        Stream.SendFrame(runner);
    }

    static DebugStream Stream {
        get {
            if (_stream != null) return _stream;

            _stream = new DebugStream(
                (message, data) => EngineDebugger.SendMessage($"{Prefix}:{message}", data),
                Time.GetTicksMsec);

            // Registered lazily on the first runner, so the addon needs no autoload.
            _capture = Callable.From<string, Godot.Collections.Array, bool>(_stream.OnEditorMessage);
            EngineDebugger.RegisterMessageCapture(Prefix, _capture);
            return _stream;
        }
    }
}
