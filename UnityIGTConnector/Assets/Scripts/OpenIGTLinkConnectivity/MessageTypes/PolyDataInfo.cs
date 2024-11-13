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
    public PointIndices[] Vertices { get; set; }
    public PointIndices[] Lines { get; set; }
    public PointIndices[] Polygons { get; set; }
    public PointIndices[] TriangleStrips { get; set; }
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

        int offset = (int)(polyDataInfo._headerInfo.headerSize + polyDataInfo._headerInfo.ExtHeaderSize);

        // Read the main fields
        polyDataInfo.ReadCoreFields(messageBytes, ref offset);

        // Read points
        polyDataInfo.Points = polyDataInfo.ReadPoints(messageBytes, ref offset);

        // Read lists (Vertices, Lines, Polygons, TriangleStrips)
        polyDataInfo.Vertices = PointIndices.ReadPointIndicesArray(messageBytes, polyDataInfo.NumVertices, ref offset);
        polyDataInfo.Lines = PointIndices.ReadPointIndicesArray(messageBytes, polyDataInfo.NumLines, ref offset);
        polyDataInfo.Polygons = PointIndices.ReadPointIndicesArray(messageBytes, polyDataInfo.NumPolygons, ref offset);
        polyDataInfo.TriangleStrips = PointIndices.ReadPointIndicesArray(messageBytes, polyDataInfo.NumTriangleStrips, ref offset);

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
        List<int> triangles;


        if (!polyDataInfo.HasValidPoints())
        {
            Debug.LogError("No valid points found in PolyDataInfo.");
            return null;
        }

        mesh.vertices = polyDataInfo.Points;
        UnityEngine.Debug.Log("points: " + polyDataInfo.Points.Length);

        if (polyDataInfo.HasValidPolygons())
        {
            triangles = TriangulatePolygons(polyDataInfo.Polygons);
        }

        else
        {
            if (polyDataInfo.HasValidTriangleStrips())
            {
                triangles = ConvertTriangleStripToTriangleList(polyDataInfo.TriangleStrips);
            }

            return null;
        }

        
        UnityEngine.Debug.Log("triangles: " + triangles.Count);
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        SetupMeshRenderer(newObject, mesh);

        return newObject;
    }

    public static List<int> ConvertTriangleStripToTriangleList(PointIndices[] triangleStrips)
    {
        List<int> triangleList = new List<int>();

        foreach (var strip in triangleStrips)
        {
            bool flip = false;
            // rappresenta la lista degli indici dei punti di ciascun strip
            List<uint> indices = strip.PointIndex;

            for (int i = 0; i < indices.Count - 2; i++)
            {
                int v0 = (int)indices[i];
                int v1 = (int)indices[i + 1];
                int v2 = (int)indices[i + 2];

                // Aggiungi i triangoli alternando l'ordine dei vertici
                if (flip)
                {
                    triangleList.Add(v0);
                    triangleList.Add(v2);
                    triangleList.Add(v1);
                }
                else
                {
                    triangleList.Add(v0);
                    triangleList.Add(v1);
                    triangleList.Add(v2);
                }

                flip = !flip; // Inverti l'ordine per il triangolo successivo

            }
        }

        return triangleList;
    }

    private static List<int> TriangulatePolygons(PointIndices[] polygons)
    {
        List<int> triangles = new List<int>();

        // Un poligono con n vertici può essere suddiviso in (n - 2) triangoli
        foreach(var polygon in polygons)
        {

            if (polygon.PointIndex.Count >= 3)
            {
                for (int i = 1; i < polygon.PointIndex.Count - 1; i++)
                {
                    triangles.Add((int)polygon.PointIndex[0]);    // Punto centrale (il primo)
                    triangles.Add((int)polygon.PointIndex[i]);    // Punto i
                    triangles.Add((int)polygon.PointIndex[i + 1]); // Punto i+1
                }
            }
        }

        return triangles;
    }


    private bool HasValidPoints() => NumPoints > 0 && Points != null;

    private bool HasValidPolygons() => NumPolygons > 0 && Polygons != null;

    private bool HasValidTriangleStrips() => NumTriangleStrips > 0 && TriangleStrips != null;

    private static void SetupMeshRenderer(GameObject newObject, Mesh mesh)
    {
        var meshFilter = newObject.AddComponent<MeshFilter>();
        var meshRenderer = newObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}

public class PointIndices
{
    public uint NumberOfIndices { get; set; }
    public List<uint> PointIndex { get; set; } = new List<uint>();

    public static PointIndices FromByteArray(byte[] data, ref int offset)
    {
        PointIndices structure = new PointIndices
        {
            NumberOfIndices = ByteReader.ReadUInt32(data, offset)
        };
        offset += 4;

        for (uint i = 0; i < structure.NumberOfIndices; i++)
        {
            structure.PointIndex.Add(ByteReader.ReadUInt32(data, offset));
            offset += 4;
        }

        return structure;
    }

    public static PointIndices[] ReadPointIndicesArray(byte[] data, uint numStructs, ref int offset)
    {
        PointIndices[] structs = new PointIndices[numStructs];

        for (int i = 0; i < numStructs; i++)
        {
            structs[i] = FromByteArray(data, ref offset);
        }

        return structs;
    }
}

