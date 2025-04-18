#pragma once
#include <vulkan/vulkan.h>
#include <string>
#include <vector>

#include "APPEnter.h"

namespace PR_BASE
{
    class Pipeline
    {
    public:
        ~Pipeline();
        void destroyPipe(const VkDevice& device);
        
    private:
        VkPipelineLayout pipelineLayout;
        VkShaderModule vertShaderModule;
        VkShaderModule fragShaderModule;
        static std::vector<char> loadShaderFiles(const std::string& filePath);
        VkShaderModule createShaderModule(const std::vector<char>& code, const VkDevice& device);
        void createRenderPass(const PR_BASE::APPEnter& enter);
        void createPipeline(const PR_BASE::APPEnter& enter);
    };
}
