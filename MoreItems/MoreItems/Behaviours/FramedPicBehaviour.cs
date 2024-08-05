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
        NetworkVariable<int> index = new NetworkVariable<int>(0);
        NetworkVariable<bool> indexSet = new NetworkVariable<bool>(false);
        MeshRenderer picture;
        Transform listOfPossiblePhotos;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;

            picture = transform.Find("Photo").GetComponent<MeshRenderer>();
            listOfPossiblePhotos = transform.Find("PossiblePhotoMaterials");

            if (GameNetworkManager.Instance.localPlayerController.IsHost || GameNetworkManager.Instance.localPlayerController.IsServer)
            {
                SetIndex();
            }
            StartCoroutine(SetTexture());
        }

        void SetIndex()
        {
            index.Value = UnityEngine.Random.Range(0, possiblePhotos.Length - 1);
            indexSet.Value = true;
        }

        IEnumerator SetTexture()
        {
            yield return new WaitUntil(() => indexSet.Value == true);
            picture.material = listOfPossiblePhotos.Find(possiblePhotos[index.Value]).GetComponent<MeshRenderer>().material;
        }
    }
}
