namespace MilSim.Systems;

/// <summary>
/// Global registry of all selectable entities currently in the world.
/// Units and buildings call Register/_Unregister in their _Ready/_ExitTree.
/// SelectionManager queries this instead of scanning the scene tree.
/// </summary>
public static class SelectionRegistry
{
    private static readonly List<ISelectable> _selectables = new();

    public static void Register(ISelectable selectable)   => _selectables.Add(selectable);
    public static void Unregister(ISelectable selectable) => _selectables.Remove(selectable);

    public static IReadOnlyList<ISelectable> All => _selectables;
}
