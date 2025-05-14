using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QuickStart
{
    public class ShootManager : NetworkBehaviour
    {
        public Button shootButton;

        [SerializeField] private DateTime agreedTime;
        [SerializeField] private int waitTime = 1;

        [SerializeField] PlayerScript playerScript = null;
        
        void Start()
        {
            agreedTime = DateTime.Now;

            shootButton.onClick.AddListener(OnButtonClick);
            shootButton.gameObject.SetActive(true);
        }

        public override void OnStartClient()
        {
            Debug.Log("onstart called on ShootManager");
            //playerScript = NetworkClient.connection.identity.gameObject.GetComponent<PlayerScript>();
            if (playerScript)
            {
                Debug.Log($"ShootManager found playerScript");
            }
            else
            {
                Debug.Log("ShootManager was unable to find playerScript");
            }
            base.OnStartClient();
        }

        void OnButtonClick()
        {   
            if (isServer)
            {
                //Debug.Log("making everyone shoot");
                DateTime now = DateTime.Now;
                Debug.Log("now time:" + now);

                agreedTime = now.AddSeconds(waitTime);
                Debug.Log($"shooting at {agreedTime}");

                // DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond);
                // make everyone shoot in 5 seconds

                RpcForcePlayersShoot(agreedTime.Year, agreedTime.Month, agreedTime.Day, agreedTime.Hour, agreedTime.Minute, agreedTime.Second, agreedTime.Millisecond);
            } else
            {
                Debug.Log("youre not the server");
            }
        }
        
        [ClientRpc]
        void RpcForcePlayersShoot(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            agreedTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            //Debug.Log($"client acknowleding we are shooting at {agreedTime}");
            StartCoroutine(ClientShoot(agreedTime));
        }

        [Client]
        private IEnumerator ClientShoot(DateTime agreedTime)
        {
            playerScript = NetworkClient.connection.identity.gameObject.GetComponent<PlayerScript>();
            // !DateTime.Equals(DateTime.Now, agreedTime)
            while (DateTime.Compare(DateTime.Now, agreedTime) < 0) {
                //Debug.Log($"{DateTime.Now} != {agreedTime}");
                yield return null;
            }
            //Debug.Log("Shooting!");

            if (playerScript)
            {
                playerScript.ClientShootRay(playerScript.playerName);
            }
            yield break;
        }
    }
}