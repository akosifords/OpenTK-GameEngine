# Makina
Makina - Game Engine

## Getting Started

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later)
*   Git
*   An IDE that supports .NET development is recommended (Visual Studio, JetBrains Rider, VS Code with C# extension).

### Cloning

To get a copy of the project, clone the repository using Git:

```bash
git clone <repository-url> # Replace <repository-url> with the actual URL
cd Makina
```

### Setup & Running

There are two main ways to build and run the project:

**1. Using an IDE (Recommended):**

*   Open the `Makina.sln` solution file in your IDE (Visual Studio, Rider, etc.).
*   Set the `Sandbox` project as the startup project.
*   Build the solution (usually Ctrl+Shift+B or F6).
*   Run the `Sandbox` project (usually F5).

**2. Using the .NET CLI:**

*   Navigate to the `Sandbox` project directory in your terminal:
    ```bash
    cd Sandbox
    ```
*   Run the project:
    ```bash
    dotnet run
    ```
    This command will automatically restore dependencies, build, and run the `Sandbox` application.

The Sandbox application demonstrates the current features of the Makina engine.
