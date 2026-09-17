#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Missbehave.Editor;

/// <summary>One running <see cref="BehaviorTreeRunner"/> the game has announced.</summary>
public sealed class RunnerInfo {
    public long Id { get; init; }
    public string TreePath { get; init; }
    public string ActorName { get; init; }
    public string[] IdTable { get; init; }
}

/// <summary>
/// Decodes the debug messages coming back from the running game and drives the panel with them.
/// <para>
/// Kept out of <see cref="MissbehaveDebuggerPlugin"/> on purpose: an EditorDebuggerPlugin cannot be
/// constructed outside the editor, so the protocol handling would otherwise be untestable. It holds
/// no reference to the panel or to callbacks — the panel is passed in per call — because the plugin
/// recreates this object after an assembly reload and such references would silently be gone.
/// </para>
/// </summary>
public sealed class MissbehaveDebugRouter {
    public const string Prefixed = MissbehaveDebug.Prefix + ":";

    readonly Dictionary<long, RunnerInfo> _runners = [];

    public long Selected { get; private set; } = -1;

    /// <summary>
    /// The two directions of the debugger channel disagree about the prefix — a game-side capture
    /// callback gets the bare "watch_path", while the editor side is handed the full
    /// "missbehave:register" — so both forms are accepted everywhere.
    /// </summary>
    static string KeyOf(string message) => message.StartsWith(Prefixed) ? message[Prefixed.Length..] : message;

    /// <summary>True for the message a runner sends when it comes up, which is what starts the handshake.</summary>
    public static bool IsRegistration(string message) => KeyOf(message) == "register";

    /// <summary>True when runners came or went, so the game has to be told which one is watched now.</summary>
    public static bool ChangesRunners(string message) => KeyOf(message) is "register" or "unregister";

    public bool Handle(string message, Godot.Collections.Array data, BehaviorTreeEditorPanel panel) {
        switch (KeyOf(message)) {
            case "register":
                return Register(data, panel);

            case "unregister":
                return Unregister(data, panel);

            case "frame":
                return Frame(data, panel);

            default:
                return false;
        }
    }

    bool Register(Godot.Collections.Array data, BehaviorTreeEditorPanel panel) {
        if (data.Count < 4) return false;

        var info = new RunnerInfo {
            Id = data[0].AsInt64(),
            TreePath = data[1].AsString(),
            ActorName = data[2].AsString(),
            IdTable = data[3].AsStringArray(),
        };

        _runners[info.Id] = info;
        MissesRunners = false;
        Retarget(panel);
        return true;
    }

    bool Unregister(Godot.Collections.Array data, BehaviorTreeEditorPanel panel) {
        if (data.Count < 1) return false;

        _runners.Remove(data[0].AsInt64());
        Retarget(panel);
        return true;
    }

    /// <summary>
    /// Makes sure the watched runner is one that exists and runs the open tree, and refreshes the
    /// instance picker. Needed whenever runners come or go or another tree is opened: when the
    /// watched enemy dies, the next one of the same tree takes over rather than nothing at all.
    /// The caller tells the game about <see cref="Selected"/> afterwards.
    /// </summary>
    public void Retarget(BehaviorTreeEditorPanel panel) {
        var treePath = panel?.Tree?.ResourcePath ?? "";
        var previous = Selected;

        if (!Keeps(Selected, treePath)) Selected = Pick(treePath);
        if (Selected != previous) panel?.ClearStatuses();
        panel?.OnRunnersChanged([.. _runners.Values], Selected);
    }

    bool Keeps(long id, string treePath) {
        if (!_runners.TryGetValue(id, out var info)) return false;
        return string.IsNullOrEmpty(treePath) || info.TreePath == treePath || !_runners.Values.Any(r => r.TreePath == treePath);
    }

    long Pick(string treePath) {
        var matching = _runners.Values.FirstOrDefault(r => string.IsNullOrEmpty(treePath) || r.TreePath == treePath)
                       ?? _runners.Values.FirstOrDefault();
        return matching?.Id ?? -1;
    }

    /// <summary>
    /// True once a frame arrived from a runner this router has never been told about. The game only
    /// announces its runners when they start, so this is how a router that lost them finds out —
    /// the editor reloads its assembly on its own whenever a newer build appears, mid-game included,
    /// and the router is rebuilt empty. The caller asks the game to announce its runners again.
    /// </summary>
    public bool MissesRunners { get; private set; }

    bool Frame(Godot.Collections.Array data, BehaviorTreeEditorPanel panel) {
        if (data.Count < 2) return false;

        var id = data[0].AsInt64();
        if (!_runners.TryGetValue(id, out var info)) {
            MissesRunners = true;
            return true;
        }
        if (Selected >= 0 && id != Selected) return true;

        panel?.ShowFrame(info.IdTable, data[1].AsByteArray());
        return true;
    }

    public void Select(long runnerId, BehaviorTreeEditorPanel panel) {
        Selected = runnerId;
        panel?.ClearStatuses();
    }

    public void Reset(BehaviorTreeEditorPanel panel) {
        _runners.Clear();
        Selected = -1;
        MissesRunners = false;
        panel?.ClearStatuses();
        panel?.OnRunnersChanged([], -1);
    }
}
#endif
