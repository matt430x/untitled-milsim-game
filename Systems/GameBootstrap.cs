using MilSim.Autoloads;
using MilSim.Entities.Buildings;
using MilSim.Entities.Crystals;
using MilSim.World;

namespace MilSim.Systems;

public partial class GameBootstrap : Node
{
    private static readonly PackedScene HqScene      = GD.Load<PackedScene>("res://Scenes/Buildings/Headquarters.tscn");
    private static readonly PackedScene CrystalScene = GD.Load<PackedScene>("res://Scenes/Crystals/Crystal.tscn");

    public override void _Ready()
    {
        var settings = GameManager.Instance?.CurrentSettings ?? new GameSettings();

        int count = settings.GameMode switch
        {
            GameMode.TwoVsTwo           => 4,
            GameMode.TwoVsTwoVsTwo      => 6,
            GameMode.TwoVsTwoVsTwoVsTwo => 8,
            GameMode.ThreeVsThree       => 6,
            GameMode.FourVsFour         => 8,
            _                           => Mathf.Max(settings.PlayerCount, 2),
        };

        MapGenerator.Generate(settings.MapPreset ?? MapPreset.Continents, settings.MapSize, (int)GD.Randi(), count);

        float money = settings.StartingMoney;

        for (int i = 1; i <= count; i++)
        {
            int teamId = settings.GameMode switch
            {
                GameMode.TwoVsTwo           => (i - 1) / 2 + 1,
                GameMode.TwoVsTwoVsTwo      => (i - 1) / 2 + 1,
                GameMode.TwoVsTwoVsTwoVsTwo => (i - 1) / 2 + 1,
                GameMode.ThreeVsThree       => (i - 1) / 3 + 1,
                GameMode.FourVsFour         => (i - 1) / 4 + 1,
                _                           => i,
            };

            PlayerManager.Instance.RegisterPlayer(new PlayerContext
            {
                PlayerId      = i,
                TeamId        = teamId,
                DisplayName   = i == 1 ? "Player" : $"Opponent {i - 1}",
                IsLocalPlayer = i == 1,
                IsAI          = i != 1,
                Money         = money,
                HqCount       = 1,
            });
        }

        SpawnCrystals();
        SpawnHqsOnSuperCrystals(count);
        FocusCameraOnLocalSpawn();

        GameManager.Instance?.OnMatchLoaded();
    }

    // Scatter standard crystals across the map.
    private void SpawnCrystals()
    {
        var parent = GetTree().Root.GetNode<Node3D>("Game/World/Crystals");
        foreach (var tile in MapData.CrystalPositions)
        {
            var crystal = CrystalScene.Instantiate<CrystalNode>();
            crystal.GlobalPosition = TileCenter(tile);
            parent.AddChild(crystal);
        }
    }

    // Each spawn point gets a super crystal first, then an HQ placed on top.
    private void SpawnHqsOnSuperCrystals(int count)
    {
        var buildings = GetTree().Root.GetNode<Node3D>("Game/World/Buildings");
        var crystals  = GetTree().Root.GetNode<Node3D>("Game/World/Crystals");

        for (int i = 0; i < count; i++)
        {
            var tile = i < MapData.SpawnPoints.Length ? MapData.SpawnPoints[i] : MapData.SpawnPoints[0];
            var pos  = TileCenter(tile);

            var superCrystal = CrystalScene.Instantiate<CrystalNode>();
            superCrystal.Type           = CrystalType.Super;
            superCrystal.GlobalPosition = pos;
            crystals.AddChild(superCrystal);

            var hq = HqScene.Instantiate<BaseBuilding>();
            hq.OwnerId        = i + 1;
            hq.GlobalPosition = pos;
            buildings.AddChild(hq);
        }
    }

    private void FocusCameraOnLocalSpawn()
    {
        if (MapData.SpawnPoints.Length == 0) return;
        int localIndex = Mathf.Clamp(PlayerManager.Instance.LocalPlayerId - 1, 0, MapData.SpawnPoints.Length - 1);
        var cam = GetTree().Root.GetNode<GameCamera>("Game/Camera");
        cam.SetFocusPoint(TileCenter(MapData.SpawnPoints[localIndex]));
    }

    private static Vector3 TileCenter(Vector2I tile)
    {
        float y = MapData.IsReady ? MapData.GetWorldHeight(tile.X + 0.5f, tile.Y + 0.5f) : 0f;
        return new Vector3(tile.X + 0.5f, y, tile.Y + 0.5f);
    }
}
