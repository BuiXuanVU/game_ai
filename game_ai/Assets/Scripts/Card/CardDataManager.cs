using System.Collections.Generic;
using UnityEngine;

public class CardDataManager : MonoBehaviour
{
    public List<Sprite> facesClubs;
    public List<Sprite> facesDiamonds;
    public List<Sprite> facesHearts;
    public List<Sprite> facesSpades;
    public List<Sprite> backs;
    public CardModel prefabs;

    public void Reset()
    {
        facesClubs.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Club"));
        facesDiamonds.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Diamond"));
        facesHearts.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Heart"));
        facesSpades.AddRange(Resources.LoadAll<Sprite>("Cards/Face/Spades"));
        backs.AddRange(Resources.LoadAll<Sprite>("Cards/Back"));
        prefabs = Resources.Load<CardModel>("Prefabs/CardModel");
    }
}
