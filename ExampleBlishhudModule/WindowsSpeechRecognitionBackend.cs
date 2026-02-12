using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Blish_HUD;
using Windows.Media.SpeechRecognition;
using Windows.Globalization;
using Windows.Storage;

namespace flakysalt.AccessiblityBuddy
{
    public class WindowsSpeechRecognitionBackend : ISpeechRecognitionBackend
    {
        public event EventHandler<string> onSpeechRecognized;
        
        private readonly static Logger Logger = Logger.GetLogger<WindowsSpeechRecognitionBackend>();
        
        private SpeechRecognizer _speechRecognizer;
        private bool _isListening = false;
        private bool _isDisposed = false;
        private bool _isInitialized = false;
        private Task _initializationTask;
        
        // Lower confidence threshold for dictation (more permissive than commands)
        private const double CONFIDENCE_THRESHOLD = 0.5;

        public WindowsSpeechRecognitionBackend()
        {
            try
            {
                _speechRecognizer = new SpeechRecognizer(new Language("en-US"));
                
                // Configure for better dictation accuracy
                _speechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
                _speechRecognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(1.5);
                _speechRecognizer.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(0); // Don't timeout on continuous speech

                // Load custom vocabulary first (higher priority)
                LoadCustomVocabularyAsync();

                // Use dictation constraint optimized for conversation
                var dictationConstraint = new SpeechRecognitionTopicConstraint(
                    SpeechRecognitionScenario.Dictation, "dictation");
                _speechRecognizer.Constraints.Add(dictationConstraint);
                
                Logger.Info("Dictation constraint added");

                _initializationTask = InitializeAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize speech recognizer");
                throw;
            }
        }
        
        private async void LoadCustomVocabularyAsync()
        {
            try
            {
                var vocabPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Blish HUD", "Accessibility", "vocabulary.txt");
                
                if (File.Exists(vocabPath))
                {
                    var lines = File.ReadAllLines(vocabPath);
                    var phrases = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                        {
                            phrases.Add(line.Trim());
                        }
                    }
                    
                    if (phrases.Count > 0)
                    {
                        var listConstraint = new SpeechRecognitionListConstraint(phrases, "CustomVocabulary");
                        _speechRecognizer.Constraints.Add(listConstraint);
                        Logger.Info($"Loaded custom vocabulary with {phrases.Count} terms");
                    }
                }
                else
                {
                    Logger.Info($"No custom vocabulary file found. Creating sample at: {vocabPath}");
                    CreateSampleVocabularyFile(vocabPath);
                }
                
                // Try to load grammar file if available
                await TryLoadGrammarFileAsync();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not load custom vocabulary");
            }
        }
        
        private async Task TryLoadGrammarFileAsync()
        {
            try
            {
                var grammarPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Blish HUD", "Accessibility", "grammar.xml");
                
                if (File.Exists(grammarPath))
                {
                    var grammarFile = await StorageFile.GetFileFromPathAsync(grammarPath);
                    var grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarFile, "CustomGrammar");
                    _speechRecognizer.Constraints.Add(grammarConstraint);
                    Logger.Info($"Loaded custom grammar file from: {grammarPath}");
                }
                else
                {
                    Logger.Info($"No custom grammar file found. You can create an SRGS grammar at: {grammarPath}");
                    CreateSampleGrammarFile(grammarPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not load grammar file");
            }
        }
        
        private void CreateSampleVocabularyFile(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                var sampleVocab = @"# Custom Vocabulary for Guild Wars 2
# Add one word or phrase per line
# Lines starting with # are comments

# Common Guild Wars 2 Terms
Tyria
Divinity's Reach
Lion's Arch
Hoelbrak
Black Citadel
Rata Sum
The Grove

# Races
Asura
Charr
Norn
Sylvari

# Professions
Elementalist
Mesmer
Necromancer
Revenant
Guardian
Warrior
Ranger
Thief
Engineer

# Common Gaming Terms
GG
noob
loot
dungeon
fractal
raid
meta event
world boss";
                
                File.WriteAllText(path, sampleVocab);
                Logger.Info($"Created sample vocabulary file at: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not create sample vocabulary file");
            }
        }
        
        private void CreateSampleGrammarFile(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                var sampleGrammar = @"<?xml version=""1.0"" encoding=""utf-8""?>
<grammar version=""1.0"" xml:lang=""en-US"" root=""main"" 
         xmlns=""http://www.w3.org/2001/06/grammar""
         tag-format=""semantics/1.0"">
    
    <!-- Main rule that allows any combination -->
    <rule id=""main"" scope=""public"">
        <one-of>
            <item><ruleref uri=""#locations""/></item>
            <item><ruleref uri=""#professions""/></item>
            <item><ruleref uri=""#activities""/></item>
        </one-of>
    </rule>
    
    <!-- Guild Wars 2 Locations -->
    <rule id=""locations"">
        <one-of>
            <item>Divinity's Reach</item>
            <item>Lion's Arch</item>
            <item>Hoelbrak</item>
            <item>Black Citadel</item>
            <item>Rata Sum</item>
            <item>The Grove</item>
        </one-of>
    </rule>
    
    <!-- Professions -->
    <rule id=""professions"">
        <one-of>
            <item>Elementalist</item>
            <item>Mesmer</item>
            <item>Necromancer</item>
            <item>Revenant</item>
            <item>Guardian</item>
            <item>Warrior</item>
        </one-of>
    </rule>
    
    <!-- Activities -->
    <rule id=""activities"">
        <one-of>
            <item>looking for group</item>
            <item>LFG</item>
            <item>anyone want to do</item>
            <item>need help with</item>
        </one-of>
    </rule>
    
</grammar>";
                
                File.WriteAllText(path, sampleGrammar);
                Logger.Info($"Created sample SRGS grammar file at: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not create sample grammar file");
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += 
                        OnSpeechResultGenerated;

                    _isInitialized = true;
                    Logger.Info("WindowsSpeechRecognitionBackend initialized for dictation");
                }
                else
                {
                    Logger.Error($"Failed to compile constraints: {compilationResult.Status}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during initialization");
            }
        }

        private void OnSpeechResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            try
            {
                string recognizedText = args.Result.Text;
                double confidence = (double)args.Result.Confidence;

                Logger.Info($"[RAW RECOGNITION] Text: '{recognizedText}', Confidence: {confidence}");
                
                // Apply confidence threshold
                if (confidence < CONFIDENCE_THRESHOLD)
                {
                    Logger.Warn($"[REJECTED] Confidence {confidence} below threshold {CONFIDENCE_THRESHOLD}");
                    return;
                }
                
                Logger.Info($"[ACCEPTED] Dictation: '{recognizedText}'");
                onSpeechRecognized?.Invoke(this, recognizedText);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing speech result");
            }        
        }

        public void StartRecording()
        {
            Task.Run(async () => await StartListeningAsync()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Logger.Error(t.Exception, "Error starting recording");
                }
            });
        }

        private async Task StartListeningAsync()
        {
            if (_isDisposed)
            {
                Logger.Warn("Cannot start recording: backend is disposed");
                return;
            }

            if (_isListening)
            {
                Logger.Warn("Already listening");
                return;
            }

            try
            {
                // Wait for initialization to complete
                await _initializationTask;

                if (!_isInitialized)
                {
                    Logger.Error("Cannot start listening: initialization failed");
                    return;
                }

                if (_speechRecognizer != null)
                {
                    await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    _isListening = true;
                    Logger.Info("Started listening");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                Logger.Error(comEx, "COM exception while starting listening");
            }
            catch (InvalidOperationException invOpEx)
            {
                Logger.Error(invOpEx, "Invalid operation while starting (session may already be running)");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start listening");
            }
        }
        
        public void StopRecording()
        {
            Task.Run(async () => await StopListeningAsync()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Logger.Error(t.Exception, "Error stopping recording");
                }
            });
        }
        
        private async Task StopListeningAsync()
        {
            if (_isDisposed)
            {
                Logger.Warn("Cannot stop recording: backend is disposed");
                return;
            }

            if (!_isListening)
            {
                Logger.Warn("Not currently listening");
                return;
            }

            try
            {
                if (_speechRecognizer != null)
                {
                    await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
                    _isListening = false;
                    Logger.Info("Stopped listening");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                // Handle COM exceptions that can occur with speech recognition
                Logger.Warn(comEx, "COM exception while stopping (session may already be stopped)");
                _isListening = false;
            }
            catch (InvalidOperationException invOpEx)
            {
                // Session was not in correct state
                Logger.Warn(invOpEx, "Invalid operation while stopping (session not running)");
                _isListening = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to stop listening");
                _isListening = false; // Reset state even on error
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                if (_speechRecognizer != null)
                {
                    if (_isListening)
                    {
                        try
                        {
                            _speechRecognizer.ContinuousRecognitionSession.StopAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Error stopping session during disposal");
                        }
                    }

                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= OnSpeechResultGenerated;
                    _speechRecognizer.Dispose();
                }

                Logger.Info("WindowsSpeechRecognitionBackend disposed");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error disposing WindowsSpeechRecognitionBackend");
            }
        }
    }
}
