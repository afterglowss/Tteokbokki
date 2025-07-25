using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class IngredientBehaviour : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected bool isSubmerging = false;
    protected float sinkForce = -0.5f;
    protected float horizontalSwayAmplitude = 0.2f;
    protected float swaySpeed = 2f;
    private float swayTime;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isSubmerging) return;

        if (other.CompareTag("WaterSurface"))
        {
            OnTouchWater();
        }
    }

    protected virtual void OnTouchWater()
    {
        isSubmerging = true;

        // 중력 줄이고 물 안에서 유영
        rb.gravityScale = 0.1f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;

        // 흔들흔들 + 천천히 아래로 내려감은 Update에서 구현
    }

    protected virtual void Update()
    {
        if (isSubmerging)
        {
            swayTime += Time.deltaTime;

            // 수직 하강 + 좌우 흔들기
            float sway = Mathf.Sin(swayTime * swaySpeed) * horizontalSwayAmplitude;

            rb.linearVelocity = new Vector2(sway, sinkForce);
        }
    }
}
