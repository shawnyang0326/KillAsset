﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using HP = KA.Helper.WindowParam;

namespace KA
{
    [WorkflowOverride("Asset Cleaner" , 10000)]
    public class UselessWorkflow : AssetWorkflow
    {
        internal override GUIOptions GuiOptions => new GUIOptions()
        {
            onBottomGUICallback = OnBottomGUICallback,
            onTopGUICallback = OnTopGUICallback,
            onSelectionGUICallback = OnSelectionGUICallback,
        };

        public override void Run()
        {
            //attach check list
            List<string> checkList = null;
            string rootPath = FileUtil.GetProjectRelativePath(EditorConfig.Inst.RootPath);
            if(string.CompareOrdinal(rootPath, "Assets") != 0)
                checkList = Helper.Path.CollectAssetPaths(EditorConfig.Inst.RootPath);

            //create root
            var root = AssetTreeElement.CreateRoot();
            AssetSerializeInfo.Inst.AddDependenceItem(root, true);

            //collect Dependences.
            AssetSerializeInfo.Inst.CollectDependences(checkList);

            //refresh tree view
            var assetList = GetAssetList();
            RefreshTreeView(assetList);
        }

        public override void Clear()
        {
            if (uselessListCache != null)
            {
                uselessListCache.Clear();
                uselessListCache = null;
            }

            if (usedListCache != null)
            {
                usedListCache.Clear();
                usedListCache = null;
            }

            AssetSerializeInfo.Inst.Clear();
        }

        public override bool CanSearch(TreeElement t)
        {
            if (_toolbarSelected == (int)AssetShowMode.Summary)
            {
                if (!t.parent.IsRoot)
                    return false;
            }

            return true;
        }

        internal override void Export()
        {
            for (int i = 0; i < Enum.GetNames(typeof(AssetShowMode)).Length; i++)
            {
                var assetList = GetAssetList(i);
                AssetSerializeInfo.Inst.Export(assetList, ((AssetShowMode)i).ToString());
            }
        }

        private List<AssetTreeElement> GetAssetList(int targetSelected = -1)
        {
            int toolbarSelectIndex = _toolbarSelected;
            if (targetSelected > 0)
                toolbarSelectIndex = targetSelected;

            List<AssetTreeElement> elements = new List<AssetTreeElement>();
            if (toolbarSelectIndex == (int)AssetShowMode.Summary)
            {
                elements = AssetSerializeInfo.Inst.treeList;
            }
            else if (toolbarSelectIndex == (int)AssetShowMode.All)
            {
                AssetTreeHelper.ListToTree(AssetSerializeInfo.Inst.AllAssetPaths, elements, e => 
                {
                    if (!AssetSerializeInfo.Inst.guidToAsset.TryGetValue(e.Guid, out AssetTreeElement value))
                    {
                        AssetSerializeInfo.Inst.guidToAsset.Add(e.Guid, e);
                        AssetTreeHelper.CollectFileSize(e);
                    }
                });
            }
            else if (toolbarSelectIndex == (int)AssetShowMode.Used)
            {
                if(usedListCache == null)
                {
                    usedListCache = new List<string>();
                    foreach (var item in AssetSerializeInfo.Inst.guidToRef)
                    {
                        if (item.Value.Count <= 0)
                            continue;

                        if (AssetSerializeInfo.Inst.guidToAsset.TryGetValue(item.Key, out AssetTreeElement ele))
                        {
                            usedListCache.Add(ele.Path);
                        };
                    }
                }

                AssetTreeHelper.ListToTree(usedListCache, elements);
            }
            else
            {
                if (uselessListCache == null)
                {
                    uselessListCache = new List<string>();
                    foreach (var item in AssetSerializeInfo.Inst.guidToRef)
                    {
                        if (item.Value.Count > 0)
                            continue;

                        if (AssetSerializeInfo.Inst.guidToAsset.TryGetValue(item.Key, out AssetTreeElement ele))
                        {
                            uselessListCache.Add(ele.Path);
                        };
                    }
                }

                AssetTreeHelper.ListToTree(uselessListCache, elements);
            }

            return elements;
        }

        private void OnBottomGUICallback(ref Rect rect)
        {
            if (TreeView.HasSelection())
                rect.height -= HP.SelectionInfoHeight;
        }

        private void OnTopGUICallback(ref Rect rect)
        {
            var topRect = rect;
            topRect.x += 5;
            topRect.y = 5;
            topRect.width = topRect.height = 20;
            if (GUI.Button(topRect, GUIStyleMgr.Instance.TreeEditorRefresh))
            {
                Clear();
                Run();
                var assetList = GetAssetList();
                RefreshTreeView(assetList);
            }

            topRect.x += 25;
            topRect.width = 300;
            int selected = GUI.Toolbar(topRect, _toolbarSelected, Enum.GetNames(typeof(AssetShowMode)));
            if (_toolbarSelected != selected)
            {
                _toolbarSelected = selected;
                var assetList = GetAssetList();
                RefreshTreeView(assetList);
                TreeView.SetSelection(new List<int> { });
                if (_treeView != null)
                    _treeView.treeModel.Clear();
            }

            if (!GuiOptions.showExport)
                rect.y += 25;
            rect.height -= 25;

            if(_treeView != null && _treeView.treeModel.numberOfDataElements > 0)
            {
                rect.width /= 2;
            }
        }

        private void OnSelectionGUICallback(ref Rect rect, List<TreeElement> elements, bool lastChange)
        {
            var selectRect = new Rect(rect.x, rect.height + 60, rect.width / 2 + 100, 20);
            if (elements == null)
                return;

            if (elements.Count == 1)
            {
                var obj = elements[0] as AssetTreeElement;
                if (obj.Icon != null)
                {
                    selectRect.x += 140;
                    GUI.DrawTexture(new Rect(rect.x, rect.height + 50, 130, 130), obj.Icon);
                }
                GUISelectionLabel(ref selectRect, obj.name, EditorStyles.boldLabel);
                GUISelectionLabel(ref selectRect, string.Format("Location:  {0}", obj.Path));

                AssetSerializeInfo.Inst.guidToAsset.TryGetValue(obj.Guid, out AssetTreeElement ele);
                GUISelectionLabel(ref selectRect, string.Format("Size:  {0}", (ele == null ? "0" : Helper.Path.GetSize(ele.Size))));

                if(!string.IsNullOrEmpty(obj.Path))
                {
                    FileInfo info = new FileInfo(obj.Path);
                    GUISelectionLabel(ref selectRect, string.Format("Creat Time:  {0}", info.CreationTime));
                    GUISelectionLabel(ref selectRect, string.Format("Last WriteTime:  {0}", info.LastWriteTime));
                    GUISelectionLabel(ref selectRect, string.Format("Last AccessTime:  {0}", info.LastAccessTime));
                    GUISelectionLabel(ref selectRect, string.Format("IsReadOnly:  {0}", info.IsReadOnly));
                }
               
                AssetSerializeInfo.Inst.guidToRef.TryGetValue(obj.Guid, out List<string> refPaths);
                if (refPaths != null && refPaths.Count > 0)
                {
                    DrawTreeView(rect, refPaths, lastChange);
                }
                else
                {
                    if (_treeView != null)
                        _treeView.treeModel.Clear();
                }
            }
            else
            {
                string msg = string.Format("{0} Files.", elements.Count);
                GUISelectionLabel(ref selectRect, msg, EditorStyles.boldLabel);

                long totalSize = 0;
                for (int i = 0; i < elements.Count; i++)
                {
                    var e = elements[i] as AssetTreeElement;
                    AssetSerializeInfo.Inst.guidToAsset.TryGetValue(e.Guid, out AssetTreeElement ele);
                    totalSize += ele == null ? 0 : ele.Size;
                }

                GUISelectionLabel(ref selectRect, string.Format("Total Size:   {0}", Helper.Path.GetSize(totalSize)));

                if (_treeView != null)
                    _treeView.treeModel.Clear();
            }
        }

        private void GUISelectionLabel(ref Rect rect, string info, GUIStyle style = null)
        {
            if(style != null)
                GUI.Label(rect, info, style);
            else
                GUI.Label(rect, info);
            rect.y += 15;
        }

        private void DrawTreeView(Rect rect, List<string> refPath, bool lastChange)
        {
            if(lastChange)
            {
                _selectionList.Clear();
                if (_treeviewState == null)
                {
                    _treeviewState = new TreeViewState();
                    AssetTreeHelper.ListToTree(refPath, _selectionList);
                    var treeModel = new TreeModel<AssetTreeElement>(_selectionList);

                    if (_treeView == null)
                        _treeView = new DependTreeView(_treeviewState, treeModel);

                    _treeView.Reload();
                }
                else
                {
                    AssetTreeHelper.ListToTree(refPath, _selectionList);
                    _treeView.treeModel.SetData(_selectionList);
                    _treeView.Reload();
                }
            }

            rect.x = rect.width + 130;
            _treeView.OnGUI(rect);
        }
        private Vector2 scrollPane;
        private int _toolbarSelected = 0;
        private List<string> usedListCache = null;
        private List<string> uselessListCache = null;

        private List<AssetTreeElement> _selectionList = new List<AssetTreeElement>();
        private DependTreeView _treeView;
        private TreeViewState _treeviewState;
    }
}


