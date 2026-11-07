using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Poziția, rotația și scalarea unui obiect din scenă. Din ele se construiește matricea model,
/// care duce vârfurile obiectului din object space în world space (secțiunea 5).
/// </summary>
public class Transform2D
{
    public Vector2 Position = Vector2.Zero;   // translația, în unități de lume
    public float Rotation = 0.0f;             // unghiul, în GRADE, pozitiv = sens trigonometric
    public Vector2 Scale = Vector2.One;       // factorii de scalare pe x și pe y
    public float Depth = 0.0f;                // coordonata z; contează doar în perspectivă (secțiunea 7.2)

    // TODO (T1): scalare, apoi rotație, apoi translație
    // T1: Dacă translația ar fi aplicată înaintea rotației, rotația s-ar face în jurul originii lumii (0,0,0) 
    // și nu în jurul centrului obiectului, determinând obiectul să descrie un cerc uriaș prin spațiu în loc să se învârtă pe loc.
    public Matrix4 ModelMatrix
    {
        get
        {
            float rotRads = MathHelper.DegreesToRadians(Rotation);

            return Matrix4.CreateScale(Scale.X, Scale.Y, 1.0f) *
                   Matrix4.CreateRotationZ(rotRads) *
                   Matrix4.CreateTranslation(Position.X, Position.Y, Depth);
        }
    }
    public Transform2D Clone() => (Transform2D)MemberwiseClone();
}
