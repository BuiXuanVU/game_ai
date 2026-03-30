using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CardDataManager : MonoBehaviour
{
    public static CardDataManager Instance { get; private set; }

    public List<Sprite> facesClubs;
    public List<Sprite> facesDiamonds;
    public List<Sprite> facesHearts;
    public List<Sprite> facesSpades;
    public List<Sprite> backs;
    public List<Sprite> chips;
    public Sprite dealer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Reset()
    {
        facesClubs.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Club"));
        facesDiamonds.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Diamond"));
        facesHearts.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Heart"));
        facesSpades.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Spades"));
        backs.AddRange(Resources.LoadAll<Sprite>("Cards/Back"));
        chips.AddRange(Resources.LoadAll<Sprite>("Chips/ChipNumber"));
        dealer = Resources.Load<Sprite>("Chips/Dealer");
    }

    public Sprite GetChip(int index)
    {
        switch (index)
        {
            case 1: return chips[0];
            case 5: return chips[1];
            case 10: return chips[2];
            case 25: return chips[3];
            case 100: return chips[4];
            case 500: return chips[5];
            case 1000: return chips[6];
            case 5000: return chips[7];
        }
        return chips[0];
    }
}
