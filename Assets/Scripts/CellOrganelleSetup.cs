using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.InferenceEngine.Tokenization.PostProcessors.Templating;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// Makes the organelles of a cell prefab tappable. Added automatically by ARCellInteraction
/// when a cell is spawned, so the prefabs and scene don't need manual edits.
///
/// The cell prefabs are organised as  Cell > organelle group (e.g. "Lysosome") > instances ("Lysosome_00").
/// For each selectable instance this adds an OrganelleTarget and gives every renderer a collider.
/// </summary>
public class CellOrganelleSetup : MonoBehaviour
{
    [Tooltip("Use MeshColliders (accurate, but every mesh needs Read/Write enabled in builds). Otherwise fitted BoxColliders are used.")]
    [SerializeField] private bool useMeshColliders = false;

    // Containers / backdrops that should never be selectable (and must not block taps on what is inside them).
    private static readonly string[] NonSelectable =
    {
        "PlasmaMembrane", "CellWall", "Cytoplasm", "Cytoskeleton", "itoskeleton", "Plasmodesmata", "_cameraDirection"
    };

    // Groups whose children together form ONE organelle (nucleus = envelope + pores + nucleolus).
    private static readonly HashSet<string> WholeGroup = new HashSet<string> { "Nucleus", "Reticulum" };

    private static readonly Dictionary<string, (string name, string[] pages)> Info = new Dictionary<string, (string, string[])>
    {
        { "Nucleus",         ("Nucleus", new string[] {
            "Stores the cell's DNA and controls cellular activity.",
            "Enclosed by a double nuclear envelope containing pores for material exchange.",
            "Contains the nucleolus, the site where ribosomes are assembled."
        } ) },
        { "Mitochondrion",   ("Mitochondrion", new string[] {
            "(pronounced my-toe-KON-dree-uhn) \nJust like a phone needs a battery to run, an animal cell needs power to survive. That power comes from a tiny organelle called the mitochondria. Because of this, scientists often call it the \"powerhouse\" of the cell!",
            "What Do They Do? \nWhen you eat a meal, your stomach breaks the food down into tiny pieces. However, your body's cells can't use those pieces for energy right away. \nThat is where the mitochondria step in. They take those tiny pieces of food, mix them with the oxygen you breathe in, and turn them into a special type of \"cell energy.\" Without this energy, your cells wouldn't be able to do their jobs.",
            "What Do They Look Like? \nIf you were to look at an animal cell under a very powerful microscope, the mitochondria look a bit like little jelly beans or peanuts floating around. \nInside that jelly-bean shape is a folded membrane that zig-zags back and forth. This folded zig-zag pattern is really important because it gives the mitochondria a lot more room to build and store energy.",
            "Fun Fact: The harder a cell has to work, the more mitochondria it needs. A skin cell doesn't move much, so it only has a few. But your heart and muscle cells are constantly working and moving, so a single muscle cell can have thousands of mitochondria inside it!"
        } ) },
        { "Ribosome",        ("Ribosome", new string[] {
            "Builds proteins by reading messenger RNA.", 
        } ) },
        { "Lysosome",        ("Lysosome", new string[] {
            "Holds digestive enzymes that break down waste and worn-out cell parts.", 
        } ) },
        { "GolgiApparatus",  ("Golgi apparatus", new string[] {
            "Modifies, sorts and packages proteins and lipids for delivery.", 
        } ) },
        { "BubbleGolgi",     ("Golgi vesicle", new string[] {
            "A small vesicle that carries cargo to and from the Golgi apparatus.", 
        } ) },
        { "Reticulum",       ("Endoplasmic reticulum", new string[] {
            "Rough ER (with ribosomes) makes proteins; smooth ER makes lipids.", 
        } ) },
        { "Centriole",       ("Centriole", new string[] {
            "Helps organise microtubules during cell division.", 
        } ) },
        { "Chloroplast",     ("Chloroplast", new string[] {
            "Carries out photosynthesis, turning light into chemical energy.", 
        } ) },
        { "Leukoplast",      ("Leukoplast", new string[] {
            "A colourless plastid that stores starch, oils or proteins.", 
        } ) },
        { "CentralVacuole",  ("Central vacuole", new string[] {
            "Stores water and nutrients and maintains turgor pressure.", 
        } ) },
        { "IncorporationOf", ("Cell inclusion", new string[] {
            "A storage granule or inclusion held in the cytoplasm.", 
        } ) },
    };

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        foreach (Transform group in transform)
        {
            string key = GroupKey(group.name);
            if (IsNonSelectable(group.name)) continue;

            // Some groups ship with a single large MeshCollider that would swallow taps meant for their children.
            foreach (Collider legacy in group.GetComponents<Collider>())
                legacy.enabled = false;

            if (group.childCount == 0 || WholeGroup.Contains(key))
            {
                MakeTarget(group.gameObject, key);
            }
            else
            {
                foreach (Transform instance in group)
                    MakeTarget(instance.gameObject, key);
            }
        }
    }

    private void MakeTarget(GameObject go, string key)
    {
        if (go.GetComponentInChildren<Renderer>(true) == null) return;

        var target = go.GetComponent<OrganelleTarget>();
        if (target == null) target = go.AddComponent<OrganelleTarget>();

        if (Info.TryGetValue(key, out var entry))
            target.Initialize(entry.name, entry.pages);
        else
            target.Initialize(Humanize(key), new string[] {"No description avaiable."});

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            EnsureCollider(r);
    }

    private void EnsureCollider(Renderer r)
    {
        if (r.GetComponent<Collider>() != null) return;

        var filter = r.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        if (useMeshColliders)
        {
            r.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }
        else
        {
            // Mesh.bounds works even when the mesh isn't marked readable.
            Bounds b = filter.sharedMesh.bounds;
            var box = r.gameObject.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = b.size;
        }
    }

    private static bool IsNonSelectable(string objectName)
    {
        if (objectName.StartsWith("_")) return true;
        foreach (string s in NonSelectable)
            if (objectName.Contains(s)) return true;
        return false;
    }

    // "GolgiApparatus_type00" -> "GolgiApparatus", "Lysosome_03" -> "Lysosome"
    private static string GroupKey(string objectName)
    {
        return Regex.Replace(objectName, @"(_type\d+)?(_\d+)?$", string.Empty);
    }

    // "SomeOrganelle" -> "Some Organelle"
    private static string Humanize(string key)
    {
        return Regex.Replace(key, "(?<=[a-z])(?=[A-Z])", " ");
    }
}
