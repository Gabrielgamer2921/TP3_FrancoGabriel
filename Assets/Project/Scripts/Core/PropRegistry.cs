using System.Collections.Generic;
using UnityEngine;

public interface IResettableProp : IResettable
{
    Vector3 HomePosition { get; }
}

public static class PropRegistry
{
    private static readonly List<IResettableProp> Props = new List<IResettableProp>();

    public static void Register(IResettableProp prop)
    {
        if (!Props.Contains(prop)) Props.Add(prop);
    }

    public static void Unregister(IResettableProp prop) => Props.Remove(prop);

    public static void ResetInside(Bounds area, float margin = 1f)
    {
        area.Expand(margin * 2f);
        foreach (IResettableProp prop in Props.ToArray())
            if (prop != null && area.Contains(prop.HomePosition)) prop.ResetState();
    }
}
