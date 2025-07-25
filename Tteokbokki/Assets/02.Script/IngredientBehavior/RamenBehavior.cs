using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(TextMeshPro))]
public class RamenBehaviour : IngredientBehaviour
{
    private TextMeshPro text;
    private float originalFontSize;
    private float targetFontSize;

    private BoxCollider2D boxCol;
    private float originalSize;
    private float targetSize;

    public float fontSize = 3f;
    //public float colliderRadius = 0.2f;

    protected override void Start()
    {
        base.Start();

        text = GetComponent<TextMeshPro>();
        text.text = "라면사리";
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.darkOrange;

        originalFontSize = text.fontSize;
        targetFontSize = originalFontSize * 0.7f;

        boxCol = GetComponent<BoxCollider2D>();
        originalSize = boxCol.size.y;
        targetSize = originalSize * 0.7f;

        // 회전 초기값 약간 주기 (라면사리니까 회전하며 떨어짐)
        float angle = Random.Range(-30f, 30f);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    //protected override void Update()
    //{
    //    base.Update();

    //    // 폰트 축소
    //    if (isSubmerging)
    //    {
    //        text.fontSize = Mathf.MoveTowards(text.fontSize, targetFontSize, Time.deltaTime * 5f);

    //        Vector2 s = boxCol.size;
    //        s.y = Mathf.MoveTowards(s.y, targetSize, Time.deltaTime * 0.5f);
    //        boxCol.size = s;
    //    }
    //}
}
