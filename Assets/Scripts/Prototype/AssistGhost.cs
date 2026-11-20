using UnityEngine;

/// <summary>The partner appears for one assist attack (O hold), then fades out.</summary>
public class AssistGhost : MonoBehaviour
{
    AttackRunner runner;
    AttackData data;
    int facing;
    Vector2 velocity;
    SpriteRenderer sr;
    Color color;
    CombatController owner;
    float life;

    public static void Spawn(PartyController party, CharacterDef partner, int facing, Vector2 at)
    {
        if (partner.assist == null) return;
        var go = new GameObject("Assist " + partner.name);
        go.transform.position = at + new Vector2(-facing * 0.8f, 0f);
        var g = go.AddComponent<AssistGhost>();
        g.sr = go.AddComponent<SpriteRenderer>();
        g.sr.sprite = party.CurrentSprite;
        g.sr.sortingOrder = 5;
        g.sr.flipX = facing < 0;
        g.color = partner.tint;
        g.color.a = 0.8f;
        g.sr.color = g.color;
        g.facing = facing == 0 ? 1 : facing;
        g.data = partner.assist;
        g.owner = party.GetComponent<CombatController>();
        g.runner = new AttackRunner(Team.Player, go.transform);
        g.runner.OnHit += g.HandleHit;
        g.runner.OnActiveStart += () => g.velocity = new Vector2(g.data.selfVelocity.x * g.facing, g.data.selfVelocity.y);
        g.runner.Start(g.data);
        g.life = g.data.Total + 0.15f;
    }

    void HandleHit(IHittable target, HitInfo hit)
    {
        if (owner != null) owner.RegisterAssistHit(target, hit);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (runner.Phase != AttackPhase.Active) velocity = Vector2.MoveTowards(velocity, Vector2.zero, 120f * dt);
        transform.position += (Vector3)(velocity * dt);
        runner.Tick(dt, new AttackFrame { position = transform.position, angleDeg = 0f, facing = facing });
        life -= dt;
        if (sr != null)
        {
            Color c = color;
            c.a *= Mathf.Clamp01(life / 0.2f);
            sr.color = c;
        }
        if (life <= 0f) Destroy(gameObject);
    }
}
