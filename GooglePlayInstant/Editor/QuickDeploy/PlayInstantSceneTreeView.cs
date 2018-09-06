// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Class that encapsulates the TreeView representation of all scenes found in the current project.
    /// </summary>
    public class PlayInstantSceneTreeView : TreeView
    {
        private const int ToggleWidth = 18;
        private readonly List<TreeViewItem> _allItems = new List<TreeViewItem>();
        private int rowID = 0;

        public PlayInstantSceneTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            extraSpaceBeforeIconAndLabel = ToggleWidth;

            Reload();
        }

        /// <summary>
        /// Public inner class that extends the TreeViewItem representation of Scenes to include the enabled attribute.
        /// </summary>
        public class SceneItem : TreeViewItem
        {
            public bool Enabled;
            public bool OldEnabledValue;
            public string SceneBuildIndexString;
            
            public override bool Equals(object obj)
            {
                var that = obj as SceneItem;
                return that != null && this.displayName.Equals(that.displayName);
            }
        }

        public void AddOpenScenes()
        {
            var scenes = GetAllScenes();

            for (var i = 0; i < scenes.Length; i++)
            {
                var sceneItem = new SceneItem
                {
                    id = rowID++,
                    depth = 0,
                    displayName = scenes[i].path,
                    Enabled = true
                };

                if (!_allItems.Contains(sceneItem))
                {
                    _allItems.Add(sceneItem);
                }
            }

            EditSceneBuildIndexString();
            Reload();
        }

        private void EditSceneBuildIndexString()
        {
            var buildIndex = 0;
            foreach (var item in _allItems)
            {
                var sceneItem = (SceneItem) item;
                sceneItem.SceneBuildIndexString = sceneItem.Enabled ? "" + buildIndex++ : "";
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1,
                displayName = "Root"
            };

            SetupParentsAndChildrenFromDepths(root, _allItems);

            return root;
        }

        private static Scene[] GetAllScenes()
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (var i = 0; i < scenes.Length; i++)
            {
                scenes[i] = SceneManager.GetSceneAt(i);
            }

            return scenes;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var toggleRect = args.rowRect;
            toggleRect.x += GetContentIndent(args.item);
            toggleRect.width = ToggleWidth;

            var item = (SceneItem) args.item;

            item.OldEnabledValue = item.Enabled;

            item.Enabled = EditorGUI.Toggle(toggleRect, item.Enabled);

            if (item.OldEnabledValue != item.Enabled)
            {
                EditSceneBuildIndexString();
            }

            DefaultGUI.LabelRightAligned(args.rowRect, item.SceneBuildIndexString, args.selected, args.focused);

            base.RowGUI(args);

            var current = Event.current;

            if (args.rowRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove Selection"), false, RemoveScene, item);
                menu.ShowAsContext();
            }
        }

        private void RemoveScene(object item)
        {
            _allItems.Remove((SceneItem) item);
            EditSceneBuildIndexString();
            Reload();
        }

        //TODO: implement drag and drop
    }
}