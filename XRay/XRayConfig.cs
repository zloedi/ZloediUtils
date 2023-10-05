using UnityEngine;

public class XRayConfig : MonoBehaviour
{
    public Material XRayPassObstruct;
    public Material XRayPassActorFriendly;
    public Material XRayPassActorHostile;
    
    void Update() {
        XRay.XRayPassObstruct      = XRayPassObstruct;
        XRay.XRayPassActorFriendly = XRayPassActorFriendly;
        XRay.XRayPassActorHostile  = XRayPassActorHostile;
    }
}
