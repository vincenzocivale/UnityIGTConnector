using UnityEngine;
using System.IO;
using System.Diagnostics;

public class Texture3DToRawExporter : MonoBehaviour
{
    public Texture3D texture3D; // Assegna qui la tua Texture3D nell'Inspector
    public string filePath = "Assets/texture3d.raw"; // Percorso dove salvare il file raw

    void Start()
    {
        if (texture3D == null)
        {
            UnityEngine.Debug.LogError("Nessuna Texture3D assegnata.");
            return;
        }

        ExportTexture3DToRaw(texture3D, filePath);
    }

    void ExportTexture3DToRaw(Texture3D texture, string path)
    {
        Color[] colors = texture.GetPixels();
        int totalVoxels = colors.Length;
        int colorComponents = 4; // RGBA
        byte[] rawData = new byte[totalVoxels * colorComponents];

        for (int i = 0; i < totalVoxels; i++)
        {
            rawData[i * colorComponents] = (byte)(colors[i].r * 255);
            rawData[i * colorComponents + 1] = (byte)(colors[i].g * 255);
            rawData[i * colorComponents + 2] = (byte)(colors[i].b * 255);
            rawData[i * colorComponents + 3] = (byte)(colors[i].a * 255);
        }

        File.WriteAllBytes(path, rawData);
        UnityEngine.Debug.Log("Texture3D esportata come raw: " + path);
    }
}
