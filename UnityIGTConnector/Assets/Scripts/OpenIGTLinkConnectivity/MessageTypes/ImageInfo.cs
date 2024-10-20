using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityVolumeRendering;
using static ReadMessageFromServer;

public class ImageInfo
{
    public HeaderInfo HeaderInfo;
    public UInt16 VersionNumber { get; set; }
    public int ImComp { get; set; }
    public int ScalarType { get; set; }
    public int Endian { get; set; }
    public int ImCoord { get; set; }
    public UInt16 NumPixX { get; set; }
    public UInt16 NumPixY { get; set; }
    public UInt16 NumPixZ { get; set; }
    public float Xi { get; set; }
    public float Yi { get; set; }
    public float Zi { get; set; }
    public float Xj { get; set; }
    public float Yj { get; set; }
    public float Zj { get; set; }
    public float Xk { get; set; }
    public float Yk { get; set; }
    public float Zk { get; set; }
    public float CenterPosX { get; set; }
    public float CenterPosY { get; set; }
    public float CenterPosZ { get; set; }
    public UInt16 StartingIndexSVX { get; set; }
    public UInt16 StartingIndexSVY { get; set; }
    public UInt16 StartingIndexSVZ { get; set; }
    public UInt16 NumPixSVX { get; set; }
    public UInt16 NumPixSVY { get; set; }
    public UInt16 NumPixSVZ { get; set; }
    public int OffsetBeforeImageContent { get; set; }

    public byte[] ImageData { get; set; }


    // Read incoming image's information
    public static ImageInfo ReadImageInfo(byte[] iMSGbyteArrayComplete, HeaderInfo headerInfo)
    {
        
        // Define the variables stored in the body of the message
        int[] bodyArrayLengths = new int[] { 2, 1, 1, 1, 1, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2 };

        ImageInfo incomingImageInfo = new ImageInfo();

        incomingImageInfo.HeaderInfo = headerInfo;

        int skipTheseBytes = (int)headerInfo.headerSize + (int)headerInfo.ExtHeaderSize;

        for (int index = 0; index < bodyArrayLengths.Length; index++)
        {
            byte[] sectionByteArray = new byte[bodyArrayLengths[index]];
            Buffer.BlockCopy(iMSGbyteArrayComplete, skipTheseBytes, sectionByteArray, 0, bodyArrayLengths[index]);
            skipTheseBytes += bodyArrayLengths[index];

            if (BitConverter.IsLittleEndian && (bodyArrayLengths[index] > 1))
            {
                Array.Reverse(sectionByteArray);
            }

            switch (index)
            {
                case 0:
                    incomingImageInfo.VersionNumber = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 1:
                    incomingImageInfo.ImComp = sectionByteArray[0];
                    break;
                case 2:
                    incomingImageInfo.ScalarType = sectionByteArray[0];
                    break;
                case 3:
                    incomingImageInfo.Endian = sectionByteArray[0];
                    break;
                case 4:
                    incomingImageInfo.ImCoord = sectionByteArray[0];
                    break;
                case 5:
                    incomingImageInfo.NumPixX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 6:
                    incomingImageInfo.NumPixY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 7:
                    incomingImageInfo.NumPixZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 8:
                    incomingImageInfo.Xi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 9:
                    incomingImageInfo.Yi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 10:
                    incomingImageInfo.Zi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 11:
                    incomingImageInfo.Xj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 12:
                    incomingImageInfo.Yj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 13:
                    incomingImageInfo.Zj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 14:
                    incomingImageInfo.Xk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 15:
                    incomingImageInfo.Yk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 16:
                    incomingImageInfo.Zk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 17:
                    incomingImageInfo.CenterPosX = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 18:
                    incomingImageInfo.CenterPosY = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 19:
                    incomingImageInfo.CenterPosZ = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 20:
                    incomingImageInfo.StartingIndexSVX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 21:
                    incomingImageInfo.StartingIndexSVY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 22:
                    incomingImageInfo.StartingIndexSVZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 23:
                    incomingImageInfo.NumPixSVX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 24:
                    incomingImageInfo.NumPixSVY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 25:
                    incomingImageInfo.NumPixSVZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                default:
                    break;
            }
        }

        incomingImageInfo.OffsetBeforeImageContent = skipTheseBytes;

        int bytesPerScalar = incomingImageInfo.GetBytesPerScalar();
        int uDimension = incomingImageInfo.NumPixSVX * incomingImageInfo.NumPixSVY * incomingImageInfo.NumPixSVZ;

        byte[] imageData = new byte[uDimension * bytesPerScalar];
        int bytesToCopy = uDimension * bytesPerScalar;
        Buffer.BlockCopy(iMSGbyteArrayComplete, incomingImageInfo.OffsetBeforeImageContent, imageData, 0, bytesToCopy);

        incomingImageInfo.ImageData = imageData;

        return incomingImageInfo;
    }

   

    // Funzione per creare un VolumeRendering a partire dalle informazioni contenute in imageInfo
    public void Create3DVolume(byte[] iMSGbyteArrayComplete)
    {
        if (NumPixX > 0 && NumPixY > 0 && NumPixZ > 0)
        {
            DespawnAllDatasets();
            if (ImComp != 1)
            {
                UnityEngine.Debug.LogError("Non è possibile effettuare volume rendering per immagini con rgb");
            }
            VolumeDataset dataset = ScriptableObject.CreateInstance<VolumeDataset>();
            ImportInternal(dataset, iMSGbyteArrayComplete);

            // Spawn the object
            if (dataset != null)
            {
                VolumeObjectFactory.CreateObject(dataset);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Dimensioni dell'immagine non valide.");
        }
    }

    public void PrintImageInfo()
    {
        // Crea una stringa con le informazioni
        string info = $"Image Info:\n" +
                      $"NumPixSVX: {NumPixSVX}\n" +
                      $"NumPixSVY: {NumPixSVY}\n" +
                      $"NumPixSVZ: {NumPixSVZ}\n" +
                      $"ScalarType: {ScalarType}\n";

        // Stampa la stringa nel log di Unity
        Debug.Log(info);
    }

    private async Task<VolumeDataset> ImportAsync(byte[] iMSGbyteArrayComplete)
    {
        DespawnAllDatasets();
        if (ImComp != 1)
        {
            Debug.LogError("Non è possibile effettuare volume rendering per immagini con rgb");
            return null;
        }

        VolumeDataset dataset = ScriptableObject.CreateInstance<VolumeDataset>();
        await Task.Run(() => ImportInternal(dataset, iMSGbyteArrayComplete));
        return dataset;
    }

    private void ImportInternal(VolumeDataset dataset, byte[] iMSGbyteArrayComplete)
    {
        dataset.datasetName = null;
        dataset.filePath = null; // Se hai bisogno di un filePath, puoi impostarlo qui
        dataset.dimX = (int)NumPixSVX;
        dataset.dimY = (int)NumPixSVY;
        dataset.dimZ = (int)NumPixSVZ;

        int bytesPerScalar = GetBytesPerScalar();
        int uDimension = dataset.dimX * dataset.dimY * dataset.dimZ;
        dataset.data = new float[uDimension];

        int imageDataIndex = 0;
        for (int z = 0; z < dataset.dimZ; z++)
        {
            for (int y = 0; y < dataset.dimY; y++)
            {
                for (int x = 0; x < dataset.dimX; x++)
                {
                    int flippedIndex = x + (dataset.dimY - 1 - y) * dataset.dimX + z * dataset.dimX * dataset.dimY;
                    dataset.data[flippedIndex] = ConvertScalarToFloat(ImageData, imageDataIndex, bytesPerScalar);
                    imageDataIndex += bytesPerScalar;
                }
            }
        }

        dataset.FixDimensions();
    }

    private int GetBytesPerScalar()
    {
        return ScalarType switch
        {
            2 => 1,
            3 => 1,
            4 => 2,
            5 => 2,
            6 => 4,
            7 => 4,
            10 => 4,
            11 => 8,
            _ => throw new Exception("ScalarType non supportato: " + ScalarType)
        };
    }

    private float ConvertScalarToFloat(byte[] data, int startIndex, int bytesPerScalar)
    {
        switch (ScalarType)
        {
            case 2: // int8
                return ((float)data[startIndex] / 127f) * 256f; // Normalizza
            case 3: // uint8
                return ((float)data[startIndex] / 255f) * 256f; // Normalizza
            case 4: // int16
                return ((float)BitConverter.ToInt16(data, startIndex) / 32767f) * 256f; // Normalizza
            case 5: // uint16
                return ((float)BitConverter.ToUInt16(data, startIndex) / 65535f) * 256f; // Normalizza
            case 6: // int32
                return ((float)BitConverter.ToInt32(data, startIndex) / 2147483647f) * 256f; // Normalizza
            case 7: // uint32
                return ((float)BitConverter.ToUInt32(data, startIndex) / 4294967295f) * 256f; // Normalizza
            case 10: // float32
                return BitConverter.ToSingle(data, startIndex) * 256f; // Scala
            case 11: // float64
                return (float)(BitConverter.ToDouble(data, startIndex) * 256f); // Scala
            default:
                throw new Exception("ScalarType non supportato");
        }
    }

    private void DespawnAllDatasets()
    {
        VolumeRenderedObject[] volobjs = GameObject.FindObjectsOfType<VolumeRenderedObject>();
        foreach (VolumeRenderedObject volobj in volobjs)
        {
            GameObject.Destroy(volobj.gameObject);
        }
    }

    /*public static Color[] ExtractImageColors(byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
    {
        int totalPixels = imageInfo.NumPixSVX * imageInfo.NumPixSVY * imageInfo.NumPixSVZ;
        int numComponents = imageInfo.ImComp;
        int bytesPerScalar = imageInfo.ScalarType switch
        {
            2 => 1,
            3 => 1,
            4 => 2,
            5 => 2,
            6 => 4,
            7 => 4,
            10 => 4,
            11 => 8,
            _ => throw new Exception("ScalarType non supportato")
        };
        int imageDataSize = totalPixels * numComponents * bytesPerScalar;

        byte[] imageData = new byte[imageDataSize];
        Buffer.BlockCopy(iMSGbyteArrayComplete, imageInfo.OffsetBeforeImageContent, imageData, 0, imageDataSize);

        Color[] colors = new Color[totalPixels];
        int index = 0;

        // Calcolo delle dimensioni della sub-volume per gli indici
        int width = imageInfo.NumPixSVX;
        int height = imageInfo.NumPixSVY;
        int depth = imageInfo.NumPixSVZ;

        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 1f;

                    if (numComponents >= 1)
                        r = ConvertScalarToFloat(imageData, index, bytesPerScalar, imageInfo.ScalarType);
                    if (numComponents >= 2)
                        g = ConvertScalarToFloat(imageData, index + bytesPerScalar, bytesPerScalar, imageInfo.ScalarType);
                    if (numComponents >= 3)
                        b = ConvertScalarToFloat(imageData, index + 2 * bytesPerScalar, bytesPerScalar, imageInfo.ScalarType);
                    if (numComponents == 4)
                        a = ConvertScalarToFloat(imageData, index + 3 * bytesPerScalar, bytesPerScalar, imageInfo.ScalarType);

                    // Indice per l'array di colori considerando l'inversione dell'asse Y
                    int flippedIndex = x + (height - 1 - y) * width + z * width * height;
                    colors[flippedIndex] = new Color(r, g, b, a);

                    index += numComponents * bytesPerScalar;
                }
            }
        }


        return colors;
    }
    
     static TextureFormat DetermineTextureFormat(int scalarType, int imComp)
    {
        switch (scalarType)
        {
            case 2: // int8
            case 3: // uint8
                return imComp switch
                {
                    1 => TextureFormat.R8,
                    2 => TextureFormat.RG16,  // RG8 non è supportato da Unity, quindi passiamo a RG16
                    3 => TextureFormat.RGB24,
                    4 => TextureFormat.RGBA32,
                    _ => throw new Exception("Numero di componenti non supportato")
                };
            case 4:
                return imComp switch
                {
                    1 => TextureFormat.R16,
                    2 => TextureFormat.RG16,
                    4 => TextureFormat.RGBAHalf, // RGBA16 non esiste, quindi RGBAHalf è la migliore approssimazione
                    _ => throw new Exception("Numero di componenti non supportato")
                };
            case 5: // uint16
                return imComp switch
                {
                    1 => TextureFormat.R16,
                    2 => TextureFormat.RG16,
                    4 => TextureFormat.RGBAHalf, // RGBA16 non esiste, quindi RGBAHalf è la migliore approssimazione
                    _ => throw new Exception("Numero di componenti non supportato")
                };
            case 6: // int32
            case 7: // uint32
            case 10: // float32
                return imComp switch
                {
                    1 => TextureFormat.RFloat,
                    2 => TextureFormat.RGFloat,
                    4 => TextureFormat.RGBAFloat,
                    _ => throw new Exception("Numero di componenti non supportato")
                };
            case 11: // float64
                     // Richiede conversione a float32 prima della creazione della texture
                throw new Exception("float64 non supportato direttamente. Convertire a float32");
            default:
                throw new Exception("ScalarType non supportato");
        }
    }*/
}

