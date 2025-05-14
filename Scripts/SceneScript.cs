using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

namespace QuickStart
{
    public class SceneScript : NetworkBehaviour
    {
        public Text canvasStatusText;
        public PlayerScript playerScript;

        public SceneReference sceneReference;

        public TextMeshProUGUI lagStatusText;

        [SyncVar(hook = nameof(OnStatusTextChanged))]
        public string statusText;

        public int messages = 0;

        public Text canvasAmmoText;
        public Text canvasHealthText;

        [Server]
        public void TrimMessages()
        {
            int nextLine = statusText.IndexOf('\n');
            statusText = statusText.Substring(nextLine + 1);
        }

        void OnStatusTextChanged(string _Old, string _New)
        {
            //called from sync var hook, to update info on screen for all players
            canvasStatusText.text = _New;
        }

        public void ButtonSendMessage()
        {
            if (playerScript != null)  
                playerScript.CmdSendPlayerMessage();
        }
        public void ButtonChangeScene()
        {
            if (isServer)
            {
                Scene scene = SceneManager.GetActiveScene();
                if (scene.name == "MyScene")
                    NetworkManager.singleton.ServerChangeScene("MyOtherScene");
                else
                    NetworkManager.singleton.ServerChangeScene("MyScene");
            }
            else
                Debug.Log("You are not Host.");
        }
        public void UIAmmo(int _value)
        {
            canvasAmmoText.text = "Ammo: " + _value;
        }

        public void UIHealth(int _value)
        {
            canvasHealthText.text = "Health: " + _value;
        }

        public void UpdateLagStatusUI(float latency, float jitter, float packetLoss)
        {
            lagStatusText.text = $"Lag: {latency}ms\nJitter: {Mathf.RoundToInt(jitter * 100)}%\nPacket Loss: {packetLoss}%";
        }
        
    }
}