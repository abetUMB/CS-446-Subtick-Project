using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace QuickStart
{
    public class PlayerScript : SubTickBehaviour
    {
        public enum SubTickActions
        {
            None,
            Shoot
        }

        private bool subTick = true;

        public TextMesh playerNameText;
        public TextMesh healthBar;
        public GameObject floatingInfo;

        [SerializeField] private Material playerMaterialClone;
        [SerializeField] private Material outMaterial;

        [SerializeField] private SceneScript sceneScript;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;

        private int selectedWeaponLocal = 1;
        public GameObject[] weaponArray;

        [SyncVar(hook = nameof(OnWeaponChanged))]
        public int activeWeaponSynced = 1;

        [SerializeField] private Weapon activeWeapon;
        private float weaponCooldownTime;

        [SyncVar(hook = nameof(OnHealthChanged))]
        public int pHealth;

        public bool isOut = false;

        public int maxHealth = 1;

        private bool isLagEnabled = false;

        [SyncVar(hook = nameof(OnPingChanged))]
        public double ping = 0;

        void OnHealthChanged(int _Old, int _New)
        {
            pHealth = _New;
            if(pHealth <= 0)
            {
                isOut = true;
                healthBar.text = "<OUT>";
                OnColorChanged(new Color(), playerColor);
            } else {
                isOut = false;
                healthBar.text = new string('-', pHealth);
                OnColorChanged(new Color(), playerColor);
            }

            if (isLocalPlayer && sceneScript != null)
                sceneScript.UIHealth(pHealth);
        }

        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = $"{playerName}\n Ping: {ping} ms";
        }

        void OnPingChanged(double _Old, double _New)
        {
            ping = _New;
        }

        private NetworkManager netManager;
        public NetworkManager NetManager
        {
            get
            {
                if(netManager == null)
                {
                    return NetworkManager.singleton as NetworkManager;
                } else
                {
                    return netManager;
                }
            }
        }

        

        void Awake()
        {
            sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

            if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
            {
                activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
            }
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            if(isOut)
            {
                //Debug.Log("player is out , so setting to outmaterial");
                playerMaterialClone = outMaterial;
            } else
            {
                //Debug.Log("player isn't out, making new material");
                playerMaterialClone = new Material(GetComponent<Renderer>().material);
                playerMaterialClone.color = _New;
            }

            GetComponent<Renderer>().material = playerMaterialClone;
        }

        void OnWeaponChanged(int _Old, int _New)
        {
            if (0 < _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)
                weaponArray[_Old].SetActive(false);

            if (0 < _New && _New < weaponArray.Length && weaponArray[_New] != null)
            {
                weaponArray[_New].SetActive(true);
                activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                if (isLocalPlayer)
                    sceneScript.UIAmmo(activeWeapon.weaponAmmo);
            }
        }

        [Command]
        public void CmdChangeActiveWeapon(int newIndex)
        {
            activeWeaponSynced = newIndex;
        }

        [Client]
        public void SetLag(float lag = 0, float jitter = 0, float packetLoss = 0)
        {
            if(NetManager.transport is Mirror.LatencySimulation lagTransport)
            {
                lagTransport.SetSimulatedLatency(lag, jitter);
                lagTransport.unreliableLoss = packetLoss;
                sceneScript.UpdateLagStatusUI(lag, jitter, packetLoss);
            } else
            {
                Debug.LogWarning("LatencySimulation not found on NetworkManager.");
            }
        }

        public override void OnStartServer()
        {
            pHealth = 1;
            base.OnStartServer();
        }

        public override void OnStartLocalPlayer()
        {
            sceneScript.playerScript = this;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 0, 0);

            floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            string name = "Player" + Random.Range(100, 999);
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            CmdSetupPlayer(name, color);
        }

        [Command]
        public void CmdSendPlayerMessage()
        {
            UpdateStatusText($"{playerName} says hello {Random.Range(10, 99)}");
        }

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            SetupPlayer(_name, _col);
        }

        [Server]
        public void SetupPlayer(string _name, Color _col)
        {
            playerName = _name;
            SetColor(_col);
            UpdateStatusText($"{playerName} joined.");
        }

        [Server]
        public void SetColor(Color _col)
        {
            playerColor = _col;
        }

        void Update()
        {
            if(isServer)
            {
                ping = System.Math.Round(connectionToClient.rtt * 1000);
            }
            if(!isLocalPlayer) {
                floatingInfo.transform.LookAt(Camera.main.transform);
                playerNameText.text = $"{playerName}\n Ping: {ping} ms";
                return;
            }
            // LAG SIMULATION 
            if (Input.GetKeyDown(KeyCode.L))
            {
                isLagEnabled = !isLagEnabled;

                if (isLagEnabled)
                {
                    Debug.Log("Lag ENABLED");
                    SetLag(1f, 0f);
                }
                else
                {
                    Debug.Log("Lag DISABLED");
                    SetLag(0f, 0f, 0f);
                }
            }

            float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 110.0f;
            float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;

            transform.Rotate(0, moveX, 0);
            transform.Translate(0, 0, moveZ);

            playerNameText.text = $"{playerName}\n Ping: {ping} ms";

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                selectedWeaponLocal++;
                if (selectedWeaponLocal >= weaponArray.Length)
                    selectedWeaponLocal = 1;

                CmdChangeActiveWeapon(selectedWeaponLocal);
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (activeWeapon && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0)
                {
                    weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                    activeWeapon.weaponAmmo -= 1;
                    sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                    ClientShootRay(playerName);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                activeWeapon.weaponAmmo = activeWeapon.weaponMaxAmmo;
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
            }
        }

        [Client]
        public void ClientShootRay(string playerName)
        {
            //Debug.Log($"local client {playerName} attempting to shoot!");
            activeWeapon.muzzleFlash.Play();
            if(subTick)
            {
                SubTick.OnActionAdded?.Invoke(netIdentity.netId, (int)SubTickActions.Shoot);
            } else
            {
                CmdShootRay(playerName);
            }
        }

        [Command]
        void CmdShootRay(string playerName)
        {
            ShootRay(playerName);
        }

        [Server]
        void ShootRay(string playerName)
        {
            //Debug.Log($"{playerName} wants to shoot");
            if (isOut)
            {
                //Debug.Log($"{playerName} is out and cannot shoot!");
                return;
            }
            activeWeapon.Shoot(playerName);

            RpcMuzzleFlash();
        }

        [ClientRpc]
        void RpcMuzzleFlash()
        {
            if (isLocalPlayer) { return; }
            activeWeapon.muzzleFlash.Play();
        }

        [Server]
        public void ServerTakeDamage(string attacker, PlayerScript player)
        {
            if(isOut) { return; }
            --pHealth;
            if (pHealth <= 0)
            {
                isOut = true;
                SetColor(playerColor);
                StartCoroutine(RespawnTimer(2f));

                UpdateStatusText($"{attacker} eliminated {playerName}!");
            }
        }

        [Server]
        IEnumerator RespawnTimer(float respawnTime)
        {
            yield return new WaitForSeconds(respawnTime);
            pHealth = maxHealth;
            isOut = false;
            SetColor(playerColor);
        }

        [Server]
        private void UpdateStatusText(string message)
        {
            if (sceneScript)
            {
                Debug.Log(message);
                sceneScript.messages++;
                if(sceneScript.messages > 15)
                {
                    sceneScript.TrimMessages();
                }
                sceneScript.statusText += "\n" + message;

            } else
            {
                Debug.Log("error, sceneScript null");
            }
                
        }

        [Server]
        public override void PerformAction(int action)
        {
            switch(action)
            {
                case (int)SubTickActions.Shoot:
                    ShootRay(playerName);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}