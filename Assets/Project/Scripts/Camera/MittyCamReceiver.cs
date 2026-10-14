using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using UnityEngine;

public class MityCamReceiver : MonoBehaviour
{
    [Header("Process Control")]
    [SerializeField] private bool launchPythonProcess = true;

    // Relative to the Assets/ folder
    private readonly string relativePythonPath = "Python/.venv/Scripts/python.exe";
    private readonly string relativeScriptPath = "Python/main.py";

    private Process _pythonProcess;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;

    private Renderer _targetRenderer;
    private Texture2D _texture;
    private byte[] _pixelBuffer;
    private int _width = 0;
    private int _height = 0;
    private uint _lastFrameCount = 0;

    private const string SHM_NAME = "MityCamSharedMemory";
    private const int HEADER_SIZE = 12;

    void Start()
    {
        _targetRenderer = GetComponent<Renderer>();

        if (launchPythonProcess)
        {
            StartPythonProcess();
        }
    }

    void StartPythonProcess()
    {
        try
        {
            // Application.dataPath points to 'C:/Users/salin/.../magnification-main/Assets'
            string pythonExePath = Path.GetFullPath(Path.Combine(Application.dataPath, relativePythonPath));
            string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, relativeScriptPath));

            // Fallback to system Python if the virtual environment executable is missing
            if (!File.Exists(pythonExePath))
            {
                UnityEngine.Debug.LogWarning($"[Python] Virtual environment not found at '{pythonExePath}'. Falling back to system 'python'.");
                pythonExePath = "python";
            }

            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"[Python] Script not found at '{scriptPath}'");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath), // Sets working dir to Assets/Python so ./bin/GenTL.dll resolves cleanly
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _pythonProcess = new Process { StartInfo = startInfo };
            _pythonProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log($"[Python] {e.Data}"); };
            _pythonProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError($"[Python Err] {e.Data}"); };

            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log($"[Python] Process started using interpreter: {pythonExePath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to launch Python process: {ex.Message}");
        }
    }

    void Update()
    {
        // Try opening the shared memory block created by Python
        if (_mmf == null)
        {
            try
            {
                _mmf = MemoryMappedFile.OpenExisting(SHM_NAME);
                _accessor = _mmf.CreateViewAccessor();
            }
            catch
            {
                // Waiting for Python process to establish the memory region
                return;
            }
        }

        if (_accessor == null || !_accessor.CanRead) return;

        // Header format: [Width (int32), Height (int32), FrameCounter (uint32)]
        int imgWidth = _accessor.ReadInt32(0);
        int imgHeight = _accessor.ReadInt32(4);
        uint frameCount = _accessor.ReadUInt32(8);

        // Skip frame if counter hasn't updated or dimensions are invalid
        if (frameCount == _lastFrameCount || imgWidth <= 0 || imgHeight <= 0) return;
        _lastFrameCount = frameCount;

        int frameDataSize = imgWidth * imgHeight * 3; // RGB24 layout

        // Initialize or resize Texture2D if frame dimensions changed
        if (_width != imgWidth || _height != imgHeight || _texture == null)
        {
            _width = imgWidth;
            _height = imgHeight;
            _texture = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            _pixelBuffer = new byte[frameDataSize];

            if (_targetRenderer != null)
            {
                _targetRenderer.material.mainTexture = _texture;
            }
        }

        // Copy frame bytes into buffer and upload to GPU
        _accessor.ReadArray(HEADER_SIZE, _pixelBuffer, 0, frameDataSize);
        _texture.LoadRawTextureData(_pixelBuffer);
        _texture.Apply();
    }

    void OnDisable()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
        _accessor = null;
        _mmf = null;

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                _pythonProcess.Kill();
                _pythonProcess.Dispose();
            }
            catch { }
            _pythonProcess = null;
        }
    }
}