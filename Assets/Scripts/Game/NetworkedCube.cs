using Mirror;
using UnityEngine;

public class NetworkedCube : NetworkBehaviour
{
    [SyncVar]
    public Color cubeColor = Color.white;

    private void Start()
    {
        GetComponent<Renderer>().material.color = cubeColor;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        GetComponent<Renderer>().material.color = cubeColor;
    }
}
