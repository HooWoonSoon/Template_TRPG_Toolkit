using System.Collections.Generic;
using UnityEngine;

public class BlockCombiner
{
    private List<MeshFilter> sourceMeshFilter = new List<MeshFilter>();
    private List<MeshRenderer> sourceMeshRenderer = new List<MeshRenderer>();
    private GameObject combinedMesh;

    public BlockCombiner(Dictionary<Vector3, BlockFace> blockfaces)
    {
        float startTime = Time.realtimeSinceStartup;
        Dictionary<Material, List<CombineInstance>> materialGroupCombines = new Dictionary<Material, List<CombineInstance>>();

        foreach (var kvp in blockfaces)
        {
            Vector3 position = kvp.Key;
            BlockFace blockface = kvp.Value;
            Material material = blockface.material;

            if (!materialGroupCombines.ContainsKey(material))
            {
                materialGroupCombines[material] = new List<CombineInstance>();
            }

            foreach (Vector3Int direction in blockface.normal)
            {
                CombineInstance combine = new CombineInstance()
                {
                    mesh = MeshGenerator.GetMesh(direction),
                    transform = Matrix4x4.Translate(position)
                };
                materialGroupCombines[material].Add(combine);
            }
        }

        foreach (var kvp in materialGroupCombines)
        {
            Material material = kvp.Key;
            List<CombineInstance> combines = kvp.Value;

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combines.ToArray(), true);

            GameObject meshObject = new GameObject("Map");
            combinedMesh = meshObject;

            meshObject.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        float endTime = Time.realtimeSinceStartup;
        Debug.Log($"Mesh generating completed in {endTime - startTime:F4} seconds");
    }

    public GameObject GetCombinedMesh()
    {
        return combinedMesh;
    }
}