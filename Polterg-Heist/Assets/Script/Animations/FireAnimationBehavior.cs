using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FireAnimationBehavior : MonoBehaviour
{
    SpriteRenderer sprite;
    Light2D fire; 
    Coroutine fireAnim;
    float intensityIni;
    float fallOffIni;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite = this.GetComponent<SpriteRenderer>();
        fire = this.GetComponent<Light2D>();
        intensityIni = fire.intensity;
        fallOffIni = fire.shapeLightFalloffSize;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        fireAnim = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (fireAnim == null) { fireAnim = StartCoroutine(FireAnim()); }
    }

    IEnumerator FireAnim()
    {
        float animTime = Random.Range(.25f, .5f);
        float endTime = Time.time + animTime;
        float targetFalloff = Random.Range(fallOffIni, fallOffIni + .3f);
        float sizeDiff = Mathf.Abs(fire.shapeLightFalloffSize - targetFalloff) / animTime;

        while (Time.time <= endTime)
        {
            float value = Mathf.MoveTowards(fire.shapeLightFalloffSize, targetFalloff, sizeDiff * Time.deltaTime);
            fire.shapeLightFalloffSize = value;
            fire.intensity = value + (intensityIni - fallOffIni);
            yield return null;
        }

        //yield return new WaitForSecondsRealtime(animTime);
        sprite.flipX = !sprite.flipX;
        fireAnim = null;
    }
}
