using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Linq;

public class SendMessageServer : MonoBehaviour
{
    public static void SendMessage(GameObject modelGO, int scaleMultiplier, CRC64 crcGenerator, SocketHandler socketForUnityAndHoloLens, string messageType)
        {
            string modelName = modelGO.name;
            string deviceName = modelName ;
            string oigtlVersion = "0002";
            string body = "";
            ulong timestamp = 0;

            switch (messageType)
            {
                case "TRANSFORM":
                    body = CreateTransformBody(modelGO, scaleMultiplier);
                    break;

                case "IMAGE":
                    body = CreateImage2DBody(modelGO);
                    break;

                case "STRING":
                    body = CreateStringBody(modelGO);
                    break;

                default:
                    UnityEngine.Debug.LogError($"Send message type not supported: {messageType}");
                    break;
            }

            string[] md_keyNames = { "ModelName" };
            string[] md_keyValues = { modelName };

            // Crea il metadata e l'extended header utilizzando la lunghezza del metadata header e del metadata body
            (string hexMetaBody, string hexExtHeader) = CreateMetadata(md_keyNames, md_keyValues);

            // Corpo del messaggio (combinazione di extended header, body e metadata)
            string bodyHex = hexExtHeader + body + hexMetaBody;

            // Crea l'header
            string hexHeader = CreateHeader(oigtlVersion, messageType, deviceName, timestamp, bodyHex);

            // Crea il CRC per il messaggio completo (header + body)
            string crcHex = CreateCRC(bodyHex, crcGenerator);

            // Combina l'header, il CRC e il corpo del messaggio
            string completeMsg = hexHeader + crcHex + bodyHex;

            // Converti il messaggio finale in byte array per l'invio
            byte[] msg = StringToByteArray(completeMsg);

            socketForUnityAndHoloLens.Send(msg);
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

        UInt16 version = Convert.ToUInt16(2);
        byte[] versionBytes = BitConverter.GetBytes(version);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(versionBytes);
        }

        byte numComponents = GetImageFormatInfo(texture).numComponents;
        byte scalarType = GetImageFormatInfo(texture).scalarType;
        byte endian = 1;  // BIG endian
        byte imageCoord = 1; //RAS

        string versionString = BitConverter.ToString(versionBytes).Replace("-", "");
        string numComponentsString = numComponents.ToString("X16"); 
        string scalarTyepString = scalarType.ToString("X16");
        string edianString = endian.ToString("X16");  

        // Recupera le informazioni sull'immagine
        UInt16 width = Convert.ToUInt16(texture.width);
        UInt16 height = Convert.ToUInt16(texture.height);
        UInt16 depth = Convert.ToUInt16(1);

        string r_vector = UInt16ArrayBytesString(new UInt16[] { width, height, depth });
        string t_vector = FloatArrayBytesString(new float[] { 1.0f, 0.0f, 0.0f });
        string n_vector = FloatArrayBytesString(new float[] { 0.0f, 0.0f, 1.0f });
        string p_vector = FloatArrayBytesString(new float[] { 0.0f, 0.0f, 0.0f });
        string d_vector = UInt16ArrayBytesString(new UInt16[] { 0, 0, 0 });
        string dr_vector = UInt16ArrayBytesString(new UInt16[] { width, height, depth });


        byte[] imageData = texture.EncodeToPNG(); // Converti l'immagine in un array di byte PNG

        if (imageData == null || imageData.Length == 0)
        {
            UnityEngine.Debug.LogError("Failed to encode image to PNG format.");
            return null;
        }

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(imageData);
        }
        string imageDataString = BitConverter.ToString(imageData).Replace("-", "");

        return versionString + numComponentsString + scalarTyepString + edianString + r_vector + t_vector + n_vector + p_vector + d_vector + dr_vector + imageDataString;

    }

    public static string CreateStringBody(GameObject modelGO)
    {
        LinkManager linkManager = modelGO.GetComponent<LinkManager>();
        string messageContent = linkManager.stringMessage;

        // Verifica che il messaggio non sia vuoto
        if (string.IsNullOrEmpty(messageContent))
        {
            Debug.LogError("Il contenuto del messaggio è vuoto.");
            return "";
        }

        // Usa US-ASCII (MIBenum = 3)
        UInt16 encodingValue_UINT16 = Convert.ToUInt16(3); // US-ASCII

        // Ottieni il messaggio codificato in ASCII
        byte[] encodedBytes = Encoding.ASCII.GetBytes(messageContent);

        // Lunghezza della stringa
        UInt16 messageLength_UINT16 = Convert.ToUInt16(encodedBytes.Length);

        // Converto i campi ENCODING e LENGTH in byte[] e li inverto se Little Endian
        byte[] value_encoding_BYTES = BitConverter.GetBytes(encodingValue_UINT16);
        byte[] messageLength_BYTES = BitConverter.GetBytes(messageLength_UINT16);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(value_encoding_BYTES);
            Array.Reverse(messageLength_BYTES);
        }

        // Converto tutto in stringa esadecimale
        string ENCODING = BitConverter.ToString(value_encoding_BYTES).Replace("-", "");
        string LENGTH = BitConverter.ToString(messageLength_BYTES).Replace("-", "");
        string CONTENT = BitConverter.ToString(encodedBytes).Replace("-", "");

        // Combino i campi ENCODING, LENGTH e CONTENT
        return ENCODING + LENGTH + CONTENT;
    }


    public static (string, string) CreateMetadata(string[] md_keyNames, string[] md_keyValues)
    {
        // Controlla che il numero di chiavi e valori corrisponda
        if (md_keyNames.Length != md_keyValues.Length)
        {
            throw new ArgumentException("Il numero di chiavi e valori nei metadata non corrisponde.");
        }

        // Inizializza le stringhe per il metaheader e il metadato
        string META_HEADER = "";
        string META_DATA = "";

        // Codifica fissa per US-ASCII
        UInt16 encodingValue_UINT16 = Convert.ToUInt16(3);
        byte[] value_encoding_BYTES = BitConverter.GetBytes(encodingValue_UINT16);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(value_encoding_BYTES);
        }
        string VALUE_ENCODING = BitConverter.ToString(value_encoding_BYTES).Replace("-", "");

        for (int index = 0; index < md_keyNames.Length; index++)
        {
            // Converti la chiave e il valore in byte
            byte[] currentKey_BYTES = Encoding.ASCII.GetBytes(md_keyNames[index]);
            byte[] currentValue_BYTES = Encoding.ASCII.GetBytes(md_keyValues[index]);

            // Costruisci il META_HEADER
            UInt16 keySize_UINT16 = Convert.ToUInt16(currentKey_BYTES.Length);
            UInt32 valueSize_UINT32 = Convert.ToUInt32(currentValue_BYTES.Length);
            byte[] key_size_BYTES = BitConverter.GetBytes(keySize_UINT16);
            byte[] value_size_BYTES = BitConverter.GetBytes(valueSize_UINT32);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(key_size_BYTES);
                Array.Reverse(value_size_BYTES);
            }
            string KEY_SIZE = BitConverter.ToString(key_size_BYTES).Replace("-", "");
            string VALUE_SIZE = BitConverter.ToString(value_size_BYTES).Replace("-", "");
            META_HEADER += KEY_SIZE + VALUE_ENCODING + VALUE_SIZE;

            // Costruisci il META_DATA
            string KEY = BitConverter.ToString(currentKey_BYTES).Replace("-", "");
            string VALUE = BitConverter.ToString(currentValue_BYTES).Replace("-", "");
            META_DATA += KEY + VALUE;
        }

        // Numero di attributi (INDEX_COUNT)
        UInt16 countIndexes_UINT16 = Convert.ToUInt16(md_keyNames.Length);
        byte[] index_count_BYTES = BitConverter.GetBytes(countIndexes_UINT16);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(index_count_BYTES);
        }
        string INDEX_COUNT = BitConverter.ToString(index_count_BYTES).Replace("-", "");

        // Combina il metaheader e il metadato in un'unica stringa esadecimale
        string hexMetaBody = INDEX_COUNT + META_HEADER + META_DATA;

        // Creo l'extended header in base ai dai meta
        string hexExtHeader = CreateExtendedHeader(INDEX_COUNT, META_HEADER, META_DATA);

        return (hexMetaBody, hexExtHeader);
    }

    public static string CreateExtendedHeader(string INDEX_COUNT, string META_HEADER, string META_DATA)
    {
        // Convert values to the corresponding variable type
        UInt16 extHeaderSize_UINT16 = Convert.ToUInt16(12);
        UInt16 metadataHeaderSize_UINT16 = Convert.ToUInt16((INDEX_COUNT.Length + META_HEADER.Length) / 2);
        UInt32 metadataSize_UINT32 = Convert.ToUInt32(META_DATA.Length / 2);
        UInt32 msgID_UINT32 = Convert.ToUInt32(0);

        // Convert these variables into byte[]
        byte[] extHeaderSize_BYTES = BitConverter.GetBytes(extHeaderSize_UINT16);
        byte[] metadataHeaderSize_BYTES = BitConverter.GetBytes(metadataHeaderSize_UINT16);
        byte[] metadataSize_BYTES = BitConverter.GetBytes(metadataSize_UINT32);
        byte[] msgID_BYTES = BitConverter.GetBytes(msgID_UINT32);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(extHeaderSize_BYTES);
            Array.Reverse(metadataHeaderSize_BYTES);
            Array.Reverse(metadataSize_BYTES);
            Array.Reverse(msgID_BYTES);
        }
        // Convert the byte[] into hexadecimal strings
        string EXT_HEADER_SIZE = BitConverter.ToString(extHeaderSize_BYTES).Replace("-", "");
        string METADATA_HEADER_SIZE = BitConverter.ToString(metadataHeaderSize_BYTES).Replace("-", "");
        string METADATA_SIZE = BitConverter.ToString(metadataSize_BYTES).Replace("-", "");
        string MSG_ID = BitConverter.ToString(msgID_BYTES).Replace("-", "");

        // Create final extended header
        string hexExtHeader = EXT_HEADER_SIZE + METADATA_HEADER_SIZE + METADATA_SIZE + MSG_ID;

        return hexExtHeader;
    }


    public static string CreateHeader(string version, string messageType, string deviceName, ulong timestamp, string bodyHex)
    {
        // Versione in esadecimale (2 byte)
        string oigtlVersion = version;

        // Tipo di messaggio in esadecimale (trasformato in 12 byte, riempito di zeri a destra se necessario)
        string messageTypeHex = StringToHexString(messageType, 12);

        // Nome del dispositivo in esadecimale (trasformato in 20 byte, riempito di zeri a destra se necessario)
        string deviceNameHex = StringToHexString(deviceName, 20);

        // Timestamp in esadecimale (8 byte, rappresentato come UInt64)
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }
        string timeStampHex = BitConverter.ToString(timestampBytes).Replace("-", "");

        // Dimensione del corpo del messaggio in esadecimale (8 byte, rappresentato come UInt64)
        string bodySize = (bodyHex.Length / 2).ToString("X16");

        // Combina tutti i componenti per creare l'header
        string hexHeader = oigtlVersion + messageTypeHex + deviceNameHex + timeStampHex + bodySize;

        return hexHeader;
    }

    public static string CreateCRC(string hexMessage, CRC64 crcGenerator)
    {
        // Converti il messaggio esadecimale in un array di byte
        byte[] messageBytes = StringToByteArray(hexMessage);

        // Calcola il CRC utilizzando il generatore CRC64
        ulong crcULong = crcGenerator.Compute(messageBytes, 0, 0);

        // Converti il risultato del CRC in una stringa esadecimale di 16 caratteri (8 byte)
        string crcHex = crcULong.ToString("X16");

        return crcHex;
    }


    // Funzioni di utilità
    public static string StringToHexString(string inputString, int sizeInBytes)
    {
        if (inputString.Length > sizeInBytes)
        {
            inputString = inputString.Substring(0, sizeInBytes);
        }

        byte[] ba = Encoding.Default.GetBytes(inputString);
        string hexString = BitConverter.ToString(ba);
        hexString = hexString.Replace("-", "");
        hexString = hexString.PadRight(sizeInBytes * 2, '0');
        return hexString;
    }

    public static byte[] StringToByteArray(string hex)
    {
        byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < (hex.Length >> 1); ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
        }

        return arr;
    }

    static int GetHexVal(char hex)
    {
        int val = (int)hex;
        return val - (val < 58 ? 48 : 55);
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

    private static string UInt16ArrayBytesString(UInt16[] values)
    {
        List<string> stringArray = new List<string>();
        foreach (var value in values)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueBytes);
            }
            stringArray.Add(BitConverter.ToString(valueBytes).Replace("-", ""));
        }
        return string.Join("", stringArray);
    }

    private static string FloatArrayBytesString(float[] values)
    {
        List<string> stringArray = new List<string>();
        foreach (var value in values)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueBytes);
            }
            stringArray.Add(BitConverter.ToString(valueBytes).Replace("-", ""));
        }
        return string.Join("", stringArray);
    }

}



