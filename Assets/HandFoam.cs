using UnityEngine;

public class HandFoam : MonoBehaviour
{
    public GameObject foamVisual;

    public float foamAmount = 0f;

    private void Update()
    {
        if (foamAmount <= 0f)
        {
            foamAmount = 0f;
            foamVisual.SetActive(false);
        }
        else
        {
            foamVisual.SetActive(true);
        }
    }

    public void AddFoam()
    {
        foamAmount = 1f;
        foamVisual.SetActive(true);
    }

    public void WashFoam(float amount)
    {
        foamAmount -= amount;
    }
}