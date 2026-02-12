using System;

namespace flakysalt.AccessiblityBuddy
{
    public interface ISpeechRecognitionBackend : IDisposable
    {
        void StartRecording();
        void StopRecording();
        
        event EventHandler<string> onSpeechRecognized;
    }
}

