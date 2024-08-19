using UnityEngine;

public class ContinuousRotation : MonoBehaviour
{
    // Velocità di rotazione in gradi al secondo
    public float rotationSpeed = 30f;

    // Update viene chiamato una volta per frame
    void Update()
    {
        // Calcola la rotazione del frame corrente
        float rotationAmount = rotationSpeed * Time.deltaTime;

        // Ruota l'oggetto attorno al proprio asse Y
        transform.Rotate(Vector3.up, rotationAmount);
    }
}
