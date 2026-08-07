using System;
using System.Reflection;
using UnityEngine;

namespace BladeAndTitan.DebugHelpers;

public static class TransformExtensions
{
    public static Transform FindChildRecursive(this Transform parent, string name)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            var result = child.FindChildRecursive(name);
            if (result != null)
                return result;
        }

        return null;
    }
}
public static class ComponentExtensions
{
    public static T CopyComponent<T>(this T original, GameObject destination) where T : Component
    {
        // Add the component type to the new GameObject
        Type type = original.GetType();
        Component copy = destination.AddComponent(type);

        // Copy all public and private fields
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }

        return copy as T;
    }
    
    public static void CopyComponentValues(this Component source, Component destination)
    {
        // Make sure they are the same type
        if (source.GetType() != destination.GetType()) return;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;
        PropertyInfo[] pinfos = source.GetType().GetProperties(flags);

        foreach (var pinfo in pinfos) 
        {
            if (pinfo.CanWrite) 
            {
                try 
                {
                    pinfo.SetValue(destination, pinfo.GetValue(source, null), null);
                }
                catch { } // Skip read-only or incompatible properties
            }
        }
    }
}
