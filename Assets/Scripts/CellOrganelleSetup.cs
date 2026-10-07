using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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

    private static readonly Dictionary<string, (string name, string info)> Info = new Dictionary<string, (string, string)>
    {
        { "Nucleus",         ("Nucleus", "Stores the cell's DNA and controls gene expression. It is enclosed by a double nuclear envelope with pores.") },
        { "Mitochondrion",   ("Mitochondrion", "Produces most of the cell's ATP through cellular respiration.") },
        { "Ribosome",        ("Ribosome", "Builds proteins by reading messenger RNA.") },
        { "Lysosome",        ("Lysosome", "Holds digestive enzymes that break down waste and worn-out cell parts.") },
        { "GolgiApparatus",  ("Golgi apparatus", "Modifies, sorts and packages proteins and lipids for delivery.") },
        { "BubbleGolgi",     ("Golgi vesicle", "A small vesicle that carries cargo to and from the Golgi apparatus.") },
        { "Reticulum",       ("Endoplasmic reticulum", "Rough ER (with ribosomes) makes proteins; smooth ER makes lipids.") },
        { "Centriole",       ("Centriole", "Helps organise microtubules during cell division.") },
        { "Chloroplast",     ("Chloroplast", "Carries out photosynthesis, turning light into chemical energy.") },
        { "Leukoplast",      ("Leukoplast", "A colourless plastid that stores starch, oils or proteins.") },
        { "CentralVacuole",  ("Central vacuole", "Stores water and nutrients and maintains turgor pressure.") },
        { "IncorporationOf", ("Cell inclusion", "A storage granule or inclusion held in the cytoplasm.") },
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
            target.Initialize(entry.name, entry.info);
        else
            target.Initialize(Humanize(key), string.Empty);

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
