using UnityEngine;

public class HandSoap : MonoBehaviour
{
    public GameObject soapVisual;

    public bool HasSoap { get; private set; }

    public void AddSoap()
    {
        HasSoap = true;
        soapVisual.SetActive(true);
    }

    public void WashOffSoap()
    {
        HasSoap = false;
        soapVisual.SetActive(false);
    }
}