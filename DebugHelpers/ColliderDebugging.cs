using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeAndTitan.DebugHelpers;

/*
public class ColliderDebugDrawer : MonoBehaviour
{
    public int segments = 24;
    public float lineWidth = 0.01f;
    public Color color = Color.green;

    private readonly List<LineRenderer> lines = new();
    private int lineIndex;

    private Material sharedMat;

    void Awake()
    {
        sharedMat = new Material(Shader.Find("Sprites/Default"));
    }

    void LateUpdate()
    {
        if (!TitanAi.enableDebug)
        {
            DisableAll();
            return;
        }

        lineIndex = 0;
        var colliders = new List<Collider>();
        colliders = GetComponentsInChildren<Collider>().ToList();
        colliders.Add(GetComponent<Collider>());
        
        foreach (var col in colliders)
        {
            if (!col.enabled) continue;

            switch (col)
            {
                case SphereCollider s: DrawSphere(s); break;
                case CapsuleCollider c: DrawCapsule(c); break;
                case BoxCollider b: DrawBox(b); break;
            }
        }

        // disable unused lines
        for (int i = lineIndex; i < lines.Count; i++)
            lines[i].gameObject.SetActive(false);
    }

    void DisableAll()
    {
        foreach (var lr in lines)
            if (lr) lr.gameObject.SetActive(false);
    }

    LineRenderer GetLine(int count, bool loop = true)
    {
        LineRenderer lr;

        if (lineIndex < lines.Count)
        {
            lr = lines[lineIndex];
            lr.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("DebugLine");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform);

            lr = go.AddComponent<LineRenderer>();
            lr.material = sharedMat;
            lr.useWorldSpace = true;
            lr.widthMultiplier = lineWidth;
            lr.startColor = lr.endColor = color;

            lines.Add(lr);
        }

        lr.positionCount = count;
        lr.loop = loop;

        lineIndex++;
        return lr;
    }

    // ---------------- SPHERE ----------------
    void DrawSphere(SphereCollider col)
    {
        Vector3 center = col.transform.TransformPoint(col.center);
        float radius = col.radius * MaxScale(col.transform.lossyScale);

        DrawCircle(center, Vector3.right, Vector3.up, radius);
        DrawCircle(center, Vector3.right, Vector3.forward, radius);
        DrawCircle(center, Vector3.up, Vector3.forward, radius);
    }

    // ---------------- CAPSULE ----------------
    void DrawCapsule(CapsuleCollider col)
    {
        Transform t = col.transform;

        Vector3 center = t.TransformPoint(col.center);
        Vector3 scale = t.lossyScale;

        float radius = col.radius * MaxScale(scale);
        float height = Mathf.Max(col.height * Mathf.Abs(GetAxis(scale, col.direction)), radius * 2);
        float half = height / 2 - radius;

        Vector3 axis = GetDirection(col.direction, t);
        Vector3 top = center + axis * half;
        Vector3 bottom = center - axis * half;

        var perp1 = GetPerp1(axis);
        var perp2 = GetPerp2(axis);

        DrawCircle(top, perp1, perp2, radius);
        DrawCircle(bottom, perp1, perp2, radius);

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2;
            Vector3 dir = Mathf.Cos(angle) * perp1 + Mathf.Sin(angle) * perp2;

            var lr = GetLine(2, false);
            lr.SetPosition(0, top + dir * radius);
            lr.SetPosition(1, bottom + dir * radius);
        }
    }

    // ---------------- BOX ----------------
    void DrawBox(BoxCollider col)
    {
        Transform t = col.transform;

        Vector3 center = t.TransformPoint(col.center);
        Vector3 size = Vector3.Scale(col.size, t.lossyScale) * 0.5f;

        Vector3 right = t.right * size.x;
        Vector3 up = t.up * size.y;
        Vector3 forward = t.forward * size.z;

        Vector3[] c = new Vector3[8];

        c[0] = center + right + up + forward;
        c[1] = center + right + up - forward;
        c[2] = center + right - up + forward;
        c[3] = center + right - up - forward;
        c[4] = center - right + up + forward;
        c[5] = center - right + up - forward;
        c[6] = center - right - up + forward;
        c[7] = center - right - up - forward;

        int[,] e = {
            {0,1},{0,2},{1,3},{2,3},
            {4,5},{4,6},{5,7},{6,7},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int i = 0; i < 12; i++)
        {
            var lr = GetLine(2, false);
            lr.SetPosition(0, c[e[i, 0]]);
            lr.SetPosition(1, c[e[i, 1]]);
        }
    }

    void DrawCircle(Vector3 center, Vector3 a, Vector3 b, float r)
    {
        var lr = GetLine(segments);

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2;
            lr.SetPosition(i, center + (Mathf.Cos(angle) * a + Mathf.Sin(angle) * b) * r);
        }
    }

    float MaxScale(Vector3 s) => Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

    float GetAxis(Vector3 s, int dir) => dir == 0 ? s.x : dir == 1 ? s.y : s.z;

    Vector3 GetDirection(int dir, Transform t) => dir == 0 ? t.right : dir == 1 ? t.up : t.forward;

    Vector3 GetPerp1(Vector3 axis)
    {
        if (axis == Vector3.up || axis == Vector3.down) return Vector3.right;
        return Vector3.up;
    }

    Vector3 GetPerp2(Vector3 axis) => Vector3.Cross(axis, GetPerp1(axis)).normalized;
}
*/