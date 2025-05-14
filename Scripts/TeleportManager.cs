using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace QuickStart
{
    public class TeleportManager : NetworkBehaviour
    {
        public Button teleportButton;
        
        private void Start()
        {
            teleportButton.onClick.AddListener(ButtonClicked);
            teleportButton.gameObject.SetActive(true);
        }
        public void ButtonClicked()
        {
            if (isServer)
            {
                DoTeleportPlayers();
            }
            else
            {
                CmdTeleportPlayers();
            }
        }
        
        [Command(requiresAuthority = false)]
        public void CmdTeleportPlayers()
        {
            DoTeleportPlayers();
        }
        
        [Server]
        private void DoTeleportPlayers()
        {
            Debug.Log("finding players");
            PlayerScript[] allPlayers = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);

            if (allPlayers.Length < 2)
            {
                Debug.LogWarning("not enough, you need 3 but have " + allPlayers.Length);
                return;
            }

            PlayerScript player1 = null;
            PlayerScript player2 = null;

            for (int i = 0; i < allPlayers.Length; i++)
            {
                switch (allPlayers[i].gameObject.GetComponent<NetworkIdentity>().connectionToClient.connectionId)
                {
                    case 0:
                        break;
                    case 1:
                        player1 = allPlayers[i];
                        break;
                    case 2:
                        player2 = allPlayers[i];
                        break;
                    default:
                        Debug.Log("Something went wrong w/ teleport");
                        break;
                }
            }
            if (player1 == null || player2 == null)
            {
                Debug.LogError("error, a player is null");
                return;
            }
            
            Debug.Log($"Teleporting players: {player1.name} and {player2.name}");
            
            // Calculate positions
            Vector3 midPoint = new Vector3(0, 0, 0);
            Vector3 offset = new Vector3(5, 0, 0);
            
            // SET POS 
            player1.transform.position = midPoint + offset;
            player1.transform.rotation = Quaternion.LookRotation(-offset);
            
            player2.transform.position = midPoint - offset;
            player2.transform.rotation = Quaternion.LookRotation(offset);
           
            RpcTeleportComplete(
                player1.netIdentity.netId, midPoint + offset, Quaternion.LookRotation(-offset),
                player2.netIdentity.netId, midPoint - offset, Quaternion.LookRotation(offset)
            );
        }
        
        [ClientRpc]
        private void RpcTeleportComplete(uint player1Id, Vector3 pos1, Quaternion rot1, uint player2Id, Vector3 pos2, Quaternion rot2)
        {
            Debug.Log("teleport request recieved by client");
            try
            {
                if (NetworkClient.spawned.ContainsKey(player1Id))
                {
                    NetworkClient.spawned[player1Id].transform.position = pos1;
                    NetworkClient.spawned[player1Id].transform.rotation = rot1;
                }
                
                if (NetworkClient.spawned.ContainsKey(player2Id))
                {
                    NetworkClient.spawned[player2Id].transform.position = pos2;
                    NetworkClient.spawned[player2Id].transform.rotation = rot2;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("RpcTeleportComplete failed, reason: " + e.Message);
            }
        }
    }
}