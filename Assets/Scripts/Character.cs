using UnityEngine;

public class Character : MonoBehaviour
{
    //Character looks and aims
    //If it aims at an item, it has an outline
    //It can grab that item to eat it

    //The player has a meter
    //The meter grows the more you eat
    //And shrinks if you dont

    //The more the character eats
    //The bigger it gets
    //The less it eats the smaller it gets

    public void Start()
    {
        
    }

    public void ChangeSize(float growthRate)
    {
        transform.localScale = new Vector3(transform.localScale.x + growthRate, transform.localScale.y + growthRate, transform.localScale.y + growthRate);
    }
}
