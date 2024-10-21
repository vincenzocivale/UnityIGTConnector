using System;
using System.Collections.Generic;
using UnityEngine;

public class PolyDataInfo
{
    private const int VECTOR3_SIZE = 12; // Dimensione in byte di un Vector3 (3 float)
    private HeaderInfo _headerInfo;

    public uint NumPoints { get; set; }
    public uint NumVertices { get; set; }
    public uint SizeVertices { get; set; }
    public uint NumLines { get; set; }
    public uint SizeLines { get; set; }
    public uint NumPolygons { get; set; }
    public uint SizePolygons { get; set; }
    public uint NumTriangleStrips { get; set; }
    public uint SizeTriangleStrips { get; set; }
    public uint NumAttributes { get; set; }

    public Vector3[] Points { get; set; }
    public Struct[] Vertices { get; set; }
    public Struct[] Lines { get; set; }
    public Struct[] Polygons { get; set; }
    public Struct[] TriangleStrips { get; set; }
    public Attributes Attributes { get; set; }

    /// <summary>
    /// Reads PolyDataInfo from a byte array.
    /// </summary>
    public static PolyDataInfo ReadPolyDataInfo(byte[] messageBytes, HeaderInfo headerInfo)
    {
        PolyDataInfo polyDataInfo = new PolyDataInfo
        {
            _headerInfo = headerInfo
        };

        int offset = (int)(headerInfo.headerSize + headerInfo.ExtHeaderSize);

        // Read the main fields
        polyDataInfo.ReadCoreFields(messageBytes, ref offset);

        // Read points
        polyDataInfo.Points = polyDataInfo.ReadPoints(messageBytes, ref offset);

        // Read lists (Vertices, Lines, Polygons, TriangleStrips)
        polyDataInfo.Vertices = Struct.ReadStructArray(messageBytes, polyDataInfo.NumVertices, ref offset);
        polyDataInfo.Lines = Struct.ReadStructArray(messageBytes, polyDataInfo.NumLines, ref offset);
        polyDataInfo.Polygons = Struct.ReadStructArray(messageBytes, polyDataInfo.NumPolygons, ref offset);
        polyDataInfo.TriangleStrips = Struct.ReadStructArray(messageBytes, polyDataInfo.NumTriangleStrips, ref offset);

        // Read attribute data
        polyDataInfo.Attributes = Attributes.ReadFromBytes(messageBytes, offset, polyDataInfo.NumAttributes);

        return polyDataInfo;
    }

    private void ReadCoreFields(byte[] data, ref int offset)
    {
        NumPoints = ByteReader.ReadUInt32(data, offset);
        NumVertices = ByteReader.ReadUInt32(data, offset + 4);
        SizeVertices = ByteReader.ReadUInt32(data, offset + 8);
        NumLines = ByteReader.ReadUInt32(data, offset + 12);
        SizeLines = ByteReader.ReadUInt32(data, offset + 16);
        NumPolygons = ByteReader.ReadUInt32(data, offset + 20);
        SizePolygons = ByteReader.ReadUInt32(data, offset + 24);
        NumTriangleStrips = ByteReader.ReadUInt32(data, offset + 28);
        SizeTriangleStrips = ByteReader.ReadUInt32(data, offset + 32);
        NumAttributes = ByteReader.ReadUInt32(data, offset + 36);
        offset += 40; // Move offset past the core fields
    }

    private Vector3[] ReadPoints(byte[] data, ref int offset)
    {
        Vector3[] points = new Vector3[NumPoints];

        for (int i = 0; i < NumPoints; i++)
        {
            float x = ByteReader.ReadFloat(data, offset);
            float y = ByteReader.ReadFloat(data, offset + 4);
            float z = ByteReader.ReadFloat(data, offset + 8);
            points[i] = new Vector3(x, y, z);
            offset += VECTOR3_SIZE;
        }

        return points;
    }

    /// <summary>
    /// Generates a Unity GameObject with a Mesh from the PolyDataInfo.
    /// </summary>
    public static GameObject GenerateMeshFromPolyData(PolyDataInfo polyDataInfo)
    {
        GameObject newObject = new GameObject("PolyDataObject");
        Mesh mesh = new Mesh();

        if (!polyDataInfo.HasValidPoints())
        {
            Debug.LogError("No valid points found in PolyDataInfo.");
            return null;
        }

        mesh.vertices = polyDataInfo.Points;

        if (!polyDataInfo.HasValidPolygons())
        {
            Debug.LogError("No valid polygons found in PolyDataInfo.");
            return null;
        }

        List<int> triangles = polyDataInfo.GenerateTriangles();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        SetupMeshRenderer(newObject, mesh);

        return newObject;
    }

    private bool HasValidPoints() => NumPoints > 0 && Points != null;

    private bool HasValidPolygons() => NumPolygons > 0 && Polygons != null;

    private List<int> GenerateTriangles()
    {
        List<int> triangles = new List<int>();

        foreach (var polygon in Polygons)
        {
            if (polygon.NumberOfIndices < 3)
            {
                Debug.LogWarning("Skipping invalid polygon with less than 3 vertices.");
                continue;
            }

            // Create triangles using triangle fan pattern
            for (int i = 0; i < polygon.NumberOfIndices - 2; i++)
            {
                triangles.Add((int)polygon.PointIndices[0]);
                triangles.Add((int)polygon.PointIndices[i + 1]);
                triangles.Add((int)polygon.PointIndices[i + 2]);
            }
        }

        return triangles;
    }

    private static void SetupMeshRenderer(GameObject newObject, Mesh mesh)
    {
        var meshFilter = newObject.AddComponent<MeshFilter>();
        var meshRenderer = newObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}

public class Struct
{
    public uint NumberOfIndices { get; set; }
    public List<uint> PointIndices { get; set; } = new List<uint>();

    public static Struct FromByteArray(byte[] data, ref int offset)
    {
        Struct structure = new Struct
        {
            NumberOfIndices = ByteReader.ReadUInt32(data, offset)
        };
        offset += 4;

        for (uint i = 0; i < structure.NumberOfIndices; i++)
        {
            structure.PointIndices.Add(ByteReader.ReadUInt32(data, offset));
            offset += 4;
        }

        return structure;
    }

    public static Struct[] ReadStructArray(byte[] data, uint numStructs, ref int offset)
    {
        Struct[] structs = new Struct[numStructs];

        for (int i = 0; i < numStructs; i++)
        {
            structs[i] = FromByteArray(data, ref offset);
        }

        return structs;
    }
}

