using UnityEngine;

public interface IPunchable
{
    void ReceivePunch(int damage, Vector3 hitPoint, int hitStrength = 1);
}
