#include "hbp_core.h"
#include "../../src/api/native_objects.h"
#include "../../src/core/math_utils.h"

#include <algorithm>
#include <chrono>
#include <cmath>
#include <fstream>
#include <future>
#include <iostream>
#include <limits>
#include <string>
#include <thread>
#include <vector>

namespace {

constexpr float kTolerance = 1e-4f;

bool nearly_equal(float actual, float expected, float tolerance = kTolerance)
{
    return std::fabs(actual - expected) <= tolerance;
}

std::string fixture_path(const std::string& directory, const std::string& name)
{
    return directory + "/" + name;
}

int fail(const std::string& message)
{
    std::cerr << message << '\n';
    return 1;
}

struct GeneratorFixture {
    hbp_Volume* volume = nullptr;
    hbp_Surface* surface = nullptr;
    hbp_GeneratorSurface* generator_surface = nullptr;
    hbp_DensityGenerator* density = nullptr;
    hbp_SurfaceGenerator* surface_generator = nullptr;
    hbp_RawSiteList* sites = nullptr;

    ~GeneratorFixture()
    {
        if (surface_generator) hbp_surface_generator_destroy(surface_generator);
        if (density) hbp_density_generator_destroy(density);
        if (sites) hbp_raw_site_list_destroy(sites);
        if (generator_surface) hbp_generator_surface_destroy(generator_surface);
        if (surface) hbp_surface_destroy(surface);
        if (volume) hbp_volume_destroy(volume);
    }

    bool initialize(const std::string& nifti_path, const std::string& gifti_path, int dimension)
    {
        return hbp_volume_create(&volume) == HBP_OK
            && hbp_volume_load_nifti(volume, nifti_path.c_str()) == HBP_OK
            && hbp_surface_create(&surface) == HBP_OK
            && hbp_surface_load_gifti(surface, gifti_path.c_str()) == HBP_OK
            && hbp_generator_surface_create(&generator_surface) == HBP_OK
            && hbp_generator_surface_initialize(generator_surface, surface, volume, dimension) == HBP_OK
            && hbp_density_generator_create(&density) == HBP_OK
            && hbp_activity_generator_initialize(reinterpret_cast<hbp_ActivityGenerator*>(density), generator_surface) == HBP_OK
            && hbp_surface_generator_create(&surface_generator) == HBP_OK
            && hbp_surface_generator_initialize(surface_generator, reinterpret_cast<hbp_ActivityGenerator*>(density)) == HBP_OK
            && hbp_raw_site_list_create(&sites) == HBP_OK;
    }
};

struct LegacyProjectionResult {
    std::vector<float> values;
    std::vector<float> normalized_weights;
};

float legacy_distance_weight(float distance, float max_distance, int ratio_distance)
{
    if (max_distance <= hbp::core::kGeometryEpsilon) {
        return 0.0f;
    }
    float ratio = std::clamp(1.0f - distance / max_distance, 0.0f, 1.0f);
    switch (ratio_distance) {
    case 1:
        return ratio;
    case 2:
        return ratio * ratio;
    default:
        return 1.0f;
    }
}

LegacyProjectionResult compute_legacy_projection(
    const std::vector<hbp_Vec3>& vertices,
    const std::vector<hbp_Vec3>& sites,
    const std::vector<int>& masks,
    float max_distance,
    const std::vector<float>& amplitudes,
    int timeline_length,
    int ratio_distance)
{
    const std::size_t point_count = vertices.size();
    const std::size_t value_count = static_cast<std::size_t>(timeline_length) * point_count;
    LegacyProjectionResult result;
    result.values.assign(value_count, 0.0f);
    std::vector<float> timeline_weights(value_count, 0.0f);
    const float max_distance_squared = max_distance * max_distance;

    for (std::size_t site = 0; site < sites.size(); ++site) {
        if (masks[site] != 0) {
            continue;
        }
        for (int timeline = 0; timeline < timeline_length; ++timeline) {
            const float amplitude = amplitudes[static_cast<std::size_t>(timeline) * sites.size() + site];
            const std::size_t row_offset = static_cast<std::size_t>(timeline) * point_count;
            for (std::size_t point = 0; point < point_count; ++point) {
                hbp::core::Vec3 difference = hbp::core::subtract(
                    hbp::core::from_hbp_vec3(vertices[point]),
                    hbp::core::from_hbp_vec3(sites[site]));
                float square_distance = hbp::core::square_norm(difference);
                if (max_distance <= hbp::core::kGeometryEpsilon || square_distance > max_distance_squared) {
                    continue;
                }
                float weight = legacy_distance_weight(std::sqrt(square_distance), max_distance, ratio_distance);
                if (weight <= 0.0f) {
                    continue;
                }
                result.values[row_offset + point] += weight * amplitude;
                timeline_weights[row_offset + point] += weight;
            }
        }
    }

    float max_density = 0.0f;
    for (std::size_t i = 0; i < value_count; ++i) {
        if (timeline_weights[i] > hbp::core::kGeometryEpsilon) {
            result.values[i] /= timeline_weights[i];
        }
        max_density = std::max(max_density, timeline_weights[i]);
    }
    if (max_density <= hbp::core::kGeometryEpsilon) {
        max_density = 1.0f;
    }

    result.normalized_weights.resize(value_count);
    for (std::size_t i = 0; i < value_count; ++i) {
        result.normalized_weights[i] = std::clamp(timeline_weights[i] / max_density, 0.0f, 1.0f);
    }
    return result;
}

bool build_generated_vertices(
    const GeneratorFixture& fixture,
    int dimension,
    std::vector<hbp_Vec3>& out_vertices)
{
    hbp_SurfaceSizes surface_sizes{};
    hbp_VolumeDimensions volume_dimensions{};
    hbp_BBox* bounding_box = nullptr;
    hbp_Vec3 min{};
    hbp_Vec3 max{};
    bool ok = hbp_surface_get_sizes(fixture.surface, &surface_sizes) == HBP_OK
        && surface_sizes.vertex_count > 0
        && hbp_volume_get_dimensions(fixture.volume, &volume_dimensions) == HBP_OK
        && hbp_volume_get_bounding_box(fixture.volume, &bounding_box) == HBP_OK
        && hbp_bbox_get_min(bounding_box, &min) == HBP_OK
        && hbp_bbox_get_max(bounding_box, &max) == HBP_OK;
    if (!ok) {
        if (bounding_box) hbp_bbox_destroy(bounding_box);
        return false;
    }

    out_vertices.resize(static_cast<std::size_t>(surface_sizes.vertex_count));
    ok = hbp_surface_copy_vertices(fixture.surface, out_vertices.data(), surface_sizes.vertex_count) == HBP_OK;
    int max_dimension = std::max({volume_dimensions.x, volume_dimensions.y, volume_dimensions.z});
    int count_x = std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.x / max_dimension));
    int count_y = std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.y / max_dimension));
    int count_z = std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.z / max_dimension));
    for (int x = 0; ok && x < count_x; ++x) {
        float px = min.x + (static_cast<float>(x) / static_cast<float>(count_x - 1)) * (max.x - min.x);
        for (int y = 0; y < count_y; ++y) {
            float py = min.y + (static_cast<float>(y) / static_cast<float>(count_y - 1)) * (max.y - min.y);
            for (int z = 0; z < count_z; ++z) {
                float pz = min.z + (static_cast<float>(z) / static_cast<float>(count_z - 1)) * (max.z - min.z);
                out_vertices.push_back(hbp_Vec3{px, py, pz});
            }
        }
    }
    hbp_bbox_destroy(bounding_box);
    return ok;
}

bool compare_ieeg_with_legacy_reference(
    const char* case_name,
    hbp_GeneratorSurface* generator_surface,
    const std::vector<hbp_Vec3>& generated_vertices,
    const std::vector<hbp_Vec3>& site_positions,
    const std::vector<int>& masks,
    float max_distance,
    const std::vector<float>& amplitudes,
    int timeline_length,
    int ratio_distance,
    int worker_count = 0,
    int neighbor_batch_size = 0,
    int repetition_count = 1)
{
    hbp_RawSiteList* sites = nullptr;
    hbp_IEEGGenerator* generator = nullptr;
    bool ok = masks.size() == site_positions.size()
        && amplitudes.size() == static_cast<std::size_t>(timeline_length) * site_positions.size()
        && hbp_raw_site_list_create(&sites) == HBP_OK;
    for (int i = 0; ok && i < static_cast<int>(site_positions.size()); ++i) {
        std::string name = "S" + std::to_string(i);
        ok = hbp_raw_site_list_add_site(sites, name.c_str(), &site_positions[static_cast<std::size_t>(i)], 0, i) == HBP_OK
            && hbp_raw_site_list_update_mask(sites, i, masks[static_cast<std::size_t>(i)]) == HBP_OK;
    }
    ok = ok
        && hbp_ieeg_generator_create(&generator) == HBP_OK
        && hbp_activity_generator_initialize(reinterpret_cast<hbp_ActivityGenerator*>(generator), generator_surface) == HBP_OK
        && hbp_ieeg_generator_set_parallel_options(generator, worker_count, neighbor_batch_size) == HBP_OK
        && hbp_ieeg_generator_enable_performance_metrics(generator, 1) == HBP_OK
        && repetition_count > 0;

    hbp_IEEGComputeMetrics metrics{};
    LegacyProjectionResult expected = compute_legacy_projection(
        generated_vertices,
        site_positions,
        masks,
        max_distance,
        amplitudes,
        timeline_length,
        ratio_distance);
    for (int repetition = 0; ok && repetition < repetition_count; ++repetition) {
        ok = hbp_ieeg_generator_compute_activity_from_sites(
                generator,
                sites,
                max_distance,
                amplitudes.empty() ? nullptr : amplitudes.data(),
                timeline_length,
                ratio_distance) == HBP_OK
            && hbp_ieeg_generator_get_last_compute_metrics(generator, &metrics) == HBP_OK
            && metrics.generated_point_count == static_cast<std::int64_t>(generated_vertices.size())
            && metrics.stored_value_count == static_cast<std::int64_t>(generated_vertices.size()) * timeline_length
            && metrics.stored_weight_count == static_cast<std::int64_t>(generated_vertices.size())
            && metrics.parallel_worker_count >= 1
            && (worker_count == 0 || metrics.parallel_worker_count <= worker_count)
            && metrics.neighbor_batch_size >= 1
            && metrics.temporary_neighbor_budget_bytes == 64 * 1024 * 1024
            && metrics.temporary_neighbor_peak_bytes <= metrics.temporary_neighbor_budget_bytes * 2;
        if (!ok) {
            break;
        }
        const hbp::core::ActivityGenerator& actual =
            reinterpret_cast<const hbp_ActivityGenerator*>(generator)->activity();
        for (int timeline = 0; ok && timeline < timeline_length; ++timeline) {
            for (int point = 0; point < static_cast<int>(generated_vertices.size()); ++point) {
                std::size_t offset = static_cast<std::size_t>(timeline) * generated_vertices.size()
                    + static_cast<std::size_t>(point);
                float actual_value = actual.raw_activity(point, timeline);
                float actual_weight = actual.weight(point, timeline);
                if (actual_value != expected.values[offset]
                    || actual_weight != expected.normalized_weights[offset]) {
                    std::cerr << case_name << " mismatch at timeline " << timeline
                              << ", point " << point
                              << ": value " << actual_value << " != " << expected.values[offset]
                              << ", weight " << actual_weight << " != " << expected.normalized_weights[offset]
                              << '\n';
                    ok = false;
                    break;
                }
            }
        }
    }

    if (generator) hbp_ieeg_generator_destroy(generator);
    if (sites) hbp_raw_site_list_destroy(sites);
    return ok;
}

int test_plane_normalization()
{
    hbp_Vec3 point{1.0f, 2.0f, 3.0f};
    hbp_Vec3 normal{0.0f, 3.0f, 4.0f};
    hbp_Plane3* plane = nullptr;
    if (hbp_plane_normalize(nullptr) != HBP_INVALID_HANDLE
        || hbp_plane_create(&point, &normal, &plane) != HBP_OK
        || !plane
        || hbp_plane_normalize(plane) != HBP_OK) {
        hbp_plane_destroy(plane);
        return fail("hbp_plane_normalize ABI contract failed");
    }

    hbp_Vec3 source{1.0f, 5.0f, 7.0f};
    hbp_Vec3 projected{};
    int side = 0;
    bool ok = hbp_plane_project_point(plane, &source, &projected) == HBP_OK
        && hbp_plane_point_side(plane, &source, &side) == HBP_OK
        && side == 1
        && nearly_equal(projected.x, 1.0f)
        && nearly_equal(projected.y, 2.0f)
        && nearly_equal(projected.z, 3.0f);
    hbp_plane_destroy(plane);
    return ok ? 0 : fail("Normalized plane produced an unexpected projection");
}

int test_empty_inputs(const std::string& nifti_path)
{
    hbp_Volume* empty_volume = nullptr;
    hbp_Volume* loaded_volume = nullptr;
    hbp_Surface* empty_surface = nullptr;
    hbp_GeneratorSurface* generator_surface = nullptr;
    bool created = hbp_volume_create(&empty_volume) == HBP_OK
        && hbp_volume_create(&loaded_volume) == HBP_OK
        && hbp_volume_load_nifti(loaded_volume, nifti_path.c_str()) == HBP_OK
        && hbp_surface_create(&empty_surface) == HBP_OK
        && hbp_generator_surface_create(&generator_surface) == HBP_OK;
    bool rejected = created
        && hbp_generator_surface_set_volume_interpolation(nullptr, HBP_VOLUME_INTERPOLATION_NEAREST) == HBP_INVALID_HANDLE
        && hbp_generator_surface_set_volume_interpolation(
            generator_surface, static_cast<HBP_VolumeInterpolation>(99)) == HBP_INVALID_ARGUMENT
        && hbp_generator_surface_set_volume_interpolation(
            generator_surface, HBP_VOLUME_INTERPOLATION_NEAREST) == HBP_OK
        && hbp_generator_surface_set_volume_interpolation(
            generator_surface, HBP_VOLUME_INTERPOLATION_TRILINEAR) == HBP_OK
        && hbp_generator_surface_initialize(generator_surface, empty_surface, loaded_volume, 8) == HBP_ERROR
        && hbp_generator_surface_initialize(generator_surface, empty_surface, empty_volume, 8) == HBP_ERROR;
    if (generator_surface) hbp_generator_surface_destroy(generator_surface);
    if (empty_surface) hbp_surface_destroy(empty_surface);
    if (loaded_volume) hbp_volume_destroy(loaded_volume);
    if (empty_volume) hbp_volume_destroy(empty_volume);
    return rejected ? 0 : fail("Empty surface or volume was not rejected by GeneratorSurface");
}

int test_density_progress_maximum_and_main_uv(const std::string& nifti_path, const std::string& gifti_path)
{
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, 48)) {
        return fail("Could not initialize the native generator fixture");
    }

    hbp_ActivityGenerator* activity = reinterpret_cast<hbp_ActivityGenerator*>(fixture.density);
    float progress = -1.0f;
    float max_density = -1.0f;
    if (hbp_activity_generator_get_progress(nullptr, &progress) != HBP_INVALID_HANDLE
        || hbp_activity_generator_get_progress(activity, nullptr) != HBP_INVALID_ARGUMENT
        || hbp_density_generator_get_max_density(nullptr, &max_density) != HBP_INVALID_HANDLE
        || hbp_density_generator_get_max_density(fixture.density, nullptr) != HBP_INVALID_ARGUMENT
        || hbp_activity_generator_get_progress(activity, &progress) != HBP_OK
        || !nearly_equal(progress, 0.0f)
        || hbp_density_generator_compute_activity_from_sites(fixture.density, fixture.sites, 10.0f, 0) != HBP_OK
        || hbp_activity_generator_get_progress(activity, &progress) != HBP_OK
        || !nearly_equal(progress, 1.0f)
        || hbp_density_generator_get_max_density(fixture.density, &max_density) != HBP_OK
        || !nearly_equal(max_density, 0.0f)) {
        return fail("Empty-site progress or maximum-density contract failed");
    }

    hbp_SurfaceSizes sizes{};
    if (hbp_surface_get_sizes(fixture.surface, &sizes) != HBP_OK || sizes.vertex_count <= 0) {
        return fail("Generator surface fixture has no vertices");
    }
    std::vector<hbp_Vec3> vertices(static_cast<std::size_t>(sizes.vertex_count));
    if (hbp_surface_copy_vertices(fixture.surface, vertices.data(), sizes.vertex_count) != HBP_OK) {
        return fail("Could not copy generator fixture vertices");
    }

    hbp_Vec3 site = vertices.front();
    for (const hbp_Vec3& vertex : vertices) {
        float sampled = 0.0f;
        if (hbp_volume_sample_value(fixture.volume, &vertex, &sampled) == HBP_OK && sampled > 0.0f) {
            site = vertex;
            break;
        }
    }

    constexpr int site_count = 2048;
    for (int i = 0; i < site_count; ++i) {
        std::string name = "S" + std::to_string(i);
        if (hbp_raw_site_list_add_site(fixture.sites, name.c_str(), &site, 0, i) != HBP_OK) {
            return fail("Could not populate the progress fixture sites");
        }
    }

    if (hbp_density_generator_compute_activity_from_sites(fixture.density, fixture.sites, 1000.0f, 0) != HBP_OK
        || hbp_density_generator_get_max_density(fixture.density, &max_density) != HBP_OK
        || !nearly_equal(max_density, 0.0f)) {
        return fail("All-masked density input did not produce zero density");
    }
    for (int i = 0; i < site_count; ++i) {
        if (hbp_raw_site_list_update_mask(fixture.sites, i, 0) != HBP_OK) {
            return fail("Could not unmask the progress fixture sites");
        }
    }

    auto computation = std::async(std::launch::async, [&]() {
        return hbp_density_generator_compute_activity_from_sites(fixture.density, fixture.sites, 1000.0f, 0);
    });
    bool observed_running_progress = false;
    float previous = 0.0f;
    while (computation.wait_for(std::chrono::milliseconds(0)) != std::future_status::ready) {
        if (hbp_activity_generator_get_progress(activity, &progress) != HBP_OK
            || progress < -kTolerance || progress > 1.0f + kTolerance) {
            return fail("Concurrent progress read returned an invalid value");
        }
        if (progress < 1.0f) {
            observed_running_progress = true;
            if (progress + kTolerance < previous) {
                return fail("Activity progress decreased during a calculation");
            }
            previous = progress;
        }
        std::this_thread::yield();
    }
    if (computation.get() != HBP_OK
        || hbp_activity_generator_get_progress(activity, &progress) != HBP_OK
        || !nearly_equal(progress, 1.0f)
        || !observed_running_progress
        || hbp_density_generator_get_max_density(fixture.density, &max_density) != HBP_OK
        || !nearly_equal(max_density, static_cast<float>(site_count))) {
        return fail(
            "Concurrent progress or repeated maximum-density result failed: observed="
            + std::to_string(observed_running_progress)
            + " progress=" + std::to_string(progress)
            + " max=" + std::to_string(max_density));
    }

    if (hbp_surface_generator_compute_main_uv(fixture.surface_generator, 0.25f, 0.75f) != HBP_OK
        || hbp_surface_generator_compute_main_uv(fixture.surface_generator, std::numeric_limits<float>::quiet_NaN(), 0.75f) != HBP_INVALID_ARGUMENT
        || hbp_surface_get_sizes(fixture.surface, &sizes) != HBP_OK
        || sizes.uv_count != sizes.vertex_count) {
        return fail("SurfaceGenerator main-UV ABI call failed");
    }

    std::vector<hbp_Vec2> uvs(static_cast<std::size_t>(sizes.uv_count));
    hbp_VolumeExtrema extrema{};
    if (hbp_surface_copy_uvs(fixture.surface, uvs.data(), sizes.uv_count) != HBP_OK
        || hbp_volume_get_extrema(fixture.volume, &extrema) != HBP_OK) {
        return fail("Could not read main UV results");
    }
    float diff = extrema.recomputed_cal_max - extrema.recomputed_cal_min;
    float expected_min = extrema.recomputed_cal_min + 0.25f * diff;
    float expected_max = extrema.recomputed_cal_min + 0.75f * diff;
    bool observed_positive = false;
    for (int i = 0; i < sizes.vertex_count; ++i) {
        float value = 0.0f;
        if (hbp_volume_sample_value(fixture.volume, &vertices[static_cast<std::size_t>(i)], &value) != HBP_OK) {
            return fail("Could not sample the main-UV oracle value");
        }
        hbp_Vec2 expected{};
        if (value > 0.0f) {
            observed_positive = true;
            value = std::clamp(value, expected_min, expected_max);
            expected = hbp_Vec2{(value - expected_min) / diff, 1.0f};
        }
        const hbp_Vec2& actual = uvs[static_cast<std::size_t>(i)];
        if (!nearly_equal(actual.x, expected.x) || !nearly_equal(actual.y, expected.y)) {
            return fail("SurfaceGenerator main UV differs from the independent volume oracle");
        }
    }
    return observed_positive ? 0 : fail("Main-UV fixture did not exercise positive MRI values");
}

int test_ieeg_performance_metrics(const std::string& nifti_path, const std::string& gifti_path)
{
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, 8)) {
        return fail("Could not initialize the iEEG metrics fixture");
    }

    hbp_SurfaceSizes sizes{};
    if (hbp_surface_get_sizes(fixture.surface, &sizes) != HBP_OK || sizes.vertex_count <= 0) {
        return fail("IEEG metrics fixture has no surface vertices");
    }
    std::vector<hbp_Vec3> vertices(static_cast<std::size_t>(sizes.vertex_count));
    if (hbp_surface_copy_vertices(fixture.surface, vertices.data(), sizes.vertex_count) != HBP_OK
        || hbp_raw_site_list_add_site(fixture.sites, "S0", &vertices.front(), 0, 0) != HBP_OK
        || hbp_raw_site_list_update_mask(fixture.sites, 0, 0) != HBP_OK) {
        return fail("Could not populate the iEEG metrics fixture");
    }

    hbp_IEEGGenerator* ieeg = nullptr;
    hbp_IEEGComputeMetrics metrics{};
    const float activity[] = {1.0f, -0.5f, 0.25f};
    bool ok = hbp_ieeg_generator_get_last_compute_metrics(nullptr, &metrics) == HBP_INVALID_HANDLE
        && hbp_ieeg_generator_enable_performance_metrics(nullptr, 1) == HBP_INVALID_HANDLE
        && hbp_ieeg_generator_set_parallel_options(nullptr, 0, 0) == HBP_INVALID_HANDLE
        && hbp_ieeg_generator_create(&ieeg) == HBP_OK
        && hbp_ieeg_generator_set_parallel_options(ieeg, -1, 0) == HBP_INVALID_ARGUMENT
        && hbp_ieeg_generator_set_parallel_options(ieeg, 17, 0) == HBP_INVALID_ARGUMENT
        && hbp_ieeg_generator_set_parallel_options(ieeg, 0, -1) == HBP_INVALID_ARGUMENT
        && hbp_ieeg_generator_set_parallel_options(ieeg, 4, 8) == HBP_OK
        && hbp_ieeg_generator_get_last_compute_metrics(ieeg, nullptr) == HBP_INVALID_ARGUMENT
        && hbp_activity_generator_initialize(reinterpret_cast<hbp_ActivityGenerator*>(ieeg), fixture.generator_surface) == HBP_OK
        && hbp_ieeg_generator_enable_performance_metrics(ieeg, 1) == HBP_OK
        && hbp_ieeg_generator_compute_activity_from_sites(ieeg, fixture.sites, 1000.0f, activity, 3, 0) == HBP_OK
        && hbp_ieeg_generator_get_last_compute_metrics(ieeg, &metrics) == HBP_OK
        && metrics.total_milliseconds > 0.0
        && metrics.allocation_milliseconds >= 0.0
        && metrics.spatial_index_milliseconds >= 0.0
        && metrics.spatial_index_build_milliseconds > 0.0
        && metrics.spatial_index_lookup_milliseconds == 0.0
        && metrics.neighbor_query_milliseconds >= 0.0
        && metrics.accumulation_milliseconds >= 0.0
        && metrics.normalization_milliseconds >= 0.0
        && metrics.generated_point_count > sizes.vertex_count
        && metrics.active_site_count == 1
        && metrics.neighbor_link_count > 0
        && metrics.stored_value_count == metrics.generated_point_count * 3
        && metrics.stored_weight_count == metrics.generated_point_count
        && metrics.spatial_index_cache_hit_count == 0
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 1
        && metrics.spatial_index_cache_bytes > 0
        && metrics.spatial_index_geometry_version > 0
        && metrics.parallel_worker_count >= 1
        && metrics.parallel_worker_count <= 4
        && metrics.neighbor_batch_size >= 1
        && metrics.neighbor_batch_size <= 8
        && metrics.neighbor_batch_count == 1
        && metrics.temporary_neighbor_peak_bytes > 0
        && metrics.temporary_neighbor_peak_bytes <= metrics.temporary_neighbor_budget_bytes * 2
        && metrics.temporary_neighbor_budget_bytes == 64 * 1024 * 1024
        && metrics.timeline_length == 3
        && hbp_ieeg_generator_enable_performance_metrics(ieeg, 0) == HBP_OK
        && hbp_ieeg_generator_compute_activity_from_sites(ieeg, fixture.sites, 1000.0f, activity, 3, 0) == HBP_OK
        && hbp_ieeg_generator_get_last_compute_metrics(ieeg, &metrics) == HBP_OK
        && metrics.total_milliseconds == 0.0;

    if (ieeg) hbp_ieeg_generator_destroy(ieeg);
    return ok ? 0 : fail("IEEG opt-in performance metrics contract failed");
}

int test_ieeg_parallel_determinism(const std::string& nifti_path, const std::string& gifti_path)
{
    constexpr int dimension = 16;
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, dimension)) {
        return fail("Could not initialize the parallel iEEG fixture");
    }

    std::vector<hbp_Vec3> generated_vertices;
    if (!build_generated_vertices(fixture, dimension, generated_vertices) || generated_vertices.size() < 32) {
        return fail("Could not reconstruct points for the parallel iEEG fixture");
    }

    constexpr int site_count = 23;
    constexpr int timeline_length = 17;
    std::vector<hbp_Vec3> sites;
    std::vector<int> masks;
    sites.reserve(site_count);
    masks.reserve(site_count);
    for (int site = 0; site < site_count; ++site) {
        const std::size_t point = static_cast<std::size_t>(site + 1) * (generated_vertices.size() - 1)
            / static_cast<std::size_t>(site_count + 1);
        sites.push_back(generated_vertices[point]);
        masks.push_back(site % 7 == 0 ? 1 : 0);
    }
    std::vector<float> amplitudes(static_cast<std::size_t>(site_count * timeline_length));
    for (int timeline = 0; timeline < timeline_length; ++timeline) {
        for (int site = 0; site < site_count; ++site) {
            amplitudes[static_cast<std::size_t>(timeline * site_count + site)] =
                static_cast<float>((timeline + 1) * (site - 9)) / 37.0f;
        }
    }

    bool ok = true;
    for (int worker_count : {1, 2, 4, 8, 16}) {
        ok = ok && compare_ieeg_with_legacy_reference(
            "parallel worker-count determinism",
            fixture.generator_surface,
            generated_vertices,
            sites,
            masks,
            1000.0f,
            amplitudes,
            timeline_length,
            2,
            worker_count,
            7,
            worker_count == 8 ? 3 : 1);
    }

    std::vector<float> short_amplitudes(static_cast<std::size_t>(site_count), 0.75f);
    ok = ok && compare_ieeg_with_legacy_reference(
        "parallel timeline 1 and automatic batch",
        fixture.generator_surface,
        generated_vertices,
        sites,
        masks,
        1000.0f,
        short_amplitudes,
        1,
        1,
        16,
        0,
        2);

    constexpr int long_timeline = 65;
    std::vector<float> long_amplitudes(static_cast<std::size_t>(site_count * long_timeline));
    for (std::size_t index = 0; index < long_amplitudes.size(); ++index) {
        long_amplitudes[index] = static_cast<float>(static_cast<int>(index % 31) - 15) / 19.0f;
    }
    ok = ok && compare_ieeg_with_legacy_reference(
        "parallel long timeline and oversized requested batch",
        fixture.generator_surface,
        generated_vertices,
        sites,
        masks,
        1000.0f,
        long_amplitudes,
        long_timeline,
        0,
        16,
        1000000,
        2);

    return ok ? 0 : fail("Parallel iEEG result, determinism, worker count, or memory bound failed");
}

int test_spatial_index_cache_lifecycle(const std::string& nifti_path, const std::string& gifti_path)
{
    constexpr int initial_dimension = 8;
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, initial_dimension)) {
        return fail("Could not initialize the spatial-index cache fixture");
    }

    std::vector<hbp_Vec3> generated_vertices;
    if (!build_generated_vertices(fixture, initial_dimension, generated_vertices) || generated_vertices.empty()) {
        return fail("Could not reconstruct points for the spatial-index cache fixture");
    }

    const hbp_Vec3 first_position = generated_vertices.front();
    const hbp_Vec3 moved_position = generated_vertices.back();
    if (hbp_raw_site_list_add_site(fixture.sites, "S0", &first_position, 0, 0) != HBP_OK
        || hbp_raw_site_list_update_mask(fixture.sites, 0, 0) != HBP_OK) {
        return fail("Could not populate the spatial-index cache fixture");
    }

    hbp_RawSiteList* moved_sites = nullptr;
    hbp_IEEGGenerator* ieeg = nullptr;
    bool initialized = hbp_raw_site_list_create(&moved_sites) == HBP_OK
        && hbp_raw_site_list_add_site(moved_sites, "Moved", &moved_position, 0, 0) == HBP_OK
        && hbp_raw_site_list_update_mask(moved_sites, 0, 0) == HBP_OK
        && hbp_ieeg_generator_create(&ieeg) == HBP_OK
        && hbp_activity_generator_initialize(
            reinterpret_cast<hbp_ActivityGenerator*>(ieeg), fixture.generator_surface) == HBP_OK
        && hbp_ieeg_generator_enable_performance_metrics(ieeg, 1) == HBP_OK;
    if (!initialized) {
        if (ieeg) hbp_ieeg_generator_destroy(ieeg);
        if (moved_sites) hbp_raw_site_list_destroy(moved_sites);
        return fail("Could not initialize the spatial-index cache generator");
    }

    auto compute = [&](hbp_RawSiteList* sites, float radius, const float* amplitudes, hbp_IEEGComputeMetrics& metrics) {
        return hbp_ieeg_generator_compute_activity_from_sites(ieeg, sites, radius, amplitudes, 2, 1) == HBP_OK
            && hbp_ieeg_generator_get_last_compute_metrics(ieeg, &metrics) == HBP_OK;
    };
    auto cache_is_bounded = [](const hbp_IEEGComputeMetrics& metrics) {
        return metrics.spatial_index_cache_entry_count >= 1
            && metrics.spatial_index_cache_entry_count <= 2
            && metrics.spatial_index_cache_bytes > 0;
    };

    const float first_amplitudes[] = {1.0f, -0.5f};
    const float changed_amplitudes[] = {2.0f, -3.0f};
    hbp_IEEGComputeMetrics metrics{};
    bool ok = compute(fixture.sites, 10.0f, first_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 0
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 1
        && metrics.spatial_index_build_milliseconds > 0.0
        && metrics.spatial_index_lookup_milliseconds == 0.0
        && cache_is_bounded(metrics);
    const std::int64_t initial_geometry_version = metrics.spatial_index_geometry_version;

    const hbp::core::ActivityGenerator& activity =
        reinterpret_cast<const hbp_ActivityGenerator*>(ieeg)->activity();
    ok = ok
        && activity.raw_activity(0, 0) == first_amplitudes[0]
        && activity.raw_activity(0, 1) == first_amplitudes[1]
        && compute(fixture.sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 1
        && metrics.spatial_index_cache_miss_count == 0
        && metrics.spatial_index_build_milliseconds == 0.0
        && metrics.spatial_index_lookup_milliseconds >= 0.0
        && activity.raw_activity(0, 0) == changed_amplitudes[0]
        && activity.raw_activity(0, 1) == changed_amplitudes[1];

    ok = ok
        && hbp_raw_site_list_update_mask(fixture.sites, 0, 1) == HBP_OK
        && compute(fixture.sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 1
        && activity.raw_activity(0, 0) == 0.0f
        && activity.weight(0, 0) == 0.0f
        && hbp_raw_site_list_update_mask(fixture.sites, 0, 0) == HBP_OK
        && compute(moved_sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 1
        && activity.raw_activity(static_cast<int>(generated_vertices.size() - 1), 0) == changed_amplitudes[0]
        && activity.raw_activity(static_cast<int>(generated_vertices.size() - 1), 1) == changed_amplitudes[1];

    ok = ok
        && compute(moved_sites, 20.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 2
        && compute(moved_sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 1
        && compute(moved_sites, 30.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 2
        && compute(moved_sites, 20.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 2;

    std::int64_t cycle_memory_bound = metrics.spatial_index_cache_bytes;
    for (int cycle = 0; ok && cycle < 30; ++cycle) {
        float radius = cycle % 3 == 0 ? 10.0f : (cycle % 3 == 1 ? 20.0f : 30.0f);
        ok = compute(moved_sites, radius, changed_amplitudes, metrics)
            && cache_is_bounded(metrics);
        cycle_memory_bound = std::max(cycle_memory_bound, metrics.spatial_index_cache_bytes);
    }
    const std::int64_t point_scaled_safety_bound = metrics.generated_point_count * 512;
    ok = ok
        && cycle_memory_bound <= point_scaled_safety_bound
        && metrics.spatial_index_cache_entry_count == 2;

    ok = ok
        && hbp_generator_surface_initialize(
            fixture.generator_surface, fixture.surface, fixture.volume, initial_dimension + 2) == HBP_OK
        && compute(moved_sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_hit_count == 0
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 1
        && metrics.spatial_index_geometry_version > initial_geometry_version;
    std::int64_t version_after_dimension_change = metrics.spatial_index_geometry_version;

    hbp_Surface* other_surface = nullptr;
    ok = ok
        && hbp_surface_clone(fixture.surface, &other_surface) == HBP_OK
        && hbp_generator_surface_initialize(
            fixture.generator_surface, other_surface, fixture.volume, initial_dimension + 2) == HBP_OK
        && compute(moved_sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 1
        && metrics.spatial_index_geometry_version > version_after_dimension_change;
    std::int64_t version_after_surface_change = metrics.spatial_index_geometry_version;

    hbp_Volume* other_volume = nullptr;
    ok = ok
        && hbp_volume_create(&other_volume) == HBP_OK
        && hbp_volume_load_nifti(other_volume, nifti_path.c_str()) == HBP_OK
        && hbp_generator_surface_initialize(
            fixture.generator_surface, other_surface, other_volume, initial_dimension + 2) == HBP_OK
        && compute(moved_sites, 10.0f, changed_amplitudes, metrics)
        && metrics.spatial_index_cache_miss_count == 1
        && metrics.spatial_index_cache_entry_count == 1
        && metrics.spatial_index_geometry_version > version_after_surface_change;

    if (other_volume) hbp_volume_destroy(other_volume);
    if (other_surface) hbp_surface_destroy(other_surface);
    hbp_ieeg_generator_destroy(ieeg);
    hbp_raw_site_list_destroy(moved_sites);
    return ok ? 0 : fail("Spatial-index cache hit, LRU bound, invalidation, or result contract failed");
}

int test_ieeg_unique_weights_match_legacy_storage(const std::string& nifti_path, const std::string& gifti_path)
{
    constexpr int dimension = 8;
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, dimension)) {
        return fail("Could not initialize the iEEG storage-parity fixture");
    }

    std::vector<hbp_Vec3> generated_vertices;
    if (!build_generated_vertices(fixture, dimension, generated_vertices) || generated_vertices.size() < 2) {
        return fail("Could not reconstruct all generated points for the legacy iEEG oracle");
    }

    hbp_IEEGGenerator* invalid_generator = nullptr;
    bool zero_timeline_rejected = hbp_ieeg_generator_create(&invalid_generator) == HBP_OK
        && hbp_activity_generator_initialize(
            reinterpret_cast<hbp_ActivityGenerator*>(invalid_generator), fixture.generator_surface) == HBP_OK
        && hbp_ieeg_generator_compute_activity_from_sites(
            invalid_generator, fixture.sites, 10.0f, nullptr, 0, 0) == HBP_INVALID_ARGUMENT;
    if (invalid_generator) hbp_ieeg_generator_destroy(invalid_generator);
    if (!zero_timeline_rejected) {
        return fail("IEEG timeline length zero was not rejected");
    }

    bool ok = compare_ieeg_with_legacy_reference(
        "no sites, timeline 1",
        fixture.generator_surface,
        generated_vertices,
        {},
        {},
        10.0f,
        {},
        1,
        0);

    const hbp_Vec3 overlap = generated_vertices.front();
    ok = ok && compare_ieeg_with_legacy_reference(
        "all sites masked, timeline 3",
        fixture.generator_surface,
        generated_vertices,
        {overlap, overlap},
        {1, 1},
        1000.0f,
        {1.0f, -2.0f, 0.5f, 0.25f, 3.0f, -1.0f},
        3,
        1);

    ok = ok && compare_ieeg_with_legacy_reference(
        "overlapping sites, timeline 3",
        fixture.generator_surface,
        generated_vertices,
        {overlap, overlap},
        {0, 0},
        1000.0f,
        {1.0f, -2.0f, 0.5f, 0.25f, 3.0f, -1.0f},
        3,
        2);

    ok = ok && compare_ieeg_with_legacy_reference(
        "zero radius",
        fixture.generator_surface,
        generated_vertices,
        {overlap},
        {0},
        0.0f,
        {1.0f, -1.0f},
        2,
        0);

    std::size_t boundary_index = 1;
    while (boundary_index < generated_vertices.size()) {
        hbp::core::Vec3 difference = hbp::core::subtract(
            hbp::core::from_hbp_vec3(generated_vertices[boundary_index]),
            hbp::core::from_hbp_vec3(overlap));
        if (hbp::core::square_norm(difference) > hbp::core::kGeometryEpsilon) {
            break;
        }
        ++boundary_index;
    }
    if (boundary_index == generated_vertices.size()) {
        return fail("IEEG radius-boundary fixture has no distinct point");
    }
    hbp::core::Vec3 boundary_difference = hbp::core::subtract(
        hbp::core::from_hbp_vec3(generated_vertices[boundary_index]),
        hbp::core::from_hbp_vec3(overlap));
    float boundary_radius = std::sqrt(hbp::core::square_norm(boundary_difference));
    ok = ok && compare_ieeg_with_legacy_reference(
        "point exactly on constant-radius boundary",
        fixture.generator_surface,
        generated_vertices,
        {overlap},
        {0},
        boundary_radius,
        {0.75f},
        1,
        0);

    return ok ? 0 : fail("Unique iEEG weights or contiguous values differ from legacy storage");
}

int test_surface_activity_uses_trilinear_volume(
    const std::string& nifti_path,
    const std::string& gifti_path)
{
    constexpr int dimension = 8;
    constexpr float alpha = 0.2f;
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, dimension)) {
        return fail("Could not initialize the surface-volume projection fixture");
    }

    hbp_SurfaceSizes sizes{};
    hbp_VolumeDimensions volume_dimensions{};
    hbp_BBox* bounding_box = nullptr;
    hbp_Vec3 native_min{};
    hbp_Vec3 native_max{};
    bool layout_ready = hbp_surface_get_sizes(fixture.surface, &sizes) == HBP_OK
        && sizes.vertex_count > 0
        && hbp_volume_get_dimensions(fixture.volume, &volume_dimensions) == HBP_OK
        && hbp_volume_get_bounding_box(fixture.volume, &bounding_box) == HBP_OK
        && hbp_bbox_get_min(bounding_box, &native_min) == HBP_OK
        && hbp_bbox_get_max(bounding_box, &native_max) == HBP_OK;
    int max_dimension = std::max({volume_dimensions.x, volume_dimensions.y, volume_dimensions.z});
    const hbp::core::VolumeGridLayout layout{
        hbp::core::from_hbp_vec3(native_min),
        hbp::core::from_hbp_vec3(native_max),
        std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.x / max_dimension)),
        std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.y / max_dimension)),
        std::max(2, static_cast<int>(static_cast<float>(dimension) * volume_dimensions.z / max_dimension)),
        sizes.vertex_count};
    if (bounding_box) hbp_bbox_destroy(bounding_box);
    hbp_Vec3 first_site = hbp::core::to_hbp_vec3(layout.min);
    hbp_Vec3 second_site = hbp::core::to_hbp_vec3(layout.max);
    hbp_IEEGGenerator* ieeg = nullptr;
    const float amplitudes[] = {-0.75f, 0.9f};
    bool ok = layout_ready
        && max_dimension > 0
        && hbp_generator_surface_set_volume_interpolation(
            fixture.generator_surface,
            HBP_VOLUME_INTERPOLATION_TRILINEAR) == HBP_OK
        && hbp_raw_site_list_add_site(fixture.sites, "Min", &first_site, 0, 0) == HBP_OK
        && hbp_raw_site_list_add_site(fixture.sites, "Max", &second_site, 0, 1) == HBP_OK
        && hbp_raw_site_list_update_mask(fixture.sites, 0, 0) == HBP_OK
        && hbp_raw_site_list_update_mask(fixture.sites, 1, 0) == HBP_OK
        && hbp_ieeg_generator_create(&ieeg) == HBP_OK
        && hbp_activity_generator_initialize(
            reinterpret_cast<hbp_ActivityGenerator*>(ieeg),
            fixture.generator_surface) == HBP_OK
        && hbp_ieeg_generator_compute_activity_from_sites(
            ieeg,
            fixture.sites,
            1000.0f,
            amplitudes,
            1,
            1) == HBP_OK
        && hbp_ieeg_generator_adjust_values(ieeg, 0.0f, -1.0f, 1.0f) == HBP_OK
        && hbp_surface_generator_initialize(
            fixture.surface_generator,
            reinterpret_cast<hbp_ActivityGenerator*>(ieeg)) == HBP_OK
        && hbp_surface_generator_compute_activity_uv(
            fixture.surface_generator,
            0,
            alpha) == HBP_OK;

    std::vector<hbp_Vec3> vertices(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    std::vector<hbp_Vec2> activity_uv(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    std::vector<hbp_Vec2> alpha_uv(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    ok = ok
        && hbp_surface_copy_vertices(fixture.surface, vertices.data(), sizes.vertex_count) == HBP_OK
        && hbp_surface_generator_copy_activity_uvs(
            fixture.surface_generator,
            activity_uv.data(),
            sizes.vertex_count) == HBP_OK
        && hbp_surface_generator_copy_alpha_uvs(
            fixture.surface_generator,
            alpha_uv.data(),
            sizes.vertex_count) == HBP_OK;

    const hbp::core::ActivityGenerator& generated_activity =
        reinterpret_cast<const hbp_ActivityGenerator*>(ieeg)->activity();
    int compared_vertices = 0;
    for (int i = 0; ok && i < sizes.vertex_count; ++i) {
        const hbp::core::Vec3 point = hbp::core::from_hbp_vec3(vertices[static_cast<std::size_t>(i)]);
        hbp::core::TrilinearVolumeStencil stencil;
        float raw_activity = 0.0f;
        float raw_weight = 0.0f;
        bool sampled = hbp::core::trilinear_volume_stencil(layout, point, stencil)
            && hbp::core::sample_trilinear_weighted_volume(
                stencil,
                layout,
                [&](int index) { return generated_activity.raw_activity(index, 0); },
                [&](int index) { return generated_activity.raw_weight(index, 0); },
                raw_activity,
                raw_weight);
        float expected_activity = sampled
            ? generated_activity.normalize_activity(raw_activity)
            : 0.0f;
        float expected_weight = sampled
            ? generated_activity.normalize_weight(raw_weight, raw_activity)
            : 0.0f;
        bool projected_activity = alpha_uv[static_cast<std::size_t>(i)].y < 0.5f;
        if (projected_activity) {
            ++compared_vertices;
            float expected_alpha = expected_weight * (1.0f - alpha) + alpha;
            bool matches = expected_weight > 0.0f
                && nearly_equal(activity_uv[static_cast<std::size_t>(i)].x, expected_activity)
                && nearly_equal(activity_uv[static_cast<std::size_t>(i)].y, 0.0f)
                && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].x, expected_alpha)
                && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].y, 0.0f);
            if (!matches) {
                std::cerr << "Surface-volume mismatch at vertex " << i
                          << ": activity " << activity_uv[static_cast<std::size_t>(i)].x
                          << " != " << expected_activity
                          << ", alpha " << alpha_uv[static_cast<std::size_t>(i)].x
                          << " != " << expected_alpha
                          << ", raw weight " << raw_weight << '\n';
                ok = false;
            }
        }
    }

    if (ieeg) hbp_ieeg_generator_destroy(ieeg);
    return ok && compared_vertices > 0
        ? 0
        : fail("Surface activity did not match the shared trilinear volume projection");
}

int test_ieeg_atlas(const std::string& nifti_path, const std::string& gifti_path, const std::string& fixture_directory)
{
    GeneratorFixture fixture;
    if (!fixture.initialize(nifti_path, gifti_path, 8)) {
        return fail("Could not initialize the atlas generator fixture");
    }

    std::string index_path = fixture_path(fixture_directory, "hbp_core_generator_mars_index.csv");
    {
        std::ofstream index(index_path);
        index << "label,hemisphere,lobe,nameFS,name,fullName,BA,color\n";
        for (int label = 1; label <= 124; ++label) {
            index << label << ",L,Frontal,fs_" << label << ",Area" << label << ",Area " << label << ",,255 0 0\n";
        }
    }

    hbp_MarsAtlas* atlas = nullptr;
    hbp_IEEGGenerator* ieeg = nullptr;
    bool initialized = hbp_mars_atlas_create(&atlas) == HBP_OK
        && hbp_mars_atlas_load(atlas, index_path.c_str(), nullptr, nifti_path.c_str()) == HBP_OK
        && hbp_ieeg_generator_create(&ieeg) == HBP_OK
        && hbp_activity_generator_initialize(reinterpret_cast<hbp_ActivityGenerator*>(ieeg), fixture.generator_surface) == HBP_OK
        && hbp_surface_generator_initialize(fixture.surface_generator, reinterpret_cast<hbp_ActivityGenerator*>(ieeg)) == HBP_OK;
    if (!initialized) {
        if (ieeg) hbp_ieeg_generator_destroy(ieeg);
        if (atlas) hbp_mars_atlas_destroy(atlas);
        std::remove(index_path.c_str());
        return fail("Could not initialize IEEG atlas ABI objects");
    }

    constexpr int area_count = 125;
    constexpr int timeline_length = 2;
    std::vector<float> activity(static_cast<std::size_t>(area_count * timeline_length), 0.0f);
    std::vector<int> mask(static_cast<std::size_t>(area_count), 0);
    for (int label = 1; label < area_count; ++label) {
        activity[static_cast<std::size_t>(label * timeline_length)] = static_cast<float>(label) / 124.0f;
        activity[static_cast<std::size_t>(label * timeline_length + 1)] = -static_cast<float>(label) / 124.0f;
    }

    hbp_ActivityGenerator* base = reinterpret_cast<hbp_ActivityGenerator*>(ieeg);
    float progress = -1.0f;
    bool ok = hbp_activity_generator_get_progress(base, &progress) == HBP_OK
        && nearly_equal(progress, 0.0f)
        && hbp_ieeg_generator_compute_activity_atlas(ieeg, activity.data(), timeline_length, area_count, mask.data(), reinterpret_cast<hbp_BrainAtlas*>(atlas)) == HBP_OK
        && hbp_activity_generator_get_progress(base, &progress) == HBP_OK
        && nearly_equal(progress, 1.0f)
        && hbp_ieeg_generator_adjust_values(ieeg, 0.0f, -1.0f, 1.0f) == HBP_OK
        && hbp_surface_generator_compute_activity_uv(fixture.surface_generator, 0, 0.2f) == HBP_OK;

    hbp_SurfaceSizes sizes{};
    ok = ok && hbp_surface_get_sizes(fixture.surface, &sizes) == HBP_OK && sizes.vertex_count > 0;
    std::vector<hbp_Vec3> vertices(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    std::vector<hbp_Vec2> activity_uv(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    std::vector<hbp_Vec2> alpha_uv(static_cast<std::size_t>(std::max(0, sizes.vertex_count)));
    ok = ok
        && hbp_surface_copy_vertices(fixture.surface, vertices.data(), sizes.vertex_count) == HBP_OK
        && hbp_surface_generator_copy_activity_uvs(fixture.surface_generator, activity_uv.data(), sizes.vertex_count) == HBP_OK
        && hbp_surface_generator_copy_alpha_uvs(fixture.surface_generator, alpha_uv.data(), sizes.vertex_count) == HBP_OK;

    std::vector<hbp_Vec3> generated_vertices;
    ok = ok && build_generated_vertices(fixture, 8, generated_vertices);
    const hbp::core::ActivityGenerator& generated_activity =
        reinterpret_cast<const hbp_ActivityGenerator*>(ieeg)->activity();
    for (int i = 0; ok && i < static_cast<int>(generated_vertices.size()); ++i) {
        int label = -1;
        ok = hbp_brain_atlas_get_closest_area_index(
            reinterpret_cast<hbp_BrainAtlas*>(atlas),
            &generated_vertices[static_cast<std::size_t>(i)],
            0,
            &label) == HBP_OK;
        bool influenced = label >= 0 && label < area_count && mask[static_cast<std::size_t>(label)] == 0;
        for (int timeline = 0; ok && timeline < timeline_length; ++timeline) {
            float expected_value = influenced
                ? activity[static_cast<std::size_t>(label * timeline_length + timeline)]
                : 0.0f;
            float expected_weight = influenced ? 1.0f : 0.0f;
            ok = generated_activity.raw_activity(i, timeline) == expected_value
                && generated_activity.weight(i, timeline) == expected_weight;
        }
    }

    int influenced_vertices = 0;
    for (int i = 0; ok && i < sizes.vertex_count; ++i) {
        int label = -1;
        ok = hbp_brain_atlas_get_closest_area_index(reinterpret_cast<hbp_BrainAtlas*>(atlas), &vertices[static_cast<std::size_t>(i)], 0, &label) == HBP_OK;
        if (label <= 0 || label >= area_count) {
            continue;
        }
        ++influenced_vertices;
        float expected_activity = (activity[static_cast<std::size_t>(label * timeline_length)] + 1.0f) * 0.5f;
        ok = nearly_equal(activity_uv[static_cast<std::size_t>(i)].x, expected_activity)
            && nearly_equal(activity_uv[static_cast<std::size_t>(i)].y, 0.0f)
            && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].x, 1.0f)
            && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].y, 0.0f);
    }

    std::fill(mask.begin(), mask.end(), 1);
    ok = ok && influenced_vertices > 0
        && hbp_ieeg_generator_compute_activity_atlas(ieeg, activity.data(), timeline_length, area_count, mask.data(), reinterpret_cast<hbp_BrainAtlas*>(atlas)) == HBP_OK
        && hbp_surface_generator_compute_activity_uv(fixture.surface_generator, 1, 0.2f) == HBP_OK
        && hbp_surface_generator_copy_activity_uvs(fixture.surface_generator, activity_uv.data(), sizes.vertex_count) == HBP_OK
        && hbp_surface_generator_copy_alpha_uvs(fixture.surface_generator, alpha_uv.data(), sizes.vertex_count) == HBP_OK;
    for (int i = 0; ok && i < sizes.vertex_count; ++i) {
        ok = nearly_equal(activity_uv[static_cast<std::size_t>(i)].x, 0.5f)
            && nearly_equal(activity_uv[static_cast<std::size_t>(i)].y, 1.0f)
            && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].x, 0.01f)
            && nearly_equal(alpha_uv[static_cast<std::size_t>(i)].y, 1.0f);
    }
    for (int i = 0; ok && i < static_cast<int>(generated_vertices.size()); ++i) {
        ok = generated_activity.raw_activity(i, 0) == 0.0f
            && generated_activity.raw_activity(i, 1) == 0.0f
            && generated_activity.weight(i, 0) == 0.0f
            && generated_activity.weight(i, 1) == 0.0f;
    }

    activity[2] = std::numeric_limits<float>::quiet_NaN();
    ok = ok && hbp_ieeg_generator_compute_activity_atlas(ieeg, activity.data(), timeline_length, area_count, mask.data(), reinterpret_cast<hbp_BrainAtlas*>(atlas)) == HBP_INVALID_ARGUMENT;

    hbp_ieeg_generator_destroy(ieeg);
    hbp_mars_atlas_destroy(atlas);
    std::remove(index_path.c_str());
    return ok ? 0 : fail("IEEG atlas values, masks, progress, repeat call, or non-finite contract failed");
}

}

int main(int argc, char** argv)
{
    if (argc != 3) {
        return fail("Usage: hbp_core_generator_functional_test <nifti-fixture-dir> <gifti-fixture-dir>");
    }
    if (hbp_core_init() != HBP_OK) {
        return fail("hbp_core_init failed");
    }

    std::string nifti_directory = argv[1];
    std::string gifti_directory = argv[2];
    std::string nifti_path = fixture_path(nifti_directory, "fmri_3d.nii");
    std::string gifti_path = fixture_path(gifti_directory, "single_surface.gii");
    int result = test_plane_normalization();
    if (result == 0) result = test_empty_inputs(nifti_path);
    if (result == 0) result = test_density_progress_maximum_and_main_uv(nifti_path, gifti_path);
    if (result == 0) result = test_ieeg_performance_metrics(nifti_path, gifti_path);
    if (result == 0) result = test_ieeg_parallel_determinism(nifti_path, gifti_path);
    if (result == 0) result = test_spatial_index_cache_lifecycle(nifti_path, gifti_path);
    if (result == 0) result = test_ieeg_unique_weights_match_legacy_storage(nifti_path, gifti_path);
    if (result == 0) result = test_surface_activity_uses_trilinear_volume(nifti_path, gifti_path);
    if (result == 0) result = test_ieeg_atlas(nifti_path, gifti_path, nifti_directory);

    if (hbp_core_shutdown() != HBP_OK && result == 0) {
        return fail("hbp_core_shutdown failed");
    }
    return result;
}
