using System.Collections.Generic;

public partial class TargetManager : Singleton<TargetManager>
{
    public List<Target> Targets { get; private set; } = new List<Target>();

    public void RegisterTarget(Target newTarget)
    {
        if (Targets.Contains(newTarget))
            return;
        
        Targets.Add(newTarget);
    }

    public void UnregisterTarget(Target targetToRemove)
    {
        if (!Targets.Contains(targetToRemove))
            return;

        Targets.Remove(targetToRemove);
    }
}