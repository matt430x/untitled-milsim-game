using MilSim.Systems;

namespace MilSim.UI;

/// <summary>
/// Entry-point screen. Lets the player choose between the real gameplay loop and the
/// test realm. Both load the same Game scene; the choice only flips TestMode, which
/// gates dev powers (selecting enemies, the spawn panel, selling anything).
/// </summary>
public partial class MainMenu : Control
{
    private const string GameScene = "res://Scenes/Game.tscn";

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        BuildUi();
    }

    private void BuildUi()
    {
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 14);
        center.AddChild(column);

        var title = new Label
        {
            Text                = "UNTITLED MILSIM GAME",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        column.AddChild(title);

        column.AddChild(MakeButton("Start Game",  () => Launch(testMode: false)));
        column.AddChild(MakeButton("Test Realm",  () => Launch(testMode: true)));
        column.AddChild(MakeButton("Quit",        () => GetTree().Quit()));
    }

    private Button MakeButton(string text, Action onPressed)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(220, 40) };
        btn.Pressed += onPressed;
        return btn;
    }

    private void Launch(bool testMode)
    {
        TestMode.Enabled = testMode;
        GetTree().ChangeSceneToFile(GameScene);
    }
}
