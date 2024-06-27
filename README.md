# Station Animator IK

A VRChat prefab for creating custom in-world IK interactions compatible with most VRChat humanoid SDK3 avatars. This prefab can be used for vehicle steering‚ airplane cockpit controls, mounted turrets, or other station-based interactions.

The git repo of this package does not contain any of the demo models or assets. Please install via the provided Unity package to view those scenes or use the accompanying prefabs.

See the GunTurret Demo for a complete example of how to implement this prefab into your own project.

A publicly available demo world for this prefab is available to visit here:
https://vrchat.com/home/world/wrld_0fad1508-b756-499b-b303-aba8df2949c6

<img src="https://raw.githubusercontent.com/JulieCat680/SAIKSystemPrefab/main/_ReadmeImages/SAIK_GunTurretDemo.gif" width="40%"/>

## Prefab: SAIKCore
The core controller module for the SAIK system. Responsible for measuring avatar limb rotation characteristics and transmitting computed IK pose data from Udon into the station animator. It is not normally used on its own. Instead, it should be used alongside a custom control script that controls how IK interaction should occur. The custom control script should obtain a reference to the prefab's SAIKCoreController Udon script and call the following functions on it in its Start function:


| **SetDriverCallback**  |                   |
| ---------------------- | ----------------- |
| driver                 | The target UdonBehaviour script to receive the callback event functions supplied in this parameter list. This is usually the custom controller script that is calling this function.  |
| executeCallback        | Name of the public callback function that is responsible for computing IK and supplying limb parameters back to the core controller.                                                  |
| attachedCallback       | Name of the public callback function to be called when a player or model is connected to the SAIKController.                                                                          |
| detachedCallback       | Name of the public callback function to be called when a player or model is disconnected from the SAIKController.                                                                     |
| resetCallback          | Name of the public callback function to be called when the SAIKController has finished recomputing the avatar's limb rotation characteristics and is ready to start posing the model. |


| **SetControlFrame**    |                   |
| ---------------------- | ----------------- |
| controlFrame           | The transform that the player's model will be positioned at - effectively the station position.|
| controlFrameHeight     | An additional elevation offset to apply to the station for desktop and 3-point tracking VR users. This is used to make the 'entry' position of the station consistent across all user types.|

Other functions that you may wish to call from your control script:

| Function                                  |                   |
| ----------------------------------------- | ----------------- |
| **TryAttachPlayer**                       | Attempts to load a player into the SAIK station. This is not guaranteed to happen if another client claims the station first or the station is already occupied. Listen on the attachedCallback to determine whether this succeeds or not.|
| **TryAttachAnimator**                     | Attempts to load a static Animator model into the SAIK station. This may be useful for in-editor testing. Can fail if the station is already occupied.|
| **SetImmobilizeView**                     | Sets whether desktop users can rotate their view left and right when they are in the station. Changes will only take effect the next time someone enters the station.|
| **SetTransmission (None/BitPack/Direct)** | Sets the data transmission value to be sent to the avatar's animator next update. See the Data Transmission section for more info.|

## Prefab: SAIKAvatarInfoCache
An optional utility prefab that allows avatar information to be cached.

When a player first enters into a SAIK controlled station, the station must measure the limb rotation characteristics of the avatar. This manifests as a momentary flurry of motion before the avatar settles into the station.

By using the SAIKAvatarInfoCache, the measurements taken from one station can be used for all other stations in the world without needing to re-measure the avatar. This lasts until the player changes or resets their avatar after which the cache is invalidated. Once this happens, the avatar must be measured again the next time the player enters into a SAIK station.

Simply placing this prefab into the world will allow it to be used by all SAIKCoreController instances in the world.

## Prefab: SAIKVRHandle
A utility prefab that is setup to simplify pickup-based IK interactables.

For reasons discussed below in the Technical Limitations section, IK data for VR users cannot be computed remotely on other VRChat clients and instead must be synced across the network. The sync rate of this data is faster than the typical VRC_ObjectSync or Udon Sync data rate, so this makes it difficult to properly sync up the IK placement of the VR user's hands with a separate VRC_ObjectSync or Udon Synced IK target.

The exception to the above are pickups. Pickups held by players are synced at the same higher rate that IK sync occurs at. By having the main interaction interface of an IK interaction be a pickup, it is possible to make the custom IK appear properly synced up with the interaction IK target for all clients.

Controller scripts using SAIKVRHandle objects should call Init on them in their Start function. This function takes parameters to supply callbacks to the control script for when handle is picked up and dropped.

## UdonSharp Scripts

| Script                        |   |
| ----------------------------- | - |
| **SAIKAvatarInfo**            | A synced data store of avatar limb rotation characteristics for the owner's avatar. This is measured every time a player enters a SAIK station unless measurement data can be retrieved from a SAIKAvatarInfoCache. The data inside this data store is purged when the player leaves the SAIK station in order to minimize network load.|
| **SAIKAvatarInfoCache**       | A per-player, unsynced cache copy of SAIKAvatarInfo data. As long as the player's avatar hasn't changed or been reset, this data will be copied into the SAIKAvatarInfo of the next SAIK station that the player enters.|
| **SAIKAvatarInterface**       | A utility class that allows both player avatars and in-world animator models to be treated in roughly the same manner. Allows the SAIK system to work both with player avatars and standalone models.|
| **SAIKCoreController**        | The core SAIK system script. Responsible for facilitating data transfer between Udon and the IK animator as well as triggering the measurement cycle needed to populate the SAIKAvatarInfo.|
| **SAIKStationAnimatorReset**  | A helper script used inside the SAIKCore prefab to reset any disabled animation layers on the player's avatar once they leave the SAIK station.|
| **SAIKStationFrame**          | A helper script used inside the SAIKCore prefab to better facilitate loading and unloading players from the SAIK station.|
| **SAIKVRHandle**              | A helper script used inside the SAIKVRHandle prefab. Exposes functions to activate or deactivate the handle as well as set callbacks for when it is picked up or dropped.|

## Static C# Utility Classes

| Class                             |   |
| --------------------------------- | - |
| **SAIKBoneChains**                | Defines some simple bone chains that can be supplied into the SAIKSolver functions.|
| **SAIKChannelPacker**             | Provides functions for converting collections of limb flex rotation values into bit-packed XYZ velocity vectors that can be transmitted to the IK animator via the SetTransmissionBitPacked function on the SAIKCoreController.|
| **SAIKDefaultAnimatorInterface**  | Provides functions for converting arm rotation values into vectors that can be properly read and interpreted by the SAIKCore_DefaultIKController animator controller.|
| **SAIKEncoder**                   | Provides functions for converting collections of raw floating point values into bit-packed XYZ velocity vectors.|
| **SAIKMecanimHelper**             | Provides functions for converting from Quaternion bone rotations to standardized Mecanim flex rotation values.|
| **SAIKSolver**                    | Exposes a very basic 2-bone solver that can be used to solve for basic arm or leg poses.|
| **SAIKTransmissionHelper**        | Provides an iterative error mitigation solver function to limit the amount of bit precision error incurred due to avatar rotation when sending bit-packed data to a player's IK animator.|

## Data Transmission

IK updates and pose data should be computed in the executeCallback function that is set during initialization of the SAIKCoreController script. This callback is invoked during the SAIKCoreController's Update function at script execution order 10000.

While it is possible to set the SAIKCoreController's transmission vector in other update functions, it is advisable to do so in the executeCallback as this will allow IK computation to take place with the most up-to-date state of the world while also being right before the point where transmission occurs. Any earlier, and the world might change after the IK computation occurs causing a mismatch between the computed IK pose and any IK targets. Any later, and the computed IK pose won't get sent to the avatar until the following frame where the world also may have updated.

Data transmission is primarily facilitated through the following functions:
| Function                      |                   |
| ----------------------------- | ----------------- |
| **SetTransmissionNone**       | Resets the data transmission vector to zero. This is automatically called at the end of every LateUpdate at script execution order 10000 |
| **SetTransmissionDirect**     | Simply sets an XYZ velocity value into the data transmission vector. Note that this value is subject to bit-precision errors, so while it is more performant, it is not suitable for sending bit-packed data. |
| **SetTransmissionBitPacked**  | Sets an XYZ velocity value while attempting to mitigate any bit-precision error that may occur due to the avatar's rotation in the world. This may fail causing the value to not be sent at all, so it should not be used for sending control values or indicating distinct state changes. |

Data transmission takes place via the XYZ velocity values of the player avatar's animator. For regular data transmission via SetTransmissionDirect, this allows 3 distinct 32-bit floating point values to be transmitted into the avatar. However, using bit-packed transmission, it is possible to transmit a flexible buffer of 72-bits of information plus an additional "channel" indicator value to control how the bits are interpreted.

The exact interpretation of the channel indicator value as well as the 72-bit data buffer is dependent on the custom animator used in the SAIKStationFrame's VRC_Station. For example, the SAIKCore_DefaultIKAnimator splits the buffer up into 8 distinct 9-bit values which are then used to pose the upper and lower arm rotations of both the left and right arms. Channel (1/16) sets the left arm position, channel (1/64) sets the right arm position, and channel (1/256) sets the position of both arms at once.

Bit packing is primarily handled through the SAIKChannelPacker utility class which can take computed mecanim flex rotations produced from the IK computation and convert them into an XYZ velocity vector that can be transmitted using the SetTransmissionBitPacked function. The channel indicator flag is stored in the floating point exponent, so it is possible to manipulate it independently of the packed bits by dividing the packed bit vector by a power-of-2 value (e.g. 1/16, 1/64, 1/256, etc). The exact power-of-2 values used will depend on the implementation of the IK animator.

Refer to the SAIKDefaultAnimatorInterface utility class or the SAIKGunTurretController udon script for examples of complete channel packing functions.

## IK Computation

As noted above, IK computation should occur during the executeCallback of your control script. The exact nature of the computations will largely depend on your application - that is, IK for a flight stick or lever is going to be significantly different than IK for a steering wheel. The SAIKSolver class provides a basic 2-bone solver that can be used for basic arm and leg positioning, but anything more complicated will likely require the use of FinalIK or some other custom implementation.

Because limb placement is exclusively driven through Mecanim flex values sent via the station animator, it is necessary to convert the Quaternion rotations produced from your IK calculation into standardized Mecanim flex values. This is largely facilitated through the SAIKMecanimHelper class. It should be noted that the SAIKCore_DefaultIKAnimator limits these flex value to (-1,+1), so any rotation not representable by the model's Mecanim flex range will be truncated.

## Moving the SAIK Station

When designing vehicles or other moving stations that feature the SAIK system, it is important to consider when you intend to update the position of the SAIK ControlFrame and its IK targets.

The following movement sources are compatible with the SAIK system:
- Update (script execution order less than 10000)
- FixedUpdate
- RigidBody physics
- An animator set to AnimatePhysics

The following movement sources are incompatible with the SAIK system and will cause IK to lag behind its targets:
- Update (script execution order greater than 10000)
- LateUpdate
- PostLateUpdate
- Constraints
- An animator set to AnimateUnscaledTime

## In-Editor Testing

This prefab has intergrations with the VRChat Client Sim and will apply itself to the default Client Sim robot avatar when entering into a station. You may also use the TryAttachAnimator function on the SAIKCoreController to attach a standard Unity animator-based humanoid model to the controller for testing purposes.

## Technical Limitations
### Transmission Limits
The transmission bus between the SAIKCoreController Udon script and the avatar's animator is limited to 72 bits of information per update (plus the additional channel indicator value). The default SAIKCore_DefaultIKAnimator expects these bits to be sent as 8 distinct 9-bit fixed-point decimal values. This is enough to fully pose one limb (shoulder, arm, elbow, wrist), or partially pose 2 limbs at once (arm, elbow). For more complex posing, it is possible to pose multiple limbs in a rotating sequence, but this comes at the cost of visual smoothness and may make the IK look jittery or imperfect.

### Data Quantization
Pose data is sent to the IK animator as Mecanim flex rotation values. Mecanim defines the most typical range of human motion as between (-1, +1), but these flex values can extend outside of that range for extreme hyper-extensions.

Since flex rotation values are transmitted across a bit-limited channel, both the range and fidelity of these values have additional limitations. For the SAIKCore_DefaultIKAnimator, flex rotation values are quantized to 512 distinct steps between (-1 and +1). As a result of this, there will be a small amount of visual stutter as the limb movement snaps to these quantized thresholds. Additionally, the limited quantization range means that it is not possible to pose the model into any hyper-extended positions.

### Remote VR Player IK Delay
Data transmission from the SAIKCoreController Udon script into the avatar's animator occurs via the VelocityX, VelocityY, and VelocityZ animator parameters. For desktop users, these parameters can be set by any client with the results being observable locally for the values provided by that client. For VR users, these values can only be set by the owner client with the values set by the owner being transmitted across the network to other clients. This transmission across the network incurs significant latency and may make it difficult to properly sync up the custom IK limb positions with a given IK target. See the Prefab: SAIKVRHandle section for one possible approach to mitigating this issue.

To Summarize
- **Desktop Player Observing Self:** IK is instant
- **VR Player Observing Self:** IK is instant
- **Desktop Player Observed By Other:** IK is instant (because IK also gets computed on the observer client)
- **VR Player Observed By Other:** IK has delay based on network conditions (IK is computed on the owner client and sent to the observer client)

### No Head Tracking For Desktop Users
Due to an irregularity with how desktop users rotate inside stations, desktop users cannot have any head tracking enabled while inside a SAIK station. It may still technically be possible to indirectly control the head rotation via the SAIK engine, but doing so will be at the cost of consuming additional bits from the 72-bit transmission bus.

## Credits
- SAIK system prefab and assets by HardLight680
- Demo assets and prefabs by VowganVR:
  - Insight Camera System
  - Gun Turret Model
  - UFO Model
  - Environment Models

