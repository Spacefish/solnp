#include "solnp_c_api.h"
#include "solnp.hpp"
#include "stdafx.h"
#include <cstring>
#include <memory>

// Global function pointers to store callbacks
static objective_function_t g_objective_func = nullptr;
static constraint_function_t g_constraint_func = nullptr;
static int g_constraint_count = 0;

// Wrapper function for C++ that calls the C callback
dlib::matrix<double, 0, 1> cpp_objective_wrapper(const dlib::matrix<double, 0, 1>& params) {
    int param_count = params.nr();
    double* param_array = new double[param_count];
    
    // Copy parameters to array
    for (int i = 0; i < param_count; i++) {
        param_array[i] = params(i);
    }
    
    double result;
    g_objective_func(param_array, param_count, &result);
    
    delete[] param_array;
    return dlib::mat(result);
}

// Wrapper function for constraints
dlib::matrix<double, 0, 1> cpp_constraint_wrapper(const dlib::matrix<double, 0, 1>& params) {
    if (g_constraint_func == nullptr || g_constraint_count == 0) {
        return dlib::matrix<double, 0, 1>();
    }
    
    int param_count = params.nr();
    double* param_array = new double[param_count];
    double* constraint_array = new double[g_constraint_count + 1]; // +1 for objective
    
    // Copy parameters to array
    for (int i = 0; i < param_count; i++) {
        param_array[i] = params(i);
    }
    
    // Get objective value
    g_objective_func(param_array, param_count, &constraint_array[0]);
    
    // Get constraint values
    g_constraint_func(param_array, param_count, &constraint_array[1], g_constraint_count);
    
    dlib::matrix<double, 0, 1> result(g_constraint_count + 1);
    for (int i = 0; i < g_constraint_count + 1; i++) {
        result(i) = constraint_array[i];
    }
    
    delete[] param_array;
    delete[] constraint_array;
    return result;
}

extern "C" {

SolveResultC* solnp_solve(
    objective_function_t objective_func,
    constraint_function_t constraint_func,
    const double* initial_parameters,
    int param_count,
    const double* parameter_bounds,
    const double* constraint_values,
    int constraint_count,
    double rho,
    int max_major_iterations,
    int max_minor_iterations,
    double delta,
    double tolerance
) {
    try {
        // Store global callbacks
        g_objective_func = objective_func;
        g_constraint_func = constraint_func;
        g_constraint_count = constraint_count;
        
        // Create parameter matrix with bounds
        dlib::matrix<double> params;
        if (parameter_bounds != nullptr) {
            params.set_size(param_count, 3);
            for (int i = 0; i < param_count; i++) {
                params(i, 0) = initial_parameters[i];           // initial values
                params(i, 1) = parameter_bounds[i];             // lower bounds
                params(i, 2) = parameter_bounds[i + param_count]; // upper bounds
            }
        } else {
            params.set_size(param_count, 1);
            for (int i = 0; i < param_count; i++) {
                params(i, 0) = initial_parameters[i];           // initial values only
            }
        }
        
        cppsolnp::SolveResult result(0.0, dlib::matrix<double, 0, 1>(), false, dlib::matrix<double>());
        
        if (constraint_count > 0) {
            // With constraints
            result = cppsolnp::solnp(cpp_constraint_wrapper, params);
        } else {
            // Without constraints
            result = cppsolnp::solnp(cpp_objective_wrapper, params);
        }
        
        // Allocate and populate result structure
        SolveResultC* c_result = new SolveResultC();
        c_result->solve_value = result.solve_value;
        c_result->optimum_length = result.optimum.nr();
        c_result->optimum = new double[c_result->optimum_length];
        c_result->converged = result.converged ? 1 : 0;
        
        for (int i = 0; i < c_result->optimum_length; i++) {
            c_result->optimum[i] = result.optimum(i);
        }
        
        return c_result;
    } catch (...) {
        return nullptr;
    }
}

void solnp_free_result(SolveResultC* result) {
    if (result != nullptr) {
        delete[] result->optimum;
        delete result;
    }
}

} // extern "C"