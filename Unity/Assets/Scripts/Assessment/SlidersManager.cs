using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SliderManager : MonoBehaviour
{
    [SerializeField] private Slider[] sliders;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetSliderValues(string panelName, Dictionary<string, string> results)
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            string key = $"{panelName}_{i+1}";
            if (results.ContainsKey(key))
            {
                results[key] = sliders[i].value.ToString();
            }
            else
                results.Add(key, sliders[i].value.ToString());
        }
    }
}
