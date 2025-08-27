#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Tool để sửa lỗi BlendTree parameter type
/// </summary>
public class BlendTreeParameterFixer : EditorWindow
{
    private Vector2 scrollPosition;
    private List<FixResult> fixResults = new List<FixResult>();
    
    [System.Serializable]
    public class FixResult
    {
        public string controllerPath;
        public string status;
        public List<string> details = new List<string>();
    }

    [MenuItem("Tools/🔧 BlendTree Parameter Fixer")]
    public static void ShowWindow()
    {
        GetWindow<BlendTreeParameterFixer>("🔧 BlendTree Parameter Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("🔧 BLENDTREE PARAMETER FIXER", EditorStyles.boldLabel);
        GUILayout.Label("Sửa lỗi BlendTree sử dụng parameter không đúng kiểu", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Scan All Animator Controllers", GUILayout.Height(30)))
        {
            ScanAllAnimatorControllers();
        }
        
        if (GUILayout.Button("🚀 Fix FacingDirection Parameters", GUILayout.Height(30)))
        {
            FixFacingDirectionParameters();
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
                
                GUILayout.Label($"{statusIcon} {System.IO.Path.GetFileName(result.controllerPath)}", EditorStyles.boldLabel);
                
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

    void ScanAllAnimatorControllers()
    {
        fixResults.Clear();
        
        string[] controllerGUIDs = AssetDatabase.FindAssets("t:AnimatorController");
        
        foreach (string guid in controllerGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            
            if (controller == null) continue;
            
            var result = new FixResult
            {
                controllerPath = path,
                status = "ℹ️ Scanned"
            };
            
            // Check parameters
            bool hasFacingDirection = false;
            bool isCorrectType = false;
            
            foreach (var param in controller.parameters)
            {
                if (param.name == "FacingDirection")
                {
                    hasFacingDirection = true;
                    if (param.type == AnimatorControllerParameterType.Float)
                    {
                        isCorrectType = true;
                        result.details.Add("FacingDirection parameter: Float ✓");
                    }
                    else
                    {
                        result.details.Add($"FacingDirection parameter: {param.type} ❌ (should be Float)");
                        result.status = "⚠️ Issues";
                    }
                    break;
                }
            }
            
            if (!hasFacingDirection)
            {
                result.details.Add("No FacingDirection parameter found");
            }
            
            // Check blend trees
            CheckBlendTrees(controller, result);
            
            fixResults.Add(result);
        }
        
        Debug.Log($"📊 Scan complete: Found {fixResults.Count} animator controllers");
    }

    void CheckBlendTrees(AnimatorController controller, FixResult result)
    {
        foreach (var layer in controller.layers)
        {
            CheckStateMachineBlendTrees(layer.stateMachine, result);
        }
    }

    void CheckStateMachineBlendTrees(AnimatorStateMachine stateMachine, FixResult result)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is BlendTree blendTree)
            {
                CheckBlendTreeRecursive(blendTree, result, state.state.name);
            }
        }
        
        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            CheckStateMachineBlendTrees(subStateMachine.stateMachine, result);
        }
    }

    void CheckBlendTreeRecursive(BlendTree blendTree, FixResult result, string stateName)
    {
        if (blendTree.blendParameter == "FacingDirection")
        {
            result.details.Add($"BlendTree '{blendTree.name}' in state '{stateName}' uses FacingDirection");
        }
        
        // Check children
        for (int i = 0; i < blendTree.children.Length; i++)
        {
            var child = blendTree.children[i];
            if (child.motion is BlendTree childBlendTree)
            {
                CheckBlendTreeRecursive(childBlendTree, result, stateName);
            }
        }
    }

    void FixFacingDirectionParameters()
    {
        fixResults.Clear();
        
        string[] controllerGUIDs = AssetDatabase.FindAssets("t:AnimatorController");
        int fixedCount = 0;
        
        foreach (string guid in controllerGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            
            if (controller == null) continue;
            
            var result = new FixResult
            {
                controllerPath = path,
                status = "ℹ️ Processed"
            };
            
            bool needsFix = false;
            
            // Check and fix FacingDirection parameter
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                var param = controller.parameters[i];
                if (param.name == "FacingDirection" && param.type != AnimatorControllerParameterType.Float)
                {
                    // Remove old parameter
                    controller.RemoveParameter(i);
                    
                    // Add new float parameter
                    controller.AddParameter("FacingDirection", AnimatorControllerParameterType.Float);
                    
                    result.details.Add($"Fixed FacingDirection parameter: {param.type} → Float");
                    result.status = "✅ Fixed";
                    needsFix = true;
                    fixedCount++;
                    break;
                }
            }
            
            if (!needsFix)
            {
                // Check if it already has correct parameter
                bool hasCorrectParam = false;
                foreach (var param in controller.parameters)
                {
                    if (param.name == "FacingDirection" && param.type == AnimatorControllerParameterType.Float)
                    {
                        hasCorrectParam = true;
                        result.details.Add("FacingDirection parameter already correct");
                        break;
                    }
                }
                
                if (!hasCorrectParam)
                {
                    result.details.Add("No FacingDirection parameter to fix");
                }
            }
            
            if (result.details.Count > 0)
            {
                fixResults.Add(result);
            }
            
            if (needsFix)
            {
                EditorUtility.SetDirty(controller);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Fixed {fixedCount} animator controllers with incorrect FacingDirection parameter type");
    }
}
#endif
