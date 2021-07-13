using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace MSFD
{
    /// <summary>
    /// It is nescessary for manager which is initalizing self
    /// </summary>
    public interface IManager
    {
        void ManagerInitialization(Action OnInitializedCallback);
    }
}