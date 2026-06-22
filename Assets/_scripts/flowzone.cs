using UnityEngine;

public class SinkWaterController : MonoBehaviour
{
    public ParticleSystem water;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            water.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            water.Stop();
        }
    }
}