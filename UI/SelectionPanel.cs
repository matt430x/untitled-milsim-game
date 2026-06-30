using MilSim.Autoloads;
using MilSim.Entities.Buildings;
using MilSim.Entities.Units;
using MilSim.Systems;

namespace MilSim.UI;

public partial class SelectionPanel : Control
{
    private SelectionManager _selectionManager;
    private HFlowContainer   _flow;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.BottomLeft);
        OffsetRight = 300f;
        OffsetTop   = -110f;
        MouseFilter = MouseFilterEnum.Stop;
        Visible     = false;

        var panel  = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 6);
        panel.AddChild(margin);

        _flow = new HFlowContainer();
        _flow.AddThemeConstantOverride("h_separation", 4);
        _flow.AddThemeConstantOverride("v_separation", 4);
        margin.AddChild(_flow);

        _selectionManager = GetParent().GetNodeOrNull<SelectionManager>("SelectionManager");

        EventBus.OnSelectionChanged += OnSelectionChanged;
    }

    public override void _ExitTree()
    {
        EventBus.OnSelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(List<int> _) => Rebuild();

    private void Rebuild()
    {
        foreach (Node child in _flow.GetChildren())
            child.QueueFree();

        var selection = _selectionManager?.CurrentSelection;
        if (selection == null || selection.Count == 0)
        {
            Visible = false;
            return;
        }

        Visible = true;
        foreach (var s in selection)
        {
            if (s is Node n && !GodotObject.IsInstanceValid(n)) continue;

            string label = s switch
            {
                BaseUnit     u => u.Data?.UnitName     ?? (s as Node)?.Name ?? "Unit",
                BaseBuilding b => b.Data?.BuildingName ?? (s as Node)?.Name ?? "Building",
                _              => (s as Node)?.Name ?? "?",
            };

            var captured = s;
            var btn = new Button { Text = label, CustomMinimumSize = new Vector2(80f, 28f) };
            btn.Pressed += () => _selectionManager?.DeselectEntity(captured);
            _flow.AddChild(btn);
        }
    }
}
