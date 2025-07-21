using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Qu?n l� t?t c? c�c AI agent trong scene
/// </summary>
public class EnemyAIManager : MonoBehaviour
{
    [Header("AI Management")]
    [Tooltip("Danh s�ch t?t c? c�c AI agent ?ang ho?t ??ng")]
    public List<EnemyAIController> activeAgents = new List<EnemyAIController>();
    [Tooltip("Kho?ng th?i gian c?p nh?t cho c�c agent ? xa")]
    public float distantAgentUpdateInterval = 1f;
    [Tooltip("Kho?ng c�ch ?? coi m?t agent l� ? xa")]
    public float distantAgentThreshold = 50f;
    
    [Header("AI Spawning")]
    [Tooltip("C� cho ph�p sinh ra AI kh�ng")]
    public bool allowSpawning = true;
    [Tooltip("S? l??ng AI t?i ?a trong scene")]
    public int maxAgents = 50;
    [Tooltip("Danh s�ch c�c spawner")]
    public List<EnemySpawner> spawners = new List<EnemySpawner>();
    
    [Header("AI Groups")]
    [Tooltip("C� s? d?ng h? th?ng nh�m kh�ng")]
    public bool useGrouping = true;
    [Tooltip("Danh s�ch c�c nh�m AI")]
    public List<AIGroup> aiGroups = new List<AIGroup>();
    
    [Header("AI Events")]
    [Tooltip("S? ki?n khi m?t agent ???c th�m v�o")]
    public UnityEvent<EnemyAIController> OnAgentAdded;
    [Tooltip("S? ki?n khi m?t agent b? x�a")]
    public UnityEvent<EnemyAIController> OnAgentRemoved;
    [Tooltip("S? ki?n khi m?t agent ch?t")]
    public UnityEvent<EnemyAIController> OnAgentDeath;
    
    // Singleton instance
    public static EnemyAIManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // T�m t?t c? c�c spawner trong scene n?u ch?a ???c g�n
        if (spawners.Count == 0)
        {
            spawners.AddRange(FindObjectsOfType<EnemySpawner>());
        }
        
        // B?t ??u coroutine c?p nh?t c�c agent ? xa
        StartCoroutine(UpdateDistantAgents());
    }
    
    /// <summary>
    /// Th�m m?t agent v�o danh s�ch qu?n l�
    /// </summary>
    public void AddAgent(EnemyAIController agent)
    {
        // N?u agent ch?a c� trong danh s�ch
        if (!activeAgents.Contains(agent))
        {
            // Th�m v�o danh s�ch
            activeAgents.Add(agent);
            
            // ??ng k� s? ki?n khi agent ch?t
            Character character = agent.GetComponent<Character>();
            if (character != null)
            {
                // Correctly subscribe to the OnDeath event
                character.OnDeath += () => HandleAgentDeath(agent);
            }
            
            // G?i s? ki?n
            OnAgentAdded.Invoke(agent);
        }
    }
    
    /// <summary>
    /// X�a m?t agent kh?i danh s�ch qu?n l�
    /// </summary>
    public void RemoveAgent(EnemyAIController agent)
    {
        // N?u agent c� trong danh s�ch
        if (activeAgents.Contains(agent))
        {
            // X�a kh?i danh s�ch
            activeAgents.Remove(agent);
            
            // H?y ??ng k� s? ki?n
            Character character = agent.GetComponent<Character>();
            if (character != null)
            {
                character.OnDeath -= () => HandleAgentDeath(agent);
            }
            
            // G?i s? ki?n
            OnAgentRemoved.Invoke(agent);
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n khi m?t agent ch?t
    /// </summary>
    private void HandleAgentDeath(EnemyAIController agent)
    {
        // G?i s? ki?n
        OnAgentDeath.Invoke(agent);
        
        // X�a agent kh?i danh s�ch
        RemoveAgent(agent);
    }
    
    /// <summary>
    /// T�m agent g?n nh?t v?i m?t v? tr�
    /// </summary>
    public EnemyAIController FindNearestAgent(Vector3 position)
    {
        EnemyAIController nearestAgent = null;
        float minDistance = float.MaxValue;
        
        foreach (EnemyAIController agent in activeAgents)
        {
            float distance = Vector3.Distance(position, agent.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestAgent = agent;
            }
        }
        
        return nearestAgent;
    }
    
    /// <summary>
    /// T�m t?t c? c�c agent trong m?t b�n k�nh
    /// </summary>
    public List<EnemyAIController> FindAgentsInRadius(Vector3 position, float radius)
    {
        List<EnemyAIController> agentsInRadius = new List<EnemyAIController>();
        
        foreach (EnemyAIController agent in activeAgents)
        {
            if (Vector3.Distance(position, agent.transform.position) <= radius)
            {
                agentsInRadius.Add(agent);
            }
        }
        
        return agentsInRadius;
    }
    
    /// <summary>
    /// Coroutine c?p nh?t c�c agent ? xa
    /// </summary>
    private IEnumerator UpdateDistantAgents()
    {
        while (true)
        {
            // T�m ng??i ch?i
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 playerPosition = player.transform.position;
                
                // C?p nh?t c�c agent ? xa
                foreach (EnemyAIController agent in activeAgents)
                {
                    if (Vector3.Distance(playerPosition, agent.transform.position) > distantAgentThreshold)
                    {
                        // Gi?m t?n su?t c?p nh?t c?a agent
                        // (V� d?: v� hi?u h�a m?t s? component ho?c gi?m t?n su?t c?p nh?t c?a ch�ng)
                        agent.enabled = false; // T?m th?i v� hi?u h�a to�n b? component
                    }
                    else
                    {
                        // K�ch ho?t l?i agent
                        agent.enabled = true;
                    }
                }
            }
            
            // Ch? m?t kho?ng th?i gian
            yield return new WaitForSeconds(distantAgentUpdateInterval);
        }
    }
    
    /// <summary>
    /// T?o m?t nh�m AI m?i
    /// </summary>
    public AIGroup CreateGroup()
    {
        if (!useGrouping)
        {
            return null;
        }
        
        AIGroup newGroup = new AIGroup();
        aiGroups.Add(newGroup);
        return newGroup;
    }
    
    /// <summary>
    /// Th�m m?t agent v�o m?t nh�m
    /// </summary>
    public void AddAgentToGroup(EnemyAIController agent, AIGroup group)
    {
        if (useGrouping && group != null && !group.members.Contains(agent))
        {
            group.members.Add(agent);
            agent.group = group;
        }
    }
    
    /// <summary>
    /// X�a m?t agent kh?i m?t nh�m
    /// </summary>
    public void RemoveAgentFromGroup(EnemyAIController agent, AIGroup group)
    {
        if (useGrouping && group != null && group.members.Contains(agent))
        {
            group.members.Remove(agent);
            agent.group = null;
        }
    }
    
    /// <summary>
    /// X�a t?t c? c�c agent
    /// </summary>
    public void ClearAllAgents()
    {
        // X�a t?t c? c�c agent
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            if (activeAgents[i] != null)
            {
                Destroy(activeAgents[i].gameObject);
            }
        }
        
        // X�a danh s�ch
        activeAgents.Clear();
        
        // X�a c�c nh�m
        aiGroups.Clear();
    }
}

/// <summary>
/// L?p ??i di?n cho m?t nh�m AI
/// </summary>
[System.Serializable]
public class AIGroup
{
    [Tooltip("Danh s�ch c�c th�nh vi�n trong nh�m")]
    public List<EnemyAIController> members = new List<EnemyAIController>();
    [Tooltip("M?c ti�u chung c?a nh�m")]
    public Transform groupTarget;
    
    /// <summary>
    /// Th�ng b�o cho t?t c? c�c th�nh vi�n trong nh�m v? m?t m?c ti�u
    /// </summary>
    public void AlertAllMembers(Transform target)
    {
        groupTarget = target;
        foreach (EnemyAIController member in members)
        {
            if (member != null)
            {
                member.Alert(target);
            }
        }
    }
}