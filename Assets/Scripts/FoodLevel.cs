using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodLevel : MonoBehaviour
{
    public TMP_Text myText;
    public HungerMeter hungerMeter;
    public int level;
    private GameObject player;


    void Awake()
    {
        // player = GameObject.FindGameObjectWithTag("Player");
        if (myText == null) myText = GetComponent<TMP_Text>();
        if (hungerMeter == null)
        {
            // hungerMeter = player.GetComponent<HungerMeter>();
            hungerMeter = FindAnyObjectByType<HungerMeter>();
            Debug.Log(hungerMeter.name);
        }
        else Debug.Log("No HungerMeter found");
    }

    // Update is called once per frame
    void Update()
    {
        if (hungerMeter != null)
        {
            level = hungerMeter.GetIntLevel();
            myText.text = "Food Level: " + level;   
        }
    }
}
