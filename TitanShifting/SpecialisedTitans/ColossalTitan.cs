using System.Collections;
using BladeAndTitan.TitanShifting.Abstract;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class ColossalTitan : PlayerTitanBase
{
    public override string titanAddress => "Bert_ColossalTitanRig";
    public override float footDistance => 8f;
    public override float stepSpeed => 0.8f;
    
    public override float maxHealth => 1000f;
    public override float jumpForce => 0.2f;
    public override float speedMultiplier => 4f;
    public override float handWeight => 10f;
    
    public override string stepSoundId => "CollTitanStepAudio";

    static ParticleSystem smokeEffect;
    private static bool isCastingL;
    private static bool isCastingR;

    private static float lastCastL = float.MaxValue;
    private static float lastCastR = float.MaxValue;

    private static float lastSmokeTime = Time.time;

    protected void CTExplosion()
    {
        Debug.Log("Explosion Happened");
        PlaySound("CollTitanShiftExplosionAudio", Player.currentCreature.transform.position);
        titan.transform.FindChildRecursive("TitanTransformSpecialFX").gameObject.GetComponent<ParticleSystem>().Play();
        ApplyExplosionForce();

        //GameManager.local.StartCoroutine(ControlLight());
        
        foreach (var creature in Creature.InRadius(titan.transform.FindChildRecursiveTR("CreatureLocation").position, 60f))
        {
            creature.Inflict("Burning", "ckig", 320, 100f);
        }
    }

    IEnumerator ControlLight()
    {
        float intensityTarget = 2f;
        Light light = titan.transform.FindChildRecursive("TitanNukeRedAura").GetComponent<Light>();
        while (light.intensity < intensityTarget)
        {
            light.intensity = Mathf.Lerp(light.intensity, intensityTarget, Time.deltaTime * 2f);
            yield return Yielders.EndOfFrame;
        }
        
        yield return new WaitForSeconds(15f);
        while (light.intensity > 0)
        {
            light.intensity = Mathf.Lerp(light.intensity, 0, Time.deltaTime / 4f);
            yield return null;
        }
        light.intensity = 0;
        
    }
    
    public void ApplyExplosionForce()
    {
        Vector3 explosionPosition = titan.transform.position;
        float radius = 200f;
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, radius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                if(rb.GetComponentInParent<Player>() != null)
                    continue;

                if (rb.GetComponentInParent<Creature>() != null)
                {
                    rb.GetComponentInParent<Creature>().ragdoll.SetState(Ragdoll.State.Destabilized);
                    rb.GetComponentInParent<Creature>().AddExplosionForce(500f, explosionPosition, radius, 3, ForceMode.Impulse);

                }
                else
                {
                    rb.AddExplosionForce(500f, explosionPosition, radius, 3, ForceMode.Impulse);
                }
            }
        }
    } 

    protected override void OnTitanPossess()
    {
        base.OnTitanPossess();
        Debug.Log("Titan possess");
        smokeEffect = titan.transform.FindChildRecursive("TitanSmokeAbility").gameObject.GetComponent<ParticleSystem>();
        if(smokeEffect == null)
            Debug.LogError("Smoke effect not found");
        else
        {
            Debug.Log("Smoke effect found");
        }

        leftFoot.GetOrAddComponent<TitanFootCollider>();
        rightFoot.GetOrAddComponent<TitanFootCollider>();
        
    }

    protected override void OnLeftFootstep()
    {
        base.OnLeftFootstep();
        foreach (var creature in leftFoot.GetComponent<TitanFootCollider>().creatures)
        {
            creature?.Kill();
        }
    }
    
    protected override void OnRightFootstep()
    {
        base.OnRightFootstep();
        foreach (var creature in rightFoot.GetComponent<TitanFootCollider>().creatures)
        {
            creature.Kill();
        }
    }

    public override void Fire(bool active)
    {
        base.Fire(active);

        if (!isTransformingIn)
        {
            return;
        }

        Debug.Log("Fire is running.");
        
        if (active)
        {
            if (spellCaster.ragdollHand.side == Side.Left)
                lastCastL = Time.time;
            else
                lastCastR = Time.time;
        }
        else
        {
            if (spellCaster.ragdollHand.side == Side.Left)
                lastCastL = float.MaxValue;
            else
                lastCastR = float.MaxValue;
        }

    }

    public override void UpdateCaster()
    {
        base.UpdateCaster();
        
        
        if(!isTitan || titan == null)
            return;
        
        
        if (Time.time - lastCastL > 3f && Time.time - lastCastR > 3f)
        {
            CTExplosion();
            lastCastL = float.MaxValue;
            lastCastR = float.MaxValue;
        }
        
        
        if (spellCaster.isFiring)
        {
            if(spellCaster.ragdollHand.side == Side.Left)
                isCastingL = true;
            else
                isCastingR = true;
            
        }
        else
        {
           if(spellCaster.ragdollHand.side == Side.Left)
               isCastingL = false;
           else
               isCastingR = false;
        }

        if (isCastingL && isCastingR)
        {

            if (!smokeEffect.isPlaying)
                smokeEffect.Play();

            if (Time.time - lastSmokeTime > 0.5f)
            {
                lastSmokeTime = Time.time;
                foreach (var creature in Creature.InRadius(Player.currentCreature.transform.position, 50f))
                    if (creature != Player.currentCreature)
                        creature.Inflict("Burning", "evilSmokeTitan", 10, 10f);
            }
        }
        else
        {
            if (smokeEffect.isPlaying)
                smokeEffect.Stop();
        }

    }


    protected override void SetHands(GameObject o)
    {
        var handR = o.transform.FindChildRecursiveTR("hand.R").gameObject.AddComponent<TitanHand>();
        handR.side = Side.Right;
        handR.thumbParentName = "thumb.R";
        handR.indexParentName = "index.R";
        handR.middleParentName = "middle.R";
        handR.ringParentName = "ring.R";
        handR.pinkyParentName = "pinky.R";
        handR.Init();


        var handL = o.transform.FindChildRecursiveTR("hand.L").gameObject.AddComponent<TitanHand>();
        handL.side = Side.Left;
        handL.thumbParentName = "thumb.L";
        handL.indexParentName = "index.L";
        handL.middleParentName = "middle.L";
        handL.ringParentName = "ring.L";
        handL.pinkyParentName = "pinky.L";
        handL.Init();
        
    }
    
    
}