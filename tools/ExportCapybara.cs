using Godot;

namespace Capyball;

/// <summary>
/// One-shot headless tool: builds the capybara model from primitives and saves
/// it to disk as a PackedScene (.scn) so it becomes an authored, Inspector-
/// editable asset instead of code-generated at runtime.
///
/// Run with:  godot --headless --script res://tools/ExportCapybara.cs
/// (the C# class name must match the filename)
/// </summary>
public partial class ExportCapybara : SceneTree
{
    public override async void _Initialize()
    {
        GD.Print("=== Capybara export tool ===");

        var model = BuildCapybara();

        // Pack into a scene and save as .scn (Godot binary text scene).
        var packed = new PackedScene();
        var owner = new Node3D { Name = "Capybara" };
        owner.AddChild(model);
        model.Owner = owner;
        // Re-parent children ownership for packing.
        PackRecursive(model, owner);
        Error err = packed.Pack(owner);
        GD.Print($"Pack result: {err}");

        string scenePath = "res://assets/meshes/capybara.tscn";
        err = ResourceSaver.Save(packed, scenePath);
        GD.Print($"Saved scene -> {scenePath} : {err}");

        GD.Print("=== done ===");
        Quit();
    }

    private void PackRecursive(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            PackRecursive(child, owner);
        }
    }

    /// <summary>Builds the capybara from primitives, using disk materials now.</summary>
    private Node3D BuildCapybara()
    {
        var root = new Node3D { Name = "CapyModel" };

        Material Load(string n) => ResourceLoader.Load<Material>($"res://assets/materials/{n}.tres");

        MeshInstance3D Make(PrimitiveMesh mesh, Material mat, string name, Vector3 pos, Vector3 scale)
        {
            mesh.Material = mat;
            var mi = new MeshInstance3D { Name = name, Mesh = mesh, Position = pos, Scale = scale };
            root.AddChild(mi);
            return mi;
        }

        var furMat = Load("capy_fur");
        var furLightMat = Load("capy_fur_light");
        var snoutMat = Load("capy_snout");
        var noseMat = Load("capy_nose");
        var eyeMat = Load("capy_eye");
        var eyeWhiteMat = Load("capy_eye_white");

        Make(new SphereMesh { Radius = 0.55f, Height = 1.1f }, furMat, "Body",
            new Vector3(0, 0, 0), new Vector3(1.0f, 1.15f, 1.4f));
        Make(new SphereMesh { Radius = 0.42f, Height = 0.84f }, furMat, "Head",
            new Vector3(0, 0.28f, 0.55f), Vector3.One);
        Make(new SphereMesh { Radius = 0.24f, Height = 0.48f }, snoutMat, "Snout",
            new Vector3(0, 0.18f, 0.85f), new Vector3(1, 0.8f, 1.1f));
        Make(new SphereMesh { Radius = 0.09f, Height = 0.18f }, noseMat, "Nose",
            new Vector3(0, 0.24f, 1.03f), Vector3.One);
        Make(new SphereMesh { Radius = 0.12f, Height = 0.24f }, furLightMat, "EarL",
            new Vector3(-0.26f, 0.56f, 0.5f), Vector3.One);
        Make(new SphereMesh { Radius = 0.12f, Height = 0.24f }, furLightMat, "EarR",
            new Vector3(0.26f, 0.56f, 0.5f), Vector3.One);
        Make(new SphereMesh { Radius = 0.085f, Height = 0.17f }, eyeMat, "EyeL",
            new Vector3(-0.16f, 0.36f, 0.86f), Vector3.One);
        Make(new SphereMesh { Radius = 0.085f, Height = 0.17f }, eyeMat, "EyeR",
            new Vector3(0.16f, 0.36f, 0.86f), Vector3.One);
        Make(new SphereMesh { Radius = 0.03f, Height = 0.06f }, eyeWhiteMat, "EyeLHi",
            new Vector3(-0.13f, 0.39f, 0.92f), Vector3.One);
        Make(new SphereMesh { Radius = 0.03f, Height = 0.06f }, eyeWhiteMat, "EyeRHi",
            new Vector3(0.19f, 0.39f, 0.92f), Vector3.One);

        root.RotateY(Mathf.DegToRad(-90));
        return root;
    }
}
