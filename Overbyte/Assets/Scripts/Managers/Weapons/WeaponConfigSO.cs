using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/WeaponConfig")]
public class WeaponConfigSO : ScriptableObject
{
    [Header("Weapon Settings")]
    public string weaponName;
    public float fireRate;
    public bool isAutomatic;
    public int burstCount;
    public GameObject muzzlePrefab;
    public GameObject bulletHolePrefab;
    public float bulletHoleDelay;
    public int magazineSize;

    [Header("Recoil Settings")]
    public float recoilUp = 0.05f;
    public float recoilBack = 0.02f;
    public float recoilSide = 0f;
    public float recoilRecovery = 6f;
}
