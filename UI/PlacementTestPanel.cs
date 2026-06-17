using MilSim.Data;

namespace MilSim.UI;

/// <summary>
/// Hidden developer tool. Press , (comma) to toggle. Lists every unit and building
/// from the Data/ folders so any of them can be picked for placement testing.
/// Picking only records the selection — actual placement is not wired up yet.
/// </summary>
public partial class PlacementTestPanel : Control
{
    private const string UnitDataDir     = "res://Data/Units";
    private const string BuildingDataDir = "res://Data/Buildings";

    public PackedScene SelectedScene { get; private set; }
    public string      SelectedName  { get; private set; }

    private Label _selectedLabel;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // only the inner panel captures clicks
        Visible     = false;
        BuildUi();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Comma)
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildUi()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        panel.SetAnchorsPreset(LayoutPreset.TopLeft);
        panel.Position = new Vector2(20, 20);
        AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 10);
        panel.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        root.AddChild(new Label { Text = "PLACEMENT TEST  —  press , to toggle" });

        _selectedLabel = new Label { Text = "Selected: none" };
        root.AddChild(_selectedLabel);

        root.AddChild(new HSeparator());

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 16);
        root.AddChild(columns);

        columns.AddChild(BuildColumn("UNITS", UnitDataDir, isBuilding: false));
        columns.AddChild(BuildColumn("BUILDINGS", BuildingDataDir, isBuilding: true));
    }

    private Control BuildColumn(string header, string dir, bool isBuilding)
    {
        var column = new VBoxContainer();
        column.AddChild(new Label { Text = header });

        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(190, 440) };
        column.AddChild(scroll);

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(list);

        var entries = new List<(string Name, PackedScene Scene)>();
        foreach (var path in GetTresFiles(dir))
        {
            string name;
            PackedScene scene;
            if (isBuilding)
            {
                var data = GD.Load<BuildingData>(path);
                if (data == null) continue;
                name  = data.BuildingName;
                scene = data.Scene;
            }
            else
            {
                var data = GD.Load<UnitData>(path);
                if (data == null) continue;
                name  = data.UnitName;
                scene = data.Scene;
            }
            entries.Add((name, scene));
        }

        entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        foreach (var (name, scene) in entries)
        {
            var btn = new Button { Text = name };
            btn.Pressed += () => OnEntryPressed(name, scene);
            list.AddChild(btn);
        }

        return column;
    }

    private void OnEntryPressed(string name, PackedScene scene)
    {
        SelectedName  = name;
        SelectedScene = scene;
        _selectedLabel.Text = $"Selected: {name}";
        GD.Print($"[PlacementTest] Selected '{name}' — placement not yet implemented.");
    }

    private static List<string> GetTresFiles(string dir)
    {
        var result = new List<string>();
        using var da = DirAccess.Open(dir);
        if (da == null) return result;

        da.ListDirBegin();
        for (string file = da.GetNext(); file != ""; file = da.GetNext())
        {
            if (da.CurrentIsDir()) continue;
            if (file.EndsWith(".tres")) result.Add($"{dir}/{file}");
        }
        da.ListDirEnd();
        return result;
    }
}
