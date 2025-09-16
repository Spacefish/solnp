# SOLNP - C++ and C# Nonlinear Optimization Library

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

**IMPORTANT**: This project focuses on **C++ implementation** (in `/src/` folder) and **C# implementation** (in `/dotnet/` folder). **Ignore the Python implementation** - we only work with C++ and C# versions.

### Prerequisites and Dependencies
- Install git submodules before building:  
  `git submodule init && git submodule update`
- CMake >= 2.8.12 (usually available as `cmake`)
- C++ compiler (g++ or clang++, C++11 support required)
- Make (GNU Make)
- .NET 8.0 SDK (for C# builds)

### C++ Development - Building and Testing
- **NEVER CANCEL: First build takes 2+ minutes. NEVER CANCEL. Set timeout to 300+ seconds.**
- Configure the project:  
  `cmake .`  -- takes 4 seconds
- Build C++ tests:  
  `make solnp_tests utils_tests`  -- takes 114 seconds. **NEVER CANCEL**. Set timeout to 300+ seconds.
- Run C++ tests:  
  `./solnp_tests -r junit > solnp_tests_result.xml`  -- takes <1 second
  `./utils_tests -r junit > utils_tests_result.xml`  -- takes <1 second

### Known Issues and Workarounds
- **Catch2 SIGSTKSZ compilation issue**: If build fails with "size of array 'altStackMem' is not an integral constant-expression", apply this fix:  
  `sed -i 's/SIGSTKSZ/16384/g' library/Catch2/single_include/catch.hpp`
- This is required on modern Linux systems with glibc that makes SIGSTKSZ non-constant.

### C# Development - Building and Testing
- **Build C# projects**:
  ```bash
  cd dotnet
  dotnet build  # Takes ~15 seconds, builds both library and test app
  ```
- **Run C# test application**:
  ```bash
  cd dotnet
  dotnet run --project TestApp  # Runs Rosenbrock optimization test
  ```
- **C# project structure**:
  - `solnp/` - C# library project with SOLNP implementation
  - `TestApp/` - Console application that tests the C# implementation

## Validation

### Mandatory Testing After Changes
- **ALWAYS run both C++ test suites after making changes to C++ code:**
  ```bash
  cmake .
  make solnp_tests utils_tests  # Can build both simultaneously
  ./solnp_tests -r junit > solnp_tests_result.xml
  ./utils_tests -r junit > utils_tests_result.xml
  ```
- **ALWAYS test C# implementation after making changes to C# code:**
  ```bash
  cd dotnet
  dotnet build  # Rebuild C# projects
  dotnet run --project TestApp  # Verify functionality
  ```
- **Cross-platform consistency testing**: There are comparison tests between C++ and C# implementations to ensure consistent results. Both implementations should produce the same optimization results for the same input problems.
- **ALWAYS check test results**: Tests should exit with code 0 and run in <1 second each
- **Manual validation scenarios**: 
  - After changing core SOLNP algorithm (`src/solnp.hpp` or `dotnet/solnp/Solnp.cs`), verify basic optimization works by running both C++ and C# test examples
  - After changing utilities (`src/utils.hpp` or `dotnet/solnp/Utils.cs`), run corresponding tests
  - Check that any CMakeLists.txt or .csproj changes don't break the build
  - **Compare outputs**: Both C++ (`cpp_test/main`) and C# (`dotnet/TestApp`) should produce similar optimization results for the Rosenbrock function

### CI Integration
- GitHub Actions runs on every push/PR for:
  - Building and testing C++ implementation
  - Building and testing C# implementation  
  - CodeCov analysis (only on release branch)
- CodeCov workflow requires: `cmake -DRUN_CODECOV=TRUE . && make solnp_tests utils_tests`
- **NEVER CANCEL: CI builds can take 10+ minutes for complete testing**

## Project Structure

### Key Files and Directories
- `/src/` - Header-only C++ library files:
  - `solnp.hpp` - Main SOLNP algorithm implementation
  - `subnp.hpp` - Sub-problem solver
  - `utils.hpp` - Mathematical utilities (norms, matrix operations)
  - `stdafx.h` - Standard includes
- `/test/` - C++ tests using Catch2 framework
- `/dotnet/` - C# implementation:
  - `solnp/` - C# library project (.NET 8.0)
    - `Solnp.cs` - Main SOLNP algorithm implementation (C# version)
    - `Subnp.cs` - Sub-problem solver (C# version)  
    - `Utils.cs` - Mathematical utilities (C# version)
  - `TestApp/` - C# console test application
  - `dotnet.sln` - Solution file for C# projects
- `/cpp_test/` - C++ example application:
  - `main.cpp` - Standalone C++ test using Rosenbrock function
- `/library/` - Git submodules for dependencies:
  - `dlib/` - Mathematical library (matrix operations, optimization)
  - `Catch2/` - C++ testing framework
  - `pybind11/` - Python-C++ bindings (legacy, not used)
- `CMakeLists.txt` - Build system configuration for C++

### Build System Details
- **C++ Build System**:
  - Uses CMake with default mode: Builds C++ tests (`BUILD_PYSOLNP=FALSE`)
  - Dependencies automatically built as static libraries
  - No external BLAS/LAPACK required (uses dlib's built-in implementation)
  - CUDA support disabled (not required for this project)
- **C# Build System**:
  - Uses .NET 8.0 SDK and MSBuild
  - Dependencies managed via NuGet (MathNet.Numerics for linear algebra)
  - Cross-platform compatible (Windows, macOS, Linux)

## Common Tasks

### After Fresh Clone
```bash
# Essential setup - run these commands in order:
git submodule init && git submodule update    # 30 seconds
sed -i 's/SIGSTKSZ/16384/g' library/Catch2/single_include/catch.hpp  # Fix compilation issue
cmake .                                       # 4 seconds  
make solnp_tests utils_tests                  # 114 seconds total - NEVER CANCEL
./solnp_tests -r junit > solnp_tests_result.xml    # <1 second
./utils_tests -r junit > utils_tests_result.xml    # <1 second

# Test C# implementation
cd dotnet
dotnet build                                  # ~15 seconds
dotnet run --project TestApp                 # <1 second - should show convergence results
```

### Clean Build
```bash
make clean
# Then follow "After Fresh Clone" steps starting from cmake
```

### Development Workflow
- **For C++ changes**: Make changes to source files in `/src/` or test files in `/test/`
- **For C# changes**: Make changes to files in `/dotnet/solnp/` or `/dotnet/TestApp/`
- **ALWAYS rebuild and test both implementations**:
  ```bash
  # C++ testing
  make solnp_tests utils_tests    # Only rebuilds what changed
  ./solnp_tests -r junit > solnp_tests_result.xml
  ./utils_tests -r junit > utils_tests_result.xml
  
  # C# testing  
  cd dotnet
  dotnet build
  dotnet run --project TestApp
  ```
- **Verify consistency**: Both C++ and C# implementations should produce similar results for the same optimization problems
- Verify test results are successful (exit code 0)

### Important Notes
- **Dual Implementation**: This project maintains parallel C++ and C# implementations of the SOLNP algorithm
- **Consistency Testing**: Both implementations should produce similar results for the same optimization problems (Rosenbrock function test validates this)
- **Header-only C++ design**: Main C++ functionality is in `.hpp` files
- **DLIB dependency**: C++ version uses DLIB for matrix operations, C# version uses MathNet.Numerics
- **Git submodules**: **ALWAYS** initialize after clone or the C++ build will fail
- **Build artifacts**: Excluded by .gitignore (CMakeCache.txt, CMakeFiles/, executables, bin/, obj/, etc.)
- **Algorithm**: Both implementations use the SOLNP (Sequential Quadratic Programming) algorithm for constrained nonlinear optimization