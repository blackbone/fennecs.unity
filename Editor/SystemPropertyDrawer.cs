using System;
using System.Collections.Generic;
using System.Linq;
using fennecs.integration;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomPropertyDrawer(typeof(ISystem), false)]
    public class SystemPropertyDrawer : PropertyDrawer
    {
        private static Dictionary<string, Type> systemTypes;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            CheckTypesCache();
            
            var toolbar = new Toolbar
            {
                style =
                {
                    position = Position.Absolute,
                    width = Length.Percent(100),
                    marginLeft = 4
                }
            };
            var toolbarMenu = new ToolbarMenu
            {
                style = { flexGrow = 1 }
            };
            
            InitializeMenu(toolbarMenu, GetValue, OnTypeSelected);
            toolbar.Add(toolbarMenu);

            var baseGui = new PropertyField(property, string.Empty);
            baseGui.BindProperty(property);
            var container = new VisualElement();
            container.Add(baseGui);
            container.Add(toolbar);

            return container;

            object GetValue() => property.managedReferenceValue;

            void OnTypeSelected(Type type)
            {
                property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
                property.serializedObject.ApplyModifiedProperties();
                InitializeMenu(toolbarMenu, GetValue, OnTypeSelected);
            }
        }

        private static void InitializeMenu(ToolbarMenu toolbarMenu, Func<object> currentValue, Action<Type> callback)
        {
            toolbarMenu.menu.ClearItems();

            toolbarMenu.text = GetName(currentValue());
            toolbarMenu.menu.AppendAction("(null)", ClickCallback, StatusCallback);
            
            foreach (var (menuPath, type) in systemTypes)
                toolbarMenu.menu.AppendAction(menuPath, ClickCallback, StatusCallback, type);
            
            return;

            void ClickCallback(DropdownMenuAction action) => callback(action.userData as Type);
            
            DropdownMenuAction.Status StatusCallback(DropdownMenuAction action)
            {
                var value = currentValue();
                return value == null
                    ? action.userData == null
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal
                    : value.GetType() == (Type)action.userData
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal;
            }
        }

        private static void CheckTypesCache()
        {
            if (systemTypes is { Count: > 0 }) return;

            systemTypes = TypeCache.GetTypesDerivedFrom<ISystem>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.FullName)
                .ToDictionary(t => t.FullName.Replace(".", "/"));
        }

        private static string GetName(object value) => value?.GetType().Name ?? "(null)";
    }
}