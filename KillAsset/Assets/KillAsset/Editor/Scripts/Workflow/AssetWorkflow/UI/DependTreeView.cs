﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace KA
{
    internal class DependTreeView : TreeViewWithTreeModel<AssetTreeElement>
    {
        public DependTreeView(TreeViewState state,  TreeModel<AssetTreeElement> model)
            : base(state, model)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (20 - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = 2;
        }

        protected override string OnGetShowName(AssetTreeElement t)
        {
            return t.Path;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            var item = (TreeViewItem<AssetTreeElement>)args.item;
            Event current = Event.current;
            if (args.rowRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                BuildGenerticMemu(item);
                current.Use();
            }
        }

        void BuildGenerticMemu(TreeViewItem<AssetTreeElement> item)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Path"), false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = item.data.Path;
            });
            menu.AddSeparator("");

            if (SelectionObjects.Count > 0)
            {
                if (SelectionObjects.Count == 1)
                    menu.AddItem(new GUIContent("Show In Explorer"), false, () => EditorUtility.RevealInFinder(SelectionObjects[0].Path));
                else
                    menu.AddDisabledItem(new GUIContent("Show In Explorer"));
            }

            menu.ShowAsContext();
        }


    }

}
