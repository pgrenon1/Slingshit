using Godot;
using System;
using Godot.Collections;

public partial class LevelManager : Singleton<LevelManager>
{
    public NavigationRegion3D NavigationRegion3D { get; set; }
    
    public Vector3 GetRandomPointOnNavMesh()
    {
        Array<float> areas = new Array<float>();
        float totalArea = 0f;
        NavigationMesh navMesh = NavigationRegion3D.NavigationMesh;
        
        GD.Print(navMesh.VerticesPerPolygon);
        
        for (int i = 0; i < navMesh.GetPolygonCount(); i++)
        {
            int[] polygon = navMesh.GetPolygon(i);
            Vector3[] vertices = new Vector3[3];
            for (int j = 0; j < polygon.Length; j++)
            {
                vertices[j] = navMesh.Vertices[polygon[j]];
            }

            float area = CalculateAreaOfTrinagle(vertices[0], vertices[1], vertices[2]);

            totalArea += area;
            areas.Add(totalArea + area);
        }

        float randomFloat = GD.Randf();
        float randomArea = randomFloat * totalArea;
        int chosenTriangleIndex = Mathf.Abs(areas.BinarySearch(randomArea));

        int[] chosenTriangleVertex = navMesh.GetPolygon(chosenTriangleIndex);

        int vertexAIndex = chosenTriangleVertex[0];
        int vertexBIndex = chosenTriangleVertex[1];
        int vertexCIndex = chosenTriangleVertex[2];
        
        Vector3 vertexA = navMesh.Vertices[vertexAIndex];
        Vector3 vertexB = navMesh.Vertices[vertexBIndex];
        Vector3 vectorC = navMesh.Vertices[vertexCIndex];
        
        return GetRandomPointInsideTriangle(vertexA, vertexB, vectorC);
    }

    public Vector3 GetRandomPointInsideTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        return a + Mathf.Sqrt(GD.Randf()) * (-a + b + GD.Randf() * (c - b));
    }
    
    float CalculateAreaOfTrinagle(Vector3 n1, Vector3 n2 , Vector3 n3)
    {
        float res  = Mathf.Pow(((n2.X * n1.Y) - (n3.X * n1.Y) - (n1.X * n2.Y) + (n3.X * n2.Y) + (n1.X * n3.Y) - (n2.X * n3.Y)), 2.0f);
        res += Mathf.Pow(((n2.X * n1.Z) - (n3.X * n1.Z) - (n1.X * n2.Z) + (n3.X * n2.Z) + (n1.X * n3.Z) - (n2.X * n3.Z)), 2.0f);
        res += Mathf.Pow(((n2.Y * n1.Z) - (n3.Y * n1.Z) - (n1.Y * n2.Z) + (n3.Y * n2.Z) + (n1.Y * n3.Z) - (n2.Y * n3.Z)), 2.0f);
        return Mathf.Sqrt(res) * 0.5f;
    }

    public float GetTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        float aToB = a.DistanceTo(b);
        float bToC = b.DistanceTo(c);
        float cToA = c.DistanceTo(a);
        float s = (aToB + bToC + cToA) / 2f;
        return Mathf.Sqrt(s * (s - aToB) * (s - bToC) * (s * cToA));
    }
}
