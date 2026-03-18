using UnityEngine;

public static class MeshGenerator
{
    public static Mesh GetMesh(Vector3Int normal)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;
        Vector2[] uv;

        //  y axis upper face, up = 1
        if (normal == Vector3Int.up)
        {
            vertices = new Vector3[]
            { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f) };
            triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            uv = new Vector2[] { new Vector2(0f, 2f / 3f), new Vector2(1f / 3f, 2f / 3f), new Vector2(1f / 3f, 1f), new Vector2(0f, 1f) };
        }
        //  y axis down face, down = 0
        else if (normal == Vector3Int.down)
        {
            vertices = new Vector3[]
            { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) };
            triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            uv = new Vector2[] { new Vector2(2f / 3f, 2f / 3f), new Vector2(1f, 2f / 3f), new Vector2(1f, 1f), new Vector2(2f / 3f, 1f) };
        }
        //  z axis right face, right = 1
        else if (normal == Vector3Int.forward)
        {
            vertices = new Vector3[]
            { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f) };
            triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            uv = new Vector2[] { new Vector2(0f, 1f / 3f), new Vector2(1f / 3f, 1f / 3f), new Vector2(1f / 3f, 2f / 3f), new Vector2(0f, 2f / 3f) };
        }
        //  z axis left face, left = 0
        else if (normal == Vector3Int.back)
        {
            vertices = new Vector3[]
            { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f) };
            triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            uv = new Vector2[] { new Vector2(1f / 3f, 1f / 3f), new Vector2(2f / 3f, 1f / 3f), new Vector2(2f / 3f, 2f / 3f), new Vector2(1f / 3f, 2f / 3f) };
        }
        //  x axis left face, left = 0
        else if (normal == Vector3Int.left)
        {
            vertices = new Vector3[]
            { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f) };
            triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            uv = new Vector2[] { new Vector2(2f / 3f, 1f / 3f), new Vector2(1f, 1f / 3f), new Vector2(1f, 2f / 3f), new Vector2(2f / 3f, 2f / 3f) };
        }
        //  x axis right face, right = 1
        else
        {
            vertices = new Vector3[]
            { new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f) };
            triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(1f / 3f, 0f), new Vector2(1f / 3f, 1f / 3f), new Vector2(0f, 1f / 3f) };
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}