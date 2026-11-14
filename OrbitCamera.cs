using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

// Camera orbită se mișcă în jurul unui punct numit Target.
// Azimutul o rotește stânga-dreapta, elevația sus-jos, iar distanța o apropie sau o depărtează.
public sealed class OrbitCamera : Camera
{
    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartTarget = new(0.0f, 1.7f, 0.0f);
    private const float StartDistance = 5.0f;
    private const float StartAzimuth = 90.0f;
    private const float StartElevation = 15.0f;

    // Alegem limitele distanței, pentru a nu ajunge chiar în punctul țintă.
    public const float MinDistance = 1.0f;
    public const float MaxDistance = 50.0f;
    public const float MaxElevation = 89.0f;

    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }
    public float Elevation { get; private set; }

    public float MouseSensitivity = 0.25f;   // grade / pixel, cu click stânga ținut
    public float KeyboardTurnSpeed = 75.0f; // grade / secundă, pentru săgeți
    public float ZoomStep = 0.65f;          // unități pentru un pas al rotiței
    public float PanSpeed = 3.0f;           // unități / secundă, bonus WASD

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "orbită";

    // Aflăm poziția camerei plecând de la țintă și folosind cele două unghiuri.
    // Practic, o așezăm pe o sferă imaginară cu raza Distance.
    public override Vector3 Position
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);
            Vector3 offset = new(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az));
            return Target + Distance * offset;
        }
    }

    // Camera trebuie să privească tot timpul spre țintă, chiar dacă mutăm ținta cu WASD.
    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // Rotim cu mouse-ul doar când ținem apăsat butonul stâng.
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth -= mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        // Înmulțim mișcarea cu dt ca să rotim la fel de repede pe orice calculator.
        float rotation = KeyboardTurnSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth += rotation;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth -= rotation;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += rotation;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= rotation;
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        // Rotița ne apropie sau ne depărtează de țintă, fără să modifice FOV-ul.
        Distance = Math.Clamp(Distance - mouse.ScrollDelta.Y * ZoomStep, MinDistance, MaxDistance);

        // Cu WASD mutăm ținta pe orizontală. Camera o urmează, păstrând distanța.
        Vector3 flatForward = new(Front.X, 0.0f, Front.Z);
        flatForward = Vector3.Normalize(flatForward);  // elevația < 90° garantează componenta XZ
        Vector3 right = Vector3.Normalize(Vector3.Cross(Front, WorldUp));
        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += flatForward;
        if (keyboard.IsKeyDown(Keys.S)) move -= flatForward;
        if (keyboard.IsKeyDown(Keys.D)) move += right;
        if (keyboard.IsKeyDown(Keys.A)) move -= right;
        if (move.LengthSquared > 0.0f)
        {
            float speed = PanSpeed * (keyboard.IsKeyDown(Keys.LeftShift) ? 2.5f : 1.0f);
            Target += Vector3.Normalize(move) * speed * dt;
        }
    }

    // Când trecem din FPS în orbită, alegem o țintă la 5 unități în față.
    // Din direcția inversă calculăm unghiurile orbitei, ca să păstrăm aceeași privire.
    public void MatchView(Vector3 eye, Vector3 viewDirection)
    {
        if (viewDirection.LengthSquared <= 0.0f)
            throw new ArgumentException("Direcția camerei nu poate fi nulă.", nameof(viewDirection));

        Vector3 front = Vector3.Normalize(viewDirection);
        Distance = StartDistance;
        Target = eye + Distance * front;
        Vector3 outward = -front;
        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(outward.Y, -1.0f, 1.0f))),
                               -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(outward.Z, outward.X));
    }

    public override void Reset()
    {
        Target = StartTarget;
        Distance = StartDistance;
        Azimuth = StartAzimuth;
        Elevation = StartElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut         : {Azimuth,6:F1}°";
        yield return $"Elevație       : {Elevation,6:F1}°";
        yield return $"Poziție        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
        yield return $"Front          : ({Front.X,6:F2}, {Front.Y,6:F2}, {Front.Z,6:F2})";
    }
}
