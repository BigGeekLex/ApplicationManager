using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSFD
{
    public abstract class SingletoneBase<T> : MonoBehaviour where T : class
    {
        public static T Instance
        {
            get
            {
                if(_instance == null)
                {
                    Debug.LogError("There is no " + typeof(T) + " in current scene. Add " + typeof(T) + " to current scene to get correct access");
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        static T _instance;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                AwakeInitialization();
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
        /// <summary>
        /// Use it instead of Awake
        /// </summary>
        protected abstract void AwakeInitialization();
    }
}