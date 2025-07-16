# RAVE: A Modern Voice-Enabled Desktop Assistant

RAVE is a sophisticated, voice-activated desktop assistant engineered with .NET 8 and WPF. It empowers you to seamlessly launch applications, open files, browse the web, and execute system-level commands using natural language. All interactions are handled through a polished and modern user interface.

---

## 🚀 Key Features

-   **Modern UI**: A sleek and intuitive interface built with **MahApps.Metro**.
-   **Advanced Voice Commands**:
    -   **Whisper API** for highly accurate speech-to-text.
    -   **Groq API** for intelligent command synthesis using a cutting-edge LLM.
    -   **RNNoise CNN** for real-time noise suppression, ensuring clarity.
    -   **NAudio** for robust audio capture and processing.
-   **Secure Interaction**: Features **speaker verification** and **wake word detection** to ensure commands are executed only by authorized users.
-   **Comprehensive File and Application Management**:
    -   Launch applications, search for documents, and open websites with simple voice commands.
-   **Automated File Tracking**:
    -   Utilizes `FileSystemWatcher` for real-time monitoring of your file system.
    -   Supports popular file extensions including `.exe`, `.pdf`, `.docx`, `.pptx`, and `.txt`.
-   **Command History Module**:
    -   Maintains a detailed log of all executed commands with timestamps and their success or failure status.
-   **System Tray Integration**: Operates discreetly in the background for minimal intrusion along with `FloatingIcon`.
-   **Context-Aware Interactions**: Leverages integrated prompt engineering for more intuitive and relevant responses.

---

## 🛠️ Engineered Capabilities

> RAVE is designed for intelligent, secure, and responsive user interaction.

-   **Natural Language Control**: Translates spoken language into actionable commands via the Whisper and Groq APIs.
-   **Crystal-Clear Audio**: Employs RNNoise (ONNX) for real-time audio cleanup and voice amplification to enhance transcription accuracy.
-   **Robust Authentication**: An integrated security layer that processes commands only from verified speakers.
-   **Efficient File Indexing**: Maintains an up-to-date index of file metadata for swift and accurate access.

---

## 💻 Tech Stack

-   **Languages & Frameworks**: C#, .NET 8, WPF, Python (for supporting scripts)
-   **Libraries & APIs**:
    -   MahApps.Metro
    -   EntityFrameworkCore.Sqlite
    -   Whisper API
    -   Groq LLM API
    -   RNNoise (ONNX)
    -   NAudio
    -   Microsoft.ML
    -   Newtonsoft.Json
    -   NWaves
    -   FontAwesome.Sharp
    -   OleDB

---

## 📂 Project Structure

```
    WpfApp2/
    ├── Models/             # Entity and data models
    ├── Context/            # EF Core DbContext
    ├── Assets/             # Icons and other resources
    ├── model/              # ONNX models for machine learning features
    ├── Views/              # WPF views and UI components
    └── AppSetting.json     # Configuration for API keys and prompts
```

---

## 🏁 Getting Started

### Prerequisites

-   [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
-   [Visual Studio 2022 or later](https://visualstudio.microsoft.com/) (with the **Desktop development with WPF** workload installed)

### Setup Instructions

1.  **Clone the Repository**
    ```bash
    git clone https://github.com/See-Sharpp/RAVE.git
    cd WpfApp2
    ```

2.  **Restore NuGet Packages**
    -   Open the solution in Visual Studio.
    -   Build the project (`Ctrl+Shift+B`) to automatically restore all necessary dependencies.

3.  **Configure API Keys**
    -   Open the `AppSetting.json` file and add your API keys and custom prompts:
        ```json
        {
          "Groq_Api_Key": "your-groq-api-key-here",
          "Groq_Prompt_Api_Key1": "your-groq-api-key1(for prompt1)-here",
          "Groq_Prompt_Api_Key2": "your-groq-api-key1(for prompt2)-here"
        }
        ```

4.  **Run the Application**
    -   **From Visual Studio**: Press `F5` to start debugging.
    -   **Via CLI**:
        ```bash
        dotnet run --project WpfApp2
        ```

---

##  Usage

### Voice Commands

-   Launch the application and use the microphone icon or a designated hotkey to activate voice commands.
-   Speak naturally, for example:
    -   *"Open Chrome"*
    -   *"Search for AI research papers on Google"*
    -   *"Open Spotify"*
    -   *"Open Resume.pdf"* 
    -   *"Shutdown the PC"
    -   *"Reduce Brightness by 10%"* 


### File Scanning

-   RAVE performs an automatic scan on its first launch and subsequently on a daily basis or as set by the user to keep its index current.
-   It indexes `.exe`, `.pdf`, `.docx`, `.pptx`, and `.txt` files to facilitate faster access through voice commands.
-   It also uses `FileWatcher` to keep a track of files or application being modified, added or removed while the app is running.

### Command History

-   The command history is accessible from the main dashboard and history section.
-   You can review all previous commands, their outcomes, and the timestamps of when they were executed.

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you would like to change.

---
