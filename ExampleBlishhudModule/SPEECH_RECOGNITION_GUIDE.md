# Speech Recognition Accuracy Guide

## Improving Recognition Accuracy

This module supports custom dictionaries and vocabularies to improve speech recognition accuracy, especially for gaming terms and Guild Wars 2 specific words.

## Custom Vocabulary Files

The module automatically creates sample files in:
```
%APPDATA%\Blish HUD\Accessibility\
```

### 1. **vocabulary.txt** - Simple Word List
A plain text file with one word or phrase per line.

**Location:** `%APPDATA%\Blish HUD\Accessibility\vocabulary.txt`

**Format:**
```
# Lines starting with # are comments
Tyria
Divinity's Reach
Asura
Charr
fractal
meta event
```

**Usage:**
- Add Guild Wars 2 location names
- Add character/profession names
- Add gaming terms you use frequently
- Add player names or guild names

### 2. **grammar.xml** - SRGS Grammar (Advanced)
XML-based Speech Recognition Grammar Specification for complex patterns.

**Location:** `%APPDATA%\Blish HUD\Accessibility\grammar.xml`

See the auto-generated sample for Guild Wars 2 specific grammar rules.

### 3. **pronunciations.xml** - Custom Pronunciations (System.Speech only)
XML lexicon for teaching the recognizer how to pronounce specific words.

**Location:** `%APPDATA%\Blish HUD\Accessibility\pronunciations.xml`

**Format:**
```xml
<lexicon version="1.0" xmlns="http://www.w3.org/2005/01/pronunciation-lexicon">
    <lexeme>
        <grapheme>Tyria</grapheme>
        <phoneme>T IH1 R IY0 AH0</phoneme>
    </lexeme>
</lexicon>
```

## Windows Speech Recognition Training

For the **best accuracy**, train Windows Speech Recognition:

1. Open **Settings** → **Time & Language** → **Speech**
2. Click **"Get started"** under Speech Recognition
3. Complete the training wizard (takes 5-10 minutes)
4. The more you use it, the better it learns your voice

## System.Speech (Desktop) Training

If using SystemSpeechBackend:

1. Open **Control Panel** → **Speech Recognition**
2. Click **"Train your computer to better understand you"**
3. Complete the training sessions

## Tips for Better Accuracy

1. **Use a quality microphone** - USB headset microphones work best
2. **Reduce background noise** - Quiet environment improves accuracy
3. **Speak clearly and naturally** - Don't shout or whisper
4. **Add custom vocabulary** - Add gaming terms you use frequently
5. **Train the recognizer** - Complete Windows training for your voice
6. **Adjust confidence threshold** - Lower = more permissive (default 0.5)

## Adjusting Confidence Threshold

In the code, you can change:
```csharp
private const float CONFIDENCE_THRESHOLD = 0.5f; // 0.0 to 1.0
```

- **0.3-0.4** = Very permissive (more false positives)
- **0.5** = Balanced (recommended for dictation)
- **0.7-0.8** = Strict (fewer errors, may miss some speech)

## Additional Resources

### Download Pre-made Vocabularies:
- **Guild Wars 2 Wiki** - Export location/skill names
- **Gaming Glossary** - Common gaming terms
- Create your own based on what you type in chat

### Phoneme Reference (for pronunciations.xml):
- [CMU Pronouncing Dictionary](http://www.speech.cs.cmu.edu/cgi-bin/cmudict)
- [ARPABET Phoneme Set](https://en.wikipedia.org/wiki/ARPABET)

### SRGS Grammar Reference:
- [W3C SRGS Specification](https://www.w3.org/TR/speech-grammar/)
- [Microsoft SRGS Tutorial](https://docs.microsoft.com/en-us/previous-versions/office/developer/speech-technologies/)

## Troubleshooting

**Low accuracy?**
- Check microphone levels in Windows
- Complete voice training
- Add commonly misrecognized words to vocabulary.txt
- Speak more clearly and at a moderate pace

**Not recognizing anything?**
- Check logs for "Audio input detected"
- Verify microphone permissions
- Try restarting the module
- Check if Windows Speech Recognition is installed

**Too many false positives?**
- Increase CONFIDENCE_THRESHOLD (e.g., to 0.7)
- Reduce background noise
- Use push-to-talk (start/stop listening manually)

