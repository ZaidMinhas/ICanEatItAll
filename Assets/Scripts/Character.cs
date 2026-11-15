using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    //Character looks and aims
    //If it aims at an item, it has an outline
    //It can grab that item to eat it

    //The player has a meter
    //The meter grows the more you eat
    //And shrinks if you dont

    //The more the character eats
    //The bigger it gets
    //The less it eats the smaller it gets
    
    private HungerMeter hungerMeter;

    public void Start()
    {
        hungerMeter = GetComponent<HungerMeter>();
    }

    void Update()
    {
        ChangeSize(hungerMeter.Level);
    }

    public void ChangeSize(float level)
    {
        transform.localScale = Vector3.one * level;
    }

    public HungerMeter GetHungerMeter()
    {
        return hungerMeter;
    }
}
