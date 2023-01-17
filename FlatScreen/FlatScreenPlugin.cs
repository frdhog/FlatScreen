using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace Triquetra.FlatScreen
{
    public class FlatScreenPlugin : VTOLMOD
    {
        private GameObject monoBehaviour;

        // This method is run once, when the Mod Loader is done initialising this game object
        public override void ModLoaded()
        {
            EnableFlatScreen();
            base.ModLoaded();
        }

        public void EnableFlatScreen()
        {
            if (monoBehaviour != null)
                return;
            Log("Creating FlatScreenMonoBehaviour");
            monoBehaviour = new GameObject();
            monoBehaviour.AddComponent<FlatScreenMonoBehaviour>();
            GameObject.DontDestroyOnLoad(monoBehaviour);
        }

        private void DisableFlatScreen()
        {
            if (monoBehaviour == null)
                return;
            Log("Destroying FlatScreenMonoBehaviour");
            GameObject.Destroy(monoBehaviour);
        }
    }
}