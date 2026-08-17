#include "cut_generator.h"

#include "../core/math_utils.h"
#include "../core/parallel_for.h"

#include <algorithm>
#include <cmath>
#include <cstring>

#if defined(_MSC_VER) || defined(__SSE2__)
#include <emmintrin.h>
#define HBP_CORE_HAS_SSE2 1
#endif

namespace hbp::core {
namespace {

unsigned char color_channel_to_byte(float value)
{
    float scaled = clamp_value(value, 0.0f, 1.0f) * 255.0f + 0.5f;
    return static_cast<unsigned char>(static_cast<int>(scaled));
}

Rgba8 to_rgba8(const Color4& color)
{
    return Rgba8{
        color_channel_to_byte(color.r),
        color_channel_to_byte(color.g),
        color_channel_to_byte(color.b),
        color_channel_to_byte(color.a)};
}

unsigned char byte_range_channel_to_byte(float value)
{
    float rounded = clamp_value(value, 0.0f, 255.0f) + 0.5f;
    return static_cast<unsigned char>(static_cast<int>(rounded));
}

Rgba8 byte_range_to_rgba8(const Color4& color)
{
    return Rgba8{
        byte_range_channel_to_byte(color.r),
        byte_range_channel_to_byte(color.g),
        byte_range_channel_to_byte(color.b),
        byte_range_channel_to_byte(color.a)};
}

Color4 to_color4(const Rgba8& color)
{
    constexpr float byte_to_float = 1.0f / 255.0f;
    return make_color4(
        static_cast<float>(color.r) * byte_to_float,
        static_cast<float>(color.g) * byte_to_float,
        static_cast<float>(color.b) * byte_to_float,
        static_cast<float>(color.a) * byte_to_float);
}

Rgba8 black()
{
    return Rgba8{0, 0, 0, 255};
}

bool is_black(const Rgba8& color)
{
    return color.r == 0 && color.g == 0 && color.b == 0;
}

Rgba8 blend(Rgba8 base, Rgba8 overlay, float alpha)
{
    alpha = clamp_value(alpha, 0.0f, 1.0f);
    float base_weight = 1.0f - alpha;
    return Rgba8{
        static_cast<unsigned char>(static_cast<int>(static_cast<float>(base.r) * base_weight + static_cast<float>(overlay.r) * alpha + 0.5f)),
        static_cast<unsigned char>(static_cast<int>(static_cast<float>(base.g) * base_weight + static_cast<float>(overlay.g) * alpha + 0.5f)),
        static_cast<unsigned char>(static_cast<int>(static_cast<float>(base.b) * base_weight + static_cast<float>(overlay.b) * alpha + 0.5f)),
        255};
}

Rgba8 sample_colormap(const Rgba8* color_scheme, int color_count, float ratio)
{
    if (!color_scheme || color_count <= 0) {
        return black();
    }
    int index = truncate_to_int(clamp_value(ratio, 0.0f, 1.0f) * static_cast<float>(color_count - 1));
    return color_scheme[index];
}

float normalized_piecewise(float value, float min, float middle, float max)
{
    if (value <= middle) {
        float denominator = middle - min;
        if (std::fabs(denominator) <= kGeometryEpsilon) {
            return 0.5f;
        }
        return clamp_value((value - min) / denominator * 0.5f, 0.0f, 0.5f);
    }

    float denominator = max - middle;
    if (std::fabs(denominator) <= kGeometryEpsilon) {
        return 1.0f;
    }
    return clamp_value(0.5f + (value - middle) / denominator * 0.5f, 0.5f, 1.0f);
}

bool copy_unity_texture_colors(const std::vector<Rgba8>& colors, const CutGeometryGenerator* geometry_generator, Color4* out_colors, int color_capacity)
{
    if (!out_colors || color_capacity < static_cast<int>(colors.size())) {
        return false;
    }

    if (!geometry_generator) {
        for (std::size_t i = 0; i < colors.size(); ++i) {
            out_colors[i] = to_color4(colors[i]);
        }
        return true;
    }

    CutTextureSizes sizes = geometry_generator->texture_sizes();
    std::size_t width = static_cast<std::size_t>(sizes.width);
    std::size_t height = static_cast<std::size_t>(sizes.height);
    if (sizes.width <= 0 || sizes.height <= 0 || colors.size() != width * height) {
        for (std::size_t i = 0; i < colors.size(); ++i) {
            out_colors[i] = to_color4(colors[i]);
        }
        return true;
    }

    for (std::size_t y = 0; y < height; ++y) {
        std::size_t source_y = height - y - 1;
        for (std::size_t x = 0; x < width; ++x) {
            out_colors[y * width + x] = to_color4(colors[source_y * width + x]);
        }
    }
    return true;
}

bool copy_unity_texture_rgba8(
    const std::vector<Rgba8>& colors,
    const CutGeometryGenerator* geometry_generator,
    unsigned char* out_rgba,
    int pixel_capacity)
{
    if (!out_rgba || pixel_capacity < static_cast<int>(colors.size())) {
        return false;
    }

    CutTextureSizes sizes = geometry_generator ? geometry_generator->texture_sizes() : CutTextureSizes{};
    std::size_t width = static_cast<std::size_t>(sizes.width);
    std::size_t height = static_cast<std::size_t>(sizes.height);
    static_assert(sizeof(Rgba8) == 4, "Rgba8 must remain tightly packed.");
    if (sizes.width <= 0 || sizes.height <= 0 || colors.size() != width * height) {
        std::memcpy(out_rgba, colors.data(), colors.size() * sizeof(Rgba8));
        return true;
    }
    std::size_t row_bytes = width * sizeof(Rgba8);
    for (std::size_t y = 0; y < height; ++y) {
        std::size_t source_y = height - y - 1;
        std::memcpy(out_rgba + y * row_bytes, colors.data() + source_y * width, row_bytes);
    }
    return true;
}

int reflect_101(int index, int length)
{
    if (length <= 1) {
        return 0;
    }
    while (index < 0 || index >= length) {
        index = index < 0 ? -index : 2 * length - index - 2;
    }
    return index;
}

float sampled_neighbour_average(const Volume& volume, const Vec3& point)
{
    Vec3 ijk = volume.closest_ijk(point);
    int center_x = truncate_to_int(ijk.x);
    int center_y = truncate_to_int(ijk.y);
    int center_z = truncate_to_int(ijk.z);
    const VolumeDimensions& dimensions = volume.dimensions();

    float total = 0.0f;
    int count = 0;
    for (int z = center_z - 1; z <= center_z + 1; ++z) {
        for (int y = center_y - 1; y <= center_y + 1; ++y) {
            for (int x = center_x - 1; x <= center_x + 1; ++x) {
                if (x < 0 || y < 0 || z < 0 || x >= dimensions.x || y >= dimensions.y || z >= dimensions.z) {
                    continue;
                }
                float value = volume.voxel_data(x, y, z);
                if (!almost_equal(value, 0.0f)) {
                    total += value;
                    ++count;
                }
            }
        }
    }
    return count > 0 ? total / static_cast<float>(count) : 0.0f;
}

}

void CutGenerator::initialize(const ActivityGenerator* activity_generator, const CutGeometryGenerator* geometry_generator, int blur_factor)
{
    _activity_generator = activity_generator;
    _geometry_generator = geometry_generator;
    _blur_factor = std::max(0, blur_factor);
    int kernel_size = _blur_factor;
    if (kernel_size > 0 && kernel_size % 2 == 0) {
        ++kernel_size;
    }
    _blur_kernel.clear();
    if (kernel_size > 1) {
        _blur_kernel.resize(static_cast<std::size_t>(kernel_size));
        double coefficient = 1.0;
        double sum = 0.0;
        int order = kernel_size - 1;
        for (int i = 0; i < kernel_size; ++i) {
            if (i > 0) {
                coefficient *= static_cast<double>(order - i + 1) / static_cast<double>(i);
            }
            _blur_kernel[static_cast<std::size_t>(i)] = static_cast<float>(coefficient);
            sum += coefficient;
        }
        for (float& weight : _blur_kernel) {
            weight = static_cast<float>(static_cast<double>(weight) / sum);
        }
    }
    _blur_scratch.clear();
    _blur_scratch16.clear();
    _color_scheme.clear();
    _base_colors.clear();
    _overlay_colors.clear();
}

void CutGenerator::prepare_color_scheme(const Color4* color_scheme, int color_count)
{
    _color_scheme.resize(static_cast<std::size_t>(color_count));
    for (int i = 0; i < color_count; ++i) {
        _color_scheme[static_cast<std::size_t>(i)] = to_rgba8(color_scheme[i]);
    }
}

void CutGenerator::prepare_color_scheme_rgba8(const unsigned char* color_scheme_rgba, int color_count)
{
    static_assert(sizeof(Rgba8) == 4, "Rgba8 must remain tightly packed.");
    _color_scheme.resize(static_cast<std::size_t>(color_count));
    std::memcpy(_color_scheme.data(), color_scheme_rgba, static_cast<std::size_t>(color_count) * sizeof(Rgba8));
}

bool CutGenerator::fill_volume_colors(const Color4* color_scheme, int color_count, float cal_min, float cal_max)
{
    if (!_geometry_generator || !color_scheme || color_count <= 0) {
        return false;
    }

    prepare_color_scheme(color_scheme, color_count);
    return fill_volume_colors_prepared(cal_min, cal_max);
}

bool CutGenerator::fill_volume_colors_rgba8(const unsigned char* color_scheme_rgba, int color_count, float cal_min, float cal_max)
{
    if (!_geometry_generator || !color_scheme_rgba || color_count <= 0) {
        return false;
    }

    prepare_color_scheme_rgba8(color_scheme_rgba, color_count);
    return fill_volume_colors_prepared(cal_min, cal_max);
}

bool CutGenerator::fill_volume_colors_prepared(float cal_min, float cal_max)
{
    int color_count = static_cast<int>(_color_scheme.size());

    const std::vector<float>& values = _geometry_generator->sample_values();
    if (values.empty()) {
        return false;
    }

    const VolumeExtrema& extrema = _geometry_generator->volume_extrema();
    float raw_min = extrema.min;
    float raw_max = extrema.max;
    float min_scaling = raw_min + cal_min * (raw_max - raw_min);
    float max_scaling = raw_min + cal_max * (raw_max - raw_min);
    float diff_scaling = max_scaling - min_scaling;

    _base_colors.assign(values.size(), black());
    for (std::size_t i = 0; i < values.size(); ++i) {
        float value = values[i];
        if (almost_equal(value, 0.0f)) {
            continue;
        }
        float ratio = std::fabs(diff_scaling) <= kGeometryEpsilon ? 0.0f : (clamp_value(value, min_scaling, max_scaling) - min_scaling) / diff_scaling;
        _base_colors[i] = sample_colormap(_color_scheme.data(), color_count, ratio);
    }

    blur(_base_colors);
    _overlay_colors = _base_colors;
    return true;
}

bool CutGenerator::fill_atlas_colors(const BrainAtlas& atlas, float alpha, int selected_area)
{
    if (!has_texture()) {
        return false;
    }

    const std::vector<Vec3>& sample_points = _geometry_generator->sample_points();
    _overlay_colors = _base_colors;
    for (std::size_t i = 0; i < sample_points.size(); ++i) {
        if (is_black(_base_colors[i])) {
            continue;
        }
        int label = atlas.get_closest_area_index(sample_points[i], 0);
        if (label == -1) {
            continue;
        }
        Color4 color{};
        if (atlas.get_color(label, label == selected_area, color)) {
            _overlay_colors[i] = blend(_overlay_colors[i], to_rgba8(color), alpha);
        }
    }

    blur(_overlay_colors);
    return true;
}

bool CutGenerator::fill_activity_colors(const Color4* color_scheme, int color_count, int timeline_index, float alpha)
{
    if (!has_texture() || !_activity_generator || !color_scheme || color_count <= 0) {
        return false;
    }
    prepare_color_scheme(color_scheme, color_count);

    return fill_activity_colors_prepared(timeline_index, alpha);
}

bool CutGenerator::fill_activity_colors_rgba8(const unsigned char* color_scheme_rgba, int color_count, int timeline_index, float alpha)
{
    if (!has_texture() || !_activity_generator || !color_scheme_rgba || color_count <= 0) {
        return false;
    }
    prepare_color_scheme_rgba8(color_scheme_rgba, color_count);

    return fill_activity_colors_prepared(timeline_index, alpha);
}

bool CutGenerator::fill_activity_colors_prepared(int timeline_index, float alpha)
{
    int color_count = static_cast<int>(_color_scheme.size());
    const GeneratorSurface* generator_surface = _activity_generator
        ? _activity_generator->generator_surface()
        : nullptr;
    if (!generator_surface) {
        return false;
    }

    float alpha_diff = 1.0f - alpha;
    const std::vector<Vec3>& sample_points = _geometry_generator->sample_points();
    const ActivitySampleStencils& stencils =
        _geometry_generator->activity_sample_stencils(*generator_surface);
    _overlay_colors = _base_colors;
    if (stencils.interpolation == VolumeInterpolation::Nearest) {
        parallel_for(sample_points.size(), 2048, 256, [&](std::size_t begin, std::size_t end) {
            for (std::size_t i = begin; i < end; ++i) {
                if (is_black(_base_colors[i])) {
                    continue;
                }
                const int vertex_index = stencils.nearest_indices[i];
                if (vertex_index < 0) {
                    continue;
                }
                const float activity_weight = _activity_generator->weight(vertex_index, timeline_index);
                if (almost_equal(activity_weight, 0.0f)) {
                    continue;
                }
                const float activity = _activity_generator->activity(vertex_index, timeline_index);
                const float ratio_density = activity_weight * alpha_diff + alpha;
                _overlay_colors[i] = blend(
                    _base_colors[i],
                    sample_colormap(_color_scheme.data(), color_count, activity),
                    ratio_density);
            }
        });
    }
    else {
        parallel_for(sample_points.size(), 2048, 256, [&](std::size_t begin, std::size_t end) {
            for (std::size_t i = begin; i < end; ++i) {
                if (is_black(_base_colors[i])) {
                    continue;
                }
                const TrilinearVolumeStencil& stencil = stencils.trilinear_stencils[i];
                if (stencil.base_vertex_index < 0) {
                    continue;
                }
                float raw_activity = 0.0f;
                float raw_weight = 0.0f;
                if (!sample_trilinear_weighted_volume(
                    stencil,
                    stencils.layout,
                    [&](int index) { return _activity_generator->raw_activity(index, timeline_index); },
                    [&](int index) { return _activity_generator->raw_weight(index, timeline_index); },
                    raw_activity,
                    raw_weight)) {
                    continue;
                }
                const float activity_weight = _activity_generator->normalize_weight(
                    raw_weight,
                    raw_activity);
                if (almost_equal(activity_weight, 0.0f)) {
                    continue;
                }
                const float activity = _activity_generator->normalize_activity(raw_activity);
                const float ratio_density = activity_weight * alpha_diff + alpha;
                _overlay_colors[i] = blend(
                    _base_colors[i],
                    sample_colormap(_color_scheme.data(), color_count, activity),
                    ratio_density);
            }
        });
    }

    // The anatomical base has already been blurred by fill_volume_colors_prepared.
    // Blurring the composited activity here would mix palette values across the cut
    // and make it disagree with the same volumetric field projected on the mesh.
    return true;
}

bool CutGenerator::fill_fmri_colors(const Volume& volume, float negative_min, float negative_max, float positive_min, float positive_max, float alpha)
{
    if (!has_texture() || volume.empty()) {
        return false;
    }

    float min_value = volume.extrema().min;
    float max_value = volume.extrema().max;
    float maximum_absolute_limit = std::max(std::fabs(min_value), std::fabs(max_value));
    float negative_cal_min = negative_min * min_value;
    float negative_cal_max = negative_max * min_value;
    float positive_cal_min = positive_min * max_value;
    float positive_cal_max = positive_max * max_value;
    if (min_value >= 0.0f) {
        negative_cal_min = -maximum_absolute_limit;
        negative_cal_max = -maximum_absolute_limit;
    }
    if (max_value <= 0.0f) {
        positive_cal_min = maximum_absolute_limit;
        positive_cal_max = maximum_absolute_limit;
    }

    _overlay_colors = _base_colors;
    const std::vector<Vec3>& sample_points = _geometry_generator->sample_points();
    for (std::size_t i = 0; i < sample_points.size(); ++i) {
        if (is_black(_base_colors[i])) {
            continue;
        }
        float value = sampled_neighbour_average(volume, sample_points[i]);
        if (value > negative_cal_min && value < positive_cal_min) {
            continue;
        }

        float pixel_alpha = 0.0f;
        Rgba8 color = black();
        if (value < negative_cal_min) {
            pixel_alpha = almost_equal(negative_cal_min, negative_cal_max)
                ? 1.0f
                : clamp_value(((value - negative_cal_min) / (negative_cal_max - negative_cal_min)) * (1.0f - alpha) + alpha, 0.0f, 1.0f);
            color = Rgba8{0, 0, 255, 255};
        } else if (value > positive_cal_min) {
            pixel_alpha = almost_equal(positive_cal_min, positive_cal_max)
                ? 1.0f
                : clamp_value(((value - positive_cal_min) / (positive_cal_max - positive_cal_min)) * (1.0f - alpha) + alpha, 0.0f, 1.0f);
            color = Rgba8{255, 0, 0, 255};
        }
        _overlay_colors[i] = blend(_overlay_colors[i], color, pixel_alpha);
    }

    blur(_overlay_colors);
    return true;
}

bool CutGenerator::fill_localizer_colors(const Volume& volume, const Volume* mask, float min, float middle, float max, const Color4* color_scheme, int color_count)
{
    if (!has_texture() || volume.empty() || !color_scheme || color_count <= 0) {
        return false;
    }
    prepare_color_scheme(color_scheme, color_count);

    _overlay_colors = _base_colors;
    const std::vector<Vec3>& sample_points = _geometry_generator->sample_points();
    for (std::size_t i = 0; i < sample_points.size(); ++i) {
        if (is_black(_base_colors[i])) {
            continue;
        }

        Vec3 ijk = volume.closest_ijk(sample_points[i]);
        int x = truncate_to_int(ijk.x);
        int y = truncate_to_int(ijk.y);
        int z = truncate_to_int(ijk.z);
        if (mask && almost_equal(mask->voxel_data(x, y, z), 0.0f)) {
            continue;
        }

        float ratio = normalized_piecewise(volume.voxel_data(x, y, z), min, middle, max);
        _overlay_colors[i] = sample_colormap(_color_scheme.data(), color_count, ratio);
    }

    blur(_overlay_colors);
    return true;
}

bool CutGenerator::copy_base_colors(Color4* out_colors, int color_capacity) const
{
    return copy_unity_texture_colors(_base_colors, _geometry_generator, out_colors, color_capacity);
}

bool CutGenerator::copy_overlay_colors(Color4* out_colors, int color_capacity) const
{
    return copy_unity_texture_colors(_overlay_colors, _geometry_generator, out_colors, color_capacity);
}

bool CutGenerator::copy_base_rgba8(unsigned char* out_rgba, int pixel_capacity) const
{
    return copy_unity_texture_rgba8(_base_colors, _geometry_generator, out_rgba, pixel_capacity);
}

bool CutGenerator::copy_overlay_rgba8(unsigned char* out_rgba, int pixel_capacity) const
{
    return copy_unity_texture_rgba8(_overlay_colors, _geometry_generator, out_rgba, pixel_capacity);
}

int CutGenerator::color_count() const
{
    return static_cast<int>(_base_colors.size());
}

bool CutGenerator::has_texture() const
{
    return _geometry_generator && !_base_colors.empty();
}

void CutGenerator::blur(std::vector<Rgba8>& colors)
{
    if (!_geometry_generator || _blur_kernel.empty() || colors.empty()) {
        return;
    }

    CutTextureSizes sizes = _geometry_generator->texture_sizes();
    if (sizes.width <= 0 || sizes.height <= 0) {
        return;
    }

    if (_blur_kernel.size() == 5) {
        _blur_scratch16.resize(colors.size());
        for (int y = 0; y < sizes.height; ++y) {
            const Rgba8* input_row = colors.data() + static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width);
            Rgba16* output_row = _blur_scratch16.data() + static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width);
            auto blur_horizontal_pixel = [&](int x) {
                const Rgba8& c0 = input_row[reflect_101(x - 2, sizes.width)];
                const Rgba8& c1 = input_row[reflect_101(x - 1, sizes.width)];
                const Rgba8& c2 = input_row[x];
                const Rgba8& c3 = input_row[reflect_101(x + 1, sizes.width)];
                const Rgba8& c4 = input_row[reflect_101(x + 2, sizes.width)];
                Rgba16& output = output_row[x];
                output.r = static_cast<unsigned short>(c0.r + 4 * c1.r + 6 * c2.r + 4 * c3.r + c4.r);
                output.g = static_cast<unsigned short>(c0.g + 4 * c1.g + 6 * c2.g + 4 * c3.g + c4.g);
                output.b = static_cast<unsigned short>(c0.b + 4 * c1.b + 6 * c2.b + 4 * c3.b + c4.b);
                output.a = static_cast<unsigned short>(c0.a + 4 * c1.a + 6 * c2.a + 4 * c3.a + c4.a);
            };

            int x = 0;
            for (; x < std::min(2, sizes.width); ++x) {
                blur_horizontal_pixel(x);
            }
#if defined(HBP_CORE_HAS_SSE2)
            const __m128i zero = _mm_setzero_si128();
            auto weighted_five = [](__m128i c0, __m128i c1, __m128i c2, __m128i c3, __m128i c4) {
                __m128i neighbours = _mm_slli_epi16(_mm_add_epi16(c1, c3), 2);
                __m128i center = _mm_add_epi16(_mm_slli_epi16(c2, 2), _mm_slli_epi16(c2, 1));
                return _mm_add_epi16(_mm_add_epi16(c0, c4), _mm_add_epi16(neighbours, center));
            };
            for (; x + 3 < sizes.width - 2; x += 4) {
                __m128i p0 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(input_row + x - 2));
                __m128i p1 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(input_row + x - 1));
                __m128i p2 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(input_row + x));
                __m128i p3 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(input_row + x + 1));
                __m128i p4 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(input_row + x + 2));
                __m128i low = weighted_five(
                    _mm_unpacklo_epi8(p0, zero), _mm_unpacklo_epi8(p1, zero), _mm_unpacklo_epi8(p2, zero),
                    _mm_unpacklo_epi8(p3, zero), _mm_unpacklo_epi8(p4, zero));
                __m128i high = weighted_five(
                    _mm_unpackhi_epi8(p0, zero), _mm_unpackhi_epi8(p1, zero), _mm_unpackhi_epi8(p2, zero),
                    _mm_unpackhi_epi8(p3, zero), _mm_unpackhi_epi8(p4, zero));
                _mm_storeu_si128(reinterpret_cast<__m128i*>(output_row + x), low);
                _mm_storeu_si128(reinterpret_cast<__m128i*>(output_row + x + 2), high);
            }
#endif
            for (; x < sizes.width; ++x) {
                blur_horizontal_pixel(x);
            }
        }
        for (int y = 0; y < sizes.height; ++y) {
            const Rgba16* row0 = _blur_scratch16.data() + static_cast<std::size_t>(reflect_101(y - 2, sizes.height)) * static_cast<std::size_t>(sizes.width);
            const Rgba16* row1 = _blur_scratch16.data() + static_cast<std::size_t>(reflect_101(y - 1, sizes.height)) * static_cast<std::size_t>(sizes.width);
            const Rgba16* row2 = _blur_scratch16.data() + static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width);
            const Rgba16* row3 = _blur_scratch16.data() + static_cast<std::size_t>(reflect_101(y + 1, sizes.height)) * static_cast<std::size_t>(sizes.width);
            const Rgba16* row4 = _blur_scratch16.data() + static_cast<std::size_t>(reflect_101(y + 2, sizes.height)) * static_cast<std::size_t>(sizes.width);
            Rgba8* output_row = colors.data() + static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width);
            int x = 0;
#if defined(HBP_CORE_HAS_SSE2)
            const __m128i rounding = _mm_set1_epi16(128);
            auto weighted_five = [](__m128i c0, __m128i c1, __m128i c2, __m128i c3, __m128i c4) {
                __m128i neighbours = _mm_slli_epi16(_mm_add_epi16(c1, c3), 2);
                __m128i center = _mm_add_epi16(_mm_slli_epi16(c2, 2), _mm_slli_epi16(c2, 1));
                return _mm_add_epi16(_mm_add_epi16(c0, c4), _mm_add_epi16(neighbours, center));
            };
            for (; x + 3 < sizes.width; x += 4) {
                auto vertical_half = [&](int offset) {
                    __m128i sum = weighted_five(
                        _mm_loadu_si128(reinterpret_cast<const __m128i*>(row0 + offset)),
                        _mm_loadu_si128(reinterpret_cast<const __m128i*>(row1 + offset)),
                        _mm_loadu_si128(reinterpret_cast<const __m128i*>(row2 + offset)),
                        _mm_loadu_si128(reinterpret_cast<const __m128i*>(row3 + offset)),
                        _mm_loadu_si128(reinterpret_cast<const __m128i*>(row4 + offset)));
                    return _mm_srli_epi16(_mm_add_epi16(sum, rounding), 8);
                };
                __m128i low = vertical_half(x);
                __m128i high = vertical_half(x + 2);
                _mm_storeu_si128(reinterpret_cast<__m128i*>(output_row + x), _mm_packus_epi16(low, high));
            }
#endif
            for (; x < sizes.width; ++x) {
                const Rgba16& c0 = row0[x];
                const Rgba16& c1 = row1[x];
                const Rgba16& c2 = row2[x];
                const Rgba16& c3 = row3[x];
                const Rgba16& c4 = row4[x];
                Rgba8& output = output_row[x];
                output.r = static_cast<unsigned char>((c0.r + 4u * c1.r + 6u * c2.r + 4u * c3.r + c4.r + 128u) >> 8);
                output.g = static_cast<unsigned char>((c0.g + 4u * c1.g + 6u * c2.g + 4u * c3.g + c4.g + 128u) >> 8);
                output.b = static_cast<unsigned char>((c0.b + 4u * c1.b + 6u * c2.b + 4u * c3.b + c4.b + 128u) >> 8);
                output.a = static_cast<unsigned char>((c0.a + 4u * c1.a + 6u * c2.a + 4u * c3.a + c4.a + 128u) >> 8);
            }
        }
        return;
    }

    int radius = static_cast<int>(_blur_kernel.size() / 2);
    _blur_scratch.resize(colors.size());
    for (int y = 0; y < sizes.height; ++y) {
        for (int x = 0; x < sizes.width; ++x) {
            Color4 sum = make_color4(0.0f, 0.0f, 0.0f, 0.0f);
            for (int kernel = -radius; kernel <= radius; ++kernel) {
                int sample_x = reflect_101(x + kernel, sizes.width);
                const Rgba8& color = colors[static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width) + static_cast<std::size_t>(sample_x)];
                float weight = _blur_kernel[static_cast<std::size_t>(kernel + radius)];
                sum.r += color.r * weight;
                sum.g += color.g * weight;
                sum.b += color.b * weight;
                sum.a += color.a * weight;
            }
            std::size_t index = static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width) + static_cast<std::size_t>(x);
            _blur_scratch[index] = sum;
        }
    }
    for (int y = 0; y < sizes.height; ++y) {
        for (int x = 0; x < sizes.width; ++x) {
            Color4 sum = make_color4(0.0f, 0.0f, 0.0f, 0.0f);
            for (int kernel = -radius; kernel <= radius; ++kernel) {
                int sample_y = reflect_101(y + kernel, sizes.height);
                const Color4& color = _blur_scratch[static_cast<std::size_t>(sample_y) * static_cast<std::size_t>(sizes.width) + static_cast<std::size_t>(x)];
                float weight = _blur_kernel[static_cast<std::size_t>(kernel + radius)];
                sum.r += color.r * weight;
                sum.g += color.g * weight;
                sum.b += color.b * weight;
                sum.a += color.a * weight;
            }
            colors[static_cast<std::size_t>(y) * static_cast<std::size_t>(sizes.width) + static_cast<std::size_t>(x)] = byte_range_to_rgba8(sum);
        }
    }
}

}
