using MilSim.Data;
using MilSim.Entities.Buildings;
using MilSim.Entities.Buildings.Components;

namespace MilSim.UI;

/// <summary>
/// Popup menu that appears when the player right-clicks a friendly building that can
/// produce units. Shows available units with cost and a progress bar for the active
/// production. Click outside the panel to dismiss.
/// </summary>
public partial class BuildingProductionPopup : Control
{
    private PanelContainer    _panel;
    private Label             _title;
    private VBoxContainer     _unitList;
    private Label             _queueLabel;
    private ProgressBar       _progressBar;
    private Label             _progressLabel;

    private BaseBuilding      _building;
    private ProductionComponent _production;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible     = false;
        BuildUi();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        // Close if the building was destroyed
        if (!GodotObject.IsInstanceValid(_building))
        {
            Close();
            return;
        }

        if (_production != null)
        {
            _queueLabel.Text    = $"Queue: {_production.QueueCount}";
            _progressBar.Value  = _production.Progress;
            _progressLabel.Text = _production.IsProducing
                ? $"{(int)(_production.Progress * 100)}%"
                : "Idle";
        }
    }

    public void OpenFor(BaseBuilding building, Vector2 screenPos)
    {
        _building   = building;
        _production = building.GetNodeOrNull<ProductionComponent>("ProductionComponent");

        _title.Text = building.Data?.BuildingName ?? "Production";
        RebuildUnitList(building);

        // Position panel just to the right of the click point, clamped to viewport
        var vpSize = GetViewportRect().Size;
        Vector2 pos = screenPos + new Vector2(12f, -20f);
        pos.X = Math.Min(pos.X, vpSize.X - _panel.Size.X - 8f);
        pos.Y = Math.Clamp(pos.Y, 8f, vpSize.Y - _panel.Size.Y - 8f);
        _panel.Position = pos;

        Visible = true;
    }

    public void Close()
    {
        Visible     = false;
        _building   = null;
        _production = null;
    }

    // -------------------------------------------------------------------------

    private void BuildUi()
    {
        // Full-screen backdrop — catches any click outside the panel
        var backdrop = new Control { MouseFilter = MouseFilterEnum.Stop };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        backdrop.GuiInput += (InputEvent e) =>
        {
            if (e is InputEventMouseButton mb && mb.Pressed)
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
        };
        AddChild(backdrop);

        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        AddChild(_panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 10);
        _panel.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);

        var header = new HBoxContainer();
        _title = new Label { Text = "Production", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(_title);
        var closeBtn = new Button { Text = "X" };
        closeBtn.Pressed += Close;
        header.AddChild(closeBtn);
        root.AddChild(header);

        root.AddChild(new HSeparator());

        _unitList = new VBoxContainer();
        root.AddChild(_unitList);

        root.AddChild(new HSeparator());

        _queueLabel = new Label { Text = "Queue: 0" };
        root.AddChild(_queueLabel);

        _progressBar = new ProgressBar
        {
            MinValue                = 0,
            MaxValue                = 1,
            Value                   = 0,
            CustomMinimumSize       = new Vector2(180, 14),
            ShowPercentage          = false,
        };
        root.AddChild(_progressBar);

        _progressLabel = new Label { Text = "Idle" };
        root.AddChild(_progressLabel);
    }

    private void RebuildUnitList(BaseBuilding building)
    {
        foreach (Node child in _unitList.GetChildren())
            child.QueueFree();

        var units = building.Data?.ProducibleUnits;
        if (units == null || units.Count == 0)
        {
            _unitList.AddChild(new Label { Text = "(no units available)" });
            return;
        }

        foreach (var unitData in units)
        {
            if (unitData == null) continue;
            var btn = new Button { Text = $"{unitData.UnitName}  ${unitData.Cost}" };
            var captured = unitData;
            btn.Pressed += () => OnUnitPressed(captured);
            _unitList.AddChild(btn);
        }
    }

    private void OnUnitPressed(UnitData unitData)
    {
        if (_building == null || !GodotObject.IsInstanceValid(_building)) return;
        _building.QueueUnit(unitData); // money deducted inside QueueProduction
    }
}
