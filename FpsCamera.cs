using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

// Camera FPS (first-person): orientarea vine din două unghiuri Euler, yaw și pitch,
// controlate cu mouse-ul; deplasarea se face cu W A S D.
// În modul "mers" (implicit) camera rămâne la înălțimea ochilor; în modul "zbor liber"
// (free-fly, tasta G) se deplasează pe direcția privirii.
public class FpsCamera : Camera
{
    // --- Starea camerei ---------------------------------------------------------

    private Vector3 _position;
    public override Vector3 Position => _position;

    // Girația (yaw), în grade: rotația stânga-dreapta, în jurul axei Y. -90° = privire spre -Z.
    public float Yaw { get; private set; }

    // Tangajul (pitch), în grade: rotația sus-jos. Limitat la ±MaxPitch.
    public float Pitch { get; private set; }

    // Zbor liber (true) sau mers pe sol (false). Comutat cu tasta G (facultativ, TODO 5.7).
    public bool FreeFly { get; set; }

    // Baza camerei: trei vectori unitate, perpendiculari doi câte doi (secțiunea 5).
    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;

    public override Vector3 Front => _front;

    // --- Parametri ----------------------------------------------------------------

    public const float MaxPitch = 89.0f;               // grade; de ce nu 90, vezi secțiunea 5.3
    public float MoveSpeed = 3.0f;                     // unități de lume pe secundă
    public float SprintFactor = 2.5f;                  // Shift ținut apăsat
    public float MouseSensitivity = 0.1f;              // grade pe pixel
    public float FovStep = 2.0f;                       // grade pentru un pas al rotiței

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartPosition = new(0.0f, 1.7f, 6.0f);   // 1,7 m = înălțimea ochilor
    private const float StartYaw = -90.0f;
    private const float StartPitch = 0.0f;

    public FpsCamera()
    {
        Reset();
    }

    public override string Name => FreeFly ? "FPS – zbor liber" : "FPS – mers";

    // Matricea view: camera aflată în Position privește spre punctul Position + Front.
    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(_position, _position + _front, _up);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // --- 1. Orientarea: mouse-ul modifică yaw și pitch -------------------------

        // TODO 5.5 (a): deplasarea pe orizontală modifică yaw, cea pe verticală modifică pitch.
        //               Atenție: pe ecran Y crește în jos, iar pitch pozitiv înseamnă privire în sus.
        //               Apoi limitați pitch-ul la intervalul [-MaxPitch, MaxPitch].
        // La mouse, jos înseamnă Y pozitiv, de aceea scădem valoarea la pitch.
        // Limităm unghiul ca să nu răsturnăm camera când privim mult în sus sau în jos.
        Yaw += mouseDelta.X * MouseSensitivity;
        Pitch -= mouseDelta.Y * MouseSensitivity;
        Pitch = Math.Clamp(Pitch, -MaxPitch, MaxPitch);
        UpdateVectors();

        // --- 2. FOV: rotița mouse-ului --------------------------------------------

        // TODO 5.1 (c): rotița în sus (ScrollDelta.Y > 0) micșorează FOV-ul (efect de zoom)
        //               (folosiți ChangeFov din clasa Camera și FovStep)
        // Cu FOV mai mic pare că apropiem imaginea; nu schimbăm poziția camerei.
        ChangeFov(-mouse.ScrollDelta.Y * FovStep);

        // --- 3. Deplasarea: W A S D ---------------------------------------------------

        // TODO 5.6: direcția "înainte" (în modul mers: proiecția lui Front pe planul orizontal),
        //           apoi deplasarea cu viteză constantă, independentă de numărul de cadre pe secundă.
        //           Pași: (1) forward; (2) vectorul "move" = suma direcțiilor tastelor apăsate
        //           (W +forward, S -forward, D +_right, A -_right); (3) normalizați "move";
        //           (4) _position += move * viteză * dt (viteza crește de SprintFactor ori cu Shift stânga).

        // TODO 5.7 (facultativ): în zbor liber (FreeFly), "forward" este chiar _front,
        //           iar Space urcă și Ctrl stânga coboară (pe axa Y a lumii, WorldUp).
        // La mers ignorăm înclinarea privirii, ca W să nu ne ridice de pe sol.
        // În zbor folosim direcția întreagă și putem urca sau coborî separat.
        Vector3 forward = FreeFly ? _front : new Vector3(_front.X, 0.0f, _front.Z);
        forward = Vector3.Normalize(forward);

        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += forward;
        if (keyboard.IsKeyDown(Keys.S)) move -= forward;
        if (keyboard.IsKeyDown(Keys.D)) move += _right;
        if (keyboard.IsKeyDown(Keys.A)) move -= _right;

        if (FreeFly)
        {
            if (keyboard.IsKeyDown(Keys.Space)) move += WorldUp;
            if (keyboard.IsKeyDown(Keys.LeftControl)) move -= WorldUp;
        }

        // Normalizarea evită viteza mai mare pe diagonală; dt o face independentă de FPS.
        if (move.LengthSquared > 0.0f)
        {
            float speed = MoveSpeed * (keyboard.IsKeyDown(Keys.LeftShift) ? SprintFactor : 1.0f);
            _position += Vector3.Normalize(move) * speed * dt;
        }
    }

    // Recalculează baza camerei din yaw și pitch (unghiuri Euler -> vector de direcție).
    private void UpdateVectors()
    {
        // TODO 5.5 (b): Front din yaw și pitch (formula din secțiunea 5.2), apoi Right și Up
        //               prin produse vectoriale; toți trei normalizați.
        //               Până atunci camera privește mereu spre -Z.
        // Din unghiuri aflăm unde privește camera; produsele vectoriale dau dreapta și sus.
        float yawRadians = MathHelper.DegreesToRadians(Yaw);
        float pitchRadians = MathHelper.DegreesToRadians(Pitch);
        _front = Vector3.Normalize(new Vector3(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians)));
        _right = Vector3.Normalize(Vector3.Cross(_front, WorldUp));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public override void Reset()
    {
        _position = StartPosition;
        Yaw = StartYaw;
        Pitch = StartPitch;
        FieldOfView = 60.0f;
        UpdateVectors();
    }

    // La revenirea din camera orbită, preluăm exact poziția și direcția ei.
    // Refacem unghiurile yaw și pitch din direcție, ca imaginea să rămână aproape identică.
    public void SetPose(Vector3 position, Vector3 direction)
    {
        if (direction.LengthSquared <= 0.0f)
            throw new ArgumentException("Direcția camerei nu poate fi nulă.", nameof(direction));

        Vector3 front = Vector3.Normalize(direction);
        _position = position;
        Pitch = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(front.Y, -1.0f, 1.0f))),
                           -MaxPitch, MaxPitch);
        Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(front.Z, front.X));
        UpdateVectors();
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Poziție        : ({_position.X,6:F2}, {_position.Y,6:F2}, {_position.Z,6:F2})";
        yield return $"Yaw / pitch    : {NormalizeAngle(Yaw),7:F1}° / {Pitch,6:F1}°";
        yield return $"Front          : ({_front.X,6:F2}, {_front.Y,6:F2}, {_front.Z,6:F2})";
        yield return $"Right          : ({_right.X,6:F2}, {_right.Y,6:F2}, {_right.Z,6:F2})";
        yield return $"Up             : ({_up.X,6:F2}, {_up.Y,6:F2}, {_up.Z,6:F2})";
    }

    // Aduce un unghi în intervalul (-180°, 180°] (doar pentru afișare).
    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
