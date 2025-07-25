using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TextMeshPro))]
public class TteokBehaviour : IngredientBehaviour
{
    public float fontSize = 3f;
    public float colliderRadius = 0.2f;
    protected override void Start()
    {
        base.Start(); 
        var text = GetComponent<TextMeshPro>();
        text.text = "떡";
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        float randomAngle = Random.Range(-30f, 30f); // -30도 ~ +30도 사이
        transform.rotation = Quaternion.Euler(0, 0, randomAngle);

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.mass = 4f;
        rb.angularDamping = 3f;

        var col = GetComponent<CircleCollider2D>();
        col.radius = colliderRadius;
        col.sharedMaterial = PhysicsMaterial2DWithBounciness();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wok"))
        {
            Debug.Log("[떡] 웍에 들어감!");
        }
    }

    private PhysicsMaterial2D PhysicsMaterial2DWithBounciness()
    {
        var mat = new PhysicsMaterial2D();
        mat.bounciness = 0.2f;
        mat.friction = 0.6f;
        return mat;
    }
}
