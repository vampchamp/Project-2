using UnityEngine;

public class Gestures : MonoBehaviour
{
    public HandwashingManager.WashStep trackedStep;
    public Collider requiredColliderA;
    public Collider requiredColliderB;
    
    public enum GestureType
    {
        PalmPalm,
        PalmBack,
        Thumb,
        Fingertips
    }
}
