#include <iostream>
#include "../src/solnp.hpp"

struct rosenbrock_functor {
    template <typename T>
    dlib::matrix<double> operator()(const T& p) const {
        double a = 1.0;
        double b = 100.0;
        double f = (a - p(0)) * (a - p(0)) + b * (p(1) - p(0) * p(0)) * (p(1) - p(0) * p(0));
        dlib::matrix<double> result(1, 1);
        result = f;
        return result;
    }
};

int main() {
    dlib::matrix<double> p(2, 1);
    p = -2, -2;

    dlib::matrix<double> ib;

    cppsolnp::SolveResult result = cppsolnp::solnp(rosenbrock_functor(), p, ib);

    std::cout << "Converged: " << (result.converged ? "True" : "False") << std::endl;
    std::cout << "Optimum: " << dlib::trans(result.optimum) << std::endl;
    std::cout << "Solve Value: " << result.solve_value << std::endl;

    return 0;
}
