using Mirror;
using UnityEngine;

namespace QuickStart
{
    public class Weapon : NetworkBehaviour
    {
        public float weaponSpeed = 15.0f;
        public float weaponLife = 3.0f;
        public float weaponCooldown = 1.0f;
        public int weaponAmmo = 15;
        public int weaponMaxAmmo = 15;
        public float range = 1000f;
        public GameObject weaponHolder;
        public GameObject weaponBullet;
        public Transform weaponFirePosition;
        public ParticleSystem muzzleFlash;
        public GameObject impactEffect;

        [Server]
        public void Shoot(string attacker)
        {
            //Debug.Log($"iniating shot for {attacker}");
            RaycastHit hit;
            if (Physics.Raycast(weaponHolder.transform.position, weaponHolder.transform.forward, out hit, range))
            {
                Debug.Log(hit.transform.name);

                PlayerScript target = hit.transform.GetComponent<PlayerScript>();
                if (target != null)
                {
                    target.ServerTakeDamage(attacker, target);
                }
            } else
            {
                //Debug.Log("they missed");
            }
        }

    }
}