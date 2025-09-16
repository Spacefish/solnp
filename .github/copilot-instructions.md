# SOLNP - Python/C++ Nonlinear Optimization Library

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites and Dependencies
- Install git submodules before building:  
  `git submodule init && git submodule update`
- CMake >= 2.8.12 (usually available as `cmake`)
- C++ compiler (g++ or clang++, C++11 support required)
- Make (GNU Make)
- Python 3.6+ (for Python builds)

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

### Python Development
- **Python build currently has issues** - do not attempt `python3 setup.py build_ext --inplace` as it fails during cmake build phase
- Python tests can be run if pysolnp is installed: `python3 -m pytest python_solnp/test/test.py -v`
- Examples are available in `python_solnp/examples/` showing usage of the pysolnp module

## Validation

### Mandatory Testing After Changes
- **ALWAYS run both test suites after making changes to C++ code:**
  ```bash
  cmake .
  make solnp_tests utils_tests  # Can build both simultaneously
  ./solnp_tests -r junit > solnp_tests_result.xml
  ./utils_tests -r junit > utils_tests_result.xml
  ```
- **ALWAYS check test results**: Tests should exit with code 0 and run in <1 second each
- **Manual validation scenarios**: 
  - After changing core SOLNP algorithm (`src/solnp.hpp`), verify basic optimization works by checking test examples
  - After changing utilities (`src/utils.hpp`), run utils_tests specifically
  - Check that any CMakeLists.txt changes don't break the build

### CI Integration
- GitHub Actions runs on every push/PR for:
  - Building Python wheels (Windows, macOS, Linux)
  - CodeCov analysis (only on release branch)
- CodeCov workflow requires: `cmake -DRUN_CODECOV=TRUE . && make solnp_tests utils_tests`
- **NEVER CANCEL: CI builds can take 10+ minutes for wheel building**

## Project Structure

### Key Files and Directories
- `/src/` - Header-only C++ library files:
  - `solnp.hpp` - Main SOLNP algorithm implementation
  - `subnp.hpp` - Sub-problem solver
  - `utils.hpp` - Mathematical utilities (norms, matrix operations)
  - `stdafx.h` - Standard includes
- `/test/` - C++ tests using Catch2 framework
- `/python_solnp/` - Python wrapper using pybind11:
  - `pysolver.cpp` - Python bindings
  - `examples/` - Python usage examples
  - `test/` - Python test suite  
- `/library/` - Git submodules for dependencies:
  - `dlib/` - Mathematical library (matrix operations, optimization)
  - `Catch2/` - C++ testing framework
  - `pybind11/` - Python-C++ bindings
- `setup.py` - Python package build configuration
- `CMakeLists.txt` - Build system configuration

### Build System Details
- Uses CMake with two modes:
  - Default: Builds C++ tests (`BUILD_PYSOLNP=FALSE`)
  - Python: Builds Python module (`BUILD_PYSOLNP=TRUE`)
- Dependencies automatically built as static libraries
- No external BLAS/LAPACK required (uses dlib's built-in implementation)
- CUDA support disabled (not required for this project)

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
```

### Clean Build
```bash
make clean
# Then follow "After Fresh Clone" steps starting from cmake
```

### Development Workflow
- Make changes to source files in `/src/` or test files in `/test/`
- **ALWAYS rebuild and test**:
  ```bash
  make solnp_tests utils_tests    # Only rebuilds what changed
  ./solnp_tests -r junit > solnp_tests_result.xml
  ./utils_tests -r junit > utils_tests_result.xml
  ```
- Verify test results are successful (exit code 0)

### Important Notes
- Header-only library design: Main functionality is in `.hpp` files
- DLIB dependency: Provides matrix operations, no need to implement from scratch
- Git submodules: **ALWAYS** initialize after clone or the build will fail
- Build artifacts: Excluded by .gitignore (CMakeCache.txt, CMakeFiles/, executables, etc.)
- The library implements the SOLNP (Sequential Quadratic Programming) algorithm for constrained nonlinear optimization