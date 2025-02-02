﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KA
{
    public class EditorConfig : ScriptableObject
    {
        private static EditorConfig _instance;
        public static EditorConfig Inst
        {
            get
            {
                if (_instance == null)
                    _instance = LoadData();

                if (_instance == null)
                    Debug.LogErrorFormat("[KA]Missing Editor Config Data.");
                return _instance;
            }
        }

        private static EditorConfig LoadData()
        {
            string[] configData = AssetDatabase.FindAssets("EditorConfig t:" + typeof(KA.EditorConfig).ToString(), null);
            if (configData.Length >= 1)
            {
                return (EditorConfig)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(configData[0]), typeof(EditorConfig));
            }

            return null;
        }

        public enum ExportType
        {
            Json,
            Excel,
        }

        [SerializeField]
        internal string m_rootPath = "";

        public string RootPath
        {
            get
            {
                return Path.Combine(Application.dataPath, m_rootPath).NormalizePath();
            }
        }

        public string dataFileExtension = "kainfo";

        public List<string> ignoreExtension = new List<string> { ".cs", ".meta" };
        public List<string> ignoreDirectory;

        public ExportType exportType;

        public string OutputPath = "";
    }
}

