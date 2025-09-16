# Instructions for Jules, AI Software Engineer

This document provides instructions for working on the SOLNP project.

**Core Principles:**
- The C++ implementation in `/src` is the reference. The C# implementation in `/dotnet` is a port and must match the C++ version's behavior.
- This project focuses on C++ and C# development. Ignore the Python implementation.
- Always refer to these instructions.

## Initial Setup

Run these commands once after cloning the repository:

1.  **Initialize Git Submodules:**
    ```bash
    git submodule init && git submodule update
    ```
2.  **Apply build fix for Catch2:**
    ```bash
    sed -i 's/SIGSTKSZ/16384/g' library/Catch2/single_include/catch.hpp
    ```

## Development Workflows

### C++ Workflow

1.  **Configure the project (if not done before):**
    ```bash
    cmake .
    ```
2.  **Build C++ tests:** (This may take ~2 minutes)
    ```bash
    make solnp_tests utils_tests
    ```
3.  **Run C++ tests:**
    ```bash
    ./solnp_tests -r junit > solnp_tests_result.xml
    ./utils_tests -r junit > utils_tests_result.xml
    ```
    *Check for exit code 0 and review the XML reports for failures.*

### C# Workflow

1.  **Navigate to the dotnet directory:**
    ```bash
    cd dotnet
    ```
2.  **Build C# projects:**
    ```bash
    dotnet build
    ```
3.  **Run C# test application:**
    ```bash
    dotnet run --project TestApp
    ```
    *After making changes, `cd` back to the root directory.*

### Cross-Implementation Validation Workflow

This is mandatory when changing the core algorithm in either C++ or C#.

1.  **Build the C++ native library:** (This may take ~2 minutes)
    ```bash
    # From the root directory
    mkdir -p build && cd build
    cmake .. && make solnp_native
    cd ..
    ```
2.  **Run the cross-implementation tests:**
    ```bash
    cd dotnet
    dotnet test SolnpTests
    cd ..
    ```
    *All tests should pass to ensure consistency between C++ and C# implementations.*

## Testing Policy

-   After any C++ code change, run the **C++ Workflow**.
-   After any C# code change, run the **C# Workflow**.
-   After changes to the core SOLNP algorithm in either language, run the **Cross-Implementation Validation Workflow**.

## Project Structure

### Key Files and Directories
- `/src/` - Header-only C++ library files:
  - `solnp.hpp` - Main SOLNP algorithm implementation
  - `subnp.hpp` - Sub-problem solver
  - `utils.hpp` - Mathematical utilities (norms, matrix operations)
  - `stdafx.h` - Standard includes
  - `solnp_c_api.cpp` and `solnp_c_api.h` - C API for native library interop
- `/test/` - C++ tests using Catch2 framework
- `/dotnet/` - C# implementation:
  - `solnp/` - C# library project (.NET 8.0)
    - `Solnp.cs` - Main SOLNP algorithm implementation (C# version)
    - `Subnp.cs` - Sub-problem solver (C# version)  
    - `Utils.cs` - Mathematical utilities (C# version)
  - `TestApp/` - C# console test application
  - `SolnpTests/` - **Cross-implementation validation tests (XUnit)**
    - `SolnpCrossImplementationTests.cs` - Tests comparing C++ vs C# results
    - `CppSolnpInterop.cs` - P/Invoke wrapper for native C++ library
    - `README.md` - Documentation for the cross-implementation tests
  - `dotnet.sln` - Solution file for C# projects
- `/cpp_test/` - C++ example application:
  - `main.cpp` - Standalone C++ test using Rosenbrock function
- `/library/` - Git submodules for dependencies:
  - `dlib/` - Mathematical library (matrix operations, optimization)
  - `Catch2/` - C++ testing framework
  - `pybind11/` - Python-C++ bindings (legacy, not used)
- `CMakeLists.txt` - Build system configuration for C++

## Build System Details
- **C++ Build System**:
  - Uses CMake with default mode: Builds C++ tests (`BUILD_PYSOLNP=FALSE`)
  - Dependencies automatically built as static libraries
  - No external BLAS/LAPACK required (uses dlib's built-in implementation)
  - CUDA support disabled (not required for this project)
  - **Native library**: `make solnp_native` builds `libsolnp_native.so` from `src/solnp_c_api.cpp` for C# interop
- **C# Build System**:
  - Uses .NET 8.0 SDK and MSBuild
  - Dependencies managed via NuGet (MathNet.Numerics for linear algebra)
  - Cross-platform compatible (Windows, macOS, Linux)
  - **Cross-implementation tests**: Uses XUnit and P/Invoke to call native C++ library
