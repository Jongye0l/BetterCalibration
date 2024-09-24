using System.Collections.Generic;
using JALib.Core;
using JALib.Core.Patch;

namespace BetterCalibration.DoubleFeaturePatch;

public abstract class DoubleFeaturePatch {

    protected List<Feature> features = [];
    protected JAPatcher patcher;
    public bool patched;

    public void AddPatch(Feature feature) {
        if(features.Count == 0) {
            OnEnable();
            Patch();
        }
        features.Add(feature);
    }

    public void RemovePatch(Feature feature) {
        features.Remove(feature);
        if(features.Count == 0) {
            Unpatch();
            OnDisable();
        }
    }

    public virtual void OnEnable() {
    }

    public virtual void OnDisable() {
    }

    private void Patch() {
        if(patched) return;
        if(patcher == null) {
            patcher = new JAPatcher(Main.Instance);
            patcher.AddPatch(GetType());
        }
        patcher.Patch();
        patched = true;
    }

    private void Unpatch() {
        if(!patched) return;
        patcher.Unpatch();
        patched = false;
    }
}