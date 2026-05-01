using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class ArrowUI : MonoBehaviour
{
    
    public int segmentCount = 20;

    public RectTransform rectTransform;
    private float arrowLength;
    Vector2 endPoint;
    Vector2 startPoint;
    public float startWidth=0.25f;
    public float endWidth=0.5f;
    private Vector2 midPoint;

    void Start()
    {
        rectTransform = transform.GetComponent<RectTransform>();
    }
   
    //利用unity组件绘制曲线，改进需贝塞尔
    public void Begin(Vector3 pos)
    {
        startPoint = new Vector2(pos.x,pos.y);
        endPoint = new Vector2(pos.x,pos.y);
    }

    public void UpdateEnd(Vector3 pos)
    {
        endPoint = new Vector2(pos.x,pos.y);
        midPoint = new Vector2((endPoint.x+startPoint.x)/2,(endPoint.y+startPoint.y)/2);
        float offsetX = endPoint.x - startPoint.x;
        float offsetY = endPoint.y - startPoint.y;
        arrowLength = Mathf.Sqrt(offsetX*offsetX+offsetY*offsetY);
        //角度
        float theta = Mathf.Atan2(offsetY,offsetX);
        rectTransform.localPosition = midPoint;
        rectTransform.sizeDelta = new(arrowLength,rectTransform.sizeDelta.y);
        rectTransform.localEulerAngles = new Vector3(0,0,theta*180/Mathf.PI);
    }

    public void End()
    {
        Destroy(gameObject);
    }
}