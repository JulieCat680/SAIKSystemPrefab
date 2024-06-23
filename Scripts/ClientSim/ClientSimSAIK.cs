using UnityEngine;
using VRC.Udon;

[AddComponentMenu("")]
[ExecuteAlways]
public class ClientSimSAIK : MonoBehaviour
{
    private static int _numInstances = 0;

    private void Awake()
    {
        if (_numInstances == 0)
            UdonBehaviour.SendCustomNetworkEventHook += ClientSimSAIKCoreController.SendCustomNetworkEventHook;
        _numInstances++;
    }
    
    private void OnDestroy()
    {
        _numInstances--;
        if (_numInstances == 0)
            UdonBehaviour.SendCustomNetworkEventHook -= ClientSimSAIKCoreController.SendCustomNetworkEventHook;
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        // Create proxy object to manage registering and deregistering event callbacks.
        Object.DontDestroyOnLoad(new GameObject(nameof(ClientSimSAIK), new[] { typeof(ClientSimSAIK) }));
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ClientSimSAIK))]
    public class CustomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UnityEditor.EditorGUI.BeginDisabledGroup(true);
            UnityEditor.EditorGUILayout.IntField("Instances", _numInstances);
            UnityEditor.EditorGUI.EndDisabledGroup();
        }
    }
#endif
}
