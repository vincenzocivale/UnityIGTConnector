using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;


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
                    Text text = modelGO.GetComponent<UnityEngine.UI.Text>();
                    string messageContent = text.text;
                    body = CreateStringBody(messageContent);
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

        if (texture == null)
        {
            UnityEngine.Debug.LogError("Texture not found!");
            return null;
        }

        // Versione
        UInt16 version = Convert.ToUInt16(2);
        byte[] versionBytes = BitConverter.GetBytes(version);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(versionBytes); // Inverti per big-endian
        }
        string versionString = BitConverter.ToString(versionBytes).Replace("-", "");

        // Ottieni formato dell'immagine
        var formatInfo = GetImageFormatInfo(texture);
        byte numComponents = formatInfo.numComponents;
        byte scalarType = formatInfo.scalarType;
        byte endian = 1;  // BIG endian
        byte imageCoord = 1; // RAS coordinate

        // Converti i dati in stringhe esadecimali appropriate
        string numComponentsHex = numComponents.ToString("X2");
        string scalarTypeHex = scalarType.ToString("X2");
        string endianHex = endian.ToString("X2");
        string imageCoordHex = imageCoord.ToString("X2");

        // Ottieni dimensioni dell'immagine
        UInt16 width = Convert.ToUInt16(texture.width);
        UInt16 height = Convert.ToUInt16(texture.height);
        UInt16 depth = Convert.ToUInt16(1); // Per immagini 2D, la profondità è 1

        // Converti width, height, depth in big-endian
        byte[] widthString = BitConverter.GetBytes(width);
        byte[] heightString = BitConverter.GetBytes(height);
        byte[] depthString = BitConverter.GetBytes(depth);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(widthString);
            Array.Reverse(heightString);
            Array.Reverse(depthString);
        }

        string dimensionsHex = BitConverter.ToString(widthString).Replace("-", "") +
                                  BitConverter.ToString(heightString).Replace("-", "") +
                                  BitConverter.ToString(depthString).Replace("-", "");

        // Ottieni i dati di trasformazione e dimensioni
        string r_vector = FloatArrayBytesString(new float[] { 1.0f, 0.0f, 0.0f });
        string t_vector = FloatArrayBytesString(new float[] { 0.0f, 1.0f, 0.0f });
        string s_vector = FloatArrayBytesString(new float[] { 0.0f, 0.0f, 1.0f });
        string n_vector = FloatArrayBytesString(new float[] { 0.0f, 0.0f, 0.0f });
        string p_vector = FloatArrayBytesString(new float[] { 0.0f, 0.0f, 0.0f });
        string d_vector = UInt16ArrayBytesString(new UInt16[] { 0, 0, 0 });
        string dr_vector = dimensionsHex;

        // Ottieni i dati grezzi dell'immagine
        byte[] imageData = texture.GetRawTextureData();

        if (imageData == null || imageData.Length == 0)
        {
            UnityEngine.Debug.LogError("Failed to retrieve raw image data.");
            return null;
        }

        // Gestisci endianess per i dati immagine (se necessario)
        if (BitConverter.IsLittleEndian && endian == 1) // Se endian è big-endian
        {
            Array.Reverse(imageData);
        }

        // Converti i dati immagine in stringa esadecimale
        string imageDataHex = BitConverter.ToString(imageData).Replace("-", "");

        // Crea il body finale del messaggio
        string bodyMessage = versionString + numComponentsHex + scalarTypeHex + endianHex + imageCoordHex +
                             dimensionsHex + r_vector + t_vector + s_vector + n_vector + p_vector + d_vector + dr_vector + imageDataHex;

        return bodyMessage;
    }




    public static string CreateStringBody(string messageContent)
    {

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

    public static ulong HexToDecimal(string hex)
    {
        // Converte la stringa esadecimale in un numero decimale
        return ulong.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }

    public static string HexToString(string hex)
    {
        // Rimuovi eventuali spazi o separatori dall'input esadecimale
        hex = hex.Replace(" ", "").Replace("-", "");

        // Inizializza una lista per memorizzare i caratteri
        List<byte> bytes = new List<byte>();

        // Leggi ogni coppia di caratteri esadecimali e convertila in byte
        for (int i = 0; i < hex.Length; i += 2)
        {
            string byteValue = hex.Substring(i, 2);
            bytes.Add(Convert.ToByte(byteValue, 16));
        }

        // Converti i byte in una stringa ASCII
        return Encoding.ASCII.GetString(bytes.ToArray());
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
            case TextureFormat.DXT1:
                numComponents = 3;
                scalarType = 3;
                break;

            default:
                throw new NotSupportedException($"Formato immagine {texture.format} non supportato.");
        }

        return (scalarType, numComponents);
    }

    private static string UInt16ArrayBytesString(UInt16[] array)
    {
        byte[] bytes = new byte[array.Length * sizeof(UInt16)];

        for (int i = 0; i < array.Length; i++)
        {
            byte[] tempBytes = BitConverter.GetBytes(array[i]);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tempBytes);
            }

            Buffer.BlockCopy(tempBytes, 0, bytes, i * sizeof(UInt16), sizeof(UInt16));
        }

        return BitConverter.ToString(bytes).Replace("-", "");
    }


    private static string FloatArrayBytesString(float[] array)
    {
        byte[] bytes = new byte[array.Length * sizeof(float)];

        for (int i = 0; i < array.Length; i++)
        {
            byte[] tempBytes = BitConverter.GetBytes(array[i]);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tempBytes);
            }

            Buffer.BlockCopy(tempBytes, 0, bytes, i * sizeof(float), sizeof(float));
        }

        return BitConverter.ToString(bytes).Replace("-", "");
    }


}



