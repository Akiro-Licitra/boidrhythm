using System.Collections.Generic;
using UnityEngine;

public class Boid { public Transform t; public Vector3 pos, vel; public float born; }

// Classic boids (separation / alignment / cohesion + soft containment),
// simulated in flock-local space so the whole flock can orbit the player.
// The beat makes the flock "breathe": radial kick, cohesion boost, scale + colour pulse.
public class BoidFlock : MonoBehaviour
{
    public Vector3 Center;
    public float Bass;                       // 0..1 from audio spectrum
    public readonly List<Boid> Boids = new List<Boid>();

    public float Radius = 3.5f, NeighborRadius = 1.6f, SeparationRadius = 0.75f;
    public float SepW = 2.2f, AlignW = 1.2f, CohW = 1.0f;
    public float MinSpeed = 1f, MaxSpeed = 3f, BeatKick = 3.2f, BoidScale = 0.28f;

    float pulse;
    Material mat;
    // Deliberately dull, low-saturation slate so the flock never competes with the bright blue/pink notes.
    // The beat pulse only lifts it slightly (no more white flash).
    readonly Color baseCol = new Color(0.26f, 0.28f, 0.38f), beatCol = new Color(0.46f, 0.49f, 0.64f);

    public void Init(int n)
    {
        mat = MaterialUtil.Make(baseCol);
        for (int i = 0; i < n; i++) Spawn(true);
    }

    public Boid Spawn(bool instant)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var b = new Boid
        {
            t = go.transform,
            pos = Random.insideUnitSphere * Radius * (instant ? 0.9f : 0.3f),
            vel = Random.onUnitSphere * Random.Range(MinSpeed, MaxSpeed),
            born = instant ? -10f : Time.time
        };
        Boids.Add(b);
        return b;
    }

    public void Beat()
    {
        pulse = 1f;
        foreach (var b in Boids)
            b.vel += b.pos.normalized * BeatKick * Random.Range(0.6f, 1f);
    }

    // Remove a random boid from the flock and hand it to the caller (it "falls off").
    public Boid Detach()
    {
        if (Boids.Count == 0) return null;
        int i = Random.Range(0, Boids.Count);
        var b = Boids[i];
        Boids.RemoveAt(i);
        return b;
    }

    // The spheres are standalone GameObjects (not children of this one), so remove them
    // when the flock is destroyed - otherwise a restart leaves the old flock frozen in the scene.
    void OnDestroy()
    {
        foreach (var b in Boids) if (b.t != null) Destroy(b.t.gameObject);
        Boids.Clear();
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        pulse *= Mathf.Exp(-5f * dt);
        MaterialUtil.SetColor(mat, Color.Lerp(baseCol, beatCol, pulse));

        float nr2 = NeighborRadius * NeighborRadius, sr2 = SeparationRadius * SeparationRadius;
        float maxSpeed = MaxSpeed * (1f + Bass * 1.5f);
        float coh = CohW * (1f + pulse * 1.5f);

        int n = Boids.Count;
        for (int i = 0; i < n; i++)
        {
            var b = Boids[i];
            Vector3 sep = Vector3.zero, ali = Vector3.zero, cen = Vector3.zero;
            int cnt = 0;
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var o = Boids[j];
                Vector3 d = b.pos - o.pos;
                float sq = d.sqrMagnitude;
                if (sq < nr2)
                {
                    cnt++; ali += o.vel; cen += o.pos;
                    if (sq < sr2 && sq > 1e-4f) sep += d / sq;
                }
            }
            Vector3 acc = sep * SepW;
            if (cnt > 0)
            {
                acc += (ali / cnt - b.vel) * AlignW;
                acc += (cen / cnt - b.pos) * coh;
            }
            float dist = b.pos.magnitude;
            if (dist > Radius) acc -= b.pos / dist * (dist - Radius) * 6f;
            else acc -= b.pos * 0.15f * coh;                  // gentle pull to the middle

            b.vel += acc * dt;
            float sp = b.vel.magnitude;
            if (sp > maxSpeed) b.vel *= maxSpeed / sp;
            else if (sp < MinSpeed && sp > 1e-4f) b.vel *= MinSpeed / sp;
        }

        for (int i = 0; i < n; i++)
        {
            var b = Boids[i];
            b.pos += b.vel * dt;
            b.t.position = Center + b.pos;
            float grow = Mathf.Clamp01((Time.time - b.born) / 0.5f);
            b.t.localScale = Vector3.one * (BoidScale * grow * (1f + 0.3f * pulse));
        }
    }
}
