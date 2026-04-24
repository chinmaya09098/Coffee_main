# Coffee Order Management System

A Windows Forms coffee ordering application paired with an AI-powered automated bug-fix agent that detects errors, generates fixes via Azure OpenAI, and opens GitHub pull requests automatically.

---

## Table of Contents

- [Overview](#overview)
- [Projects](#projects)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Clone the Repository](#clone-the-repository)
  - [Build the Solution](#build-the-solution)
  - [Run the Coffee App](#run-the-coffee-app)
  - [Run the CodexFixAgent](#run-the-codexfixagent)
  - [Run Tests](#run-tests)
- [Configuration](#configuration)
  - [keys.config](#keysconfig)
  - [Environment Variables](#environment-variables)
- [Usage](#usage)
  - [Coffee Application](#coffee-application)
  - [CodexFixAgent CLI](#codexfixagent-cli)
- [Branch Naming Convention](#branch-naming-convention)
- [Project Structure](#project-structure)


---

## Overview

| Component | Description |
|---|---|
| **Coffee** | WinForms app for creating and managing coffee orders |
| **CodexFixAgent** | Console agent that auto-fixes bugs using Azure OpenAI and opens PRs on GitHub |
| **Coffee.Tests** | MSTest unit tests for the CoffeeModel |

---

## Projects

### Coffee (Windows Forms)

A desktop GUI that lets users build customised coffee orders:

- Choose bean type, milk, and sugar level (0–5 spoons)
- Confirm and list multiple orders per session
- Real-time validation with colour-coded feedback

### CodexFixAgent (Console)

An end-to-end automated bug-fix pipeline:

1. Accepts a source file and error message (interactive or via CLI flags)
2. Locates the Git repository root automatically
3. Creates a timestamped `fix/` branch from `main`
4. Calls Azure OpenAI to generate a targeted fix
5. Applies a *smart merge* (changes limited to ±20 lines of the error site)
6. Displays a colour-coded diff and asks for confirmation
7. Commits, pushes, and opens a GitHub pull request
8. Sends an email notification with the PR link

### Coffee.Tests (MSTest)

30+ unit tests covering `CoffeeModel` construction, validation, sugar arithmetic, and the `Details()` output format.

---

## Technology Stack

| Item | Detail |
|---|---|
| Framework | .NET Framework 4.8.1 |
| UI | Windows Forms (WinForms) |
| Language | C# |
| Test framework | MSTest (Microsoft.VisualStudio.QualityTools.UnitTestFramework) |
| AI | Azure OpenAI (GPT-based deployment) |
| VCS integration | Git CLI + GitHub REST API v3 |
| Email | System.Net.Mail (SMTP — Gmail / Office 365) |
| Build | MSBuild / Visual Studio |

---

## Prerequisites

- Windows 10 / 11
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community or higher) with the **.NET desktop development** workload
- .NET Framework 4.8.1 Developer Pack
- Git for Windows — must be on `PATH` (required by CodexFixAgent)
- A `keys.config` file with your Azure OpenAI and GitHub credentials (see [Configuration](#configuration))

---

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/chinmaya09098/Coffee_main.git
cd Coffee_main
```

### Build the Solution

**Using Visual Studio:**

```
File → Open → Coffee.slnx
Build → Build Solution   (Ctrl + Shift + B)
```

**Using MSBuild from the Developer Command Prompt:**

```bash
# Debug build (default)
msbuild Coffee.slnx

# Release build
msbuild Coffee.slnx /p:Configuration=Release

# Clean all output folders
msbuild Coffee.slnx /t:Clean

# Clean then rebuild in Release
msbuild Coffee.slnx /t:Clean,Build /p:Configuration=Release

# Build a single project
msbuild Coffee\Coffee.csproj /p:Configuration=Debug
msbuild CodexFixAgent\CodexFixAgent.csproj /p:Configuration=Debug
msbuild Coffee.Tests\Coffee.Tests.csproj /p:Configuration=Debug
```

### Run the Coffee App

**From Visual Studio:**

1. Right-click the **Coffee** project → *Set as Startup Project*
2. Press **F5** (with debugger) or **Ctrl + F5** (without)

**From the command line:**

```bash
# Debug build
Coffee\bin\Debug\Coffee.exe

# Release build
Coffee\bin\Release\Coffee.exe
```

### Run the CodexFixAgent

**Interactive mode** — prompts for file path and error message:

```bash
CodexFixAgent\bin\Debug\CodexFixAgent.exe
```

**With CLI arguments:**

```bash
# Supply file path and full error message
CodexFixAgent\bin\Debug\CodexFixAgent.exe "C:\path\to\File.cs" --error "System.FormatException: Index (zero based) must be greater than or equal to zero"

# Short flag alias (-e)
CodexFixAgent\bin\Debug\CodexFixAgent.exe "C:\path\to\File.cs" -e "NullReferenceException: Object reference not set"

# Auto-apply the fix without a confirmation prompt
CodexFixAgent\bin\Debug\CodexFixAgent.exe "C:\path\to\File.cs" --error "error message" --auto

# Release build variant
CodexFixAgent\bin\Release\CodexFixAgent.exe "C:\path\to\File.cs" -e "error message"
```

### Run Tests

**From Visual Studio:**

```
Test → Run All Tests   (Ctrl + R, A)
```

**Build then run with VSTest:**

```bash
# Step 1 — build the test project
msbuild Coffee.Tests\Coffee.Tests.csproj /p:Configuration=Debug

# Step 2 — run with VSTest (adjust path to match your VS installation)
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" Coffee.Tests\bin\Debug\Coffee.Tests.dll

# Run tests with verbose output
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" Coffee.Tests\bin\Debug\Coffee.Tests.dll /logger:console;verbosity=detailed

# Run a specific test by name
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" Coffee.Tests\bin\Debug\Coffee.Tests.dll /Tests:TestMethodName
```

---

## Configuration

### keys.config

Create a file named `keys.config` in the `CodexFixAgent` project folder. It is already listed in `.gitignore` and will never be committed.

```ini
AZURE_OPENAI_ENDPOINT=https://<your-resource>.openai.azure.com/openai/responses?api-version=2025-04-01-preview
AZURE_OPENAI_KEY=<your-azure-openai-api-key>
AZURE_DEPLOYMENT=<your-deployment-name>

GITHUB_TOKEN=github_pat_<your-personal-access-token>

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=you@gmail.com
SMTP_PASS=<app-password>
NOTIFY_EMAIL=recipient@example.com
```
---

## Usage

### Coffee Application

1. Launch `Coffee.exe`
2. Enter a **Bean Type** (e.g., `Arabica`)
3. Check **Add Milk** if desired
4. Set the **Sugar** level (0–5 spoons) with the spinner
5. Click **Create Cup** — current cup details appear in the panel
6. Optionally click **Add Sugar** to increment sugar on the current cup
7. Click **Confirm Order** to add it to the orders list
8. Click **New Order** to start a fresh cup, or repeat from step 2
9. Click **Clear All** to reset all orders
10. Click **Done** to exit

Validation errors (empty bean type, sugar out of range) appear in red beneath the input area. Successful actions are shown in green.

### CodexFixAgent CLI

```
Usage:
  CodexFixAgent.exe [<file-path>] [--error|-e "<error-message>"] [--auto]

Arguments:
  <file-path>     Path to the C# source file containing the bug.
                  Omit to be prompted interactively.
  --error, -e     The full exception or error message to fix.
                  Omit to be prompted interactively.
  --auto          Apply the fix without asking for confirmation.

Examples:
  CodexFixAgent.exe
  CodexFixAgent.exe MainForm.cs -e "FormatException: Index (zero based) must be >= 0"
  CodexFixAgent.exe MainForm.cs --error "NullReferenceException" --auto
  CodexFixAgent.exe "C:\full\path\to\File.cs" -e "ArgumentOutOfRangeException" --auto
```

**What the agent does after you confirm:**

```
[1]  git checkout main
[2]  git pull origin main
[3]  git checkout -b fix/<error-slug>-<yyyyMMdd-HHmm>
[4]  Azure OpenAI generates a code patch
[5]  Smart merge applied (changes limited to ±20 lines of the error site)
[6]  Colour-coded diff printed to the terminal
[7]  Prompt: y to accept / n to cancel
       If cancelled → git checkout main && git branch -D <branch>
[8]  File overwritten; original backed up as <file>.bak
[9]  git add <file>
[10] git commit -m "Fix: <error-summary>"
[11] git push -u origin <branch>
[12] GitHub PR opened via REST API — URL printed to console
[13] Email notification sent to NOTIFY_EMAIL
```
---

## Branch Naming Convention

CodexFixAgent automatically names fix branches using the pattern:

```
fix/<sanitised-error-summary>-<yyyyMMdd-HHmm>
```

Example:

```
fix/system-formatexception-index-zero-based-must-20260416-1405
```

The error summary is sanitised to lowercase, spaces replaced with hyphens, and special characters removed.

---

## Project Structure

```
Coffee-main/
├── Coffee/
│   ├── CoffeeModel.cs          # Domain model — bean type, sugar, milk
│   ├── MainForm.cs             # WinForms UI (460 x 560 px)
│   ├── Program.cs              # Application entry point
│   ├── App.config              # Runtime configuration
│   └── Coffee.csproj
│
├── CodexFixAgent/
│   ├── Program.cs              # Full agent pipeline (single file)
│   ├── keys.config             # API credentials — NOT committed
│   └── CodexFixAgent.csproj
│
├── Coffee.Tests/
│   ├── CoffeeModelTests.cs     # 30+ MSTest unit tests
│   └── Coffee.Tests.csproj
│
├── Coffee.slnx                 # Solution file (modern format)
├── .gitignore
└── README.md
```

---


