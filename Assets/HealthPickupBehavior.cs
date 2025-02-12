using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class HealthPickupBehavior : PickupCubeBehavior
{

    public int HealAmount = 1;
    
    public virtual void ScsOnSCDiedByPlayerCube(SceneCubeNetworking obj,ulong clientID)
    {
        // find the health pillar belonging to clientID of same color as destroyed cube
        var drawingGrid = FindObjectsOfType<DrawingGrid>().Where(s => s.OwnerClientId == clientID).ToArray();
        var hct = drawingGrid[0].AllHealthCubeTransforms.Where(s => s.OwningHealthCube.ColorID == obj.ColorID).ToArray();
        hct[0]._HealthPillar.RestoreHealth(HealAmount);
    }
    
    
    
    

}
