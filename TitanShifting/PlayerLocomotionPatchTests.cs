using BladeAndTitan.TitanShifting.Abstract;

namespace BladeAndTitan.TitanShifting;

using System;
using System.Reflection;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

[HarmonyPatch]
public static class TitanLocomotionPatches
{
    /*
     * How far above the normal groundDetectionDistance an already-grounded
     * player is allowed to be before we actually consider them airborne.
     *
     * This is deliberately small. Increase it if you still get tiny
     * terrain-induced hops.
     */
    private const float GroundGraceDistance = 2f;

    private static readonly MethodInfo OnGroundMethod =
        AccessTools.Method(typeof(Locomotion), "OnGround");

    private static readonly MethodInfo OnFlyMethod =
        AccessTools.Method(typeof(Locomotion), "OnFly");


    // ============================================================
    // CROUCH
    // ============================================================

    [HarmonyPatch(typeof(Locomotion), "CrouchCheck")]
    [HarmonyPrefix]
    private static bool CrouchCheckPrefix(Locomotion __instance)
    {
        if (!PlayerTitanBase.isTitan)
            return true;

        // Only modify the player's locomotion.
        if (__instance != Player.local?.locomotion)
            return true;

        /*
         * B&S determines crouching from:
         *
         *     player.creature.GetAnimatorHeightRatio()
         *
         * When the player is massively scaled, small changes in the
         * tracked/animated height can make this cross the 0.8 threshold.
         *
         * We don't want that automatic check while transformed.
         */

        if (__instance.isCrouched)
        {
            __instance.isCrouched = false;

            // Reproduce OnCrouch(false) so anything listening for the
            // crouch event gets notified.
            InvokeOnCrouch(__instance, false);
        }

        // Skip B&S's CrouchCheck entirely while transformed.
        return false;
    }


    // ============================================================
    // GROUNDING
    // ============================================================

    [HarmonyPatch(typeof(Locomotion), nameof(Locomotion.UpdateGrounded))]
    [HarmonyPrefix]
    private static bool UpdateGroundedPrefix(
        Locomotion __instance,
        bool forceInvokeFlyGround)
    {
        if (!PlayerTitanBase.isTitan)
            return true;

        // Don't affect NPC/creature locomotion.
        if (__instance != Player.local?.locomotion)
            return true;

        var capsule = __instance.capsuleCollider;

        if (capsule == null)
            return true;

        /*
         * This reproduces B&S's SphereCast almost exactly.
         */

        Vector3 up = Vector3.up;

        float lossyScaleY = __instance.transform.lossyScale.y;

        Vector3 castOrigin =
            capsule.transform.TransformPoint(capsule.center);

        float castRadius =
            capsule.radius * 0.99f * lossyScaleY;

        bool hitGround = Physics.SphereCast(
            castOrigin,
            castRadius,
            -up,
            out RaycastHit groundHit,
            1000f,
            __instance.groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitGround)
        {
            /*
             * There is genuinely no ground beneath us.
             *
             * Do NOT apply the grace period here. This prevents us from
             * accidentally making real jumps/falls impossible.
             */
            if (forceInvokeFlyGround || __instance.isGrounded)
            {
                groundHit.distance = 1000f;
                groundHit.normal = up;

                InvokeOnFly(__instance, forceInvokeFlyGround);
            }

            return false;
        }

        __instance.groundHit = groundHit;

        __instance.groundAngle =
            Vector3.Angle(up, groundHit.normal);

        /*
         * This is the same distance calculation used by the original
         * Locomotion.UpdateGrounded().
         */
        float capsuleBottom =
            (capsule.height / 2f -
             capsule.radius * 0.99f) *
            lossyScaleY;

        float groundDistance =
            Mathf.Clamp(
                groundHit.distance - capsuleBottom,
                0f,
                float.PositiveInfinity
            );

        float normalThreshold =
            __instance.groundDetectionDistance;

        /*
         * NORMAL B&S CHECK
         */
        if (groundDistance <= normalThreshold)
        {
            InvokeOnGround(
                __instance,
                groundHit.point,
                __instance.velocity,
                capsule,
                forceInvokeFlyGround
            );

            return false;
        }

        /*
         * ------------------------------------------------------------
         * GROUND GRACE
         * ------------------------------------------------------------
         *
         * If we're already grounded, don't immediately become airborne
         * because the terrain is a few centimetres below the bottom of
         * the scaled capsule.
         *
         * This is hysteresis:
         *
         *     airborne -> grounded : normal threshold
         *     grounded -> airborne : larger threshold
         *
         * This prevents rapid:
         *
         *     grounded -> airborne -> grounded
         *
         * transitions on small terrain changes.
         */
        if (__instance.isGrounded &&
            groundDistance <= normalThreshold + GroundGraceDistance)
        {
            // Keep the player grounded.
            __instance.isGrounded = true;

            /*
             * Refresh these because B&S normally does this inside
             * OnGround().
             */
            InvokeOnGround(
                __instance,
                groundHit.point,
                __instance.velocity,
                capsule,
                silent: true
            );

            return false;
        }

        /*
         * We are genuinely far enough away from the ground to be
         * considered airborne.
         */
        InvokeOnFly(
            __instance,
            forceInvokeFlyGround
        );

        return false;
    }


    // ============================================================
    // PRIVATE B&S METHOD INVOCATION
    // ============================================================

    private static void InvokeOnGround(
        Locomotion locomotion,
        Vector3 groundPoint,
        Vector3 velocity,
        Collider groundCollider,
        bool silent)
    {
        if (OnGroundMethod == null)
            return;

        OnGroundMethod.Invoke(
            locomotion,
            new object[]
            {
                groundPoint,
                velocity,
                groundCollider,
                silent
            }
        );
    }


    private static void InvokeOnFly(
        Locomotion locomotion,
        bool silent)
    {
        if (OnFlyMethod == null)
            return;

        OnFlyMethod.Invoke(
            locomotion,
            new object[]
            {
                silent
            }
        );
    }


    private static void InvokeOnCrouch(
        Locomotion locomotion,
        bool crouching)
    {
        var method = AccessTools.Method(
            typeof(Locomotion),
            "OnCrouch"
        );

        if (method == null)
            return;

        method.Invoke(
            locomotion,
            new object[]
            {
                crouching
            }
        );
    }
}