#ifndef HBP_CORE_GENERATORS_SURFACE_GENERATOR_H
#define HBP_CORE_GENERATORS_SURFACE_GENERATOR_H

#include "activity_generator.h"

#include "../core/value_types.h"

#include <vector>

namespace hbp::core {

class SurfaceGenerator {
public:
    bool initialize(ActivityGenerator* activity_generator);
    bool compute_main_uv(float cal_min, float cal_max);
    bool compute_activity_uv(int timeline_index, float alpha);
    bool copy_activity_uvs(Vec2* out_uvs, int uv_capacity) const;
    bool copy_alpha_uvs(Vec2* out_uvs, int uv_capacity) const;
    int uv_count() const;

private:
    ActivityGenerator* _activity_generator = nullptr;
    std::vector<float> _surface_values;
    VolumeInterpolation _activity_interpolation = VolumeInterpolation::Nearest;
    VolumeGridLayout _activity_layout;
    std::vector<int> _activity_nearest_indices;
    std::vector<TrilinearVolumeStencil> _activity_trilinear_stencils;
    std::vector<Vec2> _activity_uvs;
    std::vector<Vec2> _alpha_uvs;
};

}

#endif
