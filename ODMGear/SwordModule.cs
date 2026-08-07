using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear
{
    public class SwordModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            // Attach behaviour to the item's GameObject so it works in-scene
            if (item != null && item.gameObject != null)
            {
                var behaviour = item.gameObject.GetComponent<SwordBehaviour>();
                if (behaviour == null) behaviour = item.gameObject.AddComponent<SwordBehaviour>();
                behaviour.item = item;
            }
        }
    }

    // A lightweight adaptation of the VR sword scripts for Blade & Sorcery (ThunderRoad)
    // Handles trigger visuals/sound and a simple "gas" resource used for boosts.
    public class SwordBehaviour : MonoBehaviour
    {
        [Header("References")]
        public Transform fireTrigger;
        public Transform boostTrigger;
        public AudioSource audioSource;
        public AudioClip triggerSound;
        public AudioClip gasReloadSound;

        // Optional Resources paths (place clips under Resources in your mod)
        public string triggerSoundResourcePath;
        public string gasReloadSoundResourcePath;

        [Header("Gas")]
        public float maxGas = 200f;
        public float consumePerSecond = 100f / 60f; // similar rate to original
        public bool restocking;
        public float restockDuration = 5f;

        [HideInInspector]
        public Item item;

        private float gas;
        private float reloadTimer;
        private Vector3 fireTriggerPos = new Vector3(0f, 0.175f, 0f);
        private Vector3 boostTriggerPos = new Vector3(0f, 0.2f, 0f);
        private bool gasActive;

        void Start()
        {
            // Auto-wire when you can't use the inspector
            if (fireTrigger == null) fireTrigger = FindChildByNameContains(transform, "fire");
            if (boostTrigger == null) boostTrigger = FindChildByNameContains(transform, "boost");

            if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            // Load AudioClips from Resources if not assigned
            if (triggerSound == null && !string.IsNullOrEmpty(triggerSoundResourcePath))
            {
                triggerSound = Resources.Load<AudioClip>(triggerSoundResourcePath);
            }
            if (gasReloadSound == null && !string.IsNullOrEmpty(gasReloadSoundResourcePath))
            {
                gasReloadSound = Resources.Load<AudioClip>(gasReloadSoundResourcePath);
            }

            gas = maxGas;
        }

        void Update()
        {
            // Require item and a handler to be held
            if (item == null) return;

            var handler = item.mainHandler;
            if (handler == null)
            {
                // Not held: reset triggers and active gas
                ResetTriggers();
                gasActive = false;
                return;
            }

            var hand = handler.playerHand;
            if (hand == null) return;

            // FIRE (primary use)
            if (hand.controlHand.usePressed)
            {
                if (fireTrigger != null) fireTrigger.localPosition = fireTriggerPos;
                if (triggerSound != null && audioSource != null) audioSource.PlayOneShot(triggerSound);
            }
            else
            {
                if (fireTrigger != null) fireTrigger.localPosition = Vector3.zero;
            }

            // BOOST (alternate use)
            if (hand.controlHand.alternateUsePressed)
            {
                if (boostTrigger != null) boostTrigger.localPosition = boostTriggerPos;
                if (triggerSound != null && audioSource != null) audioSource.PlayOneShot(triggerSound);

                if (!restocking && gas > 0f)
                {
                    gasActive = true;
                }
            }
            else
            {
                if (boostTrigger != null) boostTrigger.localPosition = Vector3.zero;
                gasActive = false;
            }

            // Consume gas when active
            if (gasActive && gas > 0f && !restocking)
            {
                gas -= Time.deltaTime * consumePerSecond;
                if (gas <= 0f)
                {
                    gas = 0f;
                    gasActive = false;
                }
            }

            // Restock handling
            if (!restocking) return;

            reloadTimer += Time.deltaTime;
            if (reloadTimer >= restockDuration)
            {
                FinishRestock();
            }
        }

        private Transform FindChildByNameContains(Transform parent, string token)
        {
            token = token.ToLowerInvariant();
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLowerInvariant().Contains(token)) return child;
            }
            return null;
        }

        private void ResetTriggers()
        {
            if (fireTrigger != null) fireTrigger.localPosition = Vector3.zero;
            if (boostTrigger != null) boostTrigger.localPosition = Vector3.zero;
        }

        public void StartRestock()
        {
            restocking = true;
            reloadTimer = 0f;
            // You can add visual feedback here (e.g., UI) by editing the Item prefab in editor
        }

        private void FinishRestock()
        {
            restocking = false;
            gas = maxGas;
            if (gasReloadSound != null && audioSource != null) audioSource.PlayOneShot(gasReloadSound);
        }

        // Expose some helper methods
        public float GetGasFraction()
        {
            return Mathf.Clamp01(gas / maxGas);
        }

        public bool IsOutOfGas()
        {
            return gas <= 0f;
        }

        private void OnDisable()
        {
            ResetTriggers();
        }
    }
}
