using System.Collections.Generic;
using JALib.Core;
using JALib.Core.Patch;

namespace BetterCalibration.DoubleFeaturePatch;

public abstract class DoubleFeaturePatch {
    protected List<Feature> Features = [];
    protected JAPatcher Patcher;
    public bool Patched;

    public void AddPatch(Feature feature) {
        if(Features.Count == 0) {
            OnEnable();
            Patch();
        }
        Features.Add(feature);
    }

    public void RemovePatch(Feature feature) {
        Features.Remove(feature);
        if(Features.Count == 0) {
            Unpatch();
            OnDisable();
        }
    }

    public virtual void OnEnable() {
    }

    public virtual void OnDisable() {
    }

    private void Patch() {
        if(Patched) return;
        if(Patcher == null) {
            Patcher = new JAPatcher(Main.Instance);
            Patcher.AddPatch(GetType());
        }
        Patcher.Patch();
        Patched = true;
    }

    private void Unpatch() {
        if(!Patched) return;
        Patcher.Unpatch();
        Patched = false;
    }
}