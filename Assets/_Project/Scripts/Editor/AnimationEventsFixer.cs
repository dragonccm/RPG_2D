#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Tool để sửa các Animation Events bị lỗi (thiếu function name)
/// </summary>
public class AnimationEventsFixer : EditorWindow
{
    private Vector2 scrollPosition;
    private List<FixResult> fixResults = new List<FixResult>();
    
    [System.Serializable]
    public class FixResult
    {
        public string filePath;
        public string status;
        public int eventsFixed;
        public List<string> details = new List<string>();
    }

    [MenuItem("Tools/🔧 Animation Events Fixer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationEventsFixer>("🔧 Animation Events Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("🔧 ANIMATION EVENTS FIXER", EditorStyles.boldLabel);
        GUILayout.Label("Sửa các Animation Events thiếu function name", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Scan All Animation Clips", GUILayout.Height(30)))
        {
            ScanAllAnimationClips();
        }
        
        if (GUILayout.Button("🚀 Fix All Animation Events", GUILayout.Height(30)))
        {
            FixAllAnimationEvents();
        }
        
        if (GUILayout.Button("🧹 Remove Empty Events", GUILayout.Height(30)))
        {
            RemoveEmptyEvents();
        }
        
        GUILayout.Space(10);
        
        if (fixResults.Count > 0)
        {
            GUILayout.Label("📊 Results:", EditorStyles.boldLabel);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var result in fixResults)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                string statusIcon = result.status == "✅ Fixed" ? "✅" : 
                                   result.status == "⚠️ Issues" ? "⚠️" : "ℹ️";
                
                GUILayout.Label($"{statusIcon} {Path.GetFileName(result.filePath)}", EditorStyles.boldLabel);
                GUILayout.Label($"Events Fixed: {result.eventsFixed}", EditorStyles.miniLabel);
                
                foreach (var detail in result.details)
                {
                    GUILayout.Label($"  • {detail}", EditorStyles.miniLabel);
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
            
            GUILayout.EndScrollView();
        }
    }

    void ScanAllAnimationClips()
    {
        fixResults.Clear();
        
        string[] animClipGUIDs = AssetDatabase.FindAssets("t:AnimationClip");
        int totalClips = animClipGUIDs.Length;
        int clipsWithEvents = 0;
        int clipsWithEmptyEvents = 0;
        
        foreach (string guid in animClipGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null) continue;
            
            var events = AnimationUtility.GetAnimationEvents(clip);
            
            if (events.Length > 0)
            {
                clipsWithEvents++;
                
                var result = new FixResult
                {
                    filePath = path,
                    status = "ℹ️ Scanned"
                };
                
                bool hasEmptyEvents = false;
                
                foreach (var animEvent in events)
                {
                    if (string.IsNullOrEmpty(animEvent.functionName))
                    {
                        hasEmptyEvents = true;
                        result.details.Add($"Empty event at time {animEvent.time:F2}");
                    }
                    else
                    {
                        result.details.Add($"Valid event: {animEvent.functionName} at {animEvent.time:F2}");
                    }
                }
                
                if (hasEmptyEvents)
                {
                    clipsWithEmptyEvents++;
                    result.status = "⚠️ Has Empty Events";
                }
                
                fixResults.Add(result);
            }
        }
        
        Debug.Log($"📊 Scan complete: {totalClips} total clips, {clipsWithEvents} with events, {clipsWithEmptyEvents} with empty events");
    }

    void FixAllAnimationEvents()
    {
        fixResults.Clear();
        
        string[] animClipGUIDs = AssetDatabase.FindAssets("t:AnimationClip");
        int totalFixed = 0;
        
        foreach (string guid in animClipGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null) continue;
            
            var events = AnimationUtility.GetAnimationEvents(clip);
            
            if (events.Length > 0)
            {
                var result = new FixResult
                {
                    filePath = path,
                    status = "ℹ️ Processed"
                };
                
                List<AnimationEvent> fixedEvents = new List<AnimationEvent>();
                bool needsFix = false;
                
                foreach (var animEvent in events)
                {
                    if (string.IsNullOrEmpty(animEvent.functionName))
                    {
                        // Assign default function name based on timing
                        string defaultFunction = GetDefaultFunctionName(animEvent.time, clip.length);
                        animEvent.functionName = defaultFunction;
                        
                        result.details.Add($"Fixed empty event at {animEvent.time:F2} → {defaultFunction}");
                        result.eventsFixed++;
                        needsFix = true;
                    }
                    else
                    {
                        result.details.Add($"Kept: {animEvent.functionName} at {animEvent.time:F2}");
                    }
                    
                    fixedEvents.Add(animEvent);
                }
                
                if (needsFix)
                {
                    AnimationUtility.SetAnimationEvents(clip, fixedEvents.ToArray());
                    EditorUtility.SetDirty(clip);
                    result.status = "✅ Fixed";
                    totalFixed++;
                }
                
                if (result.eventsFixed > 0 || result.details.Count > 0)
                {
                    fixResults.Add(result);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Fixed {totalFixed} animation clips with empty events");
    }

    void RemoveEmptyEvents()
    {
        fixResults.Clear();
        
        string[] animClipGUIDs = AssetDatabase.FindAssets("t:AnimationClip");
        int totalCleaned = 0;
        
        foreach (string guid in animClipGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null) continue;
            
            var events = AnimationUtility.GetAnimationEvents(clip);
            
            if (events.Length > 0)
            {
                List<AnimationEvent> validEvents = new List<AnimationEvent>();
                bool hasEmptyEvents = false;
                
                foreach (var animEvent in events)
                {
                    if (!string.IsNullOrEmpty(animEvent.functionName))
                    {
                        validEvents.Add(animEvent);
                    }
                    else
                    {
                        hasEmptyEvents = true;
                    }
                }
                
                if (hasEmptyEvents)
                {
                    AnimationUtility.SetAnimationEvents(clip, validEvents.ToArray());
                    EditorUtility.SetDirty(clip);
                    
                    var result = new FixResult
                    {
                        filePath = path,
                        status = "✅ Cleaned",
                        eventsFixed = events.Length - validEvents.Count
                    };
                    
                    result.details.Add($"Removed {result.eventsFixed} empty events");
                    result.details.Add($"Kept {validEvents.Count} valid events");
                    
                    fixResults.Add(result);
                    totalCleaned++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"🧹 Cleaned {totalCleaned} animation clips by removing empty events");
    }

    string GetDefaultFunctionName(float time, float clipLength)
    {
        float timePercent = time / clipLength;
        
        // Assign function based on timing within the animation
        if (timePercent < 0.2f)
        {
            return "OnAnimationStart";
        }
        else if (timePercent > 0.8f)
        {
            return "OnAnimationEnd";
        }
        else if (timePercent >= 0.4f && timePercent <= 0.6f)
        {
            return "OnAnimationMidpoint";
        }
        else
        {
            return "OnAnimationEvent";
        }
    }
}

/// <summary>
/// Script để thêm vào Player hoặc các objects có animation events
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    [Header("🎵 Animation Event Handler")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Default animation event handlers
    public void OnAnimationStart()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Animation Start Event");
    }
    
    public void OnAnimationEnd()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Animation End Event");
    }
    
    public void OnAnimationMidpoint()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Animation Midpoint Event");
    }
    
    public void OnAnimationEvent()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Animation Event");
    }
    
    // Common animation events used in the project
    public void OnAttackHit()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Attack Hit Event");
    }
    
    public void OnActionComplete()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Action Complete Event");
            
        // Reset busy state if this is a PlayerController
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetBusy(false);
        }
    }
    
    public void OnFootstep()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Footstep Event");
    }
}
#endif
