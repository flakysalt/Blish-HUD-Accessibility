using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;

using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace flakysalt.AccessiblityBuddy
{
    [Export(typeof(Module))]
    public class AccessiblityBuddyModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger<AccessiblityBuddyModule>();

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        [ImportingConstructor]
        public AccessiblityBuddyModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
        }

        protected override void DefineSettings(SettingCollection settings)
        {
        }
        
        ISpeechRecognitionBackend _speechBackend;
        
        private Label _statusLabel;
        private Label _commandLabel;
        private StandardButton _startButton;
        private StandardButton _stopButton;
        private bool _isListening = false;

        protected override async Task LoadAsync()
        {
            CreateUI();
            InitializeSpeechRecognizer();
        }

        private void CreateUI()
        {
            var window = new StandardWindow(
                GameService.Content.DatAssetCache.GetTextureFromAssetId(155985),
                new Rectangle(25, 26, 450, 400),
                new Rectangle(40, 50, 410, 340))
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Voice to Text - Chat Helper",
                Location = new Point(100, 100),
                SavesPosition = true,
                Id = "VoiceCommands_Window"
            };

            var instructionsLabel = new Label
            {
                Parent = window,
                Location = new Point(20, 20),
                Size = new Point(370, 100),
                Text = "Voice to Text for Chat:\n" +
                       "1. Click 'Start Listening'\n" +
                       "2. Speak naturally what you want to type\n" +
                       "3. Text will appear below - copy and paste into game chat\n" +
                       "4. Click 'Stop Listening' when done",
                WrapText = true,
                AutoSizeHeight = true
            };

            _statusLabel = new Label
            {
                Parent = window,
                Location = new Point(20, 130),
                Size = new Point(370, 30),
                Text = "Status: Ready",
                WrapText = true
            };

            _startButton = new StandardButton
            {
                Parent = window,
                Location = new Point(20, 170),
                Size = new Point(150, 40),
                Text = "Start Listening",
            };
            _startButton.Click += (s, e) =>  
            {
                _speechBackend.StartRecording();
                _statusLabel.Text = "Status: Listening... Speak now!";
            };

            _stopButton = new StandardButton
            {
                Parent = window,
                Location = new Point(180, 170),
                Size = new Point(150, 40),
                Text = "Stop Listening",
            };
            _stopButton.Click +=  (s, e) =>  
            {
                _speechBackend.StopRecording();
                _statusLabel.Text = "Status: Stopped";
            };

            _commandLabel = new Label
            {
                Parent = window,
                Location = new Point(20, 220),
                Size = new Point(370, 100),
                Text = "Your speech will appear here...\n(Copy and paste into game chat)",
                WrapText = true,
                AutoSizeHeight = true
            };

            window.Show();
        }

        private void InitializeSpeechRecognizer()
        {
            _speechBackend = new WindowsSpeechRecognitionBackend();
            //_speechBackend = new SystemSpeechBackend();

            
            _speechBackend.onSpeechRecognized += (s, text) =>
            {
                // Append new text to existing text for continuous dictation
                string currentText = _commandLabel.Text;
                
                // If it's the first recognition, replace the default text
                if (currentText.Contains("Your speech will appear here"))
                {
                    _commandLabel.Text = text;
                }
                else
                {
                    // Append with a space
                    _commandLabel.Text = currentText + " " + text;
                }
            };
        }
        protected override void Unload()
        {
            _speechBackend?.StopRecording();
            _speechBackend?.Dispose();
            _speechBackend = null;
            
            base.Unload();
        }
    }
}