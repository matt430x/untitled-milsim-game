using MilSim.Autoloads;
using MilSim.Systems;

namespace MilSim.UI;

public partial class SingleplayerMenu : Control
{
    private const string GameScene = "res://Scenes/Game.tscn";
    private const string MenuScene = "res://Scenes/MainMenu.tscn";

    private static readonly (string Label, GameMode Mode, int FixedCount)[] Modes =
    {
        ("FFA",     GameMode.FFA,                 0),
        ("2v2",     GameMode.TwoVsTwo,            4),
        ("2v2v2",   GameMode.TwoVsTwoVsTwo,       6),
        ("2v2v2v2", GameMode.TwoVsTwoVsTwoVsTwo,  8),
        ("3v3",     GameMode.ThreeVsThree,        6),
        ("4v4",     GameMode.FourVsFour,          8),
    };

    private GameMode _selectedMode     = GameMode.FFA;
    private MapSize  _selectedMapSize  = MapSize.Medium;
    private int      _ffaPlayerCount   = 4;
    private Label    _playerCountLabel;
    private HBoxContainer _playerCountRow;

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

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 16);
        center.AddChild(col);

        col.AddChild(new Label
        {
            Text                = "SINGLEPLAYER",
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        col.AddChild(new HSeparator());
        col.AddChild(new Label { Text = "Game Mode" });

        var group   = new ButtonGroup();
        var modeRow = new HBoxContainer();
        modeRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(modeRow);

        foreach (var (label, mode, _) in Modes)
        {
            var captured = mode;
            var btn = new Button
            {
                Text              = label,
                ToggleMode        = true,
                ButtonGroup       = group,
                CustomMinimumSize = new Vector2(72f, 36f),
            };
            if (mode == GameMode.FFA) btn.ButtonPressed = true;
            btn.Toggled += (on) => { if (on) SelectMode(captured); };
            modeRow.AddChild(btn);
        }

        _playerCountRow = new HBoxContainer();
        _playerCountRow.AddThemeConstantOverride("separation", 8);
        col.AddChild(_playerCountRow);

        _playerCountRow.AddChild(new Label { Text = "Players:" });

        var minus = new Button { Text = "-", CustomMinimumSize = new Vector2(30f, 30f) };
        minus.Pressed += () => SetFfaCount(_ffaPlayerCount - 1);
        _playerCountRow.AddChild(minus);

        _playerCountLabel = new Label
        {
            Text                = _ffaPlayerCount.ToString(),
            CustomMinimumSize   = new Vector2(24f, 0f),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _playerCountRow.AddChild(_playerCountLabel);

        var plus = new Button { Text = "+", CustomMinimumSize = new Vector2(30f, 30f) };
        plus.Pressed += () => SetFfaCount(_ffaPlayerCount + 1);
        _playerCountRow.AddChild(plus);

        col.AddChild(new Label { Text = "Map Size" });

        var sizeGroup  = new ButtonGroup();
        var sizeRow    = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(sizeRow);

        foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
        {
            var captured = size;
            var btn = new Button
            {
                Text              = size.ToString(),
                ToggleMode        = true,
                ButtonGroup       = sizeGroup,
                CustomMinimumSize = new Vector2(80f, 36f),
            };
            if (size == MapSize.Medium) btn.ButtonPressed = true;
            btn.Toggled += (on) => { if (on) _selectedMapSize = captured; };
            sizeRow.AddChild(btn);
        }

        col.AddChild(new HSeparator());

        var nav = new HBoxContainer();
        nav.AddThemeConstantOverride("separation", 10);
        col.AddChild(nav);

        var back = new Button { Text = "Back", CustomMinimumSize = new Vector2(100f, 40f) };
        back.Pressed += () => GetTree().ChangeSceneToFile(MenuScene);
        nav.AddChild(back);

        var start = new Button { Text = "Start", CustomMinimumSize = new Vector2(100f, 40f) };
        start.Pressed += StartGame;
        nav.AddChild(start);
    }

    private void SelectMode(GameMode mode)
    {
        _selectedMode               = mode;
        _playerCountRow.Visible     = mode == GameMode.FFA;
    }

    private void SetFfaCount(int count)
    {
        _ffaPlayerCount        = Mathf.Clamp(count, 2, 8);
        _playerCountLabel.Text = _ffaPlayerCount.ToString();
    }

    private void StartGame()
    {
        int playerCount = _selectedMode == GameMode.FFA
            ? _ffaPlayerCount
            : Array.Find(Modes, m => m.Mode == _selectedMode).FixedCount;

        TestMode.Enabled = false;
        GameManager.Instance?.StartMatch(new GameSettings
        {
            GameMode    = _selectedMode,
            PlayerCount = playerCount,
            MapSize     = _selectedMapSize,
        });
        GetTree().ChangeSceneToFile(GameScene);
    }
}
