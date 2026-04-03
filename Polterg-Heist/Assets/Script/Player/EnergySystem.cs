using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class EnergySystem : MonoBehaviour
{
    [SerializeField] float energyRegen;
    private float stockRegen;
    public float energyValue;
    public float maxEnergy;
    public float minEnergy;

    Slider slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider = gameObject.GetComponent<Slider>();
        maxEnergy = slider.maxValue;
        minEnergy = slider.minValue;
        energyValue = slider.value;
    }

    void FixedUpdate()
    {
        ModifyEnergy(energyRegen);
    }

    public void StopResumeRegen(bool isRegenStopping)
    {
        if (isRegenStopping)
        {
            stockRegen = energyRegen;
            energyRegen = 0;
        }
        else
            energyRegen = stockRegen;
    }

    public float CurrentEnergy()
    {
        return energyValue;
    }

    public void ModifyEnergy(float addedValue)
    {
        float newValue = energyValue + addedValue;
        if (!(newValue > maxEnergy || newValue < minEnergy))
        {
            energyValue = newValue;
            slider.value = energyValue;
        }
        else
        {
            if (newValue > maxEnergy)
                energyValue = maxEnergy;
            else
                energyValue = minEnergy;
        }
    }
}
