using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

// Clasa de bază a camerelor 3D. Conține ce au în comun toate camerele:
// proiecția în perspectivă (FOV, near, far, raportul de aspect) și "contractul" pe care
// trebuie să-l respecte fiecare cameră concretă: poziție, direcția privirii, matricea view,
// reacția la tastatură și mouse.
// Camera FPS (FpsCamera) se scrie în laborator; camera orbită (OrbitCamera) este tema.
public abstract class Camera
{
    // --- Projection (secțiunea 4) ---------------------------------------------

    public const float MinFov = 10.0f;     // grade
    public const float MaxFov = 120.0f;    // grade

    // Câmpul vizual VERTICAL, în grade (unghiul de sus până jos al imaginii).
    public float FieldOfView = 60.0f;

    public float Near = 0.1f;              // planul apropiat: ce e mai aproape nu se desenează
    public float Far = 100.0f;             // planul depărtat: ce e mai departe nu se desenează

    // Raportul lățime / înălțime al ferestrei.
    public float AspectRatio { get; private set; } = 4.0f / 3.0f;

    // Matricea de proiecție în perspectivă.
    public Matrix4 ProjectionMatrix =>
        // TODO 5.1 (a): proiecția în perspectivă, cu FieldOfView (în radiani!), AspectRatio, Near, Far.
        //               Până atunci se folosește proiecția ortografică din laboratorul 4.
        // Transformăm FOV-ul din grade în radiani, cum cere funcția de perspectivă.
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView),
                                              AspectRatio, Near, Far);

    // Proiecția ortografică din laboratorul 4 (provizorie; se șterge după TODO 5.1).
    // Nu mai avem nevoie de metoda provizorie: perspectiva este calculată direct mai sus.

    // Câmpul vizual ORIZONTAL, în grade, care rezultă din cel vertical și din raportul de aspect.
    public float HorizontalFov
    {
        get
        {
            // TODO 5.1 (b): hFOV = 2 · atan( tan(vFOV / 2) · aspect ); atenție la grade / radiani
            // Calculăm întâi unghiul pe orizontală, apoi îl transformăm înapoi în grade.
            float verticalRadians = MathHelper.DegreesToRadians(FieldOfView);
            float horizontalRadians = 2.0f * MathF.Atan(MathF.Tan(verticalRadians / 2.0f) * AspectRatio);
            return MathHelper.RadiansToDegrees(horizontalRadians);
        }
    }

    // Apelată din Game.OnResize: reține noul raport de aspect.
    public void Resize(int width, int height)
    {
        // La minimizare fereastra are 0 × 0 pixeli; un aspect 0 ar face proiecția invalidă.
        if (width <= 0 || height <= 0)
        {
            return;
        }
        AspectRatio = width / (float)height;
    }

    // Modifică FOV-ul cu deltaDegrees și îl păstrează între MinFov și MaxFov.
    public void ChangeFov(float deltaDegrees)
    {
        FieldOfView = Math.Clamp(FieldOfView + deltaDegrees, MinFov, MaxFov);
    }

    // --- Ce trebuie să ofere fiecare cameră concretă -----------------------------

    // Numele afișat în titlul ferestrei și în consolă.
    public abstract string Name { get; }

    // Poziția camerei (ochiul), în coordonate de lume.
    public abstract Vector3 Position { get; }

    // Direcția în care privește camera (vector unitate).
    public abstract Vector3 Front { get; }

    // Matricea view: world space -> view space.
    public abstract Matrix4 ViewMatrix { get; }

    // Apelată la fiecare cadru din Game.OnUpdateFrame.
    // mouseDelta = deplasarea mouse-ului față de cadrul anterior, în pixeli.
    // dt = timpul scurs de la cadrul anterior, în secunde.
    public abstract void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt);

    // Readuce camera în starea inițială (tasta R).
    public abstract void Reset();

    // Rândurile cu starea camerei, afișate în consolă la tasta I.
    public abstract IEnumerable<string> Describe();
}
