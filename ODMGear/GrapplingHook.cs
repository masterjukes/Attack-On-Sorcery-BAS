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
    public bool holdPosition;

    public Transform hookPoint;
    public Transform hookOrigin;

    public float radius = 0.003f;
    public int sides = 6;

    private float time;
    private bool active;
    private bool retracting;

    private Vector3 start;
    private Vector3 end;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    // Reusable buffers
    private Vector3[] points;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    public bool Grappling => active && !retracting;
    public bool Retracting => retracting;
    
    public static Material material; 


    public void Init()
    {
        effectOverTime = new AnimationCurve(
            new Keyframe(0.222f, 0.604f),
            new Keyframe(0.987f, -0.76f),
            new Keyframe(2.009f, 0.001f),
            new Keyframe(3.76f, -0.004f),
            new Keyframe(5.405945f, 0f)
        );

        curve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(1.5f, 1.5f),
            new Keyframe(2f, 1f)
        );

        curveEffectOverDistance = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3673519f, 1.047478f),
            new Keyframe(1.461602f, 0.6029292f),
            new Keyframe(3f, 0f)
        );

        curveSize = 20f;
        scrollSpeed = 5f;
        segments = 75;
        animSpeed = 1.5f;
        retractSpeed = 100f;
        holdPosition = false;

        sides = Mathf.Max(3, sides);
        segments = Mathf.Max(1, segments);

        mesh = new Mesh();
        mesh.name = "Grappling Rope";

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        gameObject.GetOrAddComponent<MeshFilter>().mesh = mesh;

        meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
        if (material != null)
        {
            meshRenderer.material = material;
        }
        else
        {
            Catalog.LoadAssetAsync<Material>("RopeODMMaterial", mat =>
            {
                material = mat;
                meshRenderer.material = material;
            }, "RopeODMMaterial");
        }

        AllocateBuffers();
        meshRenderer.enabled = false;
    }

    private void AllocateBuffers()
    {
        int pointCount = segments + 1;

        points = new Vector3[pointCount];

        vertices = new Vector3[pointCount * sides];
        uvs = new Vector2[vertices.Length];

        triangles = new int[(pointCount - 1) * sides * 6];

        // UVs don't change, so calculate them once.
        for (int i = 0; i < pointCount; i++)
        {
            float v = i / (float)(pointCount - 1);

            for (int j = 0; j < sides; j++)
            {
                int index = i * sides + j;

                uvs[index] = new Vector2(
                    j / (float)sides,
                    v
                );
            }
        }

        int triangleIndex = 0;

        for (int i = 0; i < pointCount - 1; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int current = i * sides + j;
                int next = i * sides + (j + 1) % sides;

                int nextRing = (i + 1) * sides + j;
                int nextRingNext = (i + 1) * sides + (j + 1) % sides;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = nextRing;
                triangles[triangleIndex++] = next;

                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = nextRing;
                triangles[triangleIndex++] = nextRingNext;
            }
        }
    }

    public void Grapple(Transform newStart, Transform newEnd)
    {
        hookOrigin = newStart;
        hookPoint = newEnd;

        active = true;
        retracting = false;
        time = 0f;

        start = newStart.position;
        end = newEnd.position;

        meshRenderer.enabled = true;
    }

    public void SetEnd(Vector3 value)
    {
        end = value;
    }

    public void UpdateStart(Vector3 value)
    {
        start = value;
    }

    public void UnGrapple()
    {
        if (!active)
            return;

        Collider collider = hookPoint.GetComponent<Collider>();

        if (collider != null)
            collider.enabled = false;

        HookColliderChecker checker =
            hookPoint.GetComponent<HookColliderChecker>();

        if (checker != null)
            checker.isHooked = false;

        retracting = true;
        holdPosition = false;
    }

    private void Update()
    {
        if (!active)
            return;

        if (hookOrigin != null)
            start = hookOrigin.position;

        if (!retracting && hookPoint != null)
            end = hookPoint.position;
    }

    private void LateUpdate()
    {
        if (!active)
            return;

        if (retracting)
        {
            end = Vector3.MoveTowards(
                end,
                start,
                retractSpeed * Time.deltaTime
            );

            if ((end - start).sqrMagnitude <= 0.0001f)
            {
                active = false;
                retracting = false;

                meshRenderer.enabled = false;

                return;
            }
        }

        ProcessBounce();
        BuildRopeMesh();
    }

    private void ProcessBounce()
    {
        time = Mathf.MoveTowards(
            time,
            1f,
            Mathf.Max(
                Mathf.Lerp(time, 1f, animSpeed * Time.deltaTime) - time,
                0.2f * Time.deltaTime
            )
        );

        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.0001f)
        {
            for (int i = 0; i <= segments; i++)
                points[i] = start;

            return;
        }

        Vector3 up = Quaternion.LookRotation(direction) * Vector3.up;

        points[0] = start;

        for (int i = 1; i <= segments; i++)
        {
            float distance = i / (float)segments;

            float curveTime =
                Mathf.Repeat(distance * curveSize, 1f);

            float scrollTime =
                Mathf.Repeat(
                    curveTime - scrollSpeed * time,
                    1f
                );

            float offset =
                Eval(effectOverTime, time) *
                Eval(curveEffectOverDistance, distance) *
                Eval(curve, scrollTime);

            points[i] =
                Vector3.Lerp(start, end, distance) +
                up * offset;
        }
    }

    private void BuildRopeMesh()
    {
        int pointCount = segments + 1;

        /*
         * Stable frame.
         *
         * Instead of calculating the ring orientation from
         * Vector3.up every time, we transport the previous
         * frame along the rope.
         */

        Vector3 previousForward =
            GetForward(0, pointCount);

        Vector3 previousUp;

        // Pick a starting up vector that isn't parallel to forward.
        if (Mathf.Abs(Vector3.Dot(previousForward, Vector3.up)) < 0.95f)
            previousUp = Vector3.up;
        else
            previousUp = Vector3.right;

        previousUp =
            Vector3.ProjectOnPlane(
                previousUp,
                previousForward
            ).normalized;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 forward = GetForward(i, pointCount);

            if (i > 0)
            {
                // Parallel transport previousUp onto the new plane.
                previousUp =
                    Vector3.ProjectOnPlane(
                        previousUp,
                        forward
                    );

                if (previousUp.sqrMagnitude < 0.0001f)
                {
                    previousUp =
                        Vector3.Cross(
                            forward,
                            Vector3.right
                        );

                    if (previousUp.sqrMagnitude < 0.0001f)
                    {
                        previousUp =
                            Vector3.Cross(
                                forward,
                                Vector3.up
                            );
                    }
                }

                previousUp.Normalize();
            }

            Vector3 right =
                Vector3.Cross(
                    forward,
                    previousUp
                ).normalized;

            previousUp =
                Vector3.Cross(
                    right,
                    forward
                ).normalized;

            for (int j = 0; j < sides; j++)
            {
                float angle =
                    j / (float)sides *
                    Mathf.PI * 2f;

                Vector3 offset =
                    (
                        right * Mathf.Cos(angle) +
                        previousUp * Mathf.Sin(angle)
                    ) * radius;

                int index = i * sides + j;

                vertices[index] =
                    transform.InverseTransformPoint(
                        points[i] + offset
                    );
            }
        }

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 GetForward(int index, int pointCount)
    {
        if (index == 0)
        {
            Vector3 direction =
                points[1] - points[0];

            return direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector3.forward;
        }

        if (index == pointCount - 1)
        {
            Vector3 direction =
                points[index] - points[index - 1];

            return direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector3.forward;
        }

        Vector3 forward =
            points[index + 1] - points[index - 1];

        return forward.sqrMagnitude > 0.000001f
            ? forward.normalized
            : Vector3.forward;
    }

    private static float Eval(
        AnimationCurve animationCurve,
        float value)
    {
        Keyframe[] keys = animationCurve.keys;

        if (keys.Length == 0)
            return 0f;

        float maxTime = keys.Max(key => key.time);

        return animationCurve.Evaluate(
            value * maxTime
        );
    }
}