using UnityEngine;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using UdonSharp;
using System.Linq;
using System.Reflection;

[AddComponentMenu("")]
public class ClientSimSAIKCoreController : MonoBehaviour
{
    private ClientSimPlayer m_clientSimPlayer = null;
    private ClientSimDesktopTrackingProvider m_clientSimTrackingProvider = null;
    private Transform m_clientSimHeadTransform = null;
    private Animator m_clientSimAnimator = null;
    private Animator m_clientSimAnimatorProxy = null;

    private UdonBehaviour m_stationFrame = null;

    private void Awake()
    {
        ClientSimPlayer clientSimPlayer = Networking.LocalPlayer.GetClientSimPlayer();
        ClientSimPlayerController clientSimPlayerController = clientSimPlayer.GetPlayerController();
        ClientSimDesktopTrackingProvider clientSimTrackingProvider = clientSimPlayer.GetTrackingProvider() as ClientSimDesktopTrackingProvider;
        Transform clientSimHeadTransform = clientSimTrackingProvider.transform.Find("Head");
        Animator clientSimAnimator = clientSimPlayerController.GetComponentInChildren<Animator>();

        UdonBehaviour udonCoreController = GetBackingUdonBehaviour(GetComponent<SAIKCoreController>());
        UdonBehaviour udonAvatarInterface = GetBackingUdonBehaviour(GetComponent<SAIKAvatarInterface>());
        UdonBehaviour udonStationFrame = udonCoreController.GetProgramVariable<UdonBehaviour>("m_stationFrame");

        // Clone the client sim animator, then set the clone to use the station's animator and be visible to the camera.
        Animator animatorProxy = GameObject.Instantiate(clientSimAnimator, udonStationFrame.transform);
        animatorProxy.runtimeAnimatorController = udonStationFrame.GetComponent<VRCStation>().animatorController;
        animatorProxy.gameObject.layer = 0;
        foreach (Transform child in animatorProxy.GetComponentInChildren<Transform>(true))
            child.gameObject.layer = 0;

        // Attach the clone to the SAIK controller and hide the actual client sim animator.
        udonAvatarInterface.SetProgramVariable<Animator>("m_animator", animatorProxy);
        clientSimAnimator.gameObject.SetActive(false);

        m_clientSimPlayer = clientSimPlayer;
        m_clientSimTrackingProvider = clientSimTrackingProvider;
        m_clientSimHeadTransform = clientSimHeadTransform;
        m_clientSimAnimator = clientSimAnimator;
        m_clientSimAnimatorProxy = animatorProxy;
        m_stationFrame = udonStationFrame;
    }

    private void Update()
    {
        // Immobilize the camera view if necessary
        IClientSimStation station = m_clientSimPlayer.GetStationHandler().GetCurrentStation();
        if (station != null && station.GetStationGameObject() == m_stationFrame.gameObject)
        {
            if (m_stationFrame.GetProgramVariable<bool>("m_immobilizeView"))
                m_clientSimHeadTransform.localRotation = Quaternion.identity;
        }

        // Account for avatar scaling.
        m_clientSimAnimatorProxy.transform.localScale = m_clientSimAnimator.transform.lossyScale;
    }

    private void LateUpdate()
    {
        // TemporaryPoseSpace doesn't work in client sim, so we approximate it by moving the eye height to the proxy model's head height.
        Transform headTransform = m_clientSimAnimatorProxy.GetBoneTransform(HumanBodyBones.Head);
        float proxyAnimatorEyeHeight = headTransform.position.y;
        float clientSimAnimatorHeight = m_clientSimAnimator.transform.position.y;
        float clientSimEyeHeight = clientSimAnimatorHeight + 1.75f * m_clientSimAnimator.transform.lossyScale.y;
        
        m_clientSimTrackingProvider.transform.localPosition = new Vector3(0.0f, proxyAnimatorEyeHeight - clientSimEyeHeight, 0.0f);
    }

    private void OnDestroy()
    {
        if (m_clientSimAnimatorProxy != null)
            GameObject.Destroy(m_clientSimAnimatorProxy.gameObject);
        if (m_clientSimAnimator != null)
            m_clientSimAnimator.gameObject.SetActive(true);
        if (m_clientSimTrackingProvider != null)
            m_clientSimTrackingProvider.transform.localPosition = Vector3.zero;
    }

    public static void ClientSimAttach(UdonBehaviour target)
    {
        if (target.gameObject.GetComponent<ClientSimSAIKCoreController>() == null)
            target.gameObject.AddComponent<ClientSimSAIKCoreController>();
    }

    public static void ClientSimDetach(UdonBehaviour target)
    {
        ClientSimSAIKCoreController controllerProxy = target.GetComponent<ClientSimSAIKCoreController>();
        if (controllerProxy != null)
            GameObject.Destroy(controllerProxy);
    }

    public static void SendCustomNetworkEventHook(UdonBehaviour target, NetworkEventTarget netTarget, string eventName)
    {
        if (target != GetBackingUdonBehaviour(target.GetComponent<SAIKCoreController>()))
            return;

        try
        {
            MethodInfo[] staticMethods = typeof(ClientSimSAIKCoreController).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo eventMethod = staticMethods.FirstOrDefault(x => x.Name == eventName && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(UdonBehaviour));
            eventMethod?.Invoke(null, new[] { target });
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogError(ex.InnerException);
        }
    }

    private static UdonBehaviour GetBackingUdonBehaviour(UdonSharpBehaviour behaviour)
    {
#if UNITY_EDITOR
        return UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
#else
        return null;
#endif
    }
}
