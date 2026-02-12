using System;
using System.Diagnostics;
using System.Speech.Recognition;
using System.IO;
using System.Xml;
using Blish_HUD;

namespace flakysalt.AccessiblityBuddy
{
    public class SystemSpeechBackend : ISpeechRecognitionBackend, IDisposable
    {
        private readonly static Logger Logger = Logger.GetLogger<SystemSpeechBackend>();
        
        public event EventHandler<string> onSpeechRecognized;
        
        private SpeechRecognitionEngine recognizer;
        private bool _isListening = false;
        private bool _isDisposed = false;
        
        // Lower confidence threshold for dictation (more permissive than commands)
        private const float CONFIDENCE_THRESHOLD = 0.5f;
        
        public SystemSpeechBackend()
        {
            try
            {
                recognizer = new SpeechRecognitionEngine();
                
                // Configure recognition settings optimized for dictation
                recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(5); // More time to start speaking
                recognizer.BabbleTimeout = TimeSpan.FromSeconds(0); // Don't timeout on continuous speech
                recognizer.EndSilenceTimeout = TimeSpan.FromSeconds(1.5); // Pause before finalizing
                recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(2);
                
                // Increase recognizer's buffer for better accuracy
                recognizer.MaxAlternates = 10; // Consider more alternatives
                
                // Load custom pronunciation if available
                LoadCustomPronunciations();
                
                // Load ONLY dictation grammar for natural speech
                var dictationGrammar = new DictationGrammar();
                dictationGrammar.Name = "Dictation";
                dictationGrammar.Enabled = true;
                // Set dictation weight for better prioritization
                dictationGrammar.Weight = 1.0f;
                recognizer.LoadGrammar(dictationGrammar);
                Logger.Info("Dictation grammar loaded");
                
                // Load custom vocabulary if available
                LoadCustomVocabulary();
            
                // Set up event handlers
                recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                recognizer.SpeechDetected += Recognizer_SpeechDetected;
                recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
                recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
                recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
                Logger.Info("Event handlers subscribed");
            
                // Set input to default microphone
                Logger.Info("Setting input to default audio device...");
                recognizer.SetInputToDefaultAudioDevice();
                
                // Update recognizer's audio settings for better quality
                recognizer.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", 30);
                recognizer.UpdateRecognizerSetting("AdaptationOn", 1); // Enable acoustic adaptation
                
                Logger.Info("Audio device set successfully");
                
                Logger.Info("=== SystemSpeechBackend initialized for dictation ===");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "!!! Failed to initialize SystemSpeechBackend !!!");
                throw;
            }
        }
        
        private void LoadCustomPronunciations()
        {
            try
            {
                // You can add custom pronunciations for commonly misrecognized words
                // This uses the Windows Speech Recognition format
                var pronunciationPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Blish HUD", "Accessibility", "pronunciations.xml");
                
                if (File.Exists(pronunciationPath))
                {
                    // Load custom pronunciation lexicon
                    // Note: This requires a properly formatted XML lexicon file
                    Logger.Info($"Custom pronunciation file found at: {pronunciationPath}");
                    // Implementation would load the lexicon here
                }
                else
                {
                    Logger.Info($"No custom pronunciation file found. You can create one at: {pronunciationPath}");
                    CreateSamplePronunciationFile(pronunciationPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not load custom pronunciations");
            }
        }
        
        private void LoadCustomVocabulary()
        {
            try
            {
                // Load custom vocabulary for gaming terms, character names, etc.
                var vocabPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Blish HUD", "Accessibility", "vocabulary.txt");
                
                if (File.Exists(vocabPath))
                {
                    var lines = File.ReadAllLines(vocabPath);
                    var choices = new Choices();
                    int count = 0;
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                        {
                            choices.Add(line.Trim());
                            count++;
                        }
                    }
                    
                    if (count > 0)
                    {
                        var vocabBuilder = new GrammarBuilder();
                        vocabBuilder.Culture = recognizer.RecognizerInfo.Culture;
                        vocabBuilder.Append(choices);
                        
                        var vocabGrammar = new Grammar(vocabBuilder);
                        vocabGrammar.Name = "CustomVocabulary";
                        vocabGrammar.Priority = 100; // High priority
                        vocabGrammar.Weight = 1.5f; // Higher weight than dictation
                        
                        recognizer.LoadGrammar(vocabGrammar);
                        Logger.Info($"Loaded custom vocabulary with {count} terms");
                    }
                }
                else
                {
                    Logger.Info($"No custom vocabulary file found. You can create one at: {vocabPath}");
                    CreateSampleVocabularyFile(vocabPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not load custom vocabulary");
            }
        }
        
        private void CreateSamplePronunciationFile(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                var sampleXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Custom Pronunciation Lexicon for Guild Wars 2 -->
<!-- Add your custom pronunciations here -->
<lexicon version=""1.0"" xmlns=""http://www.w3.org/2005/01/pronunciation-lexicon"">
    <!-- Example entries for Guild Wars 2 terms -->
    <lexeme>
        <grapheme>Tyria</grapheme>
        <phoneme>T IH1 R IY0 AH0</phoneme>
    </lexeme>
    <lexeme>
        <grapheme>Charr</grapheme>
        <phoneme>CH AA1 R</phoneme>
    </lexeme>
    <lexeme>
        <grapheme>Asura</grapheme>
        <phoneme>AH0 S UH1 R AH0</phoneme>
    </lexeme>
</lexicon>";
                
                File.WriteAllText(path, sampleXml);
                Logger.Info($"Created sample pronunciation file at: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not create sample pronunciation file");
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
Human

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
meta
DPS
tank
healer";
                
                File.WriteAllText(path, sampleVocab);
                Logger.Info($"Created sample vocabulary file at: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not create sample vocabulary file");
            }
        }
        
        private void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Logger.Info($"[SpeechRejected] Low confidence speech rejected (confidence: {e.Result.Confidence})");
        }
        
        private void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Logger.Info($"[SpeechDetected] Audio input detected at position: {e.AudioPosition}");
        }

        private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Logger.Info($"[SpeechHypothesized] Possible text: '{e.Result.Text}' (confidence: {e.Result.Confidence})");
        }

        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            Logger.Info($"[RecognizeCompleted] Result: {e.Result?.Text ?? "null"}, " +
                       $"BabbleTimeout: {e.BabbleTimeout}, InitialSilenceTimeout: {e.InitialSilenceTimeout}");
        }

        public void StartRecording()
        {
            if (_isDisposed)
            {
                Logger.Warn("!!! Cannot start recording: backend is disposed !!!");
                return;
            }

            if (_isListening)
            {
                Logger.Warn("!!! Already listening !!!");
                return;
            }

            try
            {
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                Logger.Info("*** Started listening for dictation ***");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "!!! Failed to start recording !!!");
            }
        }

        public void StopRecording()
        {
            if (_isDisposed)
            {
                return;
            }

            if (!_isListening)
            {
                return;
            }

            try
            {
                recognizer.RecognizeAsyncStop();
                _isListening = false;
                Logger.Info("*** Stopped listening ***");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "!!! Failed to stop recording !!!");
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                Logger.Info($"[RAW RECOGNITION] Text: '{e.Result.Text}', Confidence: {e.Result.Confidence}");
                
                // Apply confidence threshold
                if (e.Result.Confidence < CONFIDENCE_THRESHOLD)
                {
                    Logger.Warn($"[REJECTED] Confidence {e.Result.Confidence} below threshold {CONFIDENCE_THRESHOLD}");
                    return;
                }
                
                Logger.Info($"[ACCEPTED] Dictation: '{e.Result.Text}'");
                onSpeechRecognized?.Invoke(this, e.Result.Text);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "!!! Error processing speech result !!!");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                Logger.Info("Already disposed, returning");
                return;
            }

            try
            {
                if (_isListening)
                {
                    Logger.Info("Canceling recognition...");
                    recognizer.RecognizeAsyncCancel();
                }

                if (recognizer != null)
                {
                    recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;
                    recognizer.SpeechDetected -= Recognizer_SpeechDetected;
                    recognizer.SpeechHypothesized -= Recognizer_SpeechHypothesized;
                    recognizer.RecognizeCompleted -= Recognizer_RecognizeCompleted;
                    recognizer.SpeechRecognitionRejected -= Recognizer_SpeechRecognitionRejected;
                    
                    recognizer.Dispose();
                }

                Logger.Info("=== SystemSpeechBackend disposed successfully ===");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "!!! Error disposing SystemSpeechBackend !!!");
            }
            finally
            {
                _isDisposed = true;
                Logger.Info($"Final state - IsDisposed: {_isDisposed}");
            }
        }
    }
}
