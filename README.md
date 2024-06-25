# Station Animator IK

A VRChat prefab for creating custom in-world IK interactions compatible with most VRChat humanoid SDK3 avatars. This prefab can be used for vehicle steering‚ airplane cockpit controls, mounted turrets, or other station-based interactions.

The git repo of this package does not contain any of the demo models or assets. Please install via the provided Unity package to view those scenes or use the accompanying prefabs.

See the GunTurret Demo for a complete example of how to implement this prefab into your own project.

A publicly available demo world for this prefab is available to visit here:
https://vrchat.com/home/world/wrld_0fad1508-b756-499b-b303-aba8df2949c6

<img src="https://raw.githubusercontent.com/JulieCat680/SAIKSystemPrefab/main/_ReadmeImages/SAIK_GunTurretDemo.gif" width="40%"/>

## Prefab: SAIKCore
The core controller module for the SAIK system. Responsible for measuring avatar limb rotation characteristics and transmitting computed IK pose data from Udon into the station animator. It is not normally used on its own. Instead, it should be used alongside a custom control script that controls how IK interaction should occur. The custom control script should call the following functions in its Start callback:

**SetDriverCallback** 
| Parameter        |                   |
| ---------------- | ----------------- |
| driver           | The target UdonBehaviour script to receive the callback event functions supplied in this parameter list. This is usually the custom controller script that is calling this function.  |
| executeCallback  | Name of the public callback function that is responsible for computing IK and supplying limb parameters back to the core controller.                                                  |
| attachedCallback | Name of the public callback function to be called when a player or model is connected to the SAIKController.                                                                          |
| detachedCallback | Name of the public callback function to be called when a player or model is disconnected from the SAIKController.                                                                     |
| resetCallback    | Name of the public callback function to be called when the SAIKController has finished recomputing the avatar's limb rotation characteristics and is ready to start posing the model. |

**SetControlFrame**
| Parameter          |                   |
| ------------------ | ----------------- |
| controlFrame       | The transform that the player's model will be positioned at - effectively the station position.                                                                                              |
| controlFrameHeight | An additional elevation offset to apply to the station for desktop and 3-point tracking VR users. This is used to make the 'entry' position of the station consistent across all user types. |

Other functions that you may wish to call from your control script:

| Function                              |                   |
| ------------------------------------- | ----------------- |
| TryAttachPlayer                       | Attempts to load a player into the SAIK station. This is not guaranteed to happen if another client claims the station first or the station is already occupied. Listen on the attachedCallback to determine whether this succeeds or not. |
| TryAttachAnimator                     | Attempts to load a static Animator model into the SAIK station. This may be useful for in-editor testing. Can fail if the station is already occupied                 |
| SetImmobilizeView                     | Sets whether desktop users can rotate their view left and right when they are in the station. Changes will only take effect the next time someone enters the station. |
| SetTransmission (None/BitPack/Direct) | Sets the data transmission value to be sent to the avatar's animator next update. See the Data Transmission section for more info.                                    |

## Prefab: SAIKAvatarInfoCache
An optional utility prefab that allows avatar information to be cached.

When a player first enters into a SAIK controlled station, the station must measure the limb rotation characteristics of the avatar. This manifests as a momentary flurry of motion before the avatar settles into the station.

By using the SAIKAvatarInfoCache, the measurements taken from one station can be used for all other stations in the world without needing to re-measure the avatar. This lasts until the player changes or resets their avatar after which the cache is invalidated. Once this happens, the avatar must be measured again the next time the player enters into a SAIK station.

Simply placing this prefab into the world will allow it to be used by all SAIKCoreController instances in the world.

## Prefab: SAIKVRHandle
A utility prefab that is setup to simplify pickup-based IK interactables.

For reasons discussed below in the Technical Limitations section, IK data for VR users cannot be computed remotely on other VRChat clients and instead must be synced across the network. The sync rate of this data is faster than the typical VRC_ObjectSync or Udon Sync data rate, so this makes it difficult to properly sync up the IK placement of the VR user's hands with a separate VRC_ObjectSync or Udon Synced object.

The exception to the above are pickups. Pickups held by players are synced at the same higher rate that IK sync occurs at. By having the main interaction interface of an IK interaction be a pickup, it is possible to make the custom IK appear properly synced up with the interaction object for all clients.

Controller scripts using SAIKVRHandle objects should call Init on them in their Start function. This function takes parameters to supply callbacks to the control script for when handle is picked up and dropped.

## Data Transmission
More info coming soon.

## In-Editor Testing

This prefab has intergrations with the VRChat Client Sim and will apply itself to the default Client Sim robot avatar when entering into a station. You may also use the TryAttachAnimator function on the SAIKCoreController to attach an standard unity Animator-based humanoid model to the controller for testing purposes.

## Technical Limitations
### Transmission Limits
The transmission bus between the SAIKCoreController Udon script and the avatar's animator is limited to 72 bits of information per update (plus some additional control flags). The default data transmission mode sends these bits as 8 distinct 9-bit fixed-point decimal values. This is enough to fully pose one limb (shoulder, arm, elbow, wrist), or partially pose 2 limbs at once (arm, elbow). For more complex posing, it is possible to pose multiple limbs in a rotating sequence, but this comes at the cost of visual smoothness and may make the IK look jittery or imperfect.

### Data Quantization
Because rotation values are sent to the animator as 9-bit values, there is a limit to the amount of precision that can be represented in the pose data. As a result, there will be a small amount of visual stutter as the limb snaps between these 
quantized thresholds.

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

