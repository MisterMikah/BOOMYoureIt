using UnityEngine;

public class BombCarrier : MonoBehaviour
{
    [SerializeField] private GameObject bombIcon; // drag your BombIcon child here
    public bool IsCarrier { get; private set; }

    public void SetCarrier(bool value)
    {
        IsCarrier = value;
        if (bombIcon) bombIcon.SetActive(value);
    }
}
