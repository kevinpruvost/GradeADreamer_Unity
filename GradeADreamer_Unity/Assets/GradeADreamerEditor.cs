using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(GradeADreamer))]
public class GradeADreamerEditor : Editor
{
    private Vector2 scrollPos;
    private ReorderableList directoryList;
    private bool showCommandSection = false;

    private void OnEnable()
    {
        GradeADreamer sshManager = (GradeADreamer)target;

        directoryList = new ReorderableList(sshManager.directories, typeof(string), false, true, false, false)
        {
            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Directories");
            },

            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < sshManager.directories.Count)
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), sshManager.directories[index]);
                }
            },

            onSelectCallback = (ReorderableList list) =>
            {
                sshManager.selectedDirectory = sshManager.directories[list.index];
                Debug.Log($"Selected Directory: {sshManager.selectedDirectory}");
            },

            elementHeight = EditorGUIUtility.singleLineHeight + 4
        };
    }

    public override void OnInspectorGUI()
    {
        GradeADreamer sshManager = (GradeADreamer)target;

        if (sshManager.isConnected)
        {
            GUILayout.Label("Connected to SSH Server", EditorStyles.boldLabel);

            showCommandSection = EditorGUILayout.Foldout(showCommandSection, "Command Section");
            if (showCommandSection)
            {
                EditorGUI.indentLevel++;

                sshManager.command = EditorGUILayout.TextField("Command", sshManager.command);

                if (GUILayout.Button("Execute Command"))
                {
                    sshManager.ExecuteCommand(sshManager.command);
                }

                GUILayout.Label("Command Result:");
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
                EditorGUILayout.TextArea(sshManager.commandResult, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Refresh Models List"))
            {
                sshManager.ListDirectories();
                directoryList.list = sshManager.directories;
            }

            GUILayout.Label("Models:");
            float listHeight = Mathf.Min(sshManager.directories.Count, 6) * (EditorGUIUtility.singleLineHeight + 4) + EditorGUIUtility.singleLineHeight;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(listHeight));
            directoryList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Download Selected Model"))
            {
                string selectedModel = sshManager.selectedDirectory;
                sshManager.DownloadModel(selectedModel);
            }

            if (GUILayout.Button("Disconnect from SSH Server"))
            {
                sshManager.DisconnectFromSshServer();
            }
        }
        else
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Connect to SSH Server"))
            {
                sshManager.ConnectToSshServer();
            }
            GUILayout.Label("Not connected to SSH Server", EditorStyles.boldLabel);
        }
    }
}
