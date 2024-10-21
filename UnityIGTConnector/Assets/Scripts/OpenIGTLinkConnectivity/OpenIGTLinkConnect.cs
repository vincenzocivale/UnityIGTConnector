using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityVolumeRendering;

public class OpenIGTLinkConnect : MonoBehaviour
{

    ///////// CONNECT TO IGT Server PARAMETERS /////////
    uint headerSize = 58; // Size of the header of every OpenIGTLink message
    private SocketHandler socketForUnityAndHoloLens; // Socket to connect to Slicer
    bool isConnected; // Boolean to check if the socket is connected

    // Esposto per l'editor di Unity
    [SerializeField]
    private string ipString = "localhost"; // IP address of the computer running Slicer (default is localhost)
    [SerializeField]
    private int port = 18944; // Port of the computer running Slicer (default port)

    ///////// GENERAL VARIABLES /////////
    int scaleMultiplier = 1000; // Help variable to transform meters to millimeters and vice versa

    ///////// SEND /////////
    CRC64 crcGenerator;
    string CRC;
    ulong crcPolynomial;
    string crcPolynomialBinary = "0100001011110000111000011110101110101001111010100011011010010011";

    ///////// LISTEN /////////

    void Start()
    {
        // Initialize CRC Generator
        crcGenerator = new CRC64();
        crcPolynomial = Convert.ToUInt64(crcPolynomialBinary, 2);
        crcGenerator.Init(crcPolynomial);

        // Ottieni il componente MessageTracker dallo stesso GameObject
       /* messageTracker = GetComponent<MessageTracker>();

        if (messageTracker == null)
        {
            UnityEngine.Debug.LogError("MessageTracker component not found on this GameObject.");
        }
*/
        // Prova a connettersi al server all'avvio
        if (OnConnectToIGTServer())
        {
            UnityEngine.Debug.Log("Successfully connected to IGTLink Server");
            /*StartCoroutine(SendIGTInfo());*/
            StartCoroutine(ListenIGTInfo());
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to connect to the server.");
        }
    }

    // Modificato per non avere parametri e usare le variabili di classe
    public bool OnConnectToIGTServer()
    {
        isConnected = ConnectToIGTServer(ipString, port);
        return isConnected;
    }

    // Create a new socket handler and connect it to the server with the ip address and port provided in the function
    bool ConnectToIGTServer(string ipString, int port)
    {
        socketForUnityAndHoloLens = new SocketHandler();

        bool isConnected = socketForUnityAndHoloLens.Connect(ipString, port);

        return isConnected;
    }

    // Routine that continuously sends the transform information of every model in infoToSend to 3D Slicer
    /*public IEnumerator SendIGTInfo()
    {
        while (true)
        {
            yield return null; // Wait for the next frame

            Dictionary<GameObject, string> changedObjects = messageTracker.CheckForChanges();

            // Iterate through each element in the dictionary
            foreach (KeyValuePair<GameObject, string> entry in changedObjects)
            {
                GameObject modelGO = entry.Key; // Access the GameObject
                string messageType = entry.Value; // Access the message type

                SendMessageServer.SendMessage(modelGO, scaleMultiplier, crcGenerator, socketForUnityAndHoloLens, messageType);
                UnityEngine.Debug.Log("Send");
            }
        }
    }*/

    public IEnumerator ListenIGTInfo()
    {
        while (true)
        {
            yield return null; // Wait for the next frame

            // Read the header of incoming messages
            byte[] iMSGbyteArray = socketForUnityAndHoloLens.Listen(headerSize);

            if (iMSGbyteArray.Length >= (int)headerSize)
            {
                HeaderInfo iHeaderInfo = HeaderInfo.ReadHeaderInfo(iMSGbyteArray);
              
                // Read the body of incoming messages
                uint BodySize = Convert.ToUInt32(iHeaderInfo.BodySize);

                // Process the message when it is complete
                if (iMSGbyteArray.Length >= (int)BodySize + (int)headerSize)
                {
                    if ((iHeaderInfo.MsgType).Contains("TRANSFORM"))
                    {
                        // Handle transform messages here
                    }
                    else if ((iHeaderInfo.MsgType).Contains("IMAGE"))
                    {
                        UnityEngine.Debug.Log("Image received");
                        ImageInfo imageInfo = ImageInfo.ReadImageInfo(iMSGbyteArray, iHeaderInfo);
                        if (imageInfo != null)
                        {
                            imageInfo.Create3DVolume(iMSGbyteArray);
                        }
                    }
                    else if ((iHeaderInfo.MsgType).Contains("POLYDATA"))
                    {
                        UnityEngine.Debug.Log("PolyData received");
                        PolyDataInfo polyDatInfo = PolyDataInfo.ReadPolyDataInfo(iMSGbyteArray, iHeaderInfo);
                        GameObject go = PolyDataInfo.GenerateMeshFromPolyData(polyDatInfo);
                        /*ReadMessageFromServer.ReadPolyDataInfo(iMSGbyteArray, iHeaderInfo.headerSize, iHeaderInfo.ExtHeaderSize);*/
                    }
                    else if ((iHeaderInfo.MsgType).Contains("STRING"))
                    {
                        ReadMessageFromServer.StringInfo stringInfo = ReadMessageFromServer.ReadStringInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);
                        UnityEngine.Debug.Log("String received: " + stringInfo.text);
                    }
                }
            }
        }
    }

    /// Apply transform information to GameObject ///
    void ApplyTransformToGameObject(Matrix4x4 matrix, GameObject gameObject)
    {
        Vector3 translation = matrix.GetColumn(3);
        if (translation.x > 10000 || translation.y > 10000 || translation.z > 10000)
        {
            gameObject.transform.position = new Vector3(0, 0, 0.5f);
            UnityEngine.Debug.Log("Out of limits. Default position assigned.");
        }
        else
        {
            gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
            Vector3 rotation = matrix.rotation.eulerAngles;
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



    private void ImageImporter(byte[] iMSGbyteArray, HeaderInfo iHeaderInfo)
    {
        // Leggi le informazioni sull'immagine
        ReadMessageFromServer.ImageInfo iImageInfo = ReadMessageFromServer.ReadImageInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);

        if (iImageInfo.NumPixX > 0 && iImageInfo.NumPixY > 0 && iImageInfo.NumPixZ > 0)
        {
            // Import the 3D Image
            IGTVolumeImporter importer = new IGTVolumeImporter(iMSGbyteArray, iImageInfo);
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


    // Execute this function when the user exits the application
    void OnApplicationQuit()
    {
        // Release the socket.
        if (socketForUnityAndHoloLens != null)
        {
            socketForUnityAndHoloLens.Disconnect();
        }
    }

    
}