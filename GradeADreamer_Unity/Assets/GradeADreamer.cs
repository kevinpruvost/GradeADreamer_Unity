using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Renci.SshNet;
using System;
using UnityEditor;
using System.Text;
using System.Linq;
using UnityEngine.Windows;

public class GradeADreamer : MonoBehaviour
{
    public string configFile = "Assets/config_default.json";

    [HideInInspector]
    public string command = "pwd";

    private ShellStream shellStream;
    private SshClient sshClient;
    [HideInInspector]
    public string commandResult = "";
    [HideInInspector]
    public bool isConnected = false;

    [HideInInspector]
    public List<string> directories = new List<string>();
    [HideInInspector]
    public string selectedDirectory = string.Empty;

    public Camera camera;

    public class Config
    {
        public string host;
        public int port;
        public string username;
        public string password;
        public string pathToGradeADreamer;
    }

    [HideInInspector]
    public Config config = null;

    public void ConnectToSshServer()
    {
        // Open config file and read the JSON data
        string json = System.IO.File.ReadAllText(configFile);
        config = JsonUtility.FromJson<Config>(json);

        try
        {
            sshClient = new SshClient(config.host, config.port, config.username, config.password);

            sshClient.HostKeyReceived += (sender, e) =>
            {
                Debug.Log(e);
                // Add logic here to handle host key verification if needed
                // e.CanTrust = true; // Example: Trust the host key
            };

            sshClient.ErrorOccurred += (sender, e) =>
            {
                Debug.LogError($"SSH error: {e.Exception.Message}");
            };

            sshClient.Connect();

            if (sshClient.IsConnected)
            {
                Debug.Log("SSH connection established.");
                isConnected = true;

                shellStream = sshClient.CreateShellStream("customStream", 80, 24, 800, 600, 1024);
                ExecuteCommand("cd " + config.pathToGradeADreamer);
                ExecuteCommand("cd logs");
                ListDirectories();
            }
            else
            {
                Debug.LogError("SSH connection failed.");
                isConnected = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SSH connection error: {ex.Message}");
            isConnected = false;
        }
    }

    public void ExecuteCommand(string command)
    {
        if (shellStream != null && sshClient.IsConnected)
        {
            try
            {
                // Write the command to the shell stream
                shellStream.WriteLine(command);

                // Read the output from the shell stream
                string result = ReadShellStream(shellStream);
                commandResult = result;
                Debug.Log($"Command [{command}] result: {commandResult}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Command execution error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("SSH client is not connected.");
        }
    }

    private string ReadShellStream(ShellStream stream)
    {
        var output = new StringBuilder();
        string line;
        while ((line = stream.ReadLine(TimeSpan.FromSeconds(1))) != null)
        {
            output.AppendLine(line);
        }
        return output.ToString();
    }

    public void ListDirectories()
    {
        directories.Clear();
        ExecuteCommand("ls -1d */");

        // Parse the command result to extract directory names
        string[] lines = commandResult.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Start from index 1 to omit the first line
        for (int i = 1; i < lines.Length; i++)
        {
            directories.Add(lines[i].TrimEnd('/'));
        }
    }

    public void DisconnectFromSshServer()
    {
        if (sshClient != null && sshClient.IsConnected)
        {
            shellStream.Close();
            sshClient.Disconnect();
            sshClient.Dispose();
            sshClient = null;
            isConnected = false;
            Debug.Log("SSH connection closed.");
        }
    }

    private int downscaleFactor = 4;
    Color GetDominantColor(Material mat)
    {
        Texture2D texture = mat.mainTexture as Texture2D;
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        // Downscale the texture to reduce the number of pixels to process
        int downscaledWidth = width / downscaleFactor;
        int downscaledHeight = height / downscaleFactor;
        Color32[] downscaledPixels = new Color32[downscaledWidth * downscaledHeight];

        for (int y = 0; y < downscaledHeight; y++)
        {
            for (int x = 0; x < downscaledWidth; x++)
            {
                downscaledPixels[y * downscaledWidth + x] = pixels[(y * downscaleFactor) * width + (x * downscaleFactor)];
            }
        }

        // Count colors
        Dictionary<Color32, int> colorCount = new Dictionary<Color32, int>();
        foreach (Color32 pixel in downscaledPixels)
        {
            if (colorCount.ContainsKey(pixel))
            {
                colorCount[pixel]++;
            }
            else
            {
                colorCount[pixel] = 1;
            }
        }

        // Find the most frequent color
        Color32 dominantColor = new Color32();
        int maxCount = 0;
        foreach (var kvp in colorCount)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                dominantColor = kvp.Key;
            }
        }

        return dominantColor;
    }

    public static Color GetContrastingColor(Color color)
    {
        // Convert RGB to HSL
        float h, s, l;
        Color.RGBToHSV(color, out h, out s, out l);

        // Rotate hue by 180 degrees
        h = (h + 0.5f) % 1.0f;

        // Convert HSL back to RGB
        return Color.HSVToRGB(h, s, l);
    }

    public void SetNewBackground()
    {
        Material meshMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;

        if (meshMaterial != null)
        {
            Color dominantColor = GetDominantColor(meshMaterial);
            Color contrastingColor = GetContrastingColor(dominantColor);

            camera.backgroundColor = contrastingColor;
            Debug.Log($"Set skybox color to: {contrastingColor}");
        }
    }

    public void DownloadModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            Debug.LogError("Model name is empty.");
            return;
        }

        string localPath = Application.dataPath + $"/GeneratedModels/{modelName}/";

        // Check if the directory already exists
        if (Directory.Exists(localPath))
        {
#if UNITY_EDITOR
            bool replace = EditorUtility.DisplayDialog(
                "Directory Already Exists",
                $"The directory '{localPath}' already exists. Do you want to replace it?",
                "Yes",
                "No"
            );

            if (!replace)
            {
                Debug.Log("Operation cancelled by the user.");
                ModelAssigner.AssignModel(localPath + "mesh", gameObject);
                SetNewBackground();
                return;
            }
#endif
            // Optionally delete the existing directory if replacing
            Directory.Delete(localPath);
        }

        // Create the directory
        Directory.CreateDirectory(localPath);

        string remotePath = $"{config.pathToGradeADreamer}/logs/{modelName}/appearance/dmtet_mesh/";

        Debug.Log(localPath);
        Debug.Log(remotePath);

        SftpClient sftpClient = new SftpClient(config.host, config.port, config.username, config.password);
        try
        {
            sftpClient.Connect();
            if (sftpClient.IsConnected)
            {
                // Download content of directory
                var files = sftpClient.ListDirectory(remotePath);
                int fileCount = files.Count();
                int downloadedCount = 0;

                foreach (var file in files)
                {
                    if (!file.IsDirectory)
                    {
                        string remoteFilePath = file.FullName;
                        string localFilePath = localPath + file.Name;

                        using (var fileStream = System.IO.File.Create(localFilePath))
                        {
                            sftpClient.DownloadFile(remoteFilePath, fileStream);
                        }

                        downloadedCount++;
                        float progress = (float)downloadedCount / fileCount;
#if UNITY_EDITOR
                        EditorUtility.DisplayProgressBar("Downloading Model", $"Downloading {file.Name}", progress);
#endif
                        Debug.Log($"Downloaded file: {file.Name}");
                    }
                }
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh(); // Refresh the Unity Assets view
                ModelAssigner.AssignModel(localPath + "mesh", gameObject);
                SetNewBackground();
#endif
            }
            else
            {
                Debug.LogError("SFTP connection failed.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SFTP connection error: {ex.Message}");
        }
        finally
        {
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            sftpClient.Disconnect();
            sftpClient.Dispose();
        }
    }

    private void OnApplicationQuit()
    {
        if (sshClient != null && sshClient.IsConnected)
        {
            DisconnectFromSshServer();
        }
    }
}
