// OpenIGTLinkConnect: Gestisce la connessione e la comunicazione con 3D Slicer via OpenIGTLink.
// Start: Configura il CRC e le texture.
// OnConnectToSlicerClick e ConnectToSlicer: Gestiscono la connessione a Slicer.
// SendTransformInfo e ListenSlicerInfo: Gestiscono l'invio e la ricezione di messaggi.
// ApplyTransformToGameObject e ApplyImageInfo: Applicano trasformazioni e immagini agli oggetti di gioco.

using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Runtime;

using Microsoft.MixedReality.Toolkit.UI;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using UnityVolumeRendering;


public class OpenIGTLinkConnect : MonoBehaviour
{
    ///////// CONNECT TO 3D SLICER PARAMETERS /////////
    uint headerSize = 58; // Size of the header of every OpenIGTLink message
    private SocketHandler socketForUnityAndHoloLens; // Socket to connect to Slicer
    bool isConnected; // Boolean to check if the socket is connected
    public string ipString; // IP address of the computer running Slicer
    public int port; // Port of the computer running Slicer
    

    ///////// GENERAL VARIABLES /////////
    int scaleMultiplier = 1000; // Help variable to transform meters to millimeters and vice versa
    
       
    ///////// SEND /////////
    /*public List<ModelInfo> infoToSend; // Array of Models to send to Slicer*/
    
    /// CRC ECMA-182 to send messages to Slicer ///
    CRC64 crcGenerator;
    string CRC;
    ulong crcPolynomial;
    string crcPolynomialBinary = "0100001011110000111000011110101110101001111010100011011010010011";


    ///////// LISTEN /////////

    /// Image transfer information ///
    /*[HideInInspector] public GameObject movingPlane; // Plane to display image on
    Material mediaMaterial; // Material of the plane*/
    

    /*GameObject fixPlane; // Fix plane to display image on
    Material fixPlaneMaterial; // Material of the plane*/

    public GameObject ImageDisplay;
    Texture2D mediaTexture; // Texture of the plane
    Material ImageDisplayMaterial; // Material of the plane


    void Start()
    {
        // Initialize CRC Generator
        crcGenerator = new CRC64();
        crcPolynomial = Convert.ToUInt64(crcPolynomialBinary, 2);
        crcGenerator.Init(crcPolynomial);
    }

    // This function is called when the user activates the connectivity switch to start the communication with 3D Slicer
    public bool OnConnectToSlicerClick(string ipString, int port)
    {
        isConnected = ConnectToSlicer(ipString, port);
        return isConnected;
    }

    // Create a new socket handler and connect it to the server with the ip address and port provided in the function
    bool ConnectToSlicer(string ipString, int port)
    {
        socketForUnityAndHoloLens = new SocketHandler();

        bool isConnected = socketForUnityAndHoloLens.Connect(ipString, port);

        return isConnected;
        
    }

    // Routine that continuously sends the transform information of every model in infoToSend to 3D Slicer
    public IEnumerator SendTransformInfo()
    {
        while (true)
        {
            /*UnityEngine.Debug.Log("Sending...");*/
            yield return null; // If you had written yield return new WaitForSeconds(1); it would have waited 1 second before executing the code below.
            // Loop foreach element in infoToSend
            /*foreach (ModelInfo element in infoToSend)
            {
                SendMessageToServer.SendTransformMessage(element, scaleMultiplier, crcGenerator, CRC, socketForUnityAndHoloLens);
            }*/
        }
    }

    public IEnumerator ListenSlicerInfo()
    {
        while (true)
        {
            /*UnityEngine.Debug.Log("Listening...");*/
            yield return null;

            ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
            byte[] iMSGbyteArray = socketForUnityAndHoloLens.Listen(headerSize);

            if (iMSGbyteArray.Length >= (int)headerSize)
            {
                ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
                ReadMessageFromServer.HeaderInfo iHeaderInfo = ReadMessageFromServer.ReadHeaderInfo(iMSGbyteArray);

                ////////// READ THE BODY OF THE INCOMING MESSAGES //////////
                uint BodySize = Convert.ToUInt32(iHeaderInfo.BodySize);

                // Process the message when it is complete (that means, we have received as many bytes as the body size + the header size)
                if (iMSGbyteArray.Length >= (int)BodySize + (int)headerSize)
                {
                    // Compare different message types and act accordingly
                    if ((iHeaderInfo.MsgType).Contains("TRANSFORM"))
                    {
                        /*// Extract the transform matrix from the message
                        Matrix4x4 matrix = ReadMessageFromServer.ExtractTransformInfo(iMSGbyteArray, movingPlane, scaleMultiplier, headerSize);
                        // Apply the transform matrix to the object
                        ApplyTransformToGameObject(matrix, movingPlane);*/
                    }
                    else if ((iHeaderInfo.MsgType).Contains("IMAGE"))
                    {
                        UnityEngine.Debug.Log("Image recived");

                        // Read and display the image content to our preview plane
                        OnOpenIGTDatasetResultAsync(iMSGbyteArray, iHeaderInfo);
                        UnityEngine.Debug.Log("Image displayed");
                    }
                    else if ((iHeaderInfo.MsgType).Contains("POLYDATA"))
                    {
                        UnityEngine.Debug.Log("Recived polydata message");
                        // Extract POLYDATA information from the message
                        ReadMessageFromServer.PolyDataInfo polyDataInfo = ReadMessageFromServer.ReadPolyDataInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);
                        // Create a GameObject from the POLYDATA
                        CreateGameObjectFromPolyData(polyDataInfo);
                    }
                }
            }
        }
    }

    /// Apply transform information to GameObject ///
    void ApplyTransformToGameObject(Matrix4x4 matrix, GameObject gameObject)
    {
        Vector3 translation = matrix.GetColumn(3);
        //gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
        //Vector3 rotation= matrix.rotation.eulerAngles;
        //gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        if (translation.x > 10000 || translation.y > 10000 || translation.z > 10000)
        {
            gameObject.transform.position = new Vector3(0, 0, 0.5f);
            UnityEngine.Debug.Log("Out of limits. Default position assigned.");
        }
        else
        {
            gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
            Vector3 rotation= matrix.rotation.eulerAngles;
            gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        }
    }

    //////////////////////////////// INCOMING IMAGE MESSAGE ////////////////////////////////
    public static Color[] ExtractImageColors(byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
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

        // Salva l'array di colori in un file binario
        SaveColorsToBinaryFile(colors, "ExtractedColors.bin");

        return colors;
    }


    private static float ConvertScalarToFloat(byte[] data, int startIndex, int bytesPerScalar, int scalarType)
    {
        switch (scalarType)
        {
            case 2: // int8
                return (float)data[startIndex] / 127f;
            case 3: // uint8
                return (float)data[startIndex] / 255f;
            case 4: // int16
                return (float)BitConverter.ToInt16(data, startIndex) / 255.0f;
            case 5: // uint16
                return (float)BitConverter.ToUInt16(data, startIndex) / 255.0f;
            case 6: // int32
                return (float)BitConverter.ToInt32(data, startIndex) / 255.0f;
            case 7: // uint32
                return (float)BitConverter.ToUInt32(data, startIndex) / 255.0f;
            case 10: // float32
                return BitConverter.ToSingle(data, startIndex);
            case 11: // float64
                return (float)BitConverter.ToDouble(data, startIndex);
            default:
                throw new Exception("ScalarType non supportato");
        }
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
    }


    static Texture3D CreateTexture3D(byte[] iMSGbyteArray, ReadMessageFromServer.ImageInfo iImageInfo)
    {
        // Set the texture parameters
        int width = iImageInfo.NumPixX;
        int height = iImageInfo.NumPixY;
        int depth = iImageInfo.NumPixZ;

        // Determina il formato della texture
        TextureFormat format = DetermineTextureFormat(iImageInfo.ScalarType, iImageInfo.ImComp);

        TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        // Create the texture and apply the parameters
        Texture3D texture = new Texture3D(width, height, depth, format, false);
        texture.wrapMode = wrapMode;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 0;

        // Estrai i colori dall'immagine
        Color[] colors = ExtractImageColors(iMSGbyteArray, iImageInfo);

        // Copy the color values to the texture
        texture.SetPixels(colors);

        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply();

        return texture;
    }

    // Metodo per salvare l'array di colori in un file binario
    private static void SaveColorsToBinaryFile(Color[] colors, string fileName)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(UnityEngine.Application.persistentDataPath, fileName), FileMode.Create)))
        {
            writer.Write(colors.Length);  // Scrivi la lunghezza dell'array
            foreach (var color in colors)
            {
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
                writer.Write(color.a);
            }
        }
    }



    void Display3DImage(byte[] iMSGbyteArray, ReadMessageFromServer.HeaderInfo iHeaderInfo)
    {
        // Leggi le informazioni sull'immagine
        ReadMessageFromServer.ImageInfo iImageInfo = ReadMessageFromServer.ReadImageInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);

        if (iImageInfo.NumPixX > 0 && iImageInfo.NumPixY > 0 && iImageInfo.NumPixZ > 0)
        {
            Texture3D texture = CreateTexture3D(iMSGbyteArray, iImageInfo);

            GameObject volumeRenderingGO = GameObject.Find("VolumeRendering");

            if (volumeRenderingGO != null)
            {
                // Accedi allo script VolumeRendering attaccato al GameObject
                VolumeRendering.VolumeRendering volumeRenderingScript = volumeRenderingGO.GetComponent<VolumeRendering.VolumeRendering>();

                if (volumeRenderingScript != null)
                {
                    // Aggiorna la texture 3D utilizzando il metodo SetVolume
                    volumeRenderingScript.SetVolumeTexture(texture);

                }
                else
                {
                    UnityEngine.Debug.LogError("Il componente VolumeRendering non è stato trovato sul GameObject.");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Il GameObject 'VolumeRendering' non è stato trovato nella scena.");
            }

        }
        else
        {
            UnityEngine.Debug.LogError("Dimensioni dell'immagine non valide.");
        }
    }

    private async void OnOpenIGTDatasetResultAsync(byte[] iMSGbyteArray, ReadMessageFromServer.HeaderInfo iHeaderInfo)
    {
        // Leggi le informazioni sull'immagine
        ReadMessageFromServer.ImageInfo iImageInfo = ReadMessageFromServer.ReadImageInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);

        if (iImageInfo.NumPixX > 0 && iImageInfo.NumPixY > 0 && iImageInfo.NumPixZ > 0)
        {
            // Import the dataset
            OpenIGTImporter importer = new OpenIGTImporter(iMSGbyteArray, iImageInfo);
            VolumeDataset dataset = importer.Import(iMSGbyteArray, iImageInfo);
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



    // Called when the user disconnects Unity from 3D Slicer using the connectivity switch
    public void OnDisconnectClick()
    {
        socketForUnityAndHoloLens.Disconnect();
        UnityEngine.Debug.Log("Disconnected from the server");
    }


    // Execute this function when the user exits the application
    void OnApplicationQuit()
    {
        // Release the socket.
        if (socketForUnityAndHoloLens != null)
        {
            socketForUnityAndHoloLens.Disconnect();
        }
    }

    public static GameObject CreateGameObjectFromPolyData(ReadMessageFromServer.PolyDataInfo polyDataInfo)
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
                break; // Esci dal loop una volta trovato l'attributo del colore
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

        /*// Aggiungi il componente ContinuousRotation
        ContinuousRotation rotationScript = newObject.AddComponent<ContinuousRotation>();
        rotationScript.rotationSpeed = 30f; 
*/
        return newObject;
    }

    







}