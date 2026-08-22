using UnityEngine;

namespace BladeAndTitan.Titans.LookAnimator;

public class FPD_FoldableAttribute : PropertyAttribute
{
    public string FoldVariable;
    public FPD_FoldableAttribute(string boolFoldVariable)
    {
        FoldVariable = boolFoldVariable;
    }
}