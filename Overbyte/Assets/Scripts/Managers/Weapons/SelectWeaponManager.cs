using UnityEngine;

public class SelectWeaponManager : MonoBehaviour
{
    public static SelectWeaponManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
