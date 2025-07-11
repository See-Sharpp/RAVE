# RAVE


A modern, voice-enabled desktop assistant built with .NET 8 and WPF. WPFApp2 empowers users to launch applications, manage files, browse websites, and execute system-level commands using natural language input — all through a polished, modern UI.

---

## 🚀 Features

- **Modern UI** built with **MahApps.Metro** and **HandyControl**
- **Voice Command Support** using:
  - Whisper API for speech recognition
  - Groq API for command synthesis (LLM)
  - RNNoise CNN for real-time noise suppression
  - NAudio for audio capture and processing
- **Speaker verification** & **wake word detection** for secure command execution
- **File and Application Management**
  - Launch apps, search and open documents, browse websites via voice
- **Automated File Tracking**
  - Real-time file system monitoring via `FileSystemWatcher`
  - Supports `.exe`, `.pdf`, `.docx`, `.pptx`, `.txt` extensions
- **Command History Module**
  - Execution log with timestamps and status (success/failure)
- **System Tray Integration** for lightweight background operation
- Context-aware interaction via integrated prompt engineering

---

## Engineered Capabilities

> Designed for intelligent, secure, and responsive interaction.

- **Natural Language Control**: Converts voice to actions via Whisper + LLM
- **Noise Suppression**: Real-time audio cleanup using RNNoise (ONNX)
- **Voice Amplification**: Boosts low-volume audio for better transcription
- **Authentication Layer**: Only processes commands from verified speakers
- **File Indexing Engine**: Maintains up-to-date file metadata for quick access

---

## Tech Stack

**Languages & Frameworks**:  
C#, .NET 8, WPF, Python (supporting scripts)

**Libraries & APIs**:  
- MahApps.Metro  
- HandyControl  
- EntityFrameworkCore.Sqlite  
- Whisper API  
- Groq LLM API  
- RNNoise (ONNX)  
- NAudio  
- Microsoft.ML  
- Newtonsoft.Json  
- NWaves  
- FontAwesome.Sharp  
- OleDB

---

## Project Structure

WpfApp2/
├── Models/ # Entity and data models
├── Context/ # EF Core DbContext
├── Assets/ # Icons and resources
├── model/ # ONNX models for ML features
├── Views/ # WPF views and UI components
└── AppSetting.json # API keys, config, and prompts

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Visual Studio 2022 or later](https://visualstudio.microsoft.com/)  
  (with **Desktop development with WPF** workload)

---

### Setup Instructions

1. **Clone the Repository**

   ```bash
   git clone https://github.com/yourusername/WpfApp2.git
   cd WpfApp2
Restore NuGet Packages

Open the solution in Visual Studio.

Build the project (Ctrl+Shift+B) to restore dependencies.

Configure API Keys

Add your Whisper, Groq, and custom prompts in AppSetting.json:

{
  "GroqAPIKey": "your-key-here",
  "WhisperEndpoint": "https://api.whisper.com/...",
  "WakeWord": "assistant"
}
Run the Application

From Visual Studio: Press F5

Or via CLI:

   ```bash
   dotnet run --project WpfApp2
   ```
Usage
Voice Commands
Launch the app and use the mic icon or hotkey.

Say commands like:

"Open Chrome"

"Search for AI papers on Google"

"Delete duplicate files from Downloads"

File Scanning
Automatic scanning on first launch and daily thereafter.

Indexes .exe, .pdf, .docx, .pptx, .txt for faster voice access.

Command History
Accessible via the dashboard.

View previous commands, results, and timestamps.

🤝 Contributing
Pull requests are welcome! For major changes, please open an issue first to discuss your ideas.

📄 License
This project is licensed under the MIT License.
