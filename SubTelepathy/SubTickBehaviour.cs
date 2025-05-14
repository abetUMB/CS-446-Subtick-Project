using UnityEngine;
using Mirror;

public abstract class SubTickBehaviour : NetworkBehaviour
{
    public abstract void PerformAction(int action);
}
