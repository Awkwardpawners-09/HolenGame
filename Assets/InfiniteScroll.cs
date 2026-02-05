using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteScroll : MonoBehaviour
{
    [SerializeField] private RawImage imj;
    [SerializeField] private float x, y;

    private void Update()
    {
        imj.uvRect = new Rect(imj.uvRect.position + new Vector2(x, y) * Time.deltaTime, imj.uvRect.size);
    }
}