using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyDataInfo
{
    private const int VECTOR3_SIZE = 12;
    HeaderInfo headerInfo;
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
    public uint[] Vertices { get; set; }
    public uint[] Lines { get; set; }
    public uint[] Polygons { get; set; }
    public uint[] TriangleStrips { get; set; }
    public AttributeHeader AttributeHeader { get; set; }
    public AttributeNames AttributeNames { get; set; }
    public AttributeData AttributeData { get; set; }

    public static PolyDataInfo ReadPolyDataInfo(byte[] messageBytes, HeaderInfo headerInfo)
    {
        PolyDataInfo polyDataInfo = new PolyDataInfo();
        polyDataInfo.headerInfo = headerInfo;
        int offset = (int)(headerInfo.headerSize + headerInfo.ExtHeaderSize);

        // Leggi i campi principali
        polyDataInfo.NumPoints = ByteReader.ReadUInt32(messageBytes, offset);
        polyDataInfo.NumVertices = ByteReader.ReadUInt32(messageBytes, offset + 4);
        polyDataInfo.SizeVertices = ByteReader.ReadUInt32(messageBytes, offset + 8);
        polyDataInfo.NumLines = ByteReader.ReadUInt32(messageBytes, offset + 12);
        polyDataInfo.SizeLines = ByteReader.ReadUInt32(messageBytes, offset + 16);
        polyDataInfo.NumPolygons = ByteReader.ReadUInt32(messageBytes, offset + 20);
        polyDataInfo.SizePolygons = ByteReader.ReadUInt32(messageBytes, offset + 24);
        polyDataInfo.NumTriangleStrips = ByteReader.ReadUInt32(messageBytes, offset + 28);
        polyDataInfo.SizeTriangleStrips = ByteReader.ReadUInt32(messageBytes, offset + 32);
        polyDataInfo.NumAttributes = ByteReader.ReadUInt32(messageBytes, offset + 36);

        offset += 40;

        // Leggi i punti
        polyDataInfo.Points = new Vector3[polyDataInfo.NumPoints];
        for (int i = 0; i < polyDataInfo.NumPoints; i++)
        {
            float x = ByteReader.ReadFloat(messageBytes, offset);
            float y = ByteReader.ReadFloat(messageBytes, offset + 4);
            float z = ByteReader.ReadFloat(messageBytes, offset + 8);
            polyDataInfo.Points[i] = new Vector3(x, y, z);
            offset += VECTOR3_SIZE;
        }

        // Leggi le liste
        polyDataInfo.Vertices = ByteReader.ReadUIntArray(messageBytes, polyDataInfo.SizeVertices, ref offset);
        polyDataInfo.Lines = ByteReader.ReadUIntArray(messageBytes, polyDataInfo.SizeLines, ref offset);
        polyDataInfo.Polygons = ByteReader.ReadUIntArray(messageBytes, polyDataInfo.SizePolygons, ref offset);
        polyDataInfo.TriangleStrips = ByteReader.ReadUIntArray(messageBytes, polyDataInfo.SizeTriangleStrips, ref offset);

        // Leggi i dati degli attributi
        polyDataInfo.AttributeHeader = AttributeHeader.ReadAttributeHeader(messageBytes, ref offset, polyDataInfo.NumAttributes);
        polyDataInfo.AttributeNames = AttributeNames.ReadAttributeNames(messageBytes, ref offset, polyDataInfo.NumAttributes);
        polyDataInfo.AttributeData = AttributeData.ReadAttributeData(messageBytes, ref offset, polyDataInfo.AttributeHeader);

        return polyDataInfo;
    }

    public static GameObject GenerateMeshFromPolyData(PolyDataInfo polyDataInfo)
    {
        GameObject newObject = new GameObject("PolyDataObject");
        Mesh mesh = new Mesh();

        if (polyDataInfo.NumPoints == 0 || polyDataInfo.Points == null)
        {
            UnityEngine.Debug.LogError("No points found in PolyDataInfo.");
            return null;
        }

        // Imposta i vertici del mesh
        Vector3[] vertices = new Vector3[polyDataInfo.NumPoints];
        for (int i = 0; i < polyDataInfo.NumPoints; i++)
        {
            vertices[i] = polyDataInfo.Points[i];
        }
        mesh.vertices = vertices;

        // Imposta i triangoli del mesh
        if (polyDataInfo.NumPolygons == 0 || polyDataInfo.Polygons == null)
        {
            UnityEngine.Debug.LogError("No polygons found in PolyDataInfo.");
            return null;
        }

        List<int> triangles = new List<int>();
        uint polygonIndex = 0;

        while (polygonIndex < polyDataInfo.Polygons.Length)
        {
            if (polygonIndex >= polyDataInfo.Polygons.Length)
            {
                UnityEngine.Debug.LogError($"polygonIndex {polygonIndex} is out of bounds for polygons array with length {polyDataInfo.Polygons.Length}.");
                break;
            }

            uint verticesCount = polyDataInfo.Polygons[polygonIndex];
            polygonIndex++;

            if (polygonIndex + verticesCount > polyDataInfo.Polygons.Length)
            {
                UnityEngine.Debug.LogError("Invalid polygon data encountered.");
                break;
            }

            for (int i = 0; i < verticesCount - 2; i++)
            {
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex]);
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex + i + 1]);
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex + i + 2]);
            }

            polygonIndex += verticesCount;
        }
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Cerca un attributo che rappresenti i colori
        Color[] colors = null;
        for (int i = 0; i < polyDataInfo.NumAttributes; i++)
        {
            byte attributeType = polyDataInfo.AttributeHeader.GetAttributeType(i);
            byte numComponents = polyDataInfo.AttributeHeader.GetNumberOfComponents(i);

            // Verifica se l'attributo corrente è un colore RGB o RGBA
            if ((attributeType == 10 || attributeType == 11) && (numComponents == 3 || numComponents == 4))
            {
                colors = new Color[polyDataInfo.NumPoints];
                for (int j = 0; j < polyDataInfo.NumPoints; j++)
                {
                    float r = polyDataInfo.AttributeData.Data[i][j * numComponents];
                    float g = polyDataInfo.AttributeData.Data[i][j * numComponents + 1];
                    float b = polyDataInfo.AttributeData.Data[i][j * numComponents + 2];
                    float a = (numComponents == 4) ? polyDataInfo.AttributeData.Data[i][j * numComponents + 3] : 1.0f;

                    colors[j] = new Color(r, g, b, a);
                }
                break; 
            }
        }

        if (colors != null)
        {
            mesh.colors = colors;
        }
        else
        {
            UnityEngine.Debug.LogWarning("No color attributes found. Defaulting to white.");
        }



        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();

        meshFilter.mesh = mesh;

        // Imposta il materiale
        meshRenderer.material = new Material(Shader.Find("Standard"));

        return newObject;
    }
}

public class AttributeHeader
{
    public ushort[] TypeAttributes { get; set; }
    public uint[] NAttributes { get; set; }

    public byte GetAttributeType(int index)
    {
        return (byte)(TypeAttributes[index] & 0x00FF);
    }

    public byte GetNumberOfComponents(int index)
    {
        return (byte)((TypeAttributes[index] >> 8) & 0x00FF);
    }

    public AttributeHeader(uint numAttributes)
    {
        TypeAttributes = new ushort[numAttributes];
        NAttributes = new uint[numAttributes];
    }

    public static AttributeHeader ReadAttributeHeader(byte[] messageBytes, ref int offset, uint numAttributes)
    {
        AttributeHeader attributeHeader = new AttributeHeader(numAttributes);

        for (int i = 0; i < numAttributes; i++)
        {
            attributeHeader.TypeAttributes[i] = ByteReader.ReadUInt16(messageBytes, offset + i * 6);
            attributeHeader.NAttributes[i] = ByteReader.ReadUInt32(messageBytes, offset + i * 6 + 2);
            offset += 6;
        }

        return attributeHeader;
    }
}

public class AttributeNames
{
    public string[] Names { get; set; }
    public byte Padding { get; set; }

    public static AttributeNames ReadAttributeNames(byte[] messageBytes, ref int offset, uint numAttributes)
    {
        AttributeNames attributeNames = new AttributeNames
        {
            Names = new string[numAttributes]
        };

        for (int i = 0; i < numAttributes; i++)
        {
            int nameLength = 0;
            while (messageBytes[offset + nameLength] != 0)
            {
                nameLength++;
            }

            attributeNames.Names[i] = System.Text.Encoding.ASCII.GetString(messageBytes, offset, nameLength);
            offset += nameLength + 1;

            if ((nameLength + 1) % 2 != 0)
            {
                offset += 1;
            }
        }

        return attributeNames;
    }
}

public class AttributeData
{
    public float[][] Data { get; set; }

    public AttributeData(uint numAttributes)
    {
        Data = new float[numAttributes][];
    }

    public static AttributeData ReadAttributeData(byte[] messageBytes, ref int offset, AttributeHeader attributeHeader)
    {
        uint numAttributes = (uint)attributeHeader.TypeAttributes.Length;
        AttributeData attributeData = new AttributeData(numAttributes);

        for (int i = 0; i < numAttributes; i++)
        {
            int attributeSize = (int)(attributeHeader.NAttributes[i] * attributeHeader.GetNumberOfComponents(i));
            attributeData.Data[i] = new float[attributeSize];

            for (int j = 0; j < attributeSize; j++)
            {
                attributeData.Data[i][j] = ByteReader.ReadFloat(messageBytes, offset);
                offset += 4;
            }
        }

        return attributeData;


    }
}







