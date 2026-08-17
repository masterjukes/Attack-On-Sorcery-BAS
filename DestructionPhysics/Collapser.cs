using UnityEngine;

namespace BladeAndTitan.DestructionPhysics
{
    public abstract class Collapser : MonoBehaviour
    {
        [SerializeField] protected float startingHp;
        protected float currentHp;

        public bool HasCollapsed => currentHp <= 0;
        public float HealthRatio => currentHp / startingHp;

        void Start()
        {
            currentHp = startingHp;
        }

        public virtual void ReceiveDamage(float damage)
        {
            currentHp -= damage;
            if (HasCollapsed && gameObject.activeInHierarchy)
                Collapse();
        }

        public virtual void Collapse(){}
        public virtual void ReceiveAllDamage() => ReceiveDamage(currentHp);
    }
}
