namespace MilSim.Systems;

/// <summary>
/// True only in the prototype/test environment. Set by the dev PlacementTestPanel,
/// which is not present in a shipped game. Gates test-only powers such as selecting
/// enemy entities and selling anything that is selected.
/// </summary>
public static class TestMode
{
    public static bool Enabled { get; set; }
}
