// Standalone Android loader/ABI smoke test; no Unity activity or pairing is involved.
#include <cstdio>
#include <dlfcn.h>
#include "WrapperMath.h"

int main(int argc, char** argv)
{
    if (argc != 2) return 2;
    void* library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (!library) { std::fprintf(stderr, "%s\n", dlerror()); return 3; }
#define LOAD(name) auto call##name = reinterpret_cast<decltype(&name)>(dlsym(library, #name)); if (!call##name) return 4
    LOAD(MeanDouble); LOAD(MeanFloat); LOAD(MeanInt);
    LOAD(MedianDouble); LOAD(MedianFloat); LOAD(MedianInt);
    LOAD(StandardDeviation); LOAD(SEM); LOAD(Normalize);
    LOAD(Lerp); LOAD(BiLerp); LOAD(LinearSmooth); LOAD(LinearSmooth2D); LOAD(Interpolate);
    LOAD(PearsonCorrelationCoefficient); LOAD(WilcoxonRankSum); LOAD(WilcoxonSignedRank);
#undef LOAD
    float values[] = {1, 2, 3}, normalized[3] = {};
    double doubles[] = {1, 2, 3}; int integers[] = {1, 2, 3};
    bool exact = callMeanFloat(values, 3) == 2 && callMeanDouble(doubles, 3) == 2 && callMeanInt(integers, 3) == 2;
    exact &= callStandardDeviation(values, 3) == 1;
    callNormalize(values, 3, normalized, 2, 1);
    exact &= normalized[0] == -1 && normalized[1] == 0 && normalized[2] == 1;
    exact &= callLerp(2, 6, .5f) == 4 && callBiLerp(0, 2, 2, 4, .5f, .5f) == 2;
    float fm[] = {3, 1, 2}; double dm[] = {3, 1, 2}; int im[] = {3, 1, 2};
    exact &= callMedianFloat(fm, 3) == 2 && callMedianDouble(dm, 3) == 2 && callMedianInt(im, 3) == 2;
    float ends[] = {0, 2}, smooth[3] = {};
    callLinearSmooth(ends, 2, 2, smooth);
    exact &= smooth[0] == 0 && smooth[1] == 1 && smooth[2] == 2;
    float plane[] = {0, 2, 2, 4}, grid[9] = {};
    callLinearSmooth2D(plane, 2, 2, 2, grid);
    exact &= grid[0] == 0 && grid[4] == 2 && grid[8] == 4;
    callInterpolate(ends, 2, smooth, 3, 0, 0);
    exact &= smooth[0] == 0 && smooth[1] == 1 && smooth[2] == 2;
    double a[] = {1, 2, 3, 4, 5, 6}, b[] = {2, 3, 4, 5, 6, 7};
    // Record these outputs; this probe does not invent cross-platform scientific tolerances.
    std::printf("{\"exports\":17,\"exactSmokeChecksPassed\":%s,\"sem\":%.17g,\"pearson\":%.17g,\"wilcoxonRankSum\":%.17g,\"wilcoxonSignedRank\":%.17g}\n",
        exact ? "true" : "false", callSEM(values, 3), callPearsonCorrelationCoefficient(a, 6, b, 6), callWilcoxonRankSum(a, 6, b, 6), callWilcoxonSignedRank(a, 6, b, 6));
    dlclose(library);
    return exact ? 0 : 5;
}
