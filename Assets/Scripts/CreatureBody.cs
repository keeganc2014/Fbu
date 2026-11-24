using UnityEngine;

// The visible body of a monster: a packed model (Resources/Creatures/timmy.bytes or spider.bytes) standing on its hand tips.
// The models are single meshes (no bones), so the walk is done on the whole body: it bobs and sways with every step,
// leans forward while it hunts and rocks a little while it waits. Steps follow the distance it really walked
// (Monster calls AddDistance), so it never glides.
public class CreatureBody : MonoBehaviour
{
    public bool chasing;        // set by Monster
    public Light glow;          // light on its face (set by Monster)

    const float StrideLength = 1.5f;     // metres per full left+right step cycle

    Transform body;             // the model, origin at the floor
    float phase, speedSmooth, idleTime, lastDist;
    float height;

    public static CreatureBody Create(Transform parent, string resource, float wantedHeight)
    {
        PackedMesh.Loaded l = PackedMesh.Build(resource, "Creature", false, GtagBootstrap.PlayerLayer);
        if (l == null || l.header.height <= 0.01f) { if (l != null) Destroy(l.root); return null; }
        float scale = wantedHeight / l.header.height;
        l.root.transform.SetParent(parent, false);
        l.root.transform.localScale = Vector3.one * scale;
        var c = l.root.AddComponent<CreatureBody>();
        c.body = l.root.transform;
        c.height = wantedHeight;
        return c;
    }

    // total metres walked this frame
    public void AddDistance(float meters)
    {
        lastDist += meters;
        phase += meters / StrideLength * Mathf.PI * 2f;
    }

    void LateUpdate()
    {
        if (body == null) return;
        float dt = Time.deltaTime;
        float speed = dt > 0f ? lastDist / dt : 0f;
        lastDist = 0f;
        speedSmooth = Mathf.Lerp(speedSmooth, speed, 1f - Mathf.Exp(-8f * dt));
        float moving = Mathf.Clamp01(speedSmooth / 1.5f);
        idleTime += dt;

        float step = Mathf.Sin(phase);
        float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.07f * moving;
        float roll = step * 5f * moving + Mathf.Sin(idleTime * 0.9f) * 1.5f * (1f - moving);   // sways from hand to hand
        float pitch = (chasing ? 14f : 6f) * moving + Mathf.Sin(idleTime * 1.3f) * 1.2f * (1f - moving);

        body.localPosition = new Vector3(0f, bob, 0f);
        body.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }
}
