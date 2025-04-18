#include "Pipeline.h"
#include <fstream>

#include "APPEnter.h"

namespace PR_BASE
{
    Pipeline::~Pipeline()
    {
        
    }

    void Pipeline::destroyPipe(const VkDevice& device)
    {
        vkDestroyShaderModule(device,vertShaderModule,nullptr);
        vkDestroyShaderModule(device,fragShaderModule,nullptr);
        vkDestroyPipelineLayout(device,pipelineLayout,nullptr);
    }

    std::vector<char> Pipeline::loadShaderFiles(const std::string& filePath)
    {
        //从文件末尾提取
        std::ifstream file(filePath,std::ios::ate | std::ios::binary);
        if (!file.is_open())
        {
            throw std::runtime_error("Failed to open file: " + filePath);
        }
        size_t fileSize = (size_t) file.tellg();
        std::vector<char> buffer(fileSize);
        file.seekg(0);
        file.read(buffer.data(), fileSize);
        file.close();
        return buffer;
    }

    VkShaderModule Pipeline::createShaderModule(const std::vector<char>& code,const VkDevice& device)
    {
        VkShaderModuleCreateInfo shaderModuleCreateInfo = {};
        shaderModuleCreateInfo.sType = VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO;
        shaderModuleCreateInfo.codeSize = code.size();
        shaderModuleCreateInfo.pCode = reinterpret_cast<const uint32_t*>(code.data());
        VkShaderModule shaderModule;
        if (vkCreateShaderModule(device, &shaderModuleCreateInfo, nullptr, &shaderModule) != VK_SUCCESS)
        {
            throw std::runtime_error("Failed to create shader module!");
        }
        return shaderModule;
    }

    void Pipeline::createRenderPass(const PR_BASE::APPEnter& enter)
    {
        VkAttachmentDescription colorAttachment{};
        //设置颜色附件的采用和格式
        colorAttachment.format = enter.getVkFormat();
        colorAttachment.samples = VK_SAMPLE_COUNT_1_BIT;
        //设置渲染前后的处理方式,深度缓冲
        colorAttachment.loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
        colorAttachment.storeOp  = VK_ATTACHMENT_STORE_OP_STORE;
        //模板缓冲的设置
        colorAttachment.stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
        colorAttachment.stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;

        colorAttachment.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
        colorAttachment.finalLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
        
    }

    void Pipeline::createPipeline(const PR_BASE::APPEnter& enter)
    {
        auto vertShader = loadShaderFiles("shaders/vert.spv");
        auto fragShader = loadShaderFiles("shaders/frag.spv");
        vertShaderModule = createShaderModule(vertShader,VK_NULL_HANDLE);
        fragShaderModule = createShaderModule(fragShader,VK_NULL_HANDLE);
        //创建shaderstageInfo
        VkPipelineShaderStageCreateInfo vertShaderStageCreateInfo = {};
        vertShaderStageCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
        vertShaderStageCreateInfo.stage = VK_SHADER_STAGE_VERTEX_BIT;
        vertShaderStageCreateInfo.module = vertShaderModule;
        vertShaderStageCreateInfo.pName = "main";

        VkPipelineShaderStageCreateInfo fragShaderStageCreateInfo = {};
        fragShaderStageCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
        fragShaderStageCreateInfo.stage = VK_SHADER_STAGE_FRAGMENT_BIT;
        fragShaderStageCreateInfo.module = fragShaderModule;
        fragShaderStageCreateInfo.pName = "main";

        VkPipelineShaderStageCreateInfo shaderStages[] = { vertShaderStageCreateInfo, fragShaderStageCreateInfo };
        
        //定义渲染管线可以动态配置的数据
        std::vector<VkDynamicState> dynamicStates = {
        VK_DYNAMIC_STATE_VIEWPORT,VK_DYNAMIC_STATE_SCISSOR
        };
        VkPipelineDynamicStateCreateInfo dynamicStatesCreateInfo{};
        dynamicStatesCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO;
        dynamicStatesCreateInfo.dynamicStateCount = static_cast<uint32_t>(dynamicStates.size());
        dynamicStatesCreateInfo.pDynamicStates = dynamicStates.data();
        //创建顶点输入规则
        VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo{};
        vertexInputStateCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
        vertexInputStateCreateInfo.vertexBindingDescriptionCount = 0;
        vertexInputStateCreateInfo.pVertexBindingDescriptions = nullptr; 
        vertexInputStateCreateInfo.vertexAttributeDescriptionCount = 0;
        vertexInputStateCreateInfo.pVertexAttributeDescriptions = nullptr;
        //渲染管线渲染对象的方式
        VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo{};
        inputAssemblyStateCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
        //渲染顶点方式
        inputAssemblyStateCreateInfo.topology = VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
        inputAssemblyStateCreateInfo.primitiveRestartEnable = VK_FALSE;

        //设置视口
        VkViewport viewport{};
        viewport.x = 0.0f;
        viewport.y = 0.0f;
        viewport.height =enter.getSwapChainExtent().height;
        viewport.width = enter.getSwapChainExtent().width;
        viewport.minDepth = 0.0f;
        viewport.maxDepth = 1.0f;
        //定义剪切区域的大小，在视口范围中裁剪部分画面
        VkRect2D scissor{};
        scissor.offset = {0, 0};
        scissor.extent = enter.getSwapChainExtent();

        VkPipelineViewportStateCreateInfo viewportStateCreateInfo{};
        viewportStateCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO;
        viewportStateCreateInfo.viewportCount = 1;
        viewportStateCreateInfo.pViewports = &viewport;
        viewportStateCreateInfo.scissorCount = 1;
        viewportStateCreateInfo.pScissors = &scissor;

        //光栅化的设置
        VkPipelineRasterizationStateCreateInfo rasterizerStateCreateInfo{};
        rasterizerStateCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
        //深度钳制
        rasterizerStateCreateInfo.depthClampEnable = VK_FALSE;
        rasterizerStateCreateInfo.rasterizerDiscardEnable = VK_FALSE;
        rasterizerStateCreateInfo.polygonMode = VK_POLYGON_MODE_FILL;

        rasterizerStateCreateInfo.lineWidth = 1.0f;
        //剔除面设置
        rasterizerStateCreateInfo.cullMode = VK_CULL_MODE_BACK_BIT;
        rasterizerStateCreateInfo.frontFace = VK_FRONT_FACE_CLOCKWISE;
        //光栅器可以改变深度值，这里禁用
        rasterizerStateCreateInfo.depthBiasEnable = VK_FALSE;
        rasterizerStateCreateInfo.depthBiasConstantFactor = 0.0f;
        rasterizerStateCreateInfo.depthBiasClamp = 0.0f;
        rasterizerStateCreateInfo.depthBiasSlopeFactor = 0.0f;

        //多重采样设置 此处禁用
        VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo{};
        multisampleStateCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
        multisampleStateCreateInfo.sampleShadingEnable = VK_FALSE;
        multisampleStateCreateInfo.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
        multisampleStateCreateInfo.minSampleShading = 1.0f;
        multisampleStateCreateInfo.pSampleMask = nullptr;
        multisampleStateCreateInfo.alphaToCoverageEnable = VK_FALSE;
        multisampleStateCreateInfo.alphaToOneEnable = VK_FALSE;

        //深度缓冲模板缓冲

        //颜色混合设置 有两种模式可选 
        VkPipelineColorBlendAttachmentState colorBlendAttachmentState{};
        colorBlendAttachmentState.colorWriteMask = VK_COLOR_COMPONENT_R_BIT | VK_COLOR_COMPONENT_G_BIT | VK_COLOR_COMPONENT_B_BIT | VK_COLOR_COMPONENT_A_BIT;
        colorBlendAttachmentState.blendEnable = VK_FALSE;
        colorBlendAttachmentState.srcColorBlendFactor = VK_BLEND_FACTOR_ZERO;
        colorBlendAttachmentState.dstColorBlendFactor = VK_BLEND_FACTOR_ZERO;
        colorBlendAttachmentState.colorBlendOp = VK_BLEND_OP_ADD;
        colorBlendAttachmentState.srcAlphaBlendFactor = VK_BLEND_FACTOR_ONE;
        colorBlendAttachmentState.dstAlphaBlendFactor = VK_BLEND_FACTOR_ZERO;
        colorBlendAttachmentState.alphaBlendOp = VK_BLEND_OP_ADD;

        //uniform shader数据交换
        VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo{};
        pipelineLayoutCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
        pipelineLayoutCreateInfo.setLayoutCount = 0;
        pipelineLayoutCreateInfo.pSetLayouts = nullptr;
        pipelineLayoutCreateInfo.pushConstantRangeCount = 0;
        pipelineLayoutCreateInfo.pPushConstantRanges = nullptr;
        if (vkCreatePipelineLayout(enter.getDevice(),&pipelineLayoutCreateInfo,nullptr,&pipelineLayout) != VK_SUCCESS)
        {
            throw std::runtime_error("failed to create pipeline layout");
        }

        
    }
}
