using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class CameraScreenBehaviour : GrabbableObject
    {
        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
        }
    }
}
