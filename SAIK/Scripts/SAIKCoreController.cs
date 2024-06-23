using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using TransmissionMode = SAIKCoreController_TransmissionMode;

[DisallowMultipleComponent]
[RequireComponent(typeof(SAIKAvatarInterface))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[DefaultExecutionOrder(10000)]
public class SAIKCoreController : UdonSharpBehaviour
{
    public SAIKAvatarInterface Avatar { get => m_avatar; }
    public SAIKAvatarInfo AvatarInfo { get => m_avatarInfo; }
    public SAIKStationFrame StationFrame { get => m_stationFrame; }
    public UdonSharpBehaviour Driver { get => m_driver; }

    public bool IsReady { get => m_isReady; }
    private bool m_isReady = false;

    public Transform ControlFrame { get => m_controlFrame ?? StationFrame.ControlFrame; }
    private Transform m_controlFrame;
    private float m_controlFrameHeight;

    private Vector3 m_transmissionVector;
    private TransmissionMode m_transmissionMode = TransmissionMode.None;

    private System.DateTime m_stationFrameEntryTime;

    private UdonSharpBehaviour m_driver;
    private string m_driverExecuteCallback;
    private string m_driverResetCallback;
    private string m_driverAttachedCallback;
    private string m_driverDetachedCallback;

    [SerializeField, HideInInspector] private SAIKAvatarInterface m_avatar;
    [SerializeField, HideInInspector] private SAIKAvatarInfo m_avatarInfo;
    [SerializeField, HideInInspector] private SAIKAvatarInfoCache m_avatarInfoCache;
    [SerializeField, HideInInspector] private SAIKStationFrame m_stationFrame;
    [SerializeField, HideInInspector] private SAIKStationAnimatorReset m_resetAnimator;

    private void Start()
    {
        m_stationFrame.Init(this, nameof(OnStationFrameEntered), nameof(OnStationFrameExited));
    }

    void Update()
    {
        if (m_avatar.IsNull())
            return;
        
        if (m_avatar.IsAnimator())
            ZeroHipXForm();
        if (!m_avatarInfo.IsReady())
            CalibrateIK();
        if (m_avatarInfo.IsUpdated())
            ResetState();
        else if (!m_isReady && m_avatarInfo.IsReady())
            ReadyState();
        else if (m_isReady)
            ExecuteDriver();
        ApplyFrameState();
    }

    void LateUpdate()
    {
        if (m_avatar.IsNull())
            return;
        if (m_avatar.IsAnimator())
            ZeroHipXForm();
        SetTransmissionNone();
    }

    private void OnDisable()
    {
        Detach();
    }

    private void OnDestroy()
    {
        if (m_stationFrame == null)
            return;
        m_stationFrame.EjectFromStation();
        GameObject.Destroy(m_stationFrame);
    }

    public void SetDriverCallback(UdonSharpBehaviour driver, string executeCallback, string attachedCallback, string detachedCallback, string resetCallback)
    {
        m_driver = driver;
        m_driverExecuteCallback = executeCallback;
        m_driverResetCallback = resetCallback;
        m_driverAttachedCallback = attachedCallback;
        m_driverDetachedCallback = detachedCallback;
    }

    public void SetTransmissionNone()
    {
        m_transmissionMode = TransmissionMode.None;
        m_transmissionVector = Vector3.zero;
    }

    public void SetTransmissionBitPack(Vector3 vec)
    {
        m_transmissionMode = TransmissionMode.BitPacked;
        m_transmissionVector = vec;
    }

    public void SetTransmissionDirect(Vector3 vec)
    {
        m_transmissionMode = TransmissionMode.Direct;
        m_transmissionVector = vec;
    }
    
    public void SetControlFrame(Transform controlFrame, float controlFrameHeight)
    {
        m_controlFrame = controlFrame;
        m_controlFrameHeight = controlFrameHeight;
    }

    public void SetImmobilizeView(bool immobilize)
    {
        m_stationFrame.m_immobilizeView = immobilize;
    }

    public void TryAttachPlayer(VRCPlayerApi player)
    {
        if (!m_avatar.IsNull())
            return;

        // Center the player on the station to avoid weird offset issues.
        player.TeleportTo(ControlFrame.position, ControlFrame.rotation);

        m_stationFrame.Station.stationExitPlayerLocation = ControlFrame;
        m_stationFrame.UseStation(player);
    }

    public void TryAttachAnimator(Animator animator)
    {
        if (!m_avatar.IsNull())
            return;

        m_avatar.Init(animator);
        m_avatarInfo.ResetData();
    }

    public void Detach()
    {
        m_stationFrame.EjectFromStation();
    }

    public void OnStationFrameEntered()
    {
        VRCPlayerApi player = m_stationFrame.Occupant;
        if (!Networking.IsOwner(player, m_avatarInfo.gameObject))
            Networking.SetOwner(player, m_avatarInfo.gameObject);

        if (player.isLocal)
        {
            m_avatarInfo.ResetData();
            m_avatarInfo.RequestSerialization();
        }

        m_avatar.Init(player);

        m_stationFrame.transform.parent = null;
        m_stationFrame.transform.position = m_controlFrame.position;
        m_stationFrame.transform.rotation = m_controlFrame.rotation;
        m_stationFrameEntryTime = System.DateTime.Now;

        if (m_driver != null && m_driverAttachedCallback != null)
            m_driver.SendCustomEvent(m_driverAttachedCallback);

#if UNITY_EDITOR
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ClientSimSAIKCoreController.ClientSimAttach));
#endif
    }

    public void OnStationFrameExited()
    {
        if (m_avatar.IsLocalPlayer())
        {
            m_avatarInfo.ResetData();
            m_avatar.Player.SetVelocity(Vector3.zero);

#if !UNITY_EDITOR
            // Reset changes to the player's animator controller.
            m_resetAnimator.Apply(m_avatar.Player);
#endif
        }

        if (m_stationFrame != null)
            m_stationFrame.transform.parent = this.transform;

        m_avatar.Clear();
        m_isReady = false;
        if (m_driver != null && m_driverDetachedCallback != null)
            m_driver.SendCustomEvent(m_driverDetachedCallback);

#if UNITY_EDITOR
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ClientSimSAIKCoreController.ClientSimDetach));
#endif
    }

    public Matrix4x4 ComputeSolverFrame()
    {
        return Matrix4x4.TRS(ComputeHeightAdjustedFrame(), ControlFrame.rotation, Vector3.one);
    }

    private Vector3 ComputeHeightAdjustedFrame()
    {
        Vector3 position = ControlFrame.position;

        if (m_isReady && (m_avatar.IsAnimator() || m_avatar.IsDesktopPlayer() || m_avatarInfo.GetTrackingType() <= 3))
            position += new Vector3(0.0f, m_controlFrameHeight, 0.0f);
        
        return position;
    }

    private void CalibrateIK()
    {
        if (m_avatar.IsNull() || m_avatarInfo.IsReady())
            return;

        // Player IK controllers should only callibrate for the local player.
        if (m_avatar.IsPlayer() && !m_avatar.Player.isLocal)
            return;

        // If we are callibrating a player, then we need to wait for the animator to settle first.
        // The station animator layer gets activated with a 0.5s blend duration, so wait that duration out.
        if (m_avatar.IsPlayer() && (System.DateTime.Now - m_stationFrameEntryTime).TotalSeconds < 0.5f)
        {
            SetTransmissionDirect(Vector3.zero);
            return;
        }

        // If we can load from cache, then we can skip the callibration routine.
        // Ideally we could skip the wait time above as well, but since applying a custom pose space
        // already requires waiting for the animator to settle, we have to wait for it regardless.
        if (m_avatar.IsPlayer() && !m_avatarInfo.IsMeasuring() && m_avatarInfo.TryLoadFromCache(m_avatarInfoCache))
        {
            m_avatarInfo.RequestSerialization();
            return;
        }

#if UNITY_EDITOR
        // Callibration will fail in client sim unless we are bound to an animator.
        if (!m_avatar.IsAnimator())
        {
            Debug.LogWarning("Detaching from SAIK controller because SAIK client sim helper failed to run.");
            Detach();
            return; 
        }
#endif

        // Execute callibration steps.
        int calibrationStep = m_avatarInfo.MeasureAvatar(m_avatar);
        if (calibrationStep != -1)
        {
            SetTransmissionDirect(SAIKChannelPacker.CalibrationPose(calibrationStep));
            return;
        }

        // If calibration fails, then eject the occupant from controller.
        if (!m_avatarInfo.IsReady())
        {
            Debug.LogWarning("Detaching from SAIK controller because callibration with avatar failed.");
            Detach();
            return;
        }

        // Save measurements to local cache so we can do this faster next time.
        if (m_avatar.IsPlayer())
            m_avatarInfo.TrySaveToCache(m_avatarInfoCache);

        m_avatarInfo.RequestSerialization();
    }

    private void ResetState()
    {
        SetTransmissionDirect(SAIKChannelPacker.RestPose());
        m_avatarInfo.CommitUpdate();
        m_isReady = false;
    }

    private void ReadyState()
    {
        m_isReady = true;
        if (m_driver != null && m_driverResetCallback != null)
            m_driver.SendCustomEvent(m_driverResetCallback);
    }

    private void ExecuteDriver()
    {
        if (m_driver != null && m_driver.gameObject.activeInHierarchy)
            m_driver.SendCustomEvent(m_driverExecuteCallback);
    }

    private void ZeroHipXForm()
    {
        // VRC Stations completely zero out the hip transform, so we replicate that when working with a static animator too.
        Transform hipXForm = m_avatar.Animator.GetBoneTransform(HumanBodyBones.Hips);
        hipXForm.localPosition = Vector3.zero;
        hipXForm.localRotation = Quaternion.identity;
    }

    private void ApplyFrameState()
    {
        Vector3 controlFramePosition = ComputeHeightAdjustedFrame();
        Quaternion controlFrameRotation = ControlFrame.rotation;
        if (m_avatar.IsAnimator() && m_transmissionMode != TransmissionMode.None)
        {
            m_avatar.SetVelocityAnimator(m_transmissionVector);
        }
        else if (m_transmissionMode == TransmissionMode.Direct)
        {
            m_avatar.SetVelocityPlayer(controlFrameRotation * m_transmissionVector);
        }
        else if (m_transmissionMode == TransmissionMode.BitPacked)
        {
            bool solved = SAIKTransmissionHelper.ComputeTransmissionEquation(controlFrameRotation, m_transmissionVector, out Quaternion quat, out Vector3 vec);
            if (solved)
            {
                controlFrameRotation = quat;
                m_avatar.SetVelocityPlayer(vec);
            }
            else
            {
                m_avatar.ZeroVelocity();
                Debug.Log("Failed to find rotation.");
            }
        }
        
        m_stationFrame.transform.parent = null;
        m_stationFrame.transform.position = controlFramePosition;
        m_stationFrame.transform.rotation = controlFrameRotation;
    }

#if UNITY_EDITOR
    [UnityEditor.Callbacks.PostProcessScene(0)]
    public static void OnPostprocessScene()
    {
        SAIKAvatarInfoCache avatarInfoCache = GameObject.FindAnyObjectByType<SAIKAvatarInfoCache>();

        SAIKCoreController[] controllers = GameObject.FindObjectsByType<SAIKCoreController>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (SAIKCoreController controller in controllers)
        {
            controller.m_avatar = controller.GetComponent<SAIKAvatarInterface>();
            controller.m_avatarInfo = controller.GetComponentInChildren<SAIKAvatarInfo>();
            controller.m_stationFrame = controller.GetComponentInChildren<SAIKStationFrame>();
            controller.m_resetAnimator = controller.GetComponentInChildren<SAIKStationAnimatorReset>();

            if (controller.m_avatarInfoCache == null)
                controller.m_avatarInfoCache = avatarInfoCache;

        }
    }
#endif
}

enum SAIKCoreController_TransmissionMode
{
    None,
    Direct,
    BitPacked
}
