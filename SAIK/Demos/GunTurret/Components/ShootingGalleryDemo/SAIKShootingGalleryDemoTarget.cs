using UnityEngine;
using UdonSharp;
using System;
using AnimationState = SAIKShootingGalleryDemoTarget_AnimationState;
using Phase = SAIKShootingGalleryDemo_Phase;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAIKShootingGalleryDemoTarget : UdonSharpBehaviour
{
    public Animator m_animator;
    public ParticleSystem m_explosionParticle;
    public int m_hitPoints = 5;
    public float m_hitCooldownSeconds = 0.01f;

    [SerializeField, HideInInspector] public Matrix4x4 m_targetStandbyOffset;
    [SerializeField, HideInInspector] public AnimationClip[] m_waveAnimationClips;
    [SerializeField, HideInInspector] public AnimationClip m_targetEnterStandby;
    [SerializeField, HideInInspector] public AnimationClip m_targetExitStandby;

    private SAIKShootingGalleryDemo m_owner = null;
    private int m_firstUsedWaveIndex = -1;
    private int m_hitPointsRemaining = 0;
    private long m_lastHitTicks = 0;

    private bool m_ragdollActive = false;
    private Vector3 m_ragdollVelocity = Vector3.zero;
    private bool m_destroyed = false;

    private Transform m_activeArea = null;
    private int m_collisionMask = 0;

    public void Init(SAIKShootingGalleryDemo owner)
    {
        m_owner = owner;
        for (int i = 0; i < m_waveAnimationClips.Length && m_firstUsedWaveIndex == -1; ++i)
            if (m_waveAnimationClips[i] != null)
                m_firstUsedWaveIndex = i;
        for (int i = 0; i < 32; ++i)
            m_collisionMask |= (Physics.GetIgnoreLayerCollision(this.gameObject.layer, i) ? 0 : 1 << i);
    }

    public void ResetForNewCycle()
    {
        m_activeArea = m_owner.m_targetStandbyArea;
        m_hitPointsRemaining = m_hitPoints;
        m_ragdollActive = false;
        m_destroyed = false;

        m_animator.SetBool("IsDestroyed", false);
        ResetToActiveArea();
    }

    public void SyncedUpdate(int waveIndex, Phase phase, float syncedTime)
    {
        if (m_ragdollActive)
            return;

        if (m_destroyed)
        {
            if (phase != Phase.Wave)
            {
                m_destroyed = false;
                foreach (Collider collider in GetComponentsInChildren<Collider>())
                    collider.enabled = true;
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    renderer.enabled = true;
            }
        }

        if ((phase == Phase.WavePre || phase == Phase.WavePost || phase == Phase.Wave) && m_waveAnimationClips[waveIndex] == null)
        {
            ResetToSyncedArea(waveIndex);
            return;
        }

        if (phase == Phase.WaitAtStart)
            ResetToActiveArea();
        else if (phase == Phase.WavePre)
            AnimateInActiveArea(m_targetExitStandby, syncedTime);
        else if (phase == Phase.Wave)
            AnimateInTargetArea(m_waveAnimationClips[waveIndex], syncedTime);
        else if (phase == Phase.WavePost)
            AnimateInResultArea(m_targetEnterStandby, syncedTime);
        else if (phase == Phase.WaitAtEnd)
            ResetToActiveArea();
        else if (phase == Phase.ResetBegin)
            AnimateInActiveArea(m_targetExitStandby, syncedTime);
        else if (phase == Phase.ResetEnd)
            AnimateInStandbyArea(m_targetEnterStandby, syncedTime);
    }

    private void AnimateInTargetArea(AnimationClip animation, float animationTime)
    {
        m_activeArea = m_owner.m_targetResultArea;
        animation.SampleAnimation(this.gameObject, animationTime);
    }

    private void AnimateInActiveArea(AnimationClip animation, float animationTime)
    {
        animation.SampleAnimation(this.gameObject, animationTime);
        transform.position = m_activeArea.position + (m_activeArea.rotation * (m_targetStandbyOffset.GetPosition() + transform.localPosition));
        transform.rotation = m_activeArea.rotation * m_targetStandbyOffset.rotation * transform.localRotation;
    }

    private void AnimateInStandbyArea(AnimationClip animation, float animationTime)
    {
        m_activeArea = m_owner.m_targetStandbyArea;
        AnimateInActiveArea(animation, animationTime);
    }

    private void AnimateInResultArea(AnimationClip animation, float animationTime)
    {
        m_activeArea = m_owner.m_targetResultArea;
        AnimateInActiveArea(animation, animationTime);
    }

    private void ResetToSyncedArea(int waveIndex)
    {
        m_activeArea = waveIndex < m_firstUsedWaveIndex ? m_owner.m_targetStandbyArea : m_owner.m_targetResultArea;
        ResetToActiveArea();
    }

    private void ResetToActiveArea()
    {
        m_targetEnterStandby.SampleAnimation(this.gameObject, m_targetEnterStandby.length);
        transform.position = m_activeArea.position + (m_activeArea.rotation * m_targetStandbyOffset.GetPosition());
        transform.rotation = m_activeArea.rotation * m_targetStandbyOffset.rotation;
    }

    private void OnShotDown()
    {
        Vector2 lateralImpulse = UnityEngine.Random.insideUnitCircle;
        Vector3 impulse = new Vector3(lateralImpulse.x, 4.0f, lateralImpulse.y);
        m_ragdollVelocity = impulse;
        m_hitPointsRemaining = 0;
        m_animator.SetBool("IsDestroyed", true);
        m_destroyed = true;
        m_ragdollActive = true;
    }

    void LateUpdate()
    {
        if (m_ragdollActive)
        {
            m_ragdollVelocity.y -= 24 * Time.deltaTime;

            Vector3 displacement = m_ragdollVelocity * Time.deltaTime;
            if (m_ragdollVelocity.magnitude > Math.E)
            {
                Vector3 direction = Vector3.Normalize(m_ragdollVelocity);
                Ray ray = new Ray(transform.position, direction);
                if (Physics.SphereCast(ray, 0.5f, displacement.magnitude, m_collisionMask))
                {
                    m_ragdollActive = false;

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                    emitParams.position = m_explosionParticle.transform.InverseTransformPoint(transform.position);
                    
                    m_explosionParticle.Emit(emitParams, 1);

                    foreach (Collider collider in GetComponentsInChildren<Collider>())
                        collider.enabled = false;
                    foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                        renderer.enabled = false;
                }
            }

            transform.position += m_ragdollVelocity * Time.deltaTime;
            transform.rotation *= Quaternion.AngleAxis(360 * Time.deltaTime, Vector3.up);
        }
    }

    void OnParticleCollision()
    {
        DateTime hitTime = DateTime.UtcNow;
        float secondsSinceLastsHit = (float)(hitTime - new DateTime(m_lastHitTicks, DateTimeKind.Utc)).TotalSeconds;
        if (m_hitPointsRemaining > 0 && secondsSinceLastsHit >= m_hitCooldownSeconds)
        {
            m_hitPointsRemaining--;
            m_lastHitTicks = hitTime.Ticks;
            if (m_animator != null)
            {
                m_animator.SetTrigger("OnHit");
                if (m_hitPointsRemaining == 0)
                    OnShotDown();
            }
        }
    }
}

public enum SAIKShootingGalleryDemoTarget_AnimationState
{
    Standby,
    EnterStandby,
    ExitStandby,
    ActiveAnimated,
    ActiveRagdolled,
}