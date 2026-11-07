using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// </summary>
public class Game : GameWindow
{
    private readonly Camera _camera = new();

    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();

    private int _selected;
    private bool _wireframe;

    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    // Variabile pentru T4 și T5
    private Vector2 _pivot = Vector2.Zero;
    private float _totalTime = 0.0f; // Folosit pentru a calcula exact cele 4 secunde ale orbitei de la T5

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),
                   Profile = ContextProfile.Core,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);
        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
    }

    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

        if (KeyboardState.IsKeyPressed(Keys.Tab))
        {
            _selected++;
            if (_selected >= _scene.Count) _selected = 0;
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------
        float dt = (float)args.Time;
        _totalTime += dt;
        SceneObject sel = _scene[_selected];

        if (KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift))
        {
            dt *= 3.0f; // Toate mișcările (translație, rotație, scalare) devin de 3 ori mai rapide
        }

        const float moveSpeed = 2.0f;
        const float rotSpeed = 90.0f;
        const float scaleFactor = 1.5f; // Modificat conform cerinței T2

        // 1. Translație 
        if (KeyboardState.IsKeyDown(Keys.Left)) sel.Transform.Position.X -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Down)) sel.Transform.Position.Y -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Up)) sel.Transform.Position.Y += moveSpeed * dt;

        // 2. Rotație (Q = trig, E = orar)
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotSpeed * dt;

        // 3. Scalare limitată
        // T2: Scalarea se face prin înmulțire deoarece este o transformare relativă/proporțională. 
        // Adunarea unei cantități fixe ar deforma proporțiile formei și ar avea un impact inegal în funcție de mărimea curentă.
        if (KeyboardState.IsKeyDown(Keys.Z)) sel.Transform.Scale *= MathF.Pow(1.0f / scaleFactor, dt);
        if (KeyboardState.IsKeyDown(Keys.X)) sel.Transform.Scale *= MathF.Pow(scaleFactor, dt);

        // Limităm scara între 0.1 și 10
        sel.Transform.Scale = new Vector2(
            Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f),
            Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f)
        );

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // T3: Folosim IsButtonPressed pentru a se activa o singură dată la clic
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            Vector2 worldPos = ScreenToWorld(MouseState.Position);
            sel.Transform.Position = worldPos;
        }

        // TODO (T4, facultativ): clic dreapta = pivot; tasta P ținută = rotație în jurul pivotului
        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
        }

        if (KeyboardState.IsKeyDown(Keys.P))
        {
            float angleRad = MathHelper.DegreesToRadians(rotSpeed * dt);
            float dx = sel.Transform.Position.X - _pivot.X;
            float dy = sel.Transform.Position.Y - _pivot.Y;

            float rotatedX = dx * MathF.Cos(angleRad) - dy * MathF.Sin(angleRad);
            float rotatedY = dx * MathF.Sin(angleRad) + dy * MathF.Cos(angleRad);

            sel.Transform.Position = new Vector2(_pivot.X + rotatedX, _pivot.Y + rotatedY);
            sel.Transform.Rotation += rotSpeed * dt;
        }

        // --- Camera -----------------------------
        float camSpeed = 3.0f / _camera.Zoom;
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei ------------------------
        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        SetWireframe(_wireframe);
        _shader.Use();

        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // TODO (T5, facultativ): satelitul obiectului selectat
        // T5: Când obiectul selectat este scalat, orbita satelitului și satelitul în sine se scalează de asemenea. 
        // Motivul este acela că matricea de transformare a satelitului este înmulțită la final cu matricea Model a obiectului părinte,
        // preluând astfel absolut toate transformările acestuia (inclusiv mărirea/micșorarea).
        SceneObject parent = _scene[_selected];

        // O rotație completă la 4 secunde (timp total împărțit la 4 = procentajul dintr-un unghi complet de 2*PI)
        float orbitAngle = (_totalTime / 4.0f) * MathHelper.TwoPi;

        Matrix4 localTransform =
            Matrix4.CreateScale(0.3f) *
            Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f) *
            Matrix4.CreateRotationZ(orbitAngle);

        // Înmulțim transformarea locală cu matricea părintelui (cum zice în secțiunea 5.3)
        Matrix4 satelliteModel = localTransform * parent.Transform.ModelMatrix;

        _shader.SetMatrix4("uModel", satelliteModel);
        _shader.SetColor("uTint", Color4.Yellow);
        parent.Mesh.Draw();

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();
        base.OnUnload();
    }

    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // T3: Semnul minus (sau 1 - ...) din formula pentru ndcY apare deoarece originea axelor mouse-ului (0,0) este 
        // în colțul stânga-sus al ferestrei (y crește în jos), în timp ce în NDC și în lumea 3D, y crește în sus.
        float ndcX = (2.0f * mousePixels.X / Size.X) - 1.0f;
        float ndcY = 1.0f - (2.0f * mousePixels.Y / Size.Y);
        Vector4 ndcPos = new Vector4(ndcX, ndcY, 0.0f, 1.0f);

        Matrix4 invProj = Matrix4.Invert(_camera.ProjectionMatrix);
        Matrix4 invView = Matrix4.Invert(_camera.ViewMatrix);
        Vector4 worldPos = ndcPos * invProj * invView;

        if (worldPos.W != 0.0f)
        {
            worldPos.X /= worldPos.W;
            worldPos.Y /= worldPos.W;
        }

        return new Vector2(worldPos.X, worldPos.Y);
    }

    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}