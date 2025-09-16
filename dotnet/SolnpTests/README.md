# SOLNP Cross-Implementation Validation Tests

This project contains unit tests that validate the equality between the C++ implementation (in `src/`) and the C# implementation (in `dotnet/solnp/`) of the SOLNP optimization algorithm.

## Overview

The tests compare results from both implementations on identical optimization problems to ensure they produce equivalent results within acceptable numerical tolerances.

## Test Cases

1. **QuadraticFunction_CppAndCSharpProduceSameResults**: Tests a simple quadratic function f(x) = x²
2. **BoxFunction_CppAndCSharpProduceSameResults**: Tests the Box benchmark function with equality constraints
3. **RosenbrockFunction_CppAndCSharpProduceSameResults**: Tests the challenging Rosenbrock function

## Architecture

- **CppSolnpInterop.cs**: P/Invoke wrapper that calls the native C++ library
- **SolnpCrossImplementationTests.cs**: XUnit test cases that compare both implementations
- **libsolnp_native.so**: Native C++ library built from `src/solnp_c_api.cpp`

## Prerequisites

1. Build the native C++ library:
   ```bash
   cd ../../  # Go to repository root
   mkdir -p build && cd build
   cmake .. && make solnp_native
   ```

2. The test project automatically copies the native library to the test output directory.

## Running Tests

From the SolnpTests directory:
```bash
dotnet test
```

## Expected Results

All tests should pass, indicating that both C++ and C# implementations produce equivalent results within numerical tolerances. Small differences are expected due to:

- Different underlying linear algebra libraries (dlib vs MathNet.Numerics)
- Potential floating-point precision differences
- Different internal optimization strategies

The tests verify that both implementations:
- Converge to the same optimization status
- Find solutions within acceptable proximity to each other
- Achieve similar objective function values