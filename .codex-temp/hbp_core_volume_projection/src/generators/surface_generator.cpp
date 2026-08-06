#include "surface_generator.h"

#include "../core/math_utils.h"
#include "../core/parallel_for.h"

#include <algorithm>
#include <cmath>

namespace hbp::core {

bool SurfaceGenerator::initialize(ActivityGenerator* activity_generator)
{
    _activity_generator = activity_generator;
    _surface_values.clear();
    _activity_interpolation = VolumeInterpolation::Nearest;
    _activity_layout = VolumeGridLayout{};
    _activity_nearest_indices.clear();
    _activity_trilinear_stencils.clear();
    _activity_uvs.clear();
    _alpha_uvs.clear();

    if (!_activity_generator || !_activity_generator->generator_surface()) {
        return false;
    }

    const GeneratorSurface* generator_surface = _activity_generator->generator_surface();
    const Surface* surface = generator_surface->surface();
    const Volume* volume = generator_surface->volume();
    if (!surface || !volume) {
        return false;
    }

    const std::vector<Vec3>& vertices = surface->vertices();
    _surface_values.resize(vertices.size(), 0.0f);
    for (std::size_t i = 0; i < vertices.size(); ++i) {
        Vec3 ijk = volume->closest_ijk(vertices[i]);
        _surface_values[i] = volume->voxel_data(truncate_to_int(ijk.x), truncate_to_int(ijk.y), truncate_to_int(ijk.z));
        if (!std::isfinite(_surface_values[i])) {
            return false;
        }
    }

    _activity_interpolation = generator_surface->volume_interpolation();
    _activity_layout = generator_surface->volume_grid_layout();
    if (_activity_interpolation == VolumeInterpolation::Nearest) {
        _activity_nearest_indices.resize(vertices.size(), -1);
        for (std::size_t i = 0; i < vertices.size(); ++i) {
            _activity_nearest_indices[i] = nearest_volume_vertex_index(
                _activity_layout,
                vertices[i]);
        }
    } else {
        _activity_trilinear_stencils.resize(vertices.size());
        for (std::size_t i = 0; i < vertices.size(); ++i) {
            trilinear_volume_stencil(
                _activity_layout,
                vertices[i],
                _activity_trilinear_stencils[i]);
        }
    }
    return true;
}

bool SurfaceGenerator::compute_main_uv(float cal_min, float cal_max)
{
    if (!_activity_generator || !_activity_generator->generator_surface()) {
        return false;
    }

    const GeneratorSurface* generator_surface = _activity_generator->generator_surface();
    const Surface* surface = generator_surface->surface();
    const Volume* volume = generator_surface->volume();
    if (!surface || !volume) {
        return false;
    }

    float diff = volume->extrema().recomputed_cal_max - volume->extrema().recomputed_cal_min;
    float min_scaling_value = volume->extrema().recomputed_cal_min + cal_min * diff;
    float max_scaling_value = volume->extrema().recomputed_cal_min + cal_max * diff;
    float denominator = std::fabs(diff) <= kGeometryEpsilon ? 1.0f : diff;

    std::vector<Vec2> uvs(surface->vertices().size(), make_vec2(0.0f, 0.0f));
    for (std::size_t i = 0; i < _surface_values.size(); ++i) {
        float value = _surface_values[i];
        if (value <= 0.0f) {
            continue;
        }
        value = clamp_value(value, min_scaling_value, max_scaling_value);
        uvs[i] = make_vec2((value - min_scaling_value) / denominator, 1.0f);
    }
    return const_cast<Surface*>(surface)->set_uvs(uvs.data(), static_cast<int>(uvs.size()));
}

bool SurfaceGenerator::compute_activity_uv(int timeline_index, float alpha)
{
    if (!_activity_generator || !_activity_generator->generator_surface() || !_activity_generator->generator_surface()->surface()) {
        return false;
    }

    int vertex_count = static_cast<int>(_activity_generator->generator_surface()->surface()->vertices().size());
    _activity_uvs.assign(static_cast<std::size_t>(vertex_count), make_vec2(0.5f, 1.0f));
    _alpha_uvs.assign(static_cast<std::size_t>(vertex_count), make_vec2(0.01f, 1.0f));

    float alpha_diff = 1.0f - alpha;
    parallel_for(static_cast<std::size_t>(vertex_count), 2048, 256, [&](std::size_t begin, std::size_t end) {
        for (std::size_t i = begin; i < end; ++i) {
            float activity = 0.0f;
            float activity_weight = 0.0f;
            if (_activity_interpolation == VolumeInterpolation::Nearest) {
                int vertex_index = i < _activity_nearest_indices.size()
                    ? _activity_nearest_indices[i]
                    : -1;
                if (vertex_index >= 0) {
                    activity = _activity_generator->activity(vertex_index, timeline_index);
                    activity_weight = _activity_generator->weight(vertex_index, timeline_index);
                }
            } else if (i < _activity_trilinear_stencils.size()) {
                float raw_activity = 0.0f;
                float raw_weight = 0.0f;
                if (sample_trilinear_weighted_volume(
                        _activity_trilinear_stencils[i],
                        _activity_layout,
                        [&](int index) { return _activity_generator->raw_activity(index, timeline_index); },
                        [&](int index) { return _activity_generator->raw_weight(index, timeline_index); },
                        raw_activity,
                        raw_weight)) {
                    activity = _activity_generator->normalize_activity(raw_activity);
                    activity_weight = _activity_generator->normalize_weight(raw_weight, raw_activity);
                }
            }

            if (i < _surface_values.size() && _surface_values[i] > 0.0f && activity_weight > 0.0f) {
                _activity_uvs[i] = make_vec2(activity, 0.0f);
                _alpha_uvs[i] = make_vec2(activity_weight * alpha_diff + alpha, 0.0f);
            }
        }
    });
    return true;
}

bool SurfaceGenerator::copy_activity_uvs(Vec2* out_uvs, int uv_capacity) const
{
    if (!out_uvs || uv_capacity < static_cast<int>(_activity_uvs.size())) {
        return false;
    }
    std::copy(_activity_uvs.begin(), _activity_uvs.end(), out_uvs);
    return true;
}

bool SurfaceGenerator::copy_alpha_uvs(Vec2* out_uvs, int uv_capacity) const
{
    if (!out_uvs || uv_capacity < static_cast<int>(_alpha_uvs.size())) {
        return false;
    }
    std::copy(_alpha_uvs.begin(), _alpha_uvs.end(), out_uvs);
    return true;
}

int SurfaceGenerator::uv_count() const
{
    return static_cast<int>(_activity_uvs.size());
}

}
