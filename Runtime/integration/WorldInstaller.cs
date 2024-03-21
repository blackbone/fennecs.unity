using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace fennecs.integration
{
    /// <summary>
    /// This script will automatically install world and will execute systems.
    /// </summary>
    public class WorldInstaller : MonoBehaviour
    {
        [SerializeField] private bool persistent;
        [SerializeReference] private ISystem[] preUpdateSystems;
        [SerializeReference] private ISystem[] updateSystems;
        [SerializeReference] private ISystem[] postUpdateSystems;

        private World world;

        public World World => world;

        private void Awake()
        {
            world = new World();
            foreach (var system in preUpdateSystems.Union(updateSystems).Union(postUpdateSystems))
                system.OnAttachToWorld(world);

            hideFlags |= HideFlags.NotEditable;
        }

        private void OnEnable()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (var i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(PreUpdate))
                    foreach (var preUpdateSystem in preUpdateSystems)
                        loop.subSystemList[i].updateDelegate += preUpdateSystem.Execute;
                
                if (loop.subSystemList[i].type == typeof(Update))
                    foreach (var updateSystem in updateSystems)
                        loop.subSystemList[i].updateDelegate += updateSystem.Execute;
                
                if (loop.subSystemList[i].type == typeof(PreLateUpdate))
                    foreach (var postUpdateSystem in postUpdateSystems)
                        loop.subSystemList[i].updateDelegate += postUpdateSystem.Execute;
            }
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void OnDisable()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (var i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(PreUpdate))
                    foreach (var preUpdateSystem in preUpdateSystems)
                        loop.subSystemList[i].updateDelegate -= preUpdateSystem.Execute;
                
                if (loop.subSystemList[i].type == typeof(Update))
                    foreach (var updateSystem in updateSystems)
                        loop.subSystemList[i].updateDelegate -= updateSystem.Execute;
                
                if (loop.subSystemList[i].type == typeof(PreLateUpdate))
                    foreach (var postUpdateSystem in postUpdateSystems)
                        loop.subSystemList[i].updateDelegate -= postUpdateSystem.Execute;
            }
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void OnDestroy()
        {
            foreach (var system in preUpdateSystems.Union(updateSystems).Union(postUpdateSystems))
                system.OnDetachFromWorld(world);
            
            world.Dispose();
            world = null;
            
            hideFlags &= ~HideFlags.NotEditable;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            name = "[World Installer]";
        }
#endif
        public T GetSystem<T>() where T : ISystem
        {
            return preUpdateSystems.OfType<T>().FirstOrDefault()
                   ?? updateSystems.OfType<T>().FirstOrDefault()
                   ?? postUpdateSystems.OfType<T>().FirstOrDefault();
        }
    }
}