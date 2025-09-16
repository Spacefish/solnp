file(REMOVE_RECURSE
  "libsolnp_lib.a"
  "libsolnp_lib.pdb"
)

# Per-language clean rules from dependency scanning.
foreach(lang )
  include(CMakeFiles/solnp_lib.dir/cmake_clean_${lang}.cmake OPTIONAL)
endforeach()
