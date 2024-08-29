using System.Linq;
using System.Text;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;

public class SendMessageToServer : MonoBehaviour
{
    public static void SendMessage(GameObject modelGO, int scaleMultiplier, CRC64 crcGenerator, SocketHandler socketForUnityAndHoloLens, string messageType)
    {
        string modelName = modelGO.name;
        string deviceName = modelName + "_T";
        string oigtlVersion = "0002";
        string body = "";

        switch (messageType)
        {
            case "TRANSFORM":
                body = CreateTransformBody(modelGO, scaleMultiplier);
                break;

            case "IMAGE":
                body = CreateImage2DBody(modelGO);
                break;

            default:
                UnityEngine.Debug.LogError($"Send message type not supported: {messageType}");
                break;
        }

        // Create metadata in hexadecimal
        string hexMetaBody = CreateMetaDataHex(new Dictionary<string, string>
        {
            {"ModelName", modelName}
        });

        // Build the extended header
        string hexExtHeader = CreateExtendedHeaderHex(hexMetaBody);

        // Create the main header
        string hexHeader = CreateMainHeader(oigtlVersion, messageType, deviceName, hexExtHeader + body + hexMetaBody);

        // Calculate the CRC
        string CRC = CalculateCRC(crcGenerator, hexExtHeader + body + hexMetaBody);

        // Combine all parts into the final message and send it
        string finalMessage = hexHeader + CRC + hexExtHeader + body + hexMetaBody;

        socketForUnityAndHoloLens.Send(StringToByteArray(finalMessage));
    }

    public static string CreateMetaDataHex(Dictionary<string, string> metadata)
    {
        // Constants
        const UInt16 encodingValue = 3; // US-ASCII

        // Initialize strings
        string metaHeader = "";
        string metaData = "";

        // Iterate over metadata dictionary
        foreach (var entry in metadata)
        {
            string key = entry.Key;
            string value = entry.Value;

            metaHeader += CreateMetaHeader(key, value, encodingValue);
            metaData += CreateMetaBody(key, value);
        }

        // INDEX_COUNT (number of metadata items)
        string indexCountHex = ConvertToHex((ushort)metadata.Count);

        // Combine to form the final metadata body
        return indexCountHex + metaHeader + metaData;
    }

    private static string CreateMetaHeader(string key, string value, UInt16 encodingValue)
    {
        // Convert key and value to byte arrays
        byte[] keyBytes = Encoding.ASCII.GetBytes(key);
        byte[] valueBytes = Encoding.ASCII.GetBytes(value);

        // Create header components
        string keySizeHex = ConvertToHex((UInt16)keyBytes.Length);
        string valueSizeHex = ConvertToHex((UInt32)valueBytes.Length);
        string encodingHex = ConvertToHex(encodingValue);

        // Combine header components
        return keySizeHex + encodingHex + valueSizeHex;
    }

    private static string CreateMetaBody(string key, string value)
    {
        // Convert key and value to hexadecimal
        string keyHex = ToHexString(Encoding.ASCII.GetBytes(key));
        string valueHex = ToHexString(Encoding.ASCII.GetBytes(value));

        // Combine key and value
        return keyHex + valueHex;
    }

    private static string CreateExtendedHeaderHex(string hexMetaBody)
    {
        // Calculate header sizes
        string extHeaderSize = ConvertToHex((ushort)12);
        string metadataHeaderSize = ConvertToHex((ushort)(hexMetaBody.Length / 2));
        string metadataSize = ConvertToHex((uint)(hexMetaBody.Length / 2));
        string msgID = ConvertToHex((uint)0);

        return extHeaderSize + metadataHeaderSize + metadataSize + msgID;
    }

    private static string CreateMainHeader(string version, string type, string deviceName, string bodyContent)
    {
        string timeStamp = ConvertToHex((ulong)0);
        string bodySize = Convert.ToUInt64(bodyContent.Length / 2).ToString("X16");
        return version + StringToHexString(type, 12) + StringToHexString(deviceName, 20) + timeStamp + bodySize;
    }

    private static string CalculateCRC(CRC64 crcGenerator, string content)
    {
        ulong crc = crcGenerator.Compute(StringToByteArray(content), 0, 0);
        return crc.ToString("X16");
    }

    public static string CreateTransformBody(GameObject modelGO, int scaleMultiplier)
    {
        // Ottieni la matrice di trasformazione con le coordinate adattate
        var myOBJRotation = modelGO.transform.localRotation.eulerAngles;
        var adaptedRotationFromDeviceToSlicer = new Vector3(-myOBJRotation.x, myOBJRotation.y, -myOBJRotation.z);
        var rotationForSlicer = Quaternion.Euler(adaptedRotationFromDeviceToSlicer);

        // Matrice 4x4 con informazioni di posa dell'oggetto
        Matrix4x4 matrix = Matrix4x4.TRS(modelGO.transform.localPosition, rotationForSlicer, modelGO.transform.localScale);

        // Estrai i valori della matrice e converti in byte array
        float[] matrixElements = new float[]
        {
            matrix.GetRow(0)[0], matrix.GetRow(0)[1], matrix.GetRow(0)[2], matrix.GetRow(0)[3] * scaleMultiplier,
            matrix.GetRow(1)[0], matrix.GetRow(1)[1], matrix.GetRow(1)[2], -matrix.GetRow(1)[3] * scaleMultiplier,
            matrix.GetRow(2)[0], matrix.GetRow(2)[1], matrix.GetRow(2)[2], matrix.GetRow(2)[3] * scaleMultiplier
        };

        List<string> hexMatrixElements = new List<string>();
        foreach (var element in matrixElements)
        {
            byte[] bytes = BitConverter.GetBytes(element);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            hexMatrixElements.Add(BitConverter.ToString(bytes).Replace("-", ""));
        }

        // Crea il corpo del messaggio
        return string.Join("", hexMatrixElements);
    }

    public static string CreateImage2DBody(GameObject modelGO)
    {

        Renderer renderer = modelGO.GetComponent<Renderer>();
        Material material = renderer?.material;
        Texture2D texture = material?.mainTexture as Texture2D;

        texture = ConvertToSupportedFormat(texture);

        // Valori messi di default 
        string imageCoord = "RAS";
        float[] px_py_pz = { 0.0f, 0.0f, 0.0f };
        ushort[] di_dj_dk = { 0, 0, 0 };

        // Recupera le informazioni sull'immagine
        int width = texture.width;
        int height = texture.height;
        byte numComponents = GetImageFormatInfo(texture).numComponents;
        byte scalarType = GetImageFormatInfo(texture).scalarType;
        byte endian = BitConverter.IsLittleEndian ? (byte)2 : (byte)1;  // Determinazione dell'endianess
        byte imageCoordByte = (byte)(imageCoord == "RAS" ? 1 : 2); // 1: RAS, 2: LPS

        // Dimensioni dell'immagine
        ushort[] ri_rj_rk = { (ushort)width, (ushort)height, 1 };

        // Vettori trasversali e normali
        float[] tx_ty_tz = { 1.0f, 0.0f, 0.0f }; // Pixel size in millimeter (x-direction)
        float[] sx_sy_sz = { 0.0f, 1.0f, 0.0f }; // Pixel size in millimeter (y-direction)
        float[] nx_ny_nz = { 0.0f, 0.0f, 1.0f }; // Pixel size in millimeter or slice thickness (z-direction)

        ushort[] dri_drj_drk = { (ushort)width, (ushort)height, 1 };

        // Creazione dell'header dell'immagine
        List<byte> imageHeader = new List<byte>();

        imageHeader.AddRange(BitConverter.GetBytes((ushort)2)); // Versione
        imageHeader.Add(numComponents);
        imageHeader.Add(scalarType);
        imageHeader.Add(endian);
        imageHeader.Add(imageCoordByte);

        foreach (ushort val in ri_rj_rk) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (float val in tx_ty_tz) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (float val in sx_sy_sz) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (float val in nx_ny_nz) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (float val in px_py_pz) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (ushort val in di_dj_dk) imageHeader.AddRange(BitConverter.GetBytes(val));
        foreach (ushort val in dri_drj_drk) imageHeader.AddRange(BitConverter.GetBytes(val));

        // Immagine in formato binario (dati dell'immagine)
        byte[] imageData = texture.EncodeToPNG(); // Converti l'immagine in un array di byte PNG

        // Calcolo della dimensione dell'immagine in byte
        string hexBodySize = (imageHeader.Count + imageData.Length).ToString("X").PadLeft(16, '0');

        // Costruzione del messaggio completo
        string messageInHex = BitConverter.ToString(imageHeader.ToArray()).Replace("-", "") +
                              BitConverter.ToString(imageData).Replace("-", "");

        return messageInHex;
    }

    private static Texture2D ConvertToSupportedFormat(Texture2D originalTexture)
    {
        // Verifica se la texture è leggibile
        if (!originalTexture.isReadable)
        {
            UnityEngine.Debug.LogWarning("Original texture is not readable. Attempting to create a new readable texture.");
            // Crea una nuova texture temporanea per la lettura
            Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
            RenderTexture tempRT = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = tempRT;
            Graphics.Blit(originalTexture, tempRT);
            readableTexture.ReadPixels(new Rect(0, 0, originalTexture.width, originalTexture.height), 0, 0);
            readableTexture.Apply();
            RenderTexture.ReleaseTemporary(tempRT);
            return readableTexture;
        }

        // Crea una nuova Texture2D nel formato desiderato (RGBA32)
        Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);

        // Copia i pixel dalla texture originale
        Color[] pixels = originalTexture.GetPixels();
        newTexture.SetPixels(pixels);
        newTexture.Apply();

        return newTexture;
    }

    public static (byte scalarType, byte numComponents) GetImageFormatInfo(Texture2D texture)
    {
        byte scalarType;
        byte numComponents;

        switch (texture.format)
        {
            case TextureFormat.Alpha8:
                numComponents = 1;
                scalarType = 3; // uint8
                break;

            case TextureFormat.RGB24:
                numComponents = 3;
                scalarType = 3; // uint8
                break;

            case TextureFormat.RGBA32:
            case TextureFormat.ARGB32:
                numComponents = 4;
                scalarType = 3; // uint8
                break;

            case TextureFormat.R16:
                numComponents = 1;
                scalarType = 5; // uint16
                break;

            case TextureFormat.RG16:
                numComponents = 2;
                scalarType = 5; // uint16
                break;

            case TextureFormat.RFloat:
                numComponents = 1;
                scalarType = 10; // float32
                break;

            case TextureFormat.RGBAFloat:
                numComponents = 4;
                scalarType = 10; // float32
                break;

            case TextureFormat.RGB565:
                numComponents = 3;
                scalarType = 5; // uint16 (5-bit R, 6-bit G, 5-bit B)
                break;

            case TextureFormat.RHalf:
                numComponents = 1;
                scalarType = 10; // float16
                break;

            case TextureFormat.RGHalf:
                numComponents = 2;
                scalarType = 10; // float16
                break;

            case TextureFormat.RGBAHalf:
                numComponents = 4;
                scalarType = 10; // float16
                break;

            default:
                throw new NotSupportedException($"Formato immagine {texture.format} non supportato.");
        }

        return (scalarType, numComponents);
    }

    private static string ConvertToHex(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    private static string ToHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    private static string StringToHexString(string inputString, int sizeInBytes)
    {
        if (inputString.Length > sizeInBytes)
            inputString = inputString.Substring(0, sizeInBytes);

        byte[] ba = Encoding.Default.GetBytes(inputString);
        string hexString = BitConverter.ToString(ba).Replace("-", "").PadRight(sizeInBytes * 2, '0');
        return hexString;
    }

    private static byte[] StringToByteArray(string hex)
    {
        byte[] arr = new byte[hex.Length >> 1];
        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
        }
        return arr;
    }

    private static int GetHexVal(char hex)
    {
        int val = (int)hex;
        return val - (val < 58 ? 48 : 55); // For uppercase; lowercase would require a different calculation
    }
}
