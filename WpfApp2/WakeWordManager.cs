public static class WakeWordManager
{
    public static WakeWordHelper? Detector { get; private set; }
    public static bool IsRunning => Detector != null;

    public static event Action? WakeWordDetected;

    private static void OnWakeWordDetectedInternal()
    {
        WakeWordDetected?.Invoke();
    }

    public static void Start()
    {
        if (Detector == null)
        {
            Detector = new WakeWordHelper("model/hey_jarvis_v0.1.onnx", OnWakeWordDetectedInternal);
            Task.Run(() => Detector.Start());
        }
    }

    public static void Stop()
    {
        if (Detector != null)
        {
            Detector.Stop();
            Detector = null;
        }
    }

    public static void Pause() => Detector?.Pause();
    public static void Resume() => Detector?.Resume();
}
