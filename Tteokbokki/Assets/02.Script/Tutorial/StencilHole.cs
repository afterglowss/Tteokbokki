using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class StencilHole : Image
{
    public override Material materialForRendering
    {
        get
        {
            Material result = new Material(base.materialForRendering);
            // "나는 화면에 1번이라는 도장을 찍겠다"
            result.SetInt("_StencilComp", (int)CompareFunction.Always);
            result.SetInt("_Stencil", 1);
            result.SetInt("_StencilOp", (int)StencilOp.Replace);
            return result;
        }
    }
}