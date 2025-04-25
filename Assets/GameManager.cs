// THIS SCRIPT IS NOT MEANT TO BE USED IN THE FINAL GAME
// I CREATED IT TO SHOW YOU HOW TO BRIDGE THE MOD API WITH THE GAME OBJECTS SUCH AS PLAYERS, ENEMIES, CHAT, ETC
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public TMP_Text chatText;
    public Transform playerTransform;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
}