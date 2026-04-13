using Godot;

namespace Stamps.StampsCode.Stamps;

public static class DefaultStamps
{
    public static IEnumerable<StampDefinition> All =>
    [
        Arrow,
        Circle,
        Cross,
        Exclamation,
        Heart,
        Question,
        Star,
    ];

    public static readonly StampDefinition Arrow = new("Arrow",
    [
        new StampStroke(
        [
            new Vector2(  0f,   0f),
            new Vector2( 15f, -20f),
            new Vector2(  8f, -20f),
            new Vector2(  8f, -32f),
            new Vector2( -8f, -32f),
            new Vector2( -8f, -20f),
            new Vector2(-15f, -20f),
            new Vector2(  0f,   0f),
        ]),
    ]);

    public static readonly StampDefinition Circle = new("Circle",
    [
        new StampStroke(
        [
            new Vector2(   0f, -40f),
            new Vector2(15.3f, -37f),
            new Vector2(28.3f, -28.3f),
            new Vector2(  37f, -15.3f),
            new Vector2(  40f,   0f),
            new Vector2(  37f,  15.3f),
            new Vector2(28.3f,  28.3f),
            new Vector2(15.3f,  37f),
            new Vector2(   0f,  40f),
            new Vector2(-15.3f,  37f),
            new Vector2(-28.3f,  28.3f),
            new Vector2( -37f,  15.3f),
            new Vector2( -40f,   0f),
            new Vector2( -37f, -15.3f),
            new Vector2(-28.3f, -28.3f),
            new Vector2(-15.3f, -37f),
            new Vector2(   0f, -40f),
        ]),
    ]);

    public static readonly StampDefinition Cross = new("Cross",
    [
        new StampStroke(
        [
            new Vector2(-30f, -35f),
            new Vector2(  0f,   0f),
            new Vector2( 30f,  35f),
        ]),
        new StampStroke(
        [
            new Vector2( 30f, -35f),
            new Vector2(  0f,   0f),
            new Vector2(-30f,  35f),
        ]),
    ]);

    public static readonly StampDefinition Exclamation = new("Exclamation",
    [
        new StampStroke(
        [
            new Vector2(0f, -28f),
            new Vector2(0f,  12f),
        ]),
        new StampStroke(
        [
            new Vector2(0f, 24f),
            new Vector2(0f, 25f),
        ]),
    ]);

    public static readonly StampDefinition Heart = new("Heart",
    [
        new StampStroke(
        [
            new Vector2(  0f,  22f),
            new Vector2(-12f,  12f),
            new Vector2(-22f,   2f),
            new Vector2(-25f,  -8f),
            new Vector2(-23f, -17f),
            new Vector2(-17f, -23f),
            new Vector2(-10f, -24f),
            new Vector2( -4f, -21f),
            new Vector2(  0f, -16f),
            new Vector2(  4f, -21f),
            new Vector2( 10f, -24f),
            new Vector2( 17f, -23f),
            new Vector2( 23f, -17f),
            new Vector2( 25f,  -8f),
            new Vector2( 22f,   2f),
            new Vector2( 12f,  12f),
            new Vector2(  0f,  22f),
        ]),
    ]);

    public static readonly StampDefinition Question = new("Question",
    [
        new StampStroke(
        [
            new Vector2(-13f, -15f),
            new Vector2(-11f, -23f),
            new Vector2( -5f, -28f),
            new Vector2(  3f, -29f),
            new Vector2( 11f, -25f),
            new Vector2( 14f, -17f),
            new Vector2( 11f,  -9f),
            new Vector2(  4f,  -3f),
            new Vector2(  0f,   4f),
            new Vector2(  0f,  12f),
        ]),
        new StampStroke(
        [
            new Vector2(0f, 24f),
            new Vector2(0f, 25f),
        ]),
    ]);

    public static readonly StampDefinition Star = new("Star",
    [
        new StampStroke(
        [
            new Vector2(  0f, -25f),
            new Vector2(  6f,  -8f),
            new Vector2( 24f,  -8f),
            new Vector2( 10f,   4f),
            new Vector2( 15f,  22f),
            new Vector2(  0f,  12f),
            new Vector2(-15f,  22f),
            new Vector2(-10f,   4f),
            new Vector2(-24f,  -8f),
            new Vector2( -6f,  -8f),
            new Vector2(  0f, -25f),
        ]),
    ]);
}
