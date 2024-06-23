using UnityEngine;
using UnityEngine.Animations;
using UdonSharp;
using VRC.SDKBase;
using System;
using System.Linq;
using Phase = SAIKShootingGalleryDemo_Phase;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class SAIKShootingGalleryDemo : UdonSharpBehaviour
{
    [UdonSynced]
    private long m_refTimeTicks = 0;
    private long m_localToServerTicksOffset = 0;

    public float m_startWaitTimeSeconds = 0.5f;
    public float m_endWaitTimeSeconds = 6.0f;

    public Transform m_targetStandbyArea;
    public Transform m_targetResultArea;
    public Transform m_targetParent;

    public AnimationClip m_targetEnterStandby;
    public AnimationClip m_targetExitStandby;
    public AnimationClip[] m_targetWaveAnimationClips;

    [SerializeField, HideInInspector]
    private SAIKShootingGalleryDemoTarget[] m_targets;
    [SerializeField, HideInInspector]
    private float m_cycleTotalSeconds = 0.0f;

    private int m_lastCycleIndex = -1;

    public int CurrentWave { get => m_currentWave; }
    private int m_currentWave = -1;

    public Phase CurrentPhase { get => m_currentPhase; }
    private Phase m_currentPhase = Phase.WaitAtStart;

    void Start()
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime utcNetwork = Networking.GetNetworkDateTime();

        m_localToServerTicksOffset = (utcNetwork - utcNow).Ticks;
        if (Networking.IsOwner(this.gameObject))
            m_refTimeTicks = utcNetwork.Ticks;

        foreach (SAIKShootingGalleryDemoTarget target in m_targets)
            target.Init(this);
    }

    void LateUpdate()
    {
        if (m_targetWaveAnimationClips.Length == 0 || m_refTimeTicks == 0)
            return;

        DateTime clipTimeStart = new DateTime(m_refTimeTicks - m_localToServerTicksOffset, DateTimeKind.Utc);
        DateTime clipTimeNow = DateTime.UtcNow;
        float totalTime = (float)(clipTimeNow - clipTimeStart).TotalSeconds;
        float cycleTime = Mathf.Repeat(totalTime, m_cycleTotalSeconds);
        int cycleIndex = Mathf.FloorToInt(totalTime / m_cycleTotalSeconds);

        // Trigger a reset if we're starting a new cycle.
        if (cycleIndex != m_lastCycleIndex)
            foreach (SAIKShootingGalleryDemoTarget target in m_targets)
                target.ResetForNewCycle();
        m_lastCycleIndex = cycleIndex;

        // Determine what wave we are on.
        float waveStartTime = 0;
        int waveIndex = -1;
        if (cycleTime >= m_startWaitTimeSeconds)
        {
            for (waveIndex = 0; waveIndex < m_targetWaveAnimationClips.Length; ++waveIndex)
            {
                float duration = GetWaveDuration(waveIndex);
                if (waveStartTime + duration < (cycleTime - m_startWaitTimeSeconds))
                    waveStartTime += duration;
                else
                    break;
            }
        }

        SyncedUpdate(cycleTime, waveIndex, waveStartTime);
    }

    private void SyncedUpdate(float cycleTime, int waveIndex, float waveStartTime)
    {
        Phase phase;
        float syncedTime;
        if (waveIndex == -1)
        {
            phase = Phase.WaitAtStart;
            syncedTime = 0.0f;
        }
        else if (waveIndex < m_targetWaveAnimationClips.Length)
        {
            float waveTime = cycleTime - m_startWaitTimeSeconds - waveStartTime;
            float waveStartupDuration = m_targetExitStandby.length;
            AnimationClip waveAnimation = m_targetWaveAnimationClips[waveIndex];
            if (waveTime < waveStartupDuration)
            {
                phase = Phase.WavePre;
                syncedTime = waveTime;
            }
            else if (waveTime < waveStartupDuration + waveAnimation.length)
            {
                phase = Phase.Wave;
                syncedTime = waveTime - waveStartupDuration;
            }
            else
            {
                phase = Phase.WavePost;
                syncedTime = waveTime - waveStartupDuration - waveAnimation.length;
            }
        }
        else
        {
            float postCycleTime = cycleTime - m_startWaitTimeSeconds - waveStartTime;
            if (postCycleTime < m_endWaitTimeSeconds)
            {
                phase = Phase.WaitAtEnd;
                syncedTime = 0.0f;
            }
            else if (postCycleTime < m_endWaitTimeSeconds + m_targetExitStandby.length)
            {
                phase = Phase.ResetBegin;
                syncedTime = postCycleTime - m_endWaitTimeSeconds;
            }
            else
            {
                phase = Phase.ResetEnd;
                syncedTime = postCycleTime - m_endWaitTimeSeconds - m_targetExitStandby.length;
            }
        }
        m_currentWave = waveIndex;
        m_currentPhase = phase;

        foreach (SAIKShootingGalleryDemoTarget target in m_targets)
            target.SyncedUpdate(waveIndex, phase, syncedTime);
    }

    private float GetWaveDuration(int index)
    {
        return m_targetExitStandby.length + m_targetWaveAnimationClips[index].length + m_targetEnterStandby.length;
    }

    private float GetCycleDuration()
    {
#if UNITY_EDITOR
        return ComputeCycleDuration();
#else
        return m_cycleTotalSeconds;
#endif
    }

    private float ComputeCycleDuration()
    {
        float totalSeconds = 0.0f;
        for (int i = 0; i < m_targetWaveAnimationClips.Length; ++i)
            totalSeconds += GetWaveDuration(i);
        totalSeconds += m_targetEnterStandby.length;
        totalSeconds += m_startWaitTimeSeconds;
        totalSeconds += m_endWaitTimeSeconds;
        totalSeconds += m_targetExitStandby.length;
        return totalSeconds;
    }

#if UNITY_EDITOR
    [UnityEditor.Callbacks.PostProcessScene(0)]
    public static void OnPostprocessScene()
    {
        foreach (SAIKShootingGalleryDemo component in GameObject.FindObjectsOfType<SAIKShootingGalleryDemo>())
        {
            SAIKShootingGalleryDemoTarget[] targets = component.m_targetParent.Cast<Transform>().Select(x => x.GetComponent<SAIKShootingGalleryDemoTarget>()).Where(x => x != null).ToArray();
            string[] bindingPaths = targets.Select(x => UnityEditor.AnimationUtility.CalculateTransformPath(x.transform, component.transform)).ToArray();
            string targetRootPath = UnityEditor.AnimationUtility.CalculateTransformPath(component.m_targetParent, component.transform);

            AnimationClip[] animationClips = component.m_targetWaveAnimationClips;
            AnimationClip[][] targetWaveClips = new AnimationClip[targets.Length][];
            for (int i = 0; i < targets.Length; ++i)
                targetWaveClips[i] = new AnimationClip[animationClips.Length];
            
            // Split the wave animations up into distinct clips that can be applied to each target separately.
            for (int i = 0; i < animationClips.Length; ++i)
            {
                AnimationClip clip = animationClips[i];
                UnityEditor.EditorCurveBinding[] bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
                for (int j = 0; j < bindingPaths.Length; ++j)
                {
                    foreach (UnityEditor.EditorCurveBinding binding in bindings.Where(x => x.path.StartsWith(bindingPaths[j])))
                    {
                        if (targetWaveClips[j][i] == null)
                            targetWaveClips[j][i] = new AnimationClip() { legacy = true, hideFlags = HideFlags.DontSaveInEditor };

                        AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);
                        string bindingPath = binding.path.Substring(bindingPaths[j].Length).TrimStart('/');

                        targetWaveClips[j][i].SetCurve(bindingPath, binding.type, binding.propertyName, curve);
                    }
                }
            }

            Matrix4x4 standbyBasis = component.m_targetStandbyArea.worldToLocalMatrix;
            for (int i = 0; i < targets.Length; ++i)
            {
                targets[i].m_targetStandbyOffset = standbyBasis * targets[i].transform.localToWorldMatrix;
                targets[i].m_waveAnimationClips = targetWaveClips[i];
                targets[i].m_targetEnterStandby = component.m_targetEnterStandby;
                targets[i].m_targetExitStandby = component.m_targetExitStandby;
                GameObject.DestroyImmediate(targets[i].GetComponent<ParentConstraint>());
            }

            component.m_targets = targets.ToArray();
            component.m_cycleTotalSeconds = component.ComputeCycleDuration();
        }
    }
#endif
}

public enum SAIKShootingGalleryDemo_Phase
{
    WaitAtStart,
    WavePre,
    Wave,
    WavePost,
    WaitAtEnd,
    ResetBegin,
    ResetEnd
}