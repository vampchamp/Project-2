using System;
using UnityEngine;

public enum Handedness { Left, Right }

public class HandSoap : MonoBehaviour
{
    [Tooltip("Set to Left on the left hand and Right on the right hand.")]
    public Handedness hand = Handedness.Right;

    public GameObject soapVisual;
    public bool isWet { get; private set; }
    public bool HasSoap { get; private set; }

    public void SetWet()
    {
        isWet = true;
    }

    public void AddSoap()
    {
        HasSoap = true;
        if (soapVisual != null)
            soapVisual.SetActive(true);
    }

    public void WashOffSoap()
    {
        HasSoap = false;
        if (soapVisual != null)
            soapVisual.SetActive(false);
    }

    public void ResetHand()
    {
        HasSoap = false;
        isWet = false;
        if (soapVisual != null)
            soapVisual.SetActive(false);
    }
}
