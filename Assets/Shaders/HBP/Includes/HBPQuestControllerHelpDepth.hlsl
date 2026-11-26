#ifndef HBP_QUEST_CONTROLLER_HELP_DEPTH_INCLUDED
#define HBP_QUEST_CONTROLLER_HELP_DEPTH_INCLUDED

#include "HBPQuestPeel.hlsl"

HBP_QUEST_TEXTURE(float, _HBPQuestControllerHelpDepth);

float HBP_QuestHelpRawDepth(int2 pixel)
{
    return HBP_QUEST_LOAD(_HBPQuestControllerHelpDepth, pixel);
}

float HBP_QuestHelpEyeDepth(float depth)
{
    return unity_OrthoParams.w > 0.5 ? LinearDepthToEyeDepth(depth) : LinearEyeDepth(depth, _ZBufferParams);
}

#endif
