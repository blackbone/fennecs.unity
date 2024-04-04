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
        [SerializeReference] private ISystem[] systems;
        [SerializeReference] private IPreUpdateSystem[] preUpdateSystemsOrder;
        [SerializeReference] private IUpdateSystem[] updateSystemsOrder;
        [SerializeReference] private IPostUpdateSystem[] postUpdateSystemsOrder;

        private World world;

        public World World => world;

        private void Awake()
        {
            world = new World();
            foreach (var system in systems)
                system.OnAttachToWorld(world);

            hideFlags |= HideFlags.NotEditable;
        }

        private void OnEnable()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (var i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(PreUpdate))
                    foreach (var preUpdateSystem in preUpdateSystemsOrder)
                        loop.subSystemList[i].updateDelegate += preUpdateSystem.PreUpdateExecute;
                
                if (loop.subSystemList[i].type == typeof(Update))
                    foreach (var updateSystem in updateSystemsOrder)
                        loop.subSystemList[i].updateDelegate += updateSystem.UpdateExecute;
                
                if (loop.subSystemList[i].type == typeof(PreLateUpdate))
                    foreach (var postUpdateSystem in postUpdateSystemsOrder)
                        loop.subSystemList[i].updateDelegate += postUpdateSystem.PostUpdateExecute;
            }
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void OnDisable()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (var i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(PreUpdate))
                    foreach (var preUpdateSystem in preUpdateSystemsOrder)
                        loop.subSystemList[i].updateDelegate -= preUpdateSystem.PreUpdateExecute;
                
                if (loop.subSystemList[i].type == typeof(Update))
                    foreach (var updateSystem in updateSystemsOrder)
                        loop.subSystemList[i].updateDelegate -= updateSystem.UpdateExecute;
                
                if (loop.subSystemList[i].type == typeof(PreLateUpdate))
                    foreach (var postUpdateSystem in postUpdateSystemsOrder)
                        loop.subSystemList[i].updateDelegate -= postUpdateSystem.PostUpdateExecute;
            }
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void OnDestroy()
        {
            foreach (var system in systems)
                system.OnDetachFromWorld(world);
            
            world.Dispose();
            world = null;
            
            hideFlags &= ~HideFlags.NotEditable;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            name = "[World Installer]";
            
            preUpdateSystemsOrder = preUpdateSystemsOrder.Where(s => systems.Contains(s)).Union(systems.OfType<IPreUpdateSystem>()).ToArray();
            updateSystemsOrder = updateSystemsOrder.Where(s => systems.Contains(s)).Union(systems.OfType<IUpdateSystem>()).ToArray();
            postUpdateSystemsOrder = postUpdateSystemsOrder.Where(s => systems.Contains(s)).Union(systems.OfType<IPostUpdateSystem>()).ToArray();
        }
#endif
        public T GetSystem<T>() where T : ISystem => systems.OfType<T>().FirstOrDefault();
    }
}