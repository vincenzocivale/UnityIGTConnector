using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static AttributeType;
using static ReadMessageFromServer;

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
    public Struct[] Vertices { get; set; }
    public Struct[] Lines { get; set; }
    public Struct[] Polygons { get; set; }
    public Struct[] TriangleStrips { get; set; }
    public Attributes Attributes { get; set; }

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
        polyDataInfo.Vertices = Struct.ReadStructArray(messageBytes, polyDataInfo.NumVertices, ref offset);
        polyDataInfo.Lines = Struct.ReadStructArray(messageBytes, polyDataInfo.NumLines, ref offset);
        polyDataInfo.Polygons = Struct.ReadStructArray(messageBytes, polyDataInfo.NumPolygons, ref offset);
        polyDataInfo.TriangleStrips = Struct.ReadStructArray(messageBytes, polyDataInfo.NumTriangleStrips, ref offset);

        // Leggi i dati degli attributi
        /*polyDataInfo.Attributes = Attributes.ReadFromBytes(messageBytes, offset, polyDataInfo.NumAttributes);*/

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

        // Leggi ciascun poligono dalla lista delle strutture
        foreach (var polygon in polyDataInfo.Polygons)
        {
            // Assumi che `polygon.PointIndices` contenga gli indici dei vertici per il poligono
            uint verticesCount = polygon.NumberOfIndices;

            if (verticesCount < 3)
            {
                UnityEngine.Debug.LogWarning("Invalid polygon with less than 3 vertices, skipping.");
                continue;
            }

            // Genera i triangoli per il poligono corrente
            for (int i = 0; i < verticesCount - 2; i++)
            {
                triangles.Add((int)polygon.PointIndices[0]);         // Primo vertice del triangolo
                triangles.Add((int)polygon.PointIndices[i + 1]);     // Secondo vertice
                triangles.Add((int)polygon.PointIndices[i + 2]);     // Terzo vertice
            }
        }

        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Aggiungi i componenti necessari al GameObject
        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();

        meshFilter.mesh = mesh;

        // Imposta il materiale
        meshRenderer.material = new Material(Shader.Find("Standard"));

        return newObject;
    }

}

public class Struct
{
    public uint NumberOfIndices { get; set; }
    public List<uint> PointIndices { get; set; }

    public Struct()
    {
        PointIndices = new List<uint>();
    }

    public static Struct FromByteArray(byte[] data, ref int offset)
    {
        Struct structure = new Struct();

        // Read the number of indices
        structure.NumberOfIndices = ByteReader.ReadUInt32(data, offset);
        offset += 4;

        // Read the point indices
        for (uint i = 0; i < structure.NumberOfIndices; i++)
        {
            uint pointIndex = ByteReader.ReadUInt32(data, offset);
            structure.PointIndices.Add(pointIndex);
            offset += 4;
        }

        return structure;
    }

    public static Struct[] ReadStructArray(byte[] data, uint numStructs, ref int offset)
    {
        Struct[] structs = new Struct[numStructs];

        for (int i = 0; i < numStructs; i++)
        {
            structs[i] = Struct.FromByteArray(data, ref offset);
        }

        return structs;
    }
}

public class Attributes
{
    public AttributeHeader AttributeHeader { get; private set; }
    public AttributeNames AttributeNames { get; private set; }
    public AttributeData AttributeData { get; private set; }

    public Attributes(AttributeHeader header, AttributeNames names, AttributeData dataList)
    {
        this.AttributeHeader = header;
        this.AttributeNames = names;
        this.AttributeData = dataList;
    }

    private int GetAttributesNumber()
    {
        return this.AttributeNames.Names.Count;
    }

    public static Attributes ReadFromBytes(byte[] data, int offset, uint numberOfAttributes)
    {

        // Legge l'header
        AttributeHeader header = AttributeHeader.ReadFromBytes(data, ref offset, numberOfAttributes);

        // Legge i nomi degli attributi
        AttributeNames names = AttributeNames.ReadFromBytes(data, ref offset, numberOfAttributes);

        // Legge i dati degli attributi
        AttributeData dataList = AttributeData.ReadFromBytes(data, ref offset, header, numberOfAttributes);

        // Assicurati di passare tutti i parametri richiesti al costruttore
        return new Attributes(header, names, dataList);
    }
}

public class AttributeHeader
{
    public AttributeType[] TypeAttributesList { get; private set; }// Tipo dell'attributo
    public uint[] NAttributes { get; private set; }       // Numero di dati per ciascun attributo

    public AttributeHeader(AttributeType[] typeAttributesList, uint[] nAttributes)
    {
        TypeAttributesList = typeAttributesList;
        NAttributes = nAttributes;
    }
    public static AttributeHeader ReadFromBytes(byte[] data, ref int offset, uint numAttributes)
    {
        AttributeType[] typeAttributes = new AttributeType[numAttributes];
        uint[] nAttributes = new uint[numAttributes];

        for (int i = 0; i < numAttributes; i++)
        {
            typeAttributes[i] = AttributeType.FromByteArray(data, offset);
            offset += 2;

            nAttributes[i] = ByteReader.ReadUInt32(data, offset);
            offset += 4;
        }

        return new AttributeHeader(typeAttributes, nAttributes);
    }

    // Metodo per ottenere il numero di componenti da un attributo
    public int GetNumberOfComponents(int index)
    {
        AttributeType typeAttribute = TypeAttributesList[index];
        return typeAttribute.NumberOfComponents;  // Estrae i bit 8-15 per il numero di componenti
    }

    // Metodo per ottenere il tipo di attributo dai bit 0-7
    public AttributeTypeValue GetAttributeType(int index)
    {
        AttributeType typeAttribute = TypeAttributesList[index];
        return typeAttribute.Type;  // Estrae i bit 0-7 per il tipo di attributo
    }
}

public class AttributeType
{
    // Maschere per estrarre i vari componenti
    private const ushort TypeMask = 0x00FF; // 0xFF per i primi 8 bit (tipo di attributo)
    private const ushort ComponentMask = 0xFF00; // 0xFF00 per i successivi 8 bit (numero di componenti)

    // Tipi di attributo
    public enum AttributeTypeValue : byte
    {
        PointData_Scalars = 0x00,
        PointData_Vectors = 0x01,
        PointData_Normals = 0x02,
        PointData_Tensors = 0x03,
        PointData_RGBA = 0x04,
        CellData_Scalars = 0x10,
        CellData_Vectors = 0x11,
        CellData_Normals = 0x12,
        CellData_Tensors = 0x13,
        CellData_RGBA = 0x14
    }

    // Proprietà
    public AttributeTypeValue Type { get; private set; }
    public byte NumberOfComponents { get; private set; }

    // Costruttore
    public AttributeType(ushort value)
    {
        // Estrai il tipo di attributo
        Type = (AttributeTypeValue)(value & TypeMask);

        // Estrai il numero di componenti
        NumberOfComponents = (byte)((value & ComponentMask) >> 8);

        // Opzionale: verifica che NumberOfComponents sia nel range atteso
        ValidateNumberOfComponents();
    }

    private void ValidateNumberOfComponents()
    {
        // Controlla i valori validi in base al tipo di attributo
        if ((Type == AttributeTypeValue.PointData_Vectors ||
             Type == AttributeTypeValue.PointData_Normals) && NumberOfComponents != 3)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents),
                "Il numero di componenti deve essere 3 per Vettori e Normali.");
        }
        else if (Type == AttributeTypeValue.PointData_Tensors && NumberOfComponents != 9)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents),
                "Il numero di componenti deve essere 9 per i Tensors.");
        }
        else if (Type == AttributeTypeValue.PointData_RGBA && NumberOfComponents != 4)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents),
                "Il numero di componenti deve essere 4 per RGBA.");
        }
    }

    public static AttributeType FromByteArray(byte[] data, int offset)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "L'array di byte non può essere nullo.");

        if (offset < 0 || offset + 2 > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "L'offset deve essere all'interno dei limiti dell'array di byte.");

        // Combina i primi due byte per formare un valore ushort (16 bit)
        ushort value = (ushort)((data[offset + 1] << 8) | data[offset]);

        // Crea e restituisce un oggetto AttributeType
        return new AttributeType(value);
    }
}

public class AttributeNames
{
    public List<string> Names { get; private set; }

    public AttributeNames(List<string> names)
    {
        Names = names;
    }

    public static AttributeNames ReadFromBytes(byte[] data, ref int offset,uint numAttributes)
    {
        List<string> names = new List<string>();

        for (int i = 0; i < numAttributes; i++)
        {
            // Trova la posizione del null terminator (fine stringa)
            int start = offset;
            while (data[offset] != 0)
            {
                offset++;
            }

            // Legge la stringa e incrementa offset per saltare il terminatore nullo
            string name = System.Text.Encoding.ASCII.GetString(data, start, offset - start);
            names.Add(name);

            offset++;  // Salta il byte null terminator
        }

        // Gestione del padding per far combaciare la dimensione
        if (offset % 2 != 0)
        {
            offset++;  // Aggiungi padding se necessario
        }

        return new AttributeNames(names);

    }


}

public class AttributeData
{
    public float[][] Data { get; private set; }

    public AttributeData(uint numAttributes, int[] sizes)
    {
        Data = new float[numAttributes][];
        for (int i = 0; i < numAttributes; i++)
        {
            Data[i] = new float[sizes[i]];  // Ogni attributo ha una dimensione determinata
        }
    }

    public static AttributeData ReadFromBytes(byte[] data, ref int offset, AttributeHeader attributeHeader, uint numAttributes)
    {
        
        int[] sizes = new int[numAttributes];

        // Calcola la dimensione di ciascun attributo
        for (int i = 0; i < numAttributes; i++)
        {
            sizes[i] = (int)(attributeHeader.NAttributes[i] * attributeHeader.GetNumberOfComponents(i));
        }

        AttributeData attributeData = new AttributeData(numAttributes, sizes);

        // Legge i dati degli attributi dall'array di byte
        for (int i = 0; i < numAttributes; i++)
        {
            for (int j = 0; j < sizes[i]; j++)
            {
                if (offset + 4 > data.Length)
                {
                    throw new IndexOutOfRangeException("Trying to read beyond the bounds of the byte array.");
                }

                attributeData.Data[i][j] = BitConverter.ToSingle(data, offset);
                offset += 4;
            }
        }

        return attributeData;
    }
}




/*
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










*/