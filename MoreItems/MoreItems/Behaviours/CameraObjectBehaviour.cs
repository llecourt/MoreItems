using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class CameraObjectBehaviour : GrabbableObject
    {
        UnityEngine.Quaternion restingRotation;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
        }
    }
}
