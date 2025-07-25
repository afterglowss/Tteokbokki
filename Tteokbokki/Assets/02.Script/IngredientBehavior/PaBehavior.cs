using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TextMeshPro))]
public class PaBehaviour : IngredientBehaviour
{
    public float fontSize = 2f;
    public float colliderRadius = 0.1f;
    protected override void Start()
    {
        base.Start();
        var text = GetComponent<TextMeshPro>();
        text.text = "파";
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.darkGreen;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.mass = 0.5f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 1f;

        var col = GetComponent<CircleCollider2D>();
        col.radius = colliderRadius;
        col.sharedMaterial = CreateMaterial();

        float randomAngle = Random.Range(-180f, 180f);
        transform.rotation = Quaternion.Euler(0, 0, randomAngle);
    }

    private PhysicsMaterial2D CreateMaterial()
    {
        var mat = new PhysicsMaterial2D();
        mat.friction = 0.4f;
        mat.bounciness = 0.1f;
        return mat;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wok"))
        {
            Debug.Log("파 조각이 웍에 들어감");
        }
    }
}
