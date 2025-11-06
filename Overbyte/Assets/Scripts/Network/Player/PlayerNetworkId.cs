using UnityEngine;

public class PlayerNetworkId : MonoBehaviour
{
    public string Uuid { get; private set; }

    public void SetUuid(string uuid)
    {
        if (string.IsNullOrEmpty(uuid))
            throw new System.ArgumentException("UUID no puede ser nulo o vacío");

        if (!string.IsNullOrEmpty(Uuid))
            throw new System.InvalidOperationException("UUID ya está establecido");

        Uuid = uuid;
    }
}
