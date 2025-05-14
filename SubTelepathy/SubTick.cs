using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System;

public class SubTick : NetworkBehaviour
{
    [SerializeField] private bool inactive = false;
    [SerializeField] private int actionCount = 50;

    private Coroutine subTickRoutine = null;

    SortedList<float, SubTickAction> actions = null;

    public static Action<uint, int> OnActionAdded;

    public class SubTickAction
    {
        // the actor for this action
        public uint actor;
        // the actionId
        public int actionId { get; }
        // the time that this action occurred
        public float t { get; }

        public SubTickAction(uint actor, int actionId)
        {
            this.actor = actor;
            this.actionId = actionId;
            this.t = GetCurrentNetworkTime();
        }
        
    }
    public static float GetCurrentNetworkTime()
    {
        return (float)NetworkTime.time * NetworkManager.singleton.sendRate;
    }

    public static int GetCurrentTick()
    {
        return Mathf.FloorToInt(GetCurrentNetworkTime());
    }

    public static float GetCurrentTimeSinceTick()
    {
        float val = GetCurrentNetworkTime();
        return val - Mathf.Floor(val);
    }

    private void Start()
    {
        OnActionAdded += ClientAddAction;

        actions = new SortedList<float, SubTickAction>();
        subTickRoutine = StartCoroutine(SubTickRoutine());
    }

    private void OnDisable()
    {
        OnActionAdded -= ClientAddAction;
    }

    private IEnumerator SubTickRoutine()
    {
        int prevTick = 0;
        while(true)
        {
            int netTick = GetCurrentTick();
            float netTime = GetCurrentNetworkTime();
            //Debug.Log($"networkTick:{netTick}\nnetworkTime:{netTime}");
            // every full tick, execute all actions in order
            if (netTick != prevTick)
            {
                if (actions.Count > 0)
                {
                    // Debug.Log("executing actions!");
                    foreach (KeyValuePair<float, SubTickAction> kvp in actions)
                    {
                        SubTickAction action = kvp.Value;
                        Debug.Log($"t: {kvp.Key}, action: {action.actionId}");
                        GameObject go = NetworkServer.spawned[action.actor].connectionToClient.identity.gameObject;
                        if(go == null)
                        {
                            Debug.Log("actor was null, skipping");
                            continue;
                        }
                        SubTickBehaviour actor = go.GetComponent<SubTickBehaviour>();
                        actor.PerformAction(action.actionId);
                    }
                    // empty actions
                    actions.Clear();
                }
                // set prevTick
                prevTick = netTick;
            }
            // release main thread
            yield return null;
        }
    }

    // the client packages an action to be sent out
    [Client]
    public void ClientAddAction(uint actor, int actionId)
    {
        CmdAddAction(actor, actionId);
    }

    // the client sends this to the server
    [Command(requiresAuthority = false)]
    public void CmdAddAction(uint actor, int actionId)
    {
        ServerAddAction(actor, actionId);
    }

    // the server adds this action to the sorted actions queue
    [Server]
    public void ServerAddAction(uint actor, int actionId)
    {
        SubTickAction action = new SubTickAction(actor, actionId);
        // if somehow they both send at the exact same time
        // handle the race condition with... random chance!
        if(actions.ContainsKey(action.t))
        {
            Debug.Log("ENCOUNTERED RACE CONDITION");
            if(UnityEngine.Random.Range(0, 2) == 0)
            {
                Debug.Log("new action overwriting!");
                actions.Remove(action.t);
            } else
            {
                Debug.Log("new action DISCARDED");
                return;
            }
        }
        actions.Add(action.t, action);
    }

    // stops the subtickroutine
    public void Stop()
    {
        inactive = true;
        OnActionAdded -= ClientAddAction;
        StopCoroutine(subTickRoutine);
    }

    // makes sure that we don't start another subtickroutine if one is already running
    public void Init()
    {
        if(subTickRoutine != null)
        {
            Stop();
        }
        OnActionAdded += ClientAddAction;
        inactive = false;
        subTickRoutine = StartCoroutine(SubTickRoutine());
    }
}
