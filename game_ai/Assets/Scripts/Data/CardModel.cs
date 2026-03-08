using System.Collections.Generic;
using UnityEngine;

public class CardModel : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public List<Sprite> faces;
    public Sprite back;
    public int index;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        faces.AddRange(Resources.LoadAll<Sprite>("Cards/Face"));
        back = Resources.Load<Sprite>("Cards/Back/back01");
    }

    public void ToggleFace(bool isShowFace)
    {
        if (isShowFace)
        {
            Debug.Log(index);
            spriteRenderer.sprite = faces[index];
        }
        else
        {
            spriteRenderer.sprite = back;
        }    
    }
}
