using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Systems;

namespace MilSim.UI;

/// <summary>
/// Hidden developer tool. Press , (comma) to toggle. Lists every unit and building
/// from the Data/ folders so any of them can be picked for placement testing.
/// Picking only records the selection — actual placement is not wired up yet.
/// </summary>
public partial class PlacementTestPanel : Control
{
    private const string UnitDataDir      = "res://Data/Units";
    private const string BuildingDataDir  = "res://Data/Buildings";
    private const string CrystalSceneDir  = "res://Scenes/Crystals";

    public PackedScene SelectedScene { get; private set; }
    public string      SelectedName  { get; private set; }

    private Label _selectedLabel;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // only the inner panel captures clicks
        Visible     = false;

        // The main menu decides the mode. Outside the test realm this dev panel is
        // fully inert: no UI, no comma toggle.
        if (!TestMode.Enabled)
        {
            SetProcessUnhandledInput(false);
            return;
        }

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

        int localId    = PlayerManager.Instance?.LocalPlayerId ?? 1;
        int opponentId = localId == 1 ? 2 : 1;

        // One scroll box for the whole menu body so hovering + wheel scrolls the
        // list (and the ScrollContainer consumes the wheel, so the camera won't zoom).
        float maxHeight = GetViewportRect().Size.Y - 140f;
        var scroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(0, maxHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        root.AddChild(scroll);

        var sections = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(sections);

        sections.AddChild(new Label { Text = "— YOURS —" });

        var yours = new HBoxContainer();
        yours.AddThemeConstantOverride("separation", 16);
        sections.AddChild(yours);

        yours.AddChild(BuildColumn("UNITS", UnitDataDir, PlaceableKind.Unit, localId));
        yours.AddChild(BuildColumn("BUILDINGS", BuildingDataDir, PlaceableKind.Building, localId));
        yours.AddChild(BuildColumn("CRYSTALS", CrystalSceneDir, PlaceableKind.Crystal, localId));

        sections.AddChild(new HSeparator());
        sections.AddChild(new Label { Text = "— OPPONENT —" });

        var opponent = new HBoxContainer();
        opponent.AddThemeConstantOverride("separation", 16);
        sections.AddChild(opponent);

        opponent.AddChild(BuildColumn("UNITS", UnitDataDir, PlaceableKind.Unit, opponentId));
        opponent.AddChild(BuildColumn("BUILDINGS", BuildingDataDir, PlaceableKind.Building, opponentId));
    }

    private Control BuildColumn(string header, string dir, PlaceableKind kind, int ownerId)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(190, 0) };
        column.AddChild(new Label { Text = header });

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        column.AddChild(list);

        var entries = new List<(string Name, PackedScene Scene, BuildingData Building)>();
        string ext = kind == PlaceableKind.Crystal ? ".tscn" : ".tres";
        foreach (var path in GetFiles(dir, ext))
        {
            string name;
            PackedScene scene;
            BuildingData building = null;
            switch (kind)
            {
                case PlaceableKind.Building:
                    building = GD.Load<BuildingData>(path);
                    if (building == null) continue;
                    name  = building.BuildingName;
                    scene = building.Scene;
                    break;
                case PlaceableKind.Crystal:
                    scene = GD.Load<PackedScene>(path);
                    if (scene == null) continue;
                    name  = path.GetFile().GetBaseName();
                    break;
                default:
                    var data = GD.Load<UnitData>(path);
                    if (data == null) continue;
                    name  = data.UnitName;
                    scene = data.Scene;
                    break;
            }
            entries.Add((name, scene, building));
        }

        entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        foreach (var (name, scene, building) in entries)
        {
            var btn = new Button { Text = name };
            btn.Pressed += () => OnEntryPressed(name, scene, kind, building, ownerId);
            list.AddChild(btn);
        }

        return column;
    }

    private void OnEntryPressed(string name, PackedScene scene, PlaceableKind kind, BuildingData building, int ownerId)
    {
        SelectedName  = name;
        SelectedScene = scene;
        _selectedLabel.Text = $"Selected: {name} (P{ownerId})";
        EventBus.RaisePlacementRequested(new PlacementRequest
        {
            Scene    = scene,
            Kind     = kind,
            Building = building,
            OwnerId  = ownerId,
        });
    }

    private static List<string> GetFiles(string dir, string ext)
    {
        var result = new List<string>();
        using var da = DirAccess.Open(dir);
        if (da == null) return result;

        da.ListDirBegin();
        for (string file = da.GetNext(); file != ""; file = da.GetNext())
        {
            if (da.CurrentIsDir()) continue;
            if (file.EndsWith(ext)) result.Add($"{dir}/{file}");
        }
        da.ListDirEnd();
        return result;
    }
}
