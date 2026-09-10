using UnityEngine;

public class InspectableDetail : MonoBehaviour
{
    [Header("Detail Identity")]
    public string detailName = "Forehead Sweat";
    [TextArea(2, 5)]
    public string detailDescription = "Cold sweat beads on the forehead. Pulse is quickening; indicating rising physiological stress.";

    [Header("Stat Impact Values")]
    public float selfSanityBonus = 15f;    // Bonus to Player A's Sanity upon discovery
    public float targetHeatPenalty = 20f;  // Penalty to Target B's Heat upon discovery
}