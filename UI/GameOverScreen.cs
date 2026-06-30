using MilSim.Autoloads;

namespace MilSim.UI;

public partial class GameOverScreen : Control
{
    private const string MenuScene = "res://Scenes/MainMenu.tscn";

    private Label _headline;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        BuildUi();
        EventBus.OnMatchEnded += OnMatchEnded;
    }

    public override void _ExitTree()
    {
        EventBus.OnMatchEnded -= OnMatchEnded;
    }

    private void BuildUi()
    {
        var backdrop = new ColorRect { Color = new Color(0f, 0f, 0f, 0.6f) };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.GrowHorizontal = GrowDirection.Both;
        panel.GrowVertical   = GrowDirection.Both;
        AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 40);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        margin.AddChild(vbox);

        _headline = new Label
        {
            Text                = "Game Over",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        vbox.AddChild(_headline);

        var btn = new Button
        {
            Text              = "Return to Menu",
            CustomMinimumSize = new Vector2(200f, 40f),
        };
        btn.Pressed += () => GetTree().ChangeSceneToFile(MenuScene);
        vbox.AddChild(btn);
    }

    private void OnMatchEnded(int winnerTeamId)
    {
        int localTeam = PlayerManager.Instance
            .GetPlayer(PlayerManager.Instance.LocalPlayerId)?.TeamId ?? -1;

        _headline.Text = winnerTeamId == localTeam ? "Victory!" : "Defeat!";
        Visible = true;
    }
}
