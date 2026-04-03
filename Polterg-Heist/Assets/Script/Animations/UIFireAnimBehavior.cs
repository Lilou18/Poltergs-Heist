using UnityEngine;
using System.Collections;

public class UIFireAnimBehavior : MonoBehaviour
{
    RectTransform fireTrans;
    Coroutine fireAnim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fireTrans = this.GetComponent<RectTransform>();
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
        //float targetFalloff = Random.Range(fallOffIni, fallOffIni + .3f);
        //float sizeDiff = Mathf.Abs(fire.shapeLightFalloffSize - targetFalloff) / animTime;

        yield return new WaitForSecondsRealtime(animTime);

        /*while (Time.time <= endTime)
        {
            //float value = Mathf.MoveTowards(fire.shapeLightFalloffSize, targetFalloff, sizeDiff * Time.deltaTime);
            //fire.shapeLightFalloffSize = value;
            //fire.intensity = value + (intensityIni - fallOffIni);
            yield return null;
        }*/

        //yield return new WaitForSecondsRealtime(animTime);
        if (fireTrans.rotation.eulerAngles == Vector3.zero) { fireTrans.Rotate(Vector3.up * 180f); }
        else { fireTrans.eulerAngles = Vector3.zero; }
        fireAnim = null;
    }
}
