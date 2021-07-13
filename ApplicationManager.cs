using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MSFD
{
    /// <summary>
    /// Отвечает за загрузку всех менеджеров и дает разрешение их работы, переключает сцены и хранит информацию об общем состоянии приложения. 
    /// Все менеджеры, за которых он отвечает - в детях
    /// </summary>
    public class ApplicationManager : SingletoneBase<ApplicationManager>
    {
        [SerializeField]
        SceneLoadingModeAfterInit sceneLoadingModeAfterInit = SceneLoadingModeAfterInit.dontLoadSceneAutomatically;

//#if UNITY_EDITOR
        bool __ShowSceneSelector => SceneLoadingModeAfterInit.loadSceneByName == sceneLoadingModeAfterInit;
        [NaughtyAttributes.ShowIf("__ShowSceneSelector")]
//#endif
        [NaughtyAttributes.Scene]
        [SerializeField]
        string sceneToLoadOnLoadingComplete = GameValues.mainMenuName;

        BroadcastedProperty<ApplicationState> applicationState = new BroadcastedProperty<ApplicationState>(SystemEvents.APPLICATION_STATE_IS_CHANGED);

        IManager[] managers;
        int initializedManagersCount = 0;
        BroadcastedProperty<float> currentInitializationProgress = new BroadcastedProperty<float>(SystemEvents.FLOAT_INIT_LOADING_PROGRESS);
        SceneLoadingMode sceneLoadingMode = SceneLoadingMode.waitForExternalEvent;
        AsyncOperation sceneLoading;
        bool isSceneLoadingNow = false;
        static float sceneLoadingCheckDelay = 0.2f;
        protected override void AwakeInitialization()
        {
            isSceneLoadingNow = false;

            Messenger.Broadcast(SystemEvents.AWAKE_LOADING, MessengerMode.DONT_REQUIRE_LISTENER);
            managers = GetComponentsInChildren<IManager>();
            currentInitializationProgress.Set(0);

            if (managers.Length == 0)
            {
                InitializationComplete();
            }
            else
            {
                foreach (IManager x in managers)
                {
                    x.ManagerInitialization(OnManagerInitialized);
                }
                Invoke("StartLoadingAhead", 0);
            }
        }
        void StartLoadingAhead()
        {
            if (sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneByName)
            {
                LoadScene(sceneToLoadOnLoadingComplete, SceneLoadingMode.waitForExternalEvent);
            }
            else if (sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneWithIndex1)
            {
                LoadScene(SceneUtility.GetScenePathByBuildIndex(1), SceneLoadingMode.waitForExternalEvent);
            }
        }
        void OnManagerInitialized()
        {
            initializedManagersCount++;
            currentInitializationProgress.Set((float)initializedManagersCount / managers.Length);
            if (initializedManagersCount == managers.Length)
            {
                InitializationComplete();
            }
        }
        void InitializationComplete()
        {
            currentInitializationProgress.Set(1);
            Messenger.Broadcast(SystemEvents.AWAKE_LOADING_COMPLETE, MessengerMode.DONT_REQUIRE_LISTENER);
            //applicationState.Set(ApplicationState.initializationComplete);
            //applicationState.Set(ApplicationState.normal);      
            if (!isSceneLoadingNow)
            {
                if (sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneByName)
                {
                    LoadScene(sceneToLoadOnLoadingComplete, SceneLoadingMode.rapidSwitch);
                    //LoadScene(sceneToLoadOnLoadingComplete, SceneLoadingMode.waitForExternalEvent);
                }
                else if (sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneWithIndex1)
                {
                    LoadScene(SceneManager.GetSceneAt(1).name, SceneLoadingMode.rapidSwitch);
                }
                // Else wait for an external activation
            }
            else if (sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneByName
                || sceneLoadingModeAfterInit == SceneLoadingModeAfterInit.loadSceneWithIndex1)
            {
                AllowSceneActivation();
            }
        }
        public void LoadScene(string sceneName, SceneLoadingMode _sceneLoadingMode = SceneLoadingMode.rapidSwitch)
        {
            if (isSceneLoadingNow)
            {
                Debug.LogError("Attempt to load several scenes");
                return;
            }
            Messenger.Broadcast(SystemEvents.SCENE_LOADING_STARTED, MessengerMode.DONT_REQUIRE_LISTENER);
            sceneLoadingMode = _sceneLoadingMode;
            isSceneLoadingNow = true;
            //applicationState.Set(ApplicationState.loadSceneProcess);

            /*#if UNITY_EDITOR
                        LoadSceneParameters loadSceneParameters =
                            new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D };
                        sceneLoading = EditorSceneManager.LoadSceneAsyncInPlayMode(sceneName, loadSceneParameters);
            #else*/
            sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            //#endif
            sceneLoading.allowSceneActivation = false;
            //sceneLoading.completed += (AsyncOperation) => { isSceneLoadingNow = false; };
            sceneLoading.completed += (AsyncOperation) => { OnLevelLoaded(); };
            StartCoroutine(SceneLoadingCheck());
            if (sceneLoadingMode == SceneLoadingMode.rapidSwitch)
            {
               ActivateScene();
            }
        }
        IEnumerator SceneLoadingCheck()
        {
            while (sceneLoading.progress < 0.9)
            {
                float sceneLoadingProgress = (sceneLoading.progress / 0.9f);
                Messenger<float>.Broadcast(SystemEvents.FLOAT_SCENE_LOADING, sceneLoadingProgress, MessengerMode.DONT_REQUIRE_LISTENER);
                yield return new WaitForSeconds(sceneLoadingCheckDelay);
            }
        }
        public void AllowSceneActivation()
        {
            if (sceneLoadingMode == SceneLoadingMode.waitForExternalEvent)
            {
                ActivateScene();
            }
            else
            {
                Debug.LogError("Attempt to allow scene activation in incorrect mode");
            }
        }

        void ActivateScene()
        {
            sceneLoading.allowSceneActivation = true;
        }

        public void RestartScene(SceneLoadingMode sceneLoadingMode)
        {
            LoadScene(SceneManager.GetActiveScene().name, sceneLoadingMode);
            Messenger.Broadcast(GameEvents.RESTART_GAME);
        }

        /*
        private void OnLevelWasLoaded(int level)
        {
            //applicationState.Set(ApplicationState.newSceneLoaded);
            //applicationState.Set(ApplicationState.normal);
        }*/
        void OnLevelLoaded()
        {
            isSceneLoadingNow = false;
            Messenger.Broadcast(SystemEvents.SCENE_LOADING_COMPLETED, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        private void OnApplicationQuit()
        {
            //applicationState.Set(ApplicationState.applicationQuit);
            //Save
        }
        private void OnApplicationPause(bool pause)
        {
            //applicationState.Set(ApplicationState.applicationFold);
        }
        private void OnApplicationFocus(bool focus)
        {
            //applicationState.Set(ApplicationState.normal);
        }
        public void CloseApllication()
        {
            Debug.Log("CloseApplication");
            Application.Quit(0);
        }
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
        public enum ApplicationState { initialization, initializationComplete, loadSceneProcess, newSceneLoaded, normal, applicationFold, applicationQuit };
        public enum SceneLoadingModeAfterInit { loadSceneByName, loadSceneWithIndex1, dontLoadSceneAutomatically };
        public enum SceneLoadingMode { rapidSwitch, waitForExternalEvent };
    }
}
