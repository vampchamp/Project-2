using UnityEngine;

public class HandFoam : MonoBehaviour
{
    public GameObject foamAssetParent;
    public GameObject soap;
    
    public float FoamAmount { get; private set; } = 0f; 

    private Renderer[] foamRenderers;
    private MaterialPropertyBlock propBlock;
    private int colorID;

    void Start()
    {
        propBlock = new MaterialPropertyBlock();
        colorID = Shader.PropertyToID("_BaseColor"); 
        
        if (foamAssetParent != null)
        {
            foamRenderers = foamAssetParent.GetComponentsInChildren<Renderer>();
            UpdateFoamVisibility(0f);
            foamAssetParent.SetActive(false);
        }
    }

    public void BuildFoam(float amount)
    {
        if (!foamAssetParent.activeSelf) foamAssetParent.SetActive(true);

        FoamAmount = Mathf.Clamp01(FoamAmount + amount);
        UpdateFoamVisibility(FoamAmount);
        Renderer renderer = soap.GetComponent<Renderer>();
        Color c = renderer.material.color;
        c.a = 1f - FoamAmount;
        renderer.material.color = c;

        if (FoamAmount >= 1f)
        {
            soap.SetActive(false);
        }
    }

    public void WashFoam(float amount)
    {
        if (FoamAmount <= 0f) return;

        FoamAmount = Mathf.Clamp01(FoamAmount - amount);
        UpdateFoamVisibility(FoamAmount);

        if (FoamAmount <= 0f)
        {
            foamAssetParent.SetActive(false);
        }
    }

    private void UpdateFoamVisibility(float alpha)
    {
        foreach (Renderer ren in foamRenderers)
        {
            if (ren != null)
            {
                ren.GetPropertyBlock(propBlock);
                Color c = ren.sharedMaterial.color;
                c.a = alpha;
                propBlock.SetColor(colorID, c);
                ren.SetPropertyBlock(propBlock);
            }
        }
    }
}