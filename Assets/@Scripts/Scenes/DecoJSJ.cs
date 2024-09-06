using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
    public class DecoJSJ : MonoBehaviour
    {
        [SerializeField]
        private Define.StageName _stageName;

        public Define.StageName StageName 
        {
            get {  return _stageName; }
            set { _stageName = value; }
        }

        void Start()
        {
            CreateTile();
        }

        // Update is called once per frame
        void Update()
        {

        }


        void CreateTile()
        {

        }
    }

}