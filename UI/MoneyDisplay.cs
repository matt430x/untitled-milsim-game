using MilSim.Autoloads;

namespace MilSim.UI;

public partial class MoneyDisplay : Label
{
    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.TopRight);
        OffsetLeft              = -130f;
        OffsetRight             = -10f;
        OffsetTop               = 10f;
        OffsetBottom            = 36f;
        HorizontalAlignment     = HorizontalAlignment.Right;
        MouseFilter             = MouseFilterEnum.Ignore;

        EventBus.OnMoneyChanged += OnMoneyChanged;

        var player = PlayerManager.Instance.GetPlayer(PlayerManager.Instance.LocalPlayerId);
        Text = player != null ? $"$ {(int)player.Money}" : "$ 0";
    }

    public override void _ExitTree()
    {
        EventBus.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int playerId, float newAmount)
    {
        if (playerId != PlayerManager.Instance.LocalPlayerId) return;
        Text = $"$ {(int)newAmount}";
    }
}
