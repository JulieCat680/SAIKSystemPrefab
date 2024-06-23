Station Animator IK v1.0
========================
A VRChat prefab for creating custom in-world IK interactions compatible with most VRChat humanoid SDK3 avatars. It can be used for vehicle steering, airplane cockpit controls, mounted turrets, or other station-based interactions.

The git repo of this package does not contain any of the demo scene assets. Please install via the provided Unity package to view those scenes

See the GunTurret Demo for a complete example of how to implement this prefab into your own project.

This prefab has intergrations with the VRChat Client Sim and can be previewed in the editor using the default Client Sim robot avatar.

Controller Interface
====================
The SAIKCore prefab makes up the core of the control system. After adding it to your interactable object, you will need to initialize it by calling the following functions from your interactable control script's Start function:

**SetDriverCallback** 
| Parameter        |                   |
| ---------------- | ----------------- |
| driver           | The target of the custom event functions supplied in this parameter list. This is usually your interactable object's controller script.                                               |
| executeCallback  | Name of the public callback function to compute IK and supply limb parameters back to the core controller.                                                                            |
| attachedCallback | Name of the public callback function to be called when a player or model is connected to the SAIKController.                                                                          |
| detachedCallback | Name of the public callback function to be called when a player or model is disconnected from the SAIKController.                                                                     |
| resetCallback    | Name of the public callback function to be called when the SAIKController has finished recomputing the avatar's limb rotation characteristics and is ready to start posing the model. |

**SetControlFrame**
| Parameter          |                   |
| ------------------ | ----------------- |
| controlFrame       | The transform that the player's model will be positioned at - effectively the station position.                                                                                              |
| controlFrameHeight | An additional elevation offset to apply to the station for desktop and 3-point tracking VR users. This is used to make the 'entry' position of the station consistent across all user types. |

Data Transmission
=================
More info coming soon.

Limitations
===========
The transmission bus between the SAIKCoreController script and the avatar's animator is limited to 72 bits of information per update (plus some additional control flags). The default data transmission mode sends these bits as 8 distinct 9-bit fixed-point decimal values. This is enough to fully pose one limb (shoulder, arm, elbow, wrist), or partially pose 2 limbs at once (arm, elbow). For more complex posing, it is possible to pose multiple limbs in sequence, but this comes at the cost of visual smoothness and may make the IK look jittery or imperfect.

Because rotation values are sent to the animator as 9-bit values, there is a limit to the amount of precision that can be represented in the pose data. As a result, there will be a small amount of visual stutter as the limb snaps between these quantized thresholds.

Credits
=======

- SAIK system prefab and assets by HardLight680
- Demo assets and prefabs by VowganVR:
  - Insight Camera System
  - Gun Turret Model
  - UFO Model
  - Environment Models

