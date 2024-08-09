using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class FramedPicBehaviour : GrabbableObject
    {
        string[] possiblePhotos = new string[] { "Pigeon", "ManMug", "Prospector", "Boowomp", "Gambling", "Toto" };
        NetworkVariable<int> index = new NetworkVariable<int>(-1);
        bool materialSet = false;
        MeshRenderer picture;
        Transform listOfPossiblePhotos;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;

            picture = transform.Find("Photo").GetComponent<MeshRenderer>();
            listOfPossiblePhotos = transform.Find("PossiblePhotoMaterials");
        }

        public void FixedUpdate()
        {
            if (Utils.frameCount(10)) return;
            if (index.Value == -1 && Utils.isLocalPlayerHosting())
            {
                SetIndexServerRpc();
            }
            else if(!materialSet)
            {
                materialSet = true;
                picture.material = listOfPossiblePhotos.Find(possiblePhotos[index.Value]).GetComponent<MeshRenderer>().material;
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void SetIndexServerRpc()
        {
            index.Value = UnityEngine.Random.Range(0, possiblePhotos.Length - 1);
        }
    }
}
