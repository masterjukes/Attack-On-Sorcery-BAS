using System.Collections;
using System.Threading.Tasks;
using IngameDebugConsole;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;
using UnityEngine.SpatialTracking;
using static ThunderRoad.Yielders;
using Object = UnityEngine.Object;

namespace BladeAndTitan.TitanShifting.Abstract
{
    public abstract class PlayerTitanBase : SpellCastCharge
    {
        public static bool isTitan;
        public static bool isTransforming;
        private static float lastHeight;
        private static TitanHand leftTitanHand;
        private static TitanHand rightTitanHand;
        private static Transform unspawnLocation;

        protected static Transform leftFoot;
        protected static Transform rightFoot;

        protected static GameObject titan;

        protected static bool isTransformingIn;

        public abstract string titanAddress { get; }
        public abstract float footDistance { get; }

        public abstract float stepSpeed { get; }

        public abstract string stepSoundId { get; }
        public abstract float maxHealth { get; }
        public abstract float jumpForce { get; }
        public abstract float speedMultiplier { get; }

        public abstract float handWeight { get; }
        
        public abstract bool useXYThumbRotation { get; }

        public abstract Vector3 thumbRotationLeft { get; }
        public abstract Vector3 thumbRotationRight { get; }
        

        private float lastHeadRotation;

        private const string ShiftEffectsName = "ShiftEffects";
        private const string HeadTargetName = "HeadTarget";
        private const string RightHandTargetName = "RightHandTarget";
        private const string LeftHandTargetName = "LeftHandTarget";

        private const string TitanTopTransform = "Scale";
        private const string TitanBottomTransform = "CreatureLocation";
        protected abstract string VRIKLeftFootName { get; }
        protected abstract string VRIKRightFootName { get; }
        private const string UnshiftSmokeFXTransform = "TitanSmoke";
        private const string UnshiftEmbersFXTransform = "TitanFlames";
        

        public abstract float HeadTargetForwardOffset {get;}
        private const float LocomotionHeightMultiplier = 1f / 3f;
        private const float MaxLocomotionVelocity = 30f;
        
        protected abstract Quaternion TitanHandLeftRotation { get; }
        protected abstract Quaternion TitanHandRightRotation { get; }
        protected abstract Quaternion TitanHeadRotation { get; }


        public sealed override void Load(SpellCaster spellCaster)
        {
            base.Load(spellCaster);

            Player.selfCollision = true;
            Player.currentCreature.OnDamageEvent += CurrentCreatureOnOnDamageEvent;
            Player.currentCreature.OnKillEvent += CurrentCreatureOnOnKillEvent;
            Debug.Log("Loaded " + GetType().Name);

        #if DEBUG
        spellCaster.ragdollHand.playerHand.OnFistEvent += PlayerHandOnOnFistEvent;
        #endif
        }

        private void CurrentCreatureOnOnKillEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            titan = null;
            isTitan = false;
            isTransforming = false;
        }

        private void PlayerHandOnOnFistEvent(PlayerHand hand, bool gripping)
        {
                #if !DEBUG
                return;
                #endif
            
            Debug.Log($"{Player.local.handRight.link.attachedPhysicBody}     {Player.local.handRight.link.playerJoint.connectedBody.name}");

            if (gripping && !isTitan)
            {
                Debug.Log("Titan Shifting due to debug fist event");
                OnShift();
            }
            else if (isTitan)
            {
                Debug.Log("Titan Unshift due to debug fist event");
                OnUnshift();
            }
        }

        private void CurrentCreatureOnOnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (isTitan || isTransforming)
                return;


            var doneBySelf =
                (collisionInstance.sourceCollider.GetComponentInParent<Item>()?.handlers
                    .Contains(Player.currentCreature.handLeft) ?? false) ||
                (collisionInstance.sourceCollider.GetComponentInParent<Item>()?.handlers
                    .Contains(Player.currentCreature.handRight) ?? false);

            if (!doneBySelf)
                return;


            if (collisionInstance.damageStruct.damageType == DamageType.Pierce)
                OnShift();

            if (collisionInstance.damageStruct.damageType == DamageType.Slash)
                OnShift();
        }

        protected virtual void OnShift()
        {
            if (isTitan || isTransforming)
                return;
            Debug.Log("Titan Shifting");
            isTitan = true;
            isTransformingIn = true;
            isTransforming = true;
            Player.currentCreature.handLeft.caster.DisableSpellWheel("JohnSmithHandLeft");
            Player.currentCreature.handRight.caster.DisableSpellWheel("JohnSmithHandRight");

            if (spellCaster.ragdollHand.otherHand.caster.spellInstance?.id != spellCaster.spellInstance.id)
                spellCaster.ragdollHand.otherHand.caster.LoadSpell(Catalog.GetData<SpellCastData>(id));
            Player.currentCreature.StartCoroutine(SummonTitan());
        }
        


        protected IEnumerator SummonTitan()
        {
            CreateShiftEffects();

            yield return ForSeconds(effectGenerator.duration);

            Catalog.InstantiateAsync(
                titanAddress,
                Player.local.transform.position,
                Player.local.transform.rotation,
                null,
                OnTitanSpawned,
                "gd"
            );
        }

        private ShiftEffectGenerator effectGenerator;

        private void CreateShiftEffects()
        {
            var shiftEffects = new GameObject(ShiftEffectsName);

            shiftEffects.transform.SetPositionAndRotation(
                Player.local.transform.position,
                Player.local.transform.rotation
            );

            shiftEffects.transform.SetParent(Player.local.transform, true);

            effectGenerator = shiftEffects.AddComponent<ShiftEffectGenerator>();

            Player.currentCreature.StartCoroutine(effectGenerator.Activate());
        }

        private void OnTitanSpawned(GameObject titanObject)
        {
            titan = titanObject;

            DisableRagdoll();

            var height = GetTitanHeight();

            var vrik = SetupVRIK(titanObject);
            vrik.fixTransforms = true;

            var headTarget = CreateHeadTarget();
            var rightHandTarget = CreateRightHandTarget();
            var leftHandTarget = CreateLeftHandTarget();

            ConfigureVRIKTargets(vrik, headTarget, rightHandTarget, leftHandTarget);
            ConfigureTitanHands(titanObject, vrik, leftHandTarget, rightHandTarget);
            ConfigureLocomotion(vrik, height);

            Scale(height);
            
            Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = false);
            titanObject.transform.SetParent(Player.local.transform, true);

            isTransformingIn = false;
            isTransforming = false;
            isTitan = true;

            Player.currentCreature.HideItemsInHolders(true);

            OnTitanPossess();
        }

        private float GetTitanHeight()
        {
            var scale = titan.transform.FindChildRecursiveTR(TitanTopTransform);
            var creatureLocation = titan.transform.FindChildRecursiveTR(TitanBottomTransform);

            return scale.position.y - creatureLocation.position.y;
        }

        private VRIK SetupVRIK(GameObject titanObject)
        {
            var vrik = titanObject.AddComponent<VRIK>();
            vrik.AutoDetectReferences();

            return vrik;
        }

        private Transform CreateHeadTarget()
        {
            return CreateTarget(
                HeadTargetName,
                Player.local.head.transform,
                new Vector3(0, 0, HeadTargetForwardOffset),
                TitanHeadRotation
            );
        }

        private Transform CreateRightHandTarget()
        {
            return CreateTarget(
                RightHandTargetName,
                Player.local.handRight.transform,
                new Vector3(0.03f, -0.02f, -0.09f),
                Quaternion.Euler(TitanHandRightRotation.eulerAngles + new Quaternion(0.10280f, 0.67510f, 0.14981f, 0.71500f).eulerAngles)
            );
        }
        
        // INFO B.T.Abstract.PlayerTitanBase.UpdateCaster       : Right Hand: pos (0.03, -0.02, -0.09), rot (0.10280, 0.67510, 0.14981, 0.71500)
            
        // INFO B.T.Abstract.PlayerTitanBase.UpdateCaster       : Left Hand: pos (-0.03, -0.02, -0.09), rot (0.67510, 0.10280, -0.71500, -0.14982)

        private Transform CreateLeftHandTarget()
        {
            return CreateTarget(
                LeftHandTargetName,
                Player.local.handLeft.transform,
                new Vector3(-0.03f, -0.02f, -0.09f),
                Quaternion.Euler(TitanHandLeftRotation.eulerAngles + new Quaternion(0.67510f, 0.10280f, -0.71500f, -0.14982f).eulerAngles)
            );
        }

        private Transform CreateTarget(
            string targetName,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            var target = new GameObject(targetName);

            target.transform.position = parent.position;
            target.transform.parent = parent;
            target.transform.localPosition = localPosition;
            target.transform.localRotation = localRotation;

            return target.transform;
        }

        private void ConfigureVRIKTargets(
            VRIK vrik,
            Transform headTarget,
            Transform leftHandTarget,
            Transform rightHandTarget)
        {
            vrik.solver.spine.headTarget = headTarget;
            leftFoot = titan.transform.FindChildRecursive(VRIKLeftFootName);
            rightFoot = titan.transform.FindChildRecursive(VRIKRightFootName);

            vrik.solver.locomotion.onLeftFootstep.AddListener(OnLeftFootstep);
            vrik.solver.locomotion.onRightFootstep.AddListener(OnRightFootstep);
        }

        private void ConfigureTitanHands(
            GameObject titanObject,
            VRIK vrik,
            Transform leftHandTarget,
            Transform rightHandTarget)
        {
            SetHands(titanObject);

            leftTitanHand = null;
            rightTitanHand = null;

            foreach (var titanHand in titanObject.GetComponentsInChildren<TitanHand>(true))
                if (titanHand.side == Side.Left)
                    leftTitanHand = titanHand;
                else if (titanHand.side == Side.Right)
                    rightTitanHand = titanHand;

            if (leftTitanHand == null || rightTitanHand == null)
            {
                Debug.LogError(
                    "Titan hand setup failed: left or right TitanHand was not found."
                );

                return;
            }
            
            leftTitanHand.thumbXRot = thumbRotationLeft.x;
            leftTitanHand.thumbYRot = thumbRotationLeft.y;
            rightTitanHand.thumbXRot = thumbRotationLeft.z;;
            
            rightTitanHand.thumbXRot = thumbRotationRight.x;;
            rightTitanHand.thumbYRot = thumbRotationRight.y;
            rightTitanHand.thumnZRot = thumbRotationRight.z;;
            

            leftTitanHand.useXYSwapRotation = useXYThumbRotation;
            rightTitanHand.useXYSwapRotation = useXYThumbRotation;

            leftTitanHand.controllerMass = handWeight;
            rightTitanHand.controllerMass = handWeight;

            leftTitanHand.ConfigureControllerMass(leftHandTarget);
            rightTitanHand.ConfigureControllerMass(rightHandTarget);

            vrik.solver.leftArm.target = leftTitanHand.IkTarget;
            vrik.solver.rightArm.target = rightTitanHand.IkTarget;
        }

        private void ConfigureLocomotion(VRIK vrik, float height)
        {
            var stepHeight = height * LocomotionHeightMultiplier;

            vrik.solver.locomotion.footDistance = footDistance;

            vrik.solver.locomotion.stepHeight = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, stepHeight),
                new Keyframe(1, 0)
            );

            vrik.solver.locomotion.stepSpeed = stepSpeed;
            vrik.solver.locomotion.maxVelocity = MaxLocomotionVelocity;
            vrik.solver.locomotion.stepThreshold = stepHeight;
            vrik.solver.plantFeet = false;
        }

        protected virtual void OnLeftFootstep()
        {
            PlaySound(stepSoundId, leftFoot.position);
        }

        protected virtual void OnRightFootstep()
        {
            PlaySound(stepSoundId, rightFoot.position);
        }

        protected abstract void SetHands(GameObject o);

        protected virtual void OnTitanPossess()
        {
        }

        protected void Scale(float scale)
        {
            if (lastHeight == 0)
                lastHeight = Player.local.creature.morphology.height;
            Player.local.creature.morphology.height = scale;
            Player.local.transform.localScale = Vector3.one * (Player.local.creature.morphology.height /
                                                               Player.characterData.calibration.height);
            if (Player.local?.footLeft != null) Player.local.footLeft.playerMinHeight = 0.09f;

            if (Player.local?.footRight != null) Player.local.footRight.playerMinHeight = 0.09f;

            Player.local?.creature?.RefreshMorphology();
            Player.local?.creature?.currentLocomotion?.SetCapsuleCollider(Player.local.creature.morphology.legsLength);
            Player.local?.creature?.RefreshJointForceMultipliers();
            Player.local?.creature?.currentLocomotion?.RefreshPhysicModifiers();
            Player.local?.creature?.currentLocomotion?.RefreshSpeedModifiers();

            Player.local.creature.currentLocomotion.SetAllSpeedModifiers("creeg", speedMultiplier);
            Player.local.creature.currentLocomotion.jumpGroundForce = jumpForce;
            Player.local.creature.healthModifier.Add("Trog", maxHealth);

            Player.local?.handLeft?.link?.RefreshJointConfig();
            Player.local?.handLeft?.link?.RefreshJointModifiers();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
            Player.local?.handRight?.link?.RefreshJointConfig();
            Player.local?.handRight?.link?.RefreshJointModifiers();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();

            Player.local.locomotion.colliderRadius = 0.3f * Player.local.transform.localScale.x;
            Player.local.locomotion.groundDetectionDistance = 0.05f * Player.local.transform.localScale.x;

            Player.currentCreature.ragdoll.SetColliders(false);
            Player.local.airHelper.minHeight = 1 * Player.local.transform.localScale.x;
            //Player.local.airHelper.OnAirEvent += creature => GameManager.local.StartCoroutine(ReconfigureJoints());
            
            
        }
        

        private void UnScale()
        {
            Player.local.creature.morphology.height = lastHeight;
            Player.local.transform.localScale = Vector3.one;

            Player.local?.creature?.RefreshMorphology();
            Player.local?.creature?.currentLocomotion?.SetCapsuleCollider(Player.local.creature.morphology.legsLength);
            Player.local?.creature?.RefreshJointForceMultipliers();
            Player.local?.creature?.currentLocomotion?.RefreshPhysicModifiers();
            Player.local?.creature?.currentLocomotion?.RefreshSpeedModifiers();

            Player.local.creature.currentLocomotion.jumpGroundForce = 0.3f;
            Player.local.creature.healthModifier.Remove("Trog");
            Player.local.creature.currentLocomotion.RemoveSpeedModifier("creeg");
            Player.local.creature.currentLocomotion.ClearSpeedModifiers();


            Player.local?.handLeft?.link?.RefreshJointConfig();
            Player.local?.handLeft?.link?.RefreshJointModifiers();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
            Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
            Player.local?.handRight?.link?.RefreshJointConfig();
            Player.local?.handRight?.link?.RefreshJointModifiers();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
            Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
            Player.local.locomotion.colliderRadius = 0.3f * Player.local.transform.localScale.x;
            Player.local.locomotion.groundDetectionDistance = 0.05f * Player.local.transform.localScale.x;
            Player.local.airHelper.minHeight = 1 * Player.local.transform.localScale.x;
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();
            
            
            // INFO B.T.Abstract.PlayerTitanBase.UpdateCaster       : Right Hand: pos (0.03, -0.02, -0.09), rot (0.10280, 0.67510, 0.14981, 0.71500)
            
            // INFO B.T.Abstract.PlayerTitanBase.UpdateCaster       : Left Hand: pos (-0.03, -0.02, -0.09), rot (0.67510, 0.10280, -0.71500, -0.14982)

            
            if (!isTitan)
                return;

            var current = Player.local.head.transform.localRotation.eulerAngles.x;
            var delta = Mathf.DeltaAngle(lastHeadRotation, current);


            if (Player.local.handRight.controlHand.alternateUsePressed &&
                Player.local.handLeft.controlHand.alternateUsePressed)
                if (delta > 3f)
                {
                    OnUnshift();
                    isTransforming = true;
                }


            lastHeadRotation = Player.local.head.transform.localRotation.eulerAngles.x;
        }


        protected virtual void OnUnshift()
        {
            if (!isTitan || isTransforming)
                return;

            isTransforming = true;

            var vrik = titan.GetComponent<VRIK>();
            if (vrik != null)
                vrik.enabled = false;

            var titanPosition = titan.transform.position;
            var titanRotation = titan.transform.rotation;
            var titanScale = titan.transform.lossyScale;

            titan.transform.SetParent(null, true);

            titan.transform.position = titanPosition;
            titan.transform.rotation = titanRotation;
            titan.transform.localScale = titanScale;

            UnScale();
            Player.currentCreature.HideItemsInHolders(false);

            Object.Destroy(Player.local.head.transform.Find("j")?.gameObject);
            Object.Destroy(Player.local.handRight.transform.Find("j2")?.gameObject);
            Object.Destroy(Player.local.handLeft.transform.Find("j3")?.gameObject);

            Object.Destroy(leftTitanHand);
            Object.Destroy(rightTitanHand);

            Player.currentCreature.handLeft.caster.AllowSpellWheel("JohnSmithHandLeft");
            Player.currentCreature.handRight.caster.AllowSpellWheel("JohnSmithHandRight");

            Player.local.locomotion.allowMove = false;
            Player.local.locomotion.physicBody.useGravity = false;
            Player.local.locomotion.physicBody.velocity = Vector3.zero;


            Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = true);
            Player.local.StartCoroutine(ExitTitan());
        }


        private IEnumerator ExitTitan()
        {
            ResetVelocity();

            if (Player.currentCreature != null)
            {
                Player.local.locomotion.physicBody.useGravity = true;
                Player.local.locomotion.physicBody.velocity = Vector3.zero;
                Player.local.locomotion.allowMove = true;

                ResetRagdollHandLinks();

                yield return new WaitForFixedUpdate();

                RagdollTitan();
                Player.currentCreature.ragdoll.SetColliders(true);
                var smoke = titan.transform.FindChildRecursive(UnshiftSmokeFXTransform);
                var flames = titan.transform.FindChildRecursive(UnshiftEmbersFXTransform);
                smoke.gameObject.SetActive(true);
                flames.gameObject.SetActive(true);
                yield return new WaitForSeconds(20f);
                smoke.SetParent(null, true);
                flames.SetParent(null, true);

                var smokePs = smoke.GetComponent<ParticleSystem>();
                smokePs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Object.Destroy(smoke.gameObject, smokePs.main.startLifetime.constantMax);

                var flamesPs = flames.GetComponent<ParticleSystem>();
                flamesPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Object.Destroy(flames.gameObject, flamesPs.main.startLifetime.constantMax);
                Object.Destroy(titan);
            }

            isTitan = false;
            isTransforming = false;
            titan = null;
        }

        private void DisableRagdoll()
        {
            foreach (var r in titan.GetComponentsInChildren<Rigidbody>(true))
            {
                r.isKinematic = true;
                r.useGravity = false;
                if (r.gameObject.TryGetComponent<Collider>(out var c))
                {
                    if(!c.isTrigger)
                        c.enabled = false;
                }
            }
        }

        private void ResetVelocity()
        {
            foreach (var r in titan.GetComponentsInChildren<Rigidbody>(true))
            {
                r.velocity = Vector3.zero;
                r.angularVelocity = Vector3.zero;
            }
        }

        private void RagdollTitan()
        {
            var bodies = titan.GetComponentsInChildren<Rigidbody>(true);

            Physics.SyncTransforms();
            AlignRagdollJoints();

            foreach (var rb in bodies)
            {
                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.constraints = RigidbodyConstraints.None;
            }

            Physics.SyncTransforms();

            foreach (var c in titan.GetComponentsInChildren<Collider>(true)) c.enabled = true;


            foreach (var rb in bodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            foreach (var rb in bodies)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void AlignRagdollJoints()
        {
            foreach (var joint in titan.GetComponentsInChildren<CharacterJoint>(true))
            {
                if (joint.connectedBody == null)
                    continue;

                var worldAnchor =
                    joint.transform.TransformPoint(joint.anchor);

                joint.autoConfigureConnectedAnchor = false;

                joint.connectedAnchor =
                    joint.connectedBody.transform.InverseTransformPoint(worldAnchor);
            }

            Physics.SyncTransforms();
        }

        private void ResetRagdollHandLinks()
        {
            Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(async void (item) =>
            {
                if (Player.currentCreature.handRight.grabbedHandle)
                    Player.currentCreature.handRight.UnGrab(false);
                Player.currentCreature.handRight.Grab(item.mainHandleRight);
                await Task.Delay((int)(Time.deltaTime * 1000));
                Player.currentCreature.handRight.UnGrab(false);
                item.Despawn();
            });


            Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(async void (item) =>
            {
                if (Player.currentCreature.handLeft.grabbedHandle)
                    Player.currentCreature.handLeft.UnGrab(false);
                Player.currentCreature.handLeft.Grab(item.mainHandleLeft);
                await Task.Delay((int)(Time.deltaTime * 1000));
                Player.currentCreature.handLeft.UnGrab(false);
                item.Despawn();
            });
        }

        public void PlaySound(string soundId, Vector3 position)
        {
            Catalog.LoadAssetAsync<AudioContainer>(soundId,
                sound => sound.GetRandomAudioClip().PlayClipAtPoint(position, 1f, AudioMixerName.Effect), soundId);
        }

        public sealed override void Unload()
        {
            base.Unload();
            OnUnshift();
            Player.currentCreature.handLeft.caster.AllowSpellWheel("JohnSmithHandLeft");
            Player.currentCreature.handRight.caster.AllowSpellWheel("JohnSmithHandRight");
            isTransformingIn = false;
            Player.selfCollision = false;
            if (Player.currentCreature != null)
            {
                Player.currentCreature.OnDamageEvent -= CurrentCreatureOnOnDamageEvent;
                Player.currentCreature.OnKillEvent -= CurrentCreatureOnOnKillEvent;
            }

            if (spellCaster != null)
                spellCaster.ragdollHand.playerHand.OnFistEvent -= PlayerHandOnOnFistEvent;
        }
    }
}