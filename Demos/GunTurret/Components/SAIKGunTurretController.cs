using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class SAIKGunTurretController : UdonSharpBehaviour
{
    public Animator m_animatorRoot = null;
    public Transform m_aimAperture = null;
    public Transform m_yawAperture = null;
    public ParticleSystem m_gunParticleL = null;
    public ParticleSystem m_gunParticleR = null;

    [Space(10.0f)]
    public SAIKCoreController m_IKController = null;
    public Transform m_IKBaseFrame = null;
    public float m_IKBaseFrameHeight = 0.0f;
    public Transform m_IKTargetHandLL = null;
    public Transform m_IKTargetHandLR = null;
    public Transform m_IKTargetHandRR = null;
    public Transform m_IKTargetHandRL = null;
    public Transform m_VRHandleCenterL = null;
    public Transform m_VRHandleCenterR = null;
    public SAIKVRHandle m_VRHandlePickupL = null;
    public SAIKVRHandle m_VRHandlePickupR = null;


    private float m_inputDesktopAimYawRotation = 0.0f;
    private float m_inputDesktopAimPitchRotation = 0.0f;
    private float m_inputStationYawRotation = 0.0f;
    private bool m_inputDesktopAimPitchUsingMouse = true;

    private bool m_inputHandleHeldL = false;
    private bool m_inputHandleHeldR = false;

    private bool m_useInputL = false;
    private bool m_useInputR = false;
    private bool m_isReady = false;

    [UdonSynced(UdonSyncMode.None)] private bool m_useInput = false;
    [UdonSynced(UdonSyncMode.Linear)] private float m_stationYawRotation = 0.0f;
    [UdonSynced(UdonSyncMode.Linear)] private float m_aimYawRotation = 0.0f;
    [UdonSynced(UdonSyncMode.Linear)] private float m_aimPitchRotation = 0.0f;

    private VRCPlayerApi m_occupant = null;

    [SerializeField, HideInInspector]
    private SAIKGunTurretSettings m_settings = null;

    #region Event Functions

    void Start()
    {
        m_IKController.SetDriverCallback(this, nameof(ExecuteDriver), nameof(OnSAIKAttached), nameof(OnSAIKDetached), nameof(ResetDriver));
        m_IKController.SetControlFrame(m_IKBaseFrame, m_IKBaseFrameHeight);
        m_IKController.SetImmobilizeView(true);
        m_VRHandlePickupL.Init(this, nameof(OnHandlePickupL), nameof(OnHandleDropL));
        m_VRHandlePickupR.Init(this, nameof(OnHandlePickupR), nameof(OnHandleDropR));

        SAIKGunTurretAnimatorCallbacks animatorCallbacks = m_animatorRoot.GetComponent<SAIKGunTurretAnimatorCallbacks>();
        if (animatorCallbacks != null)
            animatorCallbacks.Init(this, nameof(OnGunFire));
    }

    void OnDisable()
    {
        m_IKController.Detach();
    }

    void Update()
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
            UpdateLocalUserState();
    
        // This executes for all clients to match the result of our synced variables.
        m_yawAperture.localRotation = Quaternion.AngleAxis(m_stationYawRotation, Vector3.up);
        m_aimAperture.localRotation = Quaternion.Euler(m_aimPitchRotation, m_aimYawRotation, 0.0f);
        m_animatorRoot.SetBool("Fire", m_useInput);
    }

    public override void PostLateUpdate()
    {
        if (m_occupant != null && m_occupant.isLocal && m_occupant.IsUserInVR() && m_isReady)
            ExecuteHandleControls();
    }

    public override void Interact()
    {
        m_IKController.TryAttachPlayer(Networking.LocalPlayer);
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
            m_inputDesktopAimYawRotation = value;
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
            if (Mathf.Abs(value) >= 0.25f)
                m_inputDesktopAimPitchUsingMouse = true;
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
            m_inputStationYawRotation = value;
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
        {
            m_inputDesktopAimPitchRotation = value;
            if (Mathf.Abs(value) >= 0.25f)
                m_inputDesktopAimPitchUsingMouse = false;
        }
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
        {
            m_IKController.Detach();
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && m_isReady)
        {
            if (args.handType == HandType.LEFT)
                m_useInputL = value;
            else if (args.handType == HandType.RIGHT)
                m_useInputR = value;
        }
    }

    public override void InputDrop(bool value, UdonInputEventArgs args)
    {
        if (m_occupant != null && m_occupant.isLocal && !m_occupant.IsUserInVR() && m_isReady)
        {
            m_IKController.Detach();
        }
    }

    public void OnHandlePickupL()
    {
        m_inputHandleHeldL = true;
    }

    public void OnHandleDropL()
    {
        m_inputHandleHeldL = false;
    }

    public void OnHandlePickupR()
    {
        m_inputHandleHeldR = true;
    }

    public void OnHandleDropR()
    {
        m_inputHandleHeldR = false;
    }

    public void OnGunFire()
    {
        m_gunParticleL.Play();
        m_gunParticleR.Play();

        if (m_occupant != null && m_occupant.isLocal && m_settings != null && m_settings.m_hapticStrength > 0.0f)
        {
            VRC_Pickup pickupL = m_occupant.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            if (pickupL == m_VRHandlePickupL.Pickup || pickupL == m_VRHandlePickupR.Pickup)
                m_occupant.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.25f, m_settings.m_hapticStrength, 200.0f);

            VRC_Pickup pickupR = m_occupant.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (pickupR == m_VRHandlePickupL.Pickup || pickupR == m_VRHandlePickupR.Pickup)
                m_occupant.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, m_settings.m_hapticStrength, 200.0f);
        }
    }

    #endregion

    #region State Functions

    private void UpdateLocalUserState()
    {
        // Controls and state changes that only apply for the
        // local player such as turret rotation, aiming, and shooting.
        if (m_occupant.IsUserInVR())
        {
            float stationYawMovement = m_inputStationYawRotation * 45.0f * Time.deltaTime;
            m_stationYawRotation = Mathf.Clamp(m_stationYawRotation + stationYawMovement, -40, 40);

            VRC_Pickup heldPickupL = m_occupant.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            VRC_Pickup heldPickupR = m_occupant.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            bool useL = m_useInputL && (heldPickupL == m_VRHandlePickupL.Pickup || heldPickupL == m_VRHandlePickupR.Pickup);
            bool useR = m_useInputR && (heldPickupR == m_VRHandlePickupR.Pickup || heldPickupR == m_VRHandlePickupL.Pickup);
            m_useInput = useL || useR;

            if (!m_inputHandleHeldL)
                m_VRHandlePickupL.transform.position = m_VRHandleCenterL.position;
            if (!m_inputHandleHeldR)
                m_VRHandlePickupR.transform.position = m_VRHandleCenterR.position;
        }
        else
        {
            if (m_inputDesktopAimPitchUsingMouse)
            {
                VRCPlayerApi.TrackingData headTracking = m_occupant.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                Vector3 targetPos = headTracking.position + headTracking.rotation * Vector3.forward * 10.0f;
                Vector3 sourcePos = m_aimAperture.parent.transform.position;
                Vector3 sourceUp = m_aimAperture.parent.transform.up;
                Vector3 sourceRight = m_aimAperture.parent.transform.right;
                Vector3 sourcePos2D = Vector3.ProjectOnPlane(sourcePos, sourceUp);
                Vector3 targetPos2D = Vector3.ProjectOnPlane(targetPos, sourceUp);

                Vector3 horizon = targetPos2D - sourcePos2D;
                Vector3 tilt = targetPos - sourcePos;
                float tiltAngle = Vector3.SignedAngle(horizon, tilt, sourceRight);

                m_aimPitchRotation = Mathf.Clamp(tiltAngle, -25, 15);
            }
            else
            {
                float aimPitchMovement = -m_inputDesktopAimPitchRotation * 45.0f * Time.deltaTime;
                m_aimPitchRotation = Mathf.Clamp(m_aimPitchRotation + aimPitchMovement, -25, 15);
            }

            VRCInputMethod inputMethod = InputManager.GetLastUsedInputMethod();
            bool isMouseAiming = (inputMethod == VRCInputMethod.Mouse || inputMethod == VRCInputMethod.Keyboard);
            float aimYawMultiplier = (isMouseAiming ? 90.0f : 45.0f);
            float aimYawMovement = m_inputDesktopAimYawRotation * aimYawMultiplier * Time.deltaTime;
            float aimYawRotationUpdated = m_aimYawRotation + aimYawMovement;
            m_aimYawRotation = Mathf.Clamp(aimYawRotationUpdated, -10, 10);

            float excessAimYawRotation = aimYawRotationUpdated - m_aimYawRotation;
            m_stationYawRotation = Mathf.Clamp(m_stationYawRotation + excessAimYawRotation, -40, 40);

            float stationYawMovement = m_inputStationYawRotation * 45.0f * Time.deltaTime;
            m_stationYawRotation = Mathf.Clamp(m_stationYawRotation + stationYawMovement, -40, 40);

            m_useInput = m_useInputL || m_useInputR;
        }
    }

    private void ExecuteHandleControls()
    {
        // Rotate the turret based on the held IK handles.
        // This is run on all clients for VR occupants to ensure that
        // the turret always lines up with the remote player's hands.
        if (!m_inputHandleHeldL && !m_inputHandleHeldR)
            return;

        Vector3 leverAtRest = Vector3.zero;
        Vector3 leverAtHeld = Vector3.zero;
        if (m_inputHandleHeldL && m_inputHandleHeldR)
        {
            Vector3 leftAtRest = m_aimAperture.parent.TransformPoint(m_aimAperture.localPosition + m_VRHandleCenterL.localPosition + m_VRHandleCenterL.GetChild(0).localPosition);
            Vector3 rightAtRest = m_aimAperture.parent.TransformPoint(m_aimAperture.localPosition + m_VRHandleCenterR.localPosition + m_VRHandleCenterR.GetChild(0).localPosition);
            leverAtRest = (leftAtRest + rightAtRest) / 2.0f;
            leverAtHeld = (m_VRHandlePickupL.transform.position + m_VRHandlePickupR.transform.position) / 2.0f;
        }
        else if (m_inputHandleHeldL)
        {
            leverAtRest = m_aimAperture.parent.TransformPoint(m_aimAperture.localPosition + m_VRHandleCenterL.localPosition + m_VRHandleCenterL.GetChild(0).localPosition);
            leverAtHeld = m_VRHandlePickupL.transform.position;
        }
        else if (m_inputHandleHeldR)
        {
            leverAtRest = m_aimAperture.parent.TransformPoint(m_aimAperture.localPosition + m_VRHandleCenterR.localPosition + m_VRHandleCenterR.GetChild(0).localPosition);
            leverAtHeld = m_VRHandlePickupR.transform.position;
        }

        Vector3 leverPoint = leverAtRest - m_aimAperture.parent.position;
        Vector3 leverFwd = -Vector3.Normalize(leverPoint);
        Vector3 leverRight = Vector3.Cross(m_aimAperture.parent.up, leverFwd);
        Vector3 leverUp = Vector3.Cross(leverFwd, leverRight);
        Vector3 leverProjected = Vector3.Project(leverPoint, m_aimAperture.parent.forward);
        Vector3 leverOffset = leverProjected - leverPoint;
        float leverOffsFwd = Vector3.Dot(leverOffset, leverFwd);
        float leverOffsRight = Vector3.Dot(leverOffset, leverRight);
        float leverOffsUp = Vector3.Dot(leverOffset, leverUp);

        Vector3 heldLeverPoint = leverAtHeld - m_aimAperture.parent.position;
        Vector3 heldLeverFwd = -Vector3.Normalize(heldLeverPoint);
        Vector3 heldLeverRight = Vector3.Cross(m_aimAperture.parent.up, heldLeverFwd);
        Vector3 heldLeverUp = Vector3.Cross(heldLeverFwd, heldLeverRight);
        Vector3 heldLeverProjected = heldLeverPoint + leverOffsFwd * heldLeverFwd + leverOffsRight * heldLeverRight + leverOffsUp * heldLeverUp;
        Vector3 aimDir = -Vector3.Normalize(heldLeverProjected);
        Vector3 aimDir2D = Vector3.ProjectOnPlane(aimDir, m_aimAperture.parent.up);

        float pitch = -Mathf.Asin(Vector3.Dot(aimDir, m_aimAperture.parent.up)) * Mathf.Rad2Deg;
        float yaw = Vector3.SignedAngle(m_aimAperture.parent.forward, aimDir2D, m_aimAperture.parent.up);
        m_aimPitchRotation = Mathf.Clamp(pitch, -25, 15);
        m_aimYawRotation = Mathf.Clamp(yaw, -50, 50);
        m_aimAperture.localRotation = Quaternion.Euler(m_aimPitchRotation, m_aimYawRotation, 0.0f);
    }

    private void ResetState()
    {
        m_occupant = null;
        m_isReady = false;
        m_stationYawRotation = 0.0f;
        m_aimYawRotation = 0.0f;
        m_aimPitchRotation = 0.0f;
        m_inputDesktopAimYawRotation = 0.0f;
        m_inputDesktopAimPitchRotation = 0.0f;
        m_inputStationYawRotation = 0.0f;
        m_inputDesktopAimPitchUsingMouse = false;
        m_inputHandleHeldL = false;
        m_inputHandleHeldR = false;
        m_useInputL = false;
        m_useInputR = false;
        m_useInput = false;
        m_VRHandlePickupL.SetInactive();
        m_VRHandlePickupR.SetInactive();
        this.DisableInteractive = false;

        foreach (ParticleSystem system in m_animatorRoot.GetComponentsInChildren<ParticleSystem>())
            if (system.collision.enabled)
            {
                ParticleSystem.CollisionModule collision = system.collision;
                collision.sendCollisionMessages = false;
            }
    }

    #endregion

    #region SAIK Driver Functions

    public void OnSAIKAttached()
    {
        // Preliminary setup. We don't actually activate any controls until the ResetDriver call happens.
        ResetState();

        this.DisableInteractive = true;

        if (m_IKController.Avatar.IsPlayer())
        {
            m_occupant = m_IKController.Avatar.Player;

            if (!Networking.IsOwner(m_occupant, this.gameObject))
                Networking.SetOwner(m_occupant, this.gameObject);
            if (!Networking.IsOwner(m_occupant, m_VRHandlePickupL.gameObject))
                Networking.SetOwner(m_occupant, m_VRHandlePickupL.gameObject);
            if (!Networking.IsOwner(m_occupant, m_VRHandlePickupR.gameObject))
                Networking.SetOwner(m_occupant, m_VRHandlePickupR.gameObject);
        }
    }

    public void OnSAIKDetached()
    {
        ResetState();
    }

    public void ResetDriver()
    {
        // Gets called when the SAIK controller is ready or has been reset due to an avatar change.
        // Enable the VR handles and the rest of the control systems.
        if (m_occupant != null && m_occupant.isLocal)
        {
            if (m_occupant.IsUserInVR())
            {
                m_VRHandlePickupL.SetActive();
                m_VRHandlePickupR.SetActive();
            }

            foreach (ParticleSystem system in m_animatorRoot.GetComponentsInChildren<ParticleSystem>())
                if (system.collision.enabled)
                {
                    ParticleSystem.CollisionModule collision = system.collision;
                    collision.sendCollisionMessages = true;
                }
        }

        m_isReady = true;
    }

    public void ExecuteDriver()
    {
        if (!m_isReady)
            return;

        if (m_occupant == null)
        {
            // Occupant is a non-player animator model
            ExecuteDriverForDesktop();
        }
        if (m_occupant.isLocal && m_occupant.IsUserInVR())
        {
            // VR IK only executes for the local player
            ExecuteDriverForVR();
        }
        else if (!m_occupant.IsUserInVR())
        {
            // Desktop IK executes for all players
            ExecuteDriverForDesktop();
        }
    }

    private void ExecuteDriverForDesktop()
    {
        m_IKController.SetTransmissionBitPack(ComputeBothArmsIK(m_IKTargetHandLL, m_IKTargetHandRR));
    }

    private void ExecuteDriverForVR()
    {
        // Select IK targets based on tracker proximity or if either of the IK handles are held.
        float proximityL = m_VRHandlePickupL.Pickup.proximity;
        float proximityR = m_VRHandlePickupR.Pickup.proximity;
        Vector3 leftHandPos = m_occupant.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        Vector3 rightHandPos = m_occupant.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;

        Transform leftTarget = null;
        if (m_VRHandlePickupL.Pickup.currentHand == VRC_Pickup.PickupHand.Left || (!m_useInput && Vector3.Distance(leftHandPos, m_VRHandleCenterL.position) < proximityL))
            leftTarget = m_IKTargetHandLL;
        else if (m_VRHandlePickupR.Pickup.currentHand == VRC_Pickup.PickupHand.Left || (!m_useInput && Vector3.Distance(leftHandPos, m_VRHandleCenterR.position) < proximityR))
            leftTarget = m_IKTargetHandRL;

        Transform rightTarget = null;
        if (m_VRHandlePickupR.Pickup.currentHand == VRC_Pickup.PickupHand.Right || (!m_useInput && Vector3.Distance(rightHandPos, m_VRHandleCenterR.position) < proximityL))
            rightTarget = m_IKTargetHandRR;
        else if (m_VRHandlePickupL.Pickup.currentHand == VRC_Pickup.PickupHand.Right || (!m_useInput && Vector3.Distance(rightHandPos, m_VRHandleCenterL.position) < proximityR))
            rightTarget = m_IKTargetHandLR;

        // Apply IK to arms based on selected targets.
        if (leftTarget != null && rightTarget != null)
        {
            m_IKController.SetTransmissionBitPack(ComputeBothArmsIK(leftTarget, rightTarget));
        }
        else if (leftTarget != null)
        {
            m_IKController.SetTransmissionBitPack(ComputeLeftArmIK(leftTarget));
        }
        else if (rightTarget != null)
        {
            m_IKController.SetTransmissionBitPack(ComputeRightArmIK(rightTarget));
        }
        else
        {
            m_IKController.SetTransmissionDirect(SignalRevertToTracking());
        }
    }

    private Vector3 ComputeLeftArmIK(Transform target)
    {
        const int channel = (1 << 4);
        Vector3[] flexLeft = ComputeLeftArmIKFlex(target);
        return SAIKChannelPacker.BothArms2Chain9933(flexLeft, new[] { Vector3.zero, Vector3.zero }) / channel;
    }

    private Vector3 ComputeRightArmIK(Transform target)
    {
        const int channel = (1 << 6);
        Vector3[] flexRight = ComputeRightArmIKFlex(target);

        return SAIKChannelPacker.BothArms2Chain9933(new[] { Vector3.zero, Vector3.zero }, flexRight) / channel;
    }

    private Vector3 ComputeBothArmsIK(Transform targetL, Transform targetR)
    {
        const int channel = (1 << 8);
        Vector3[] flexLeft = ComputeLeftArmIKFlex(targetL);
        Vector3[] flexRight = ComputeRightArmIKFlex(targetR);
        return SAIKChannelPacker.BothArms2Chain9933(flexLeft, flexRight) / channel;
    }

    private Vector3[] ComputeLeftArmIKFlex(Transform target)
    {
        int[] leftBones = SAIKBoneChains.LeftArmUpperLowerHand();
        Quaternion[] leftRotations = SAIKSolver.SolveArm2Chain(m_IKController, leftBones, target);
        Vector3[] leftAngles = SAIKMecanimHelper.ComputeMecanimFlexRotations(m_IKController.AvatarInfo, leftBones, leftRotations);

        return SAIKMecanimHelper.ComputeMecanimFlexParams(m_IKController.AvatarInfo, leftBones, leftAngles);
    }

    private Vector3[] ComputeRightArmIKFlex(Transform target)
    {
        int[] rightBones = SAIKBoneChains.RightArmUpperLowerHand();
        Quaternion[] rightRotations = SAIKSolver.SolveArm2Chain(m_IKController, rightBones, target);
        Vector3[] rightAngles = SAIKMecanimHelper.ComputeMecanimFlexRotations(m_IKController.AvatarInfo, rightBones, rightRotations);

        return SAIKMecanimHelper.ComputeMecanimFlexParams(m_IKController.AvatarInfo, rightBones, rightAngles);
    }

    private Vector3 SignalRevertToTracking()
    {
        return Vector3.zero;
    }

    #endregion

    #region Editor Functions
#if UNITY_EDITOR
    [UnityEditor.Callbacks.PostProcessScene(0)]
    public static void OnPostprocessScene()
    {
        SAIKGunTurretSettings settings = GameObject.FindAnyObjectByType<SAIKGunTurretSettings>();

        SAIKGunTurretController[] controllers = GameObject.FindObjectsByType<SAIKGunTurretController>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (SAIKGunTurretController controller in controllers)
            controller.m_settings = settings;
    }
#endif
    #endregion
}
