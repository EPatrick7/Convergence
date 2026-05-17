using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkGravityBroadcaster))]
public class PipeServer : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private NamedPipeServerStream pipeServer;
#else
    private Socket socketServer;
    private Socket clientSocket;
    private string socketPath = "/tmp/galaxy.sock";
#endif

    private Thread serverThread;
    private NetworkGravityBroadcaster broadcaster;
    private bool isRunning = true;

    public GameObject HookUI;

    void Start()
    {
        broadcaster = GetComponent<NetworkGravityBroadcaster>();
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    private void Update()
    {
        if (PauseMenu.isPaused)
            PauseMenu.Instance.UnPause();
    }

    void StartServer()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        RunWindowsPipe();
#else
        RunUnixSocket();
#endif
    }

    // ================= WINDOWS =================
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void RunWindowsPipe()
    {
        pipeServer = new NamedPipeServerStream(
            "GalaxyPipe",
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.None
        );

        Debug.Log("Waiting for Python client (Windows pipe)...");
        pipeServer.WaitForConnection();
        Debug.Log("Connected!");

        MainLoop(
            () => ReadFromPipe(),
            (data) => WriteToPipe(data)
        );
    }

    string ReadFromPipe()
    {
        byte[] buffer = new byte[4096];
        int bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    void WriteToPipe(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        pipeServer.Write(bytes, 0, bytes.Length);
        pipeServer.Flush();
    }
#endif

    // ================= MAC / LINUX =================
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    void RunUnixSocket()
    {
        if (File.Exists(socketPath))
            File.Delete(socketPath);

        socketServer = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
        socketServer.Bind(new UnixDomainSocketEndPoint(socketPath));
        socketServer.Listen(1);

        Debug.Log("Waiting for Python client (Unix socket)...");
        clientSocket = socketServer.Accept();
        Debug.Log("Connected!");

        MainLoop(
            () => ReadFromSocket(),
            (data) => WriteToSocket(data)
        );
    }

    string ReadFromSocket()
    {
        byte[] buffer = new byte[4096];
        int bytesRead = clientSocket.Receive(buffer);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    void WriteToSocket(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        clientSocket.Send(bytes);
    }
#endif

    // ================= SHARED LOOP =================
    void MainLoop(Func<string> reader, Action<string> writer)
    {
        if (HookUI != null)
            HookUI.SetActive(false);

        StringBuilder incoming = new StringBuilder();

        while (isRunning)
        {
            try
            {
                // SEND STATE
                string json = JsonUtility.ToJson(broadcaster.gravityUniverse) + "\n";
                writer(json);

                // READ INPUT
                string data = reader();
                incoming.Append(data);

                if (incoming.ToString().Contains("\n"))
                {
                    string[] parts = incoming.ToString().Split('\n');

                    foreach (string part in parts)
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;

                        NetworkedPlayerInputs inputs =
                            JsonUtility.FromJson<NetworkedPlayerInputs>(part);

                        broadcaster.currentInputs = inputs;
                        broadcaster.awaitingSimulation = true;

                        if (inputs.wantsToRestartGame)
                        {
                            Time.timeScale = 1;
                            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                            pipeServer?.Close();
#else
                            clientSocket?.Close();
#endif 

                            if (HookUI != null)
                                HookUI.SetActive(true);
                        }
                    }

                    incoming.Clear();
                }
            }
            catch (Exception e)
            {
                Debug.Log("IPC error: " + e.Message);
                break;
            }
        }

        Cleanup();
    }

    void Cleanup()
    {
        if (HookUI != null)
            HookUI.SetActive(true);

        isRunning = false;

    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        pipeServer?.Close();
    #else
            clientSocket?.Close();
            socketServer?.Close();
            if (File.Exists(socketPath))
                File.Delete(socketPath);
    #endif
    }

    private void OnDestroy()
    {
        isRunning = false;
        Cleanup();
        serverThread?.Abort();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        Cleanup();
        serverThread?.Abort();
    }
}