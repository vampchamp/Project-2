using UnityEngine;
using System.Collections.Generic;

public class HandFoam : MonoBehaviour
{
    [Header("Foam Objects")]
    [SerializeField] private GameObject[] foamAssetParents;

    [Header("Soap Objects")]
    [SerializeField] private GameObject[] soaps;

    public float FoamAmount { get; private set; } = 0f;

    private Renderer[] foamRenderers;

    void Start()
    {
        List<Renderer> renderers = new List<Renderer>();

        foreach (GameObject foam in foamAssetParents)
        {
            renderers.AddRange(foam.GetComponentsInChildren<Renderer>());
            foam.SetActive(false);
        }

        foamRenderers = renderers.ToArray();

        UpdateFoamVisibility(0f);
    }

    public void BuildFoam(float amount)
    {
        foreach (GameObject foam in foamAssetParents)
        {
            if (!foam.activeSelf)
                foam.SetActive(true);
        }

        FoamAmount = Mathf.Clamp01(FoamAmount + amount);

        UpdateFoamVisibility(FoamAmount);

        foreach (GameObject soap in soaps)
        {
            if (soap == null)
                continue;

            Renderer renderer = soap.GetComponent<Renderer>();

            if (renderer == null)
                continue;

            Color c = renderer.material.color;
            c.a = 1f - FoamAmount;
            renderer.material.color = c;

            if (FoamAmount >= 1f)
                soap.SetActive(false);
        }
    }

    public void WashFoam(float amount)
    {
        if (FoamAmount <= 0f)
            return;

        FoamAmount = Mathf.Clamp01(FoamAmount - amount);

        UpdateFoamVisibility(FoamAmount);

        if (FoamAmount <= 0f)
        {
            foreach (GameObject foam in foamAssetParents)
                foam.SetActive(false);
        }
    }

    public void ResetFoam()
    {
        FoamAmount = 0f;

        foreach (GameObject foam in foamAssetParents)
        {
            if (foam != null)
                foam.SetActive(false);
        }

        UpdateFoamVisibility(0f);

        foreach (GameObject soap in soaps)
        {
            if (soap == null)
                continue;

            Renderer renderer = soap.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            Color c = renderer.material.color;
            c.a = 1f;
            renderer.material.color = c;
        }
    }

    private void UpdateFoamVisibility(float alpha)
    {
        foreach (Renderer ren in foamRenderers)
        {
            if (ren == null)
                continue;

            Color c = ren.material.color;
            c.a = alpha;
            ren.material.color = c;
        }
    }
}