using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear;

public class GrapplingRope : MonoBehaviour
{
    public AnimationCurve effectOverTime;
    public AnimationCurve curve;
    public AnimationCurve curveEffectOverDistance;
    public float curveSize;
    public float scrollSpeed;
    public int segments;
    public float animSpeed;
    public float retractSpeed;
    public LineRenderer lineRenderer;
    public bool holdPosition;

    private Vector3 start;
    private Vector3 end;
    
    public Transform hookPoint;
    public Transform hookOrigin;
    
    private float time;
    private bool active;
    private bool retracting;

    public bool Grappling => active && !retracting;
    public bool Retracting => retracting;

    public void Init()
    {
        lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
        effectOverTime = new AnimationCurve(
            new Keyframe(0.222f, 0.604f), new Keyframe(0.987f, -0.76f),
            new Keyframe(2.009f, 0.001f), new Keyframe(3.76f, -0.004f),
            new Keyframe(5.405945f, 0f));
        curve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.5f, 0.5f),
            new Keyframe(1.5f, 1.5f), new Keyframe(2f, 1f));
        curveEffectOverDistance = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.3673519f, 1.047478f),
            new Keyframe(1.461602f, 0.6029292f), new Keyframe(3f, 0f));

        curveSize = 5f;
        scrollSpeed = 5f;
        segments = 200;
        animSpeed = 1.5f;
        retractSpeed = 200f;
        holdPosition = false;
        
        ConfigureRopeRenderer(lineRenderer);
    }

    public void Grapple(Transform newStart, Transform newEnd)
    {
        hookOrigin = newStart;
        hookPoint = newEnd;
        active = true;
        time = 0f;
        start = newStart.position;
        end = newEnd.position;
        retracting = false;
    }

    public void SetEnd(Vector3 value) => end = value;
    public void UpdateStart(Vector3 value) => start = value;

    public void UnGrapple()
    {
        if (!active) return;

        retracting = true;
        holdPosition = false;
        
    }


    private void Update()
    {
        if(!active)
            return;
        start = hookOrigin.position;
        if(!retracting)
            end = hookPoint.position;
    }

    public void LateUpdate()
    {
        if (lineRenderer == null) return;

        lineRenderer.enabled = active;
        if (!active) return;

        if (retracting)
        {
            end = Vector3.MoveTowards(end, start, retractSpeed * Time.deltaTime);
            if ((end - start).sqrMagnitude <= 0.0001f)
            {
                active = false;
                retracting = false;
                lineRenderer.enabled = false;
                return;
            }
        }

        ProcessBounce();
    }

    private void ProcessBounce()
    {
        time = Mathf.MoveTowards(time, 1f,
            Mathf.Max(Mathf.Lerp(time, 1f, animSpeed * Time.deltaTime) - time, 0.2f * Time.deltaTime));

        var positions = new List<Vector3> { start };
        var direction = end - start;
        if (direction.sqrMagnitude < 0.0001f)
        {
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, start);
            return;
        }

        var up = Quaternion.LookRotation(direction) * Vector3.up;
        for (var i = 1; i <= segments; i++)
        {
            var distance = (float)i / segments;
            var curveTime = Mathf.Repeat(distance * curveSize, 1f);
            var scrollTime = Mathf.Repeat(curveTime - scrollSpeed * time, 1f);
            var offset = Eval(effectOverTime, time) * Eval(curveEffectOverDistance, distance) * Eval(curve, scrollTime);
            positions.Add(Vector3.Lerp(start, end, distance) + up * offset);
        }

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    private static float Eval(AnimationCurve animationCurve, float value)
    {
        var maxTime = animationCurve.keys.Select(key => key.time).Max();
        return animationCurve.Evaluate(value * maxTime);
    }
    
    private static void ConfigureRopeRenderer(LineRenderer renderer)
    {
        renderer.useWorldSpace = true;
        renderer.startWidth = 0.01f;
        renderer.endWidth = 0.01f;
        renderer.startColor = Color.gray;
        renderer.endColor = Color.grey;
        renderer.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
    }
    
}

