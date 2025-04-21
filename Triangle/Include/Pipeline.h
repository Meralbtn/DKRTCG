#pragma once
#include <vulkan/vulkan.h>
#include <string>
#include <vector>


namespace PR_BASE
{
    class APPEnter;


    class Pipeline
    {
    public:
        Pipeline();
        ~Pipeline();
        
        void destroyPipe(const VkDevice& device);
        void createRenderPass( APPEnter* enter);
        void createPipeline( APPEnter* enter);
        VkPipeline pipeline;
        VkPipelineLayout pipelineLayout;
        VkShaderModule vertShaderModule;
        VkShaderModule fragShaderModule;
        VkRenderPass renderPass;
    private:
        
        static std::vector<char> loadShaderFiles(const std::string& filePath);
        VkShaderModule createShaderModule(const std::vector<char>& code, const VkDevice& device);
        
    };
}
