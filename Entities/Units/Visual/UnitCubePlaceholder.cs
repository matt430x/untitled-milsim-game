namespace MilSim.Entities.Units.Visual;

/// <summary>
/// Draws an isometric cube as a placeholder until real unit sprites are added.
/// Anchor point (0,0) sits at the center of the ground footprint.
/// </summary>
public partial class UnitCubePlaceholder : Node2D
{
    [Export] public Color TopColor   { get; set; } = new Color(0.55f, 0.72f, 1.00f);
    [Export] public Color LeftColor  { get; set; } = new Color(0.30f, 0.48f, 0.80f);
    [Export] public Color RightColor { get; set; } = new Color(0.18f, 0.32f, 0.60f);

    // Half-extents of the ground diamond (must match the 2:1 isometric ratio)
    private const float W = 20f;  // half screen-width
    private const float H = 10f;  // half screen-height of diamond
    private const float S = 20f;  // side (vertical rise in screen-space)

    // 5 unique vertices used across all three faces
    //   A  = diamond top
    //   B  = diamond right
    //   C  = diamond front (bottom of top face / front edge of sides)
    //   D  = diamond left
    //   BG = ground right  (B shifted down by S)
    //   CG = ground front  (C shifted down by S)
    //   DG = ground left   (D shifted down by S)

    private static readonly Vector2 A  = new(  0, -H - S);
    private static readonly Vector2 B  = new(  W,    - S);
    private static readonly Vector2 C  = new(  0,  H - S);
    private static readonly Vector2 D  = new( -W,    - S);
    private static readonly Vector2 BG = new(  W,      0);
    private static readonly Vector2 CG = new(  0,      H);
    private static readonly Vector2 DG = new( -W,      0);

    private static readonly Color Outline = new(0f, 0f, 0f, 0.55f);

    public override void _Draw()
    {
        // Left face (darker)
        DrawPolygon(new[] { D, C, CG, DG }, new[] { LeftColor });
        // Right face (darkest)
        DrawPolygon(new[] { C, B, BG, CG }, new[] { RightColor });
        // Top face (lightest)
        DrawPolygon(new[] { A, B, C, D }, new[] { TopColor });

        // Silhouette edges
        DrawPolyline(new[] { A, B, BG, CG, DG, D, A }, Outline, 1.5f);
        DrawLine(B, C, Outline, 1.5f);
        DrawLine(C, CG, Outline, 1.5f);
    }
}
