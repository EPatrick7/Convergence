using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(NetworkGravityBroadcaster))]
public class PipeServer : MonoBehaviour
{
    private NamedPipeServerStream pipeServer;
    private Thread serverThread;

    private NetworkGravityBroadcaster broadcaster;
    private bool isRunning = true;
    void Start()
    {
        broadcaster= GetComponent<NetworkGravityBroadcaster>();
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }
    public GameObject HookUI;
    private void Update()
    {
        if (PauseMenu.isPaused)
        {
            PauseMenu.Instance.UnPause();
        }
    }
    void StartServer()
    {
        pipeServer = new NamedPipeServerStream(
            "GalaxyPipe",
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.None
        );

        Debug.Log("Waiting for Python client...");
        pipeServer.WaitForConnection();
        Debug.Log("Python connected!");

        if(HookUI!=null)
        {
            HookUI.gameObject.SetActive(false);
        }

        while (isRunning &&pipeServer.IsConnected)
        {
            try
            {
                // SEND STATE
                string json = JsonUtility.ToJson(broadcaster.gravityUniverse) + "\n";
                byte[] sendBytes = Encoding.UTF8.GetBytes(json);

                pipeServer.Write(sendBytes, 0, sendBytes.Length);
                pipeServer.Flush();

                // READ RESPONSE
                byte[] buffer = new byte[4096];
                StringBuilder incoming = new StringBuilder();

                int bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                incoming.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                string data = incoming.ToString();

                if (data.Contains("\n"))
                {
                    string[] parts = data.Split('\n');

                    foreach (string part in parts)
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;

                        NetworkedPlayerInputs inputs =
                            JsonUtility.FromJson<NetworkedPlayerInputs>(part);

                        broadcaster.currentInputs = inputs;
                        broadcaster.awaitingSimulation = true;



                        if(broadcaster.currentInputs.wantsToRestartGame)
                        {
                            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                            pipeServer?.Close();

                            if (HookUI != null)
                            {
                                HookUI.gameObject.SetActive(true);
                            }
                        }
                    }

                    incoming.Clear();
                }
            }
            catch (IOException e)
            {
                Debug.Log("Pipe error: " + e.Message);
                break;
            }
        }

        if (HookUI != null)
        {
            HookUI.gameObject.SetActive(true);
        }
        isRunning = false;
        pipeServer.Close();
    }
    private void OnDestroy()
    {
        isRunning = false;
        pipeServer?.Close();
        serverThread?.Abort();
    }
    void OnApplicationQuit()
    {
        isRunning = false;
        pipeServer?.Close();
        serverThread?.Abort();
    }
}