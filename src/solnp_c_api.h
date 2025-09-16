#ifndef SOLNP_C_API_H
#define SOLNP_C_API_H

#ifdef __cplusplus
extern "C" {
#endif

// Structure to hold solver results
typedef struct {
    double solve_value;
    double* optimum;
    int optimum_length;
    int converged;
} SolveResultC;

// Function pointer types for callbacks
typedef void (*objective_function_t)(const double* parameters, int param_count, double* result);
typedef void (*constraint_function_t)(const double* parameters, int param_count, double* constraints, int constraint_count);

// Main solver function
SolveResultC* solnp_solve(
    objective_function_t objective_func,
    constraint_function_t constraint_func,
    const double* initial_parameters,
    int param_count,
    const double* parameter_bounds,  // lower_bounds followed by upper_bounds
    const double* constraint_values,
    int constraint_count,
    double rho,
    int max_major_iterations,
    int max_minor_iterations,
    double delta,
    double tolerance
);

// Clean up memory allocated for results
void solnp_free_result(SolveResultC* result);

#ifdef __cplusplus
}
#endif

#endif // SOLNP_C_API_H