using UnityEngine;
using System.Collections;

public class StealableAnimationBehavior : MonoBehaviour
{
    Vector3 spritePosIni;
    Quaternion spriteRotIni;
    Vector3 spriteScaleIni;

    Vector3 stealablePosIni;

    int posIndex;
    Vector3[] idleTargets = { Vector3.down * .1f, Vector3.up * .1f};
    Coroutine idleAnim;

    Transform sprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite = this.GetComponentInChildren<SpriteRenderer>().transform;

        spritePosIni = sprite.position;
        spriteRotIni = sprite.rotation;
        spriteScaleIni = sprite.localScale;

        stealablePosIni = this.transform.position;

        posIndex = 0;
    }

    private void OnDisable()
    {
        idleAnim = null;
    }

    IEnumerator StealableIdle()
    {
        float animTime = 3f;
        float startTime = Time.time;
        float endTime = startTime + animTime;
        Vector3 startPos = sprite.position;
        Vector3 targetPos = spritePosIni + idleTargets[posIndex];

        while (Time.time <= endTime)
        {
            sprite.position = Vector3.SlerpUnclamped(startPos, targetPos, Mathf.SmoothStep(0, 1f, (Time.time - startTime) / animTime));
            yield return null;
        }

        posIndex = posIndex == 1 ? 0 : 1;
        idleAnim = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (idleAnim == null) { idleAnim = StartCoroutine(StealableIdle()); }
    }
}
