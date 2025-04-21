#include "APPEnter.h"
#include <iostream>
#include <stdexcept>
#include <set>
#include <cstdint>
#include <limits>
#include<algorithm>
#define VK_USE_PLATFORM_WIN32_KHR
#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>
#define GLFW_EXPOSE_NATIVE_WIN32
#include <GLFW/glfw3native.h>
#include <vulkan/vulkan_win32.h>

void PR_BASE::APPEnter::initVulkan()
{
    createInstance();
    checkValidationLayerSupport();
    createSurfaceKHR();
    
    pickPhysicsDevice();
    createLogicalDevice();
    createSwapChain();
    createImageViews();
    pipeline.createRenderPass(this);
    pipeline.createPipeline(this);
    createFramebuffers();
    createCommandPool();
    createCommandBuffer();
    createSyncObjects();
}

void PR_BASE::APPEnter::setWindow()
{
    window =new PRWindow(width,height,appName);
    window->initWindow();
}

bool PR_BASE::QueueFamilyIndices::isComplete()
{
    return graphicsFamily.has_value()&&presentFamily.has_value();
}

PR_BASE::APPEnter::APPEnter()
{
    
}

void PR_BASE::APPEnter::run()
{
    setWindow();
    initVulkan();
    
    
    mainLoop();
    
    destroy();
}

VkExtent2D PR_BASE::APPEnter::getSwapChainExtent() 
{
    return swapChainExtent;
}

VkFormat PR_BASE::APPEnter::getVkFormat() 
{
    return swapChainImageFormat;
}

VkDevice PR_BASE::APPEnter::getDevice() 
{
    return device;
}

void PR_BASE::APPEnter::createInstance()
{
    //检测验证层是否有效
    if (enableValidationLayers && !checkValidationLayerSupport()) {
        throw std::runtime_error("validation layers requested, but not available!");
    }
    
    //创建vulkan实例
    VkApplicationInfo appInfo;
    appInfo.sType = VK_STRUCTURE_TYPE_APPLICATION_INFO;
    appInfo.pApplicationName = "APP Enter";
    appInfo.applicationVersion = VK_MAKE_VERSION(1, 0, 0);
    appInfo.pEngineName = "No Engine";
    appInfo.engineVersion = VK_MAKE_VERSION(1, 0, 0);
    appInfo.apiVersion = VK_API_VERSION_1_0;
    appInfo.pNext=NULL;
    
    VkInstanceCreateInfo createInfo{};
    createInfo.sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
    createInfo.pApplicationInfo = &appInfo;
    //设置window拓展接口
    uint32_t glfwExtensionCount = 0;
    const char** glfwExtensions = glfwGetRequiredInstanceExtensions(&glfwExtensionCount);
    createInfo.enabledExtensionCount = glfwExtensionCount;
    createInfo.ppEnabledExtensionNames = glfwExtensions;

    //设置debug验证层
    if (enableValidationLayers)
    {
        createInfo.enabledLayerCount = static_cast<uint32_t>(validationLayers.size());
        createInfo.ppEnabledLayerNames = validationLayers.data();
    }
    else
    {
        createInfo.enabledLayerCount = 0;
    }
    

    if (vkCreateInstance(&createInfo, NULL, &instance)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create instance");
    }
}

void PR_BASE::APPEnter::update()
{
}

void PR_BASE::APPEnter::mainLoop()
{
    while (!glfwWindowShouldClose(window->getWindow()))
    {
        glfwPollEvents();
        drawFrame();
    }   
}

void PR_BASE::APPEnter::destroy()
{
    //子物体销毁在instance
    for (auto imageView : swapImageViews)
    {
        vkDestroyImageView(device,imageView,NULL);
    }

    for (auto framebuffer :swapChainFramebuffers)
    {
        vkDestroyFramebuffer(device,framebuffer,NULL);
    }

    vkDestroySemaphore(device,imageAcquiredSemaphore,NULL);
    vkDestroySemaphore(device,renderFinishedSemaphore,NULL);
    vkDestroyFence(device,fence,NULL);
    
    vkDestroyCommandPool(device,commandPool,NULL);
    
    vkDestroySwapchainKHR(device,swapChain,NULL);

    pipeline.destroyPipe(device);
    vkDestroyDevice(device,NULL);
    vkDestroySurfaceKHR(instance,surface,NULL);
    vkDestroyInstance(instance, NULL);
    
    window->destroywindow();
    glfwTerminate();
}

bool PR_BASE::APPEnter::checkValidationLayerSupport()
{
    uint32_t layerCount;
    vkEnumerateInstanceLayerProperties(&layerCount, NULL);
    std::vector<VkLayerProperties> availableLayers(layerCount);
    vkEnumerateInstanceLayerProperties(&layerCount, availableLayers.data());
    for (const char* layerName : validationLayers)
    {
        bool layerFound = false;
        for (const auto& layerProperties : availableLayers)
        {
            if (strcmp(layerName, layerProperties.layerName) == 0)
            {
                layerFound = true;
                break;
            }
        }
        if (!layerFound)
            return false;
    }
    return true;
}

void PR_BASE::APPEnter::pickPhysicsDevice()
{
    //获取device列表
    uint32_t deviceCount = 0;
    vkEnumeratePhysicalDevices(instance, &deviceCount, NULL);
    if (deviceCount == 0)
    {
        throw std::runtime_error("failed to find GPUs with Vulkan support!");
    }
    std::vector<VkPhysicalDevice> physicalDevices(deviceCount);
    vkEnumeratePhysicalDevices(instance, &deviceCount, physicalDevices.data());

    //循环检查满足的设备，找到就退出
    for (const auto& device : physicalDevices)
    {
        if (isDeviceSuitable(device))
        {
            physicalDevice = device;
            break;
        }
    }
    if (physicalDevice == VK_NULL_HANDLE)
    {
        throw std::runtime_error("failed to find suitable device !");
    }
}

bool PR_BASE::APPEnter::isDeviceSuitable(const VkPhysicalDevice& device)
{
    bool extensionSupported = checkDeviceExtensionSupport(device);
    bool swapChainAdequate = false;

    //检查交换链支持是否为空
    if (extensionSupported)
    {
        SwapChainSupportDetails swapChainSupport = querySwapChainSupport(device);
        swapChainAdequate = !swapChainSupport.formats.empty() && !swapChainSupport.presentModes.empty();
    }
    
    return findQueueFamilies(device).isComplete() && extensionSupported && swapChainAdequate;
}

PR_BASE::QueueFamilyIndices PR_BASE::APPEnter::findQueueFamilies(VkPhysicalDevice device)
{
    //确认队列是否可用
    uint32_t queueFamilyCount = 0;
    vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, NULL);
    std::vector<VkQueueFamilyProperties> queueFamilies(queueFamilyCount);
    vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies.data());
    int i=0;
    QueueFamilyIndices indices;
    VkBool32 presentSupport = false;
    for (const auto& queueFamily : queueFamilies)
    {
        if (queueFamily.queueFlags & VK_QUEUE_GRAPHICS_BIT)
        {
            indices.graphicsFamily = i;
        }
        presentSupport = false;
        vkGetPhysicalDeviceSurfaceSupportKHR(device, i, surface, &presentSupport);
        if (presentSupport)
        {
            indices.presentFamily = i;
        }
        i++;
    }
    return indices;
}

void PR_BASE::APPEnter::createLogicalDevice()
{
    //创建逻辑设备 需要queueinfo physicalDeviceFeature queuecreateinfo可以接受多个queue
    QueueFamilyIndices indices = findQueueFamilies(physicalDevice);
    std::vector<VkDeviceQueueCreateInfo> queueCreateInfos;
    std::set<uint32_t> uniqueQueueFamilies{indices.graphicsFamily.value(),indices.presentFamily.value()};

    for (uint32_t QueueFamily : uniqueQueueFamilies)
    {
        VkDeviceQueueCreateInfo queueCreateInfo{};
        queueCreateInfo.sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
        queueCreateInfo.queueFamilyIndex = QueueFamily;
        queueCreateInfo.queueCount = 1;
        float queuePriority = 1.0f;
        queueCreateInfo.pQueuePriorities = &queuePriority;
        queueCreateInfos.push_back(queueCreateInfo);
    }
    
    VkPhysicalDeviceFeatures physicalDeviceFeatures{};
    
    VkDeviceCreateInfo deviceCreateInfo{};
    deviceCreateInfo.sType = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
    deviceCreateInfo.queueCreateInfoCount = static_cast<uint32_t>(queueCreateInfos.size());
    deviceCreateInfo.pQueueCreateInfos = queueCreateInfos.data();
    deviceCreateInfo.pEnabledFeatures = &physicalDeviceFeatures;

    deviceCreateInfo.enabledExtensionCount = static_cast<uint32_t>(deviceExtensions.size());
    deviceCreateInfo.ppEnabledExtensionNames = deviceExtensions.data();

    if (enableValidationLayers)
    {
        deviceCreateInfo.enabledLayerCount =static_cast<uint32_t>(validationLayers.size());
        deviceCreateInfo.ppEnabledLayerNames = validationLayers.data();
    }
    else
    {
        deviceCreateInfo.enabledLayerCount=0;
    }
    
    if(vkCreateDevice(physicalDevice, &deviceCreateInfo, NULL, &device)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create logical device");
    }

    vkGetDeviceQueue(device,indices.graphicsFamily.value(),0, &graphicsQueue);
    vkGetDeviceQueue(device,indices.presentFamily.value(),0, &presentQueue);
}

void PR_BASE::APPEnter::createSurfaceKHR()
{
    VkWin32SurfaceCreateInfoKHR surfaceCreateInfo{};
    surfaceCreateInfo.sType = VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR;
    surfaceCreateInfo.hwnd = glfwGetWin32Window(window->getWindow());
    surfaceCreateInfo.hinstance = GetModuleHandle(nullptr);

    if (vkCreateWin32SurfaceKHR(instance,&surfaceCreateInfo,nullptr,&surface)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create window surface!");
    }
}

bool PR_BASE::APPEnter::checkDeviceExtensionSupport(const VkPhysicalDevice& device)
{
    uint32_t extensionCount;
    vkEnumerateDeviceExtensionProperties(device,0,&extensionCount,NULL);
    std::vector<VkExtensionProperties> extensions(extensionCount);
    vkEnumerateDeviceExtensionProperties(device,0,&extensionCount,extensions.data());
    std::set<std::string> requireExtension(deviceExtensions.begin(),deviceExtensions.end());
    for (const auto& extension : extensions)
    {
        requireExtension.erase(extension.extensionName);
    }
    return requireExtension.empty();
}

void PR_BASE::APPEnter::createSwapChain()
{
    SwapChainSupportDetails swapChainSupportDetails = querySwapChainSupport(physicalDevice);
    VkSurfaceFormatKHR surfaceFormat = chooseSwapSurfaceFormat(swapChainSupportDetails.formats);
    VkPresentModeKHR  presentMode = chooseSwapPresentMode(swapChainSupportDetails.presentModes);
    VkExtent2D extent = chooseSwapExtent(swapChainSupportDetails.capabilities);

    uint32_t imageCount = swapChainSupportDetails.capabilities.minImageCount + 1;
    if (swapChainSupportDetails.capabilities.maxImageCount > 0 && imageCount > swapChainSupportDetails.capabilities.maxImageCount)
    {
        imageCount = swapChainSupportDetails.capabilities.maxImageCount;
    }
    //设置创建info
    VkSwapchainCreateInfoKHR swapChainCreateInfo{};
    swapChainCreateInfo.sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR;
    swapChainCreateInfo.surface = surface;

    swapChainCreateInfo.minImageCount = imageCount;
    swapChainCreateInfo.imageFormat = surfaceFormat.format;
    swapChainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
    swapChainCreateInfo.imageExtent = extent;
    swapChainCreateInfo.imageArrayLayers = 1;
    swapChainCreateInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;

    QueueFamilyIndices indices = findQueueFamilies(physicalDevice);
    uint32_t queueFamilyIndices[] = {indices.graphicsFamily.value(),indices.presentFamily.value()};

    //当队列索引不同
    if (indices.graphicsFamily.value() != indices.presentFamily.value())
    {
        swapChainCreateInfo.imageSharingMode = VK_SHARING_MODE_CONCURRENT;
        swapChainCreateInfo.queueFamilyIndexCount = 2;
        swapChainCreateInfo.pQueueFamilyIndices = queueFamilyIndices;
    }else
    {
        swapChainCreateInfo.imageSharingMode = VK_SHARING_MODE_EXCLUSIVE;
        swapChainCreateInfo.queueFamilyIndexCount = 0;
        swapChainCreateInfo.pQueueFamilyIndices = nullptr;
    }
    //图像旋转设定
    swapChainCreateInfo.preTransform = swapChainSupportDetails.capabilities.currentTransform;
    //指定是否和windows system中其他窗口混合
    swapChainCreateInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;

    swapChainCreateInfo.presentMode = presentMode;
    swapChainCreateInfo.clipped = VK_TRUE;
    //在程序运行时交换链可能失效，需要重新创建
    swapChainCreateInfo.oldSwapchain = VK_NULL_HANDLE;
    
    if (vkCreateSwapchainKHR(device,&swapChainCreateInfo,nullptr,&swapChain)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create swapchain!");
    }
    
    uint32_t swapChainImageCount;
    vkGetSwapchainImagesKHR(device,swapChain,&swapChainImageCount,nullptr);
    swapChainImages.resize(swapChainImageCount);
    vkGetSwapchainImagesKHR(device,swapChain,&swapChainImageCount,swapChainImages.data());

    swapChainImageFormat = surfaceFormat.format;
    swapChainExtent = extent;
}

void PR_BASE::APPEnter::createImageViews()
{
    swapImageViews.resize(swapChainImages.size());
    for (size_t i = 0; i < swapImageViews.size(); i++)
    {
        VkImageViewCreateInfo viewCreateInfo{};
        viewCreateInfo.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
        viewCreateInfo.image = swapChainImages[i];
        viewCreateInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
        viewCreateInfo.format = swapChainImageFormat;
        //设置纹理颜色
        viewCreateInfo.components.r= VK_COMPONENT_SWIZZLE_IDENTITY;
        viewCreateInfo.components.g= VK_COMPONENT_SWIZZLE_IDENTITY;
        viewCreateInfo.components.b= VK_COMPONENT_SWIZZLE_IDENTITY;
        viewCreateInfo.components.a= VK_COMPONENT_SWIZZLE_IDENTITY;
        //不启用mipmap
        viewCreateInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
        viewCreateInfo.subresourceRange.baseMipLevel = 0;
        viewCreateInfo.subresourceRange.levelCount = 1;
        viewCreateInfo.subresourceRange.baseArrayLayer = 0;
        viewCreateInfo.subresourceRange.layerCount = 1;

        vkCreateImageView(device,&viewCreateInfo,nullptr,&swapImageViews[i]);
    }
    
}

void PR_BASE::APPEnter::createFramebuffers()
{
    swapChainFramebuffers.resize(swapChainImages.size());
    for (size_t i = 0; i < swapChainFramebuffers.size(); i++)
    {
        VkImageView imageView[] ={swapImageViews[i]};

        VkFramebufferCreateInfo framebufferCreateInfo{};
        framebufferCreateInfo.sType = VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
        framebufferCreateInfo.renderPass = pipeline.renderPass;
        framebufferCreateInfo.attachmentCount = 1;
        framebufferCreateInfo.pAttachments = imageView;
        framebufferCreateInfo.width = swapChainExtent.width;
        framebufferCreateInfo.height = swapChainExtent.height;
        framebufferCreateInfo.layers = 1;

        if (vkCreateFramebuffer(device,&framebufferCreateInfo,nullptr,&swapChainFramebuffers[i])!=VK_SUCCESS)
        {
            throw std::runtime_error("failed to create framebuffer!");
        }
    }
}

void PR_BASE::APPEnter::createCommandPool()
{
    QueueFamilyIndices queueFamilies = findQueueFamilies(physicalDevice);
    VkCommandPoolCreateInfo poolCreateInfo{};
    poolCreateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
    poolCreateInfo.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
    poolCreateInfo.queueFamilyIndex = queueFamilies.graphicsFamily.value();

    if (vkCreateCommandPool(device,&poolCreateInfo,nullptr,&commandPool)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create command pool!");
    }
}

void PR_BASE::APPEnter::createCommandBuffer()
{
    //命令缓冲区使用vkAllocateCommandBuffers来指定命令池和要分配的缓冲区数量
    VkCommandBufferAllocateInfo allocateInfo{};
    allocateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
    allocateInfo.commandPool = commandPool;
    allocateInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    allocateInfo.commandBufferCount = 1;

    if (vkAllocateCommandBuffers(device,&allocateInfo,&commandBuffer)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to create command buffers!");
    }
}

void PR_BASE::APPEnter::createSyncObjects()
{
    //创建围栏确保过程同步
    VkSemaphoreCreateInfo semaphoreCreateInfo{};
    semaphoreCreateInfo.sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO;
    VkFenceCreateInfo fenceCreateInfo{};
    //确保第一帧的时候不会卡住
    fenceCreateInfo.sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
    fenceCreateInfo.flags = VK_FENCE_CREATE_SIGNALED_BIT;
    if (vkCreateSemaphore(device, &semaphoreCreateInfo, nullptr, &imageAcquiredSemaphore) != VK_SUCCESS ||
    vkCreateSemaphore(device, &semaphoreCreateInfo, nullptr, &renderFinishedSemaphore) != VK_SUCCESS ||
    vkCreateFence(device, &fenceCreateInfo, nullptr, &fence) != VK_SUCCESS) {
        throw std::runtime_error("failed to create semaphores!");
    }
}

void PR_BASE::APPEnter::recordCommandBuffer(VkCommandBuffer commandBuffer, uint32_t imageIndex)
{
    VkCommandBufferBeginInfo beginInfo{};
    beginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
    beginInfo.flags = 0;
    beginInfo.pInheritanceInfo = nullptr;

    if (vkBeginCommandBuffer(commandBuffer,&beginInfo)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to begin record command buffer!");
    }

    VkRenderPassBeginInfo renderPassInfo{};
    renderPassInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
    renderPassInfo.renderPass = pipeline.renderPass;
    renderPassInfo.framebuffer = swapChainFramebuffers[imageIndex];
    //设定渲染的空间
    renderPassInfo.renderArea.offset = {0, 0};
    renderPassInfo.renderArea.extent = swapChainExtent;
    //设定清空时的颜色
    VkClearValue clearColor = {{{0.0f,0.0f,0.0f,1.0f}}};
    renderPassInfo.clearValueCount = 1;
    renderPassInfo.pClearValues = &clearColor;
    //开始渲染
    vkCmdBeginRenderPass(commandBuffer,&renderPassInfo,VK_SUBPASS_CONTENTS_INLINE);
    //绑定管道
    vkCmdBindPipeline(commandBuffer,VK_PIPELINE_BIND_POINT_GRAPHICS,pipeline.pipeline);

    //设置动态视口大小
    VkViewport viewport{};
    viewport.x = 0.0f;
    viewport.y = 0.0f;
    viewport.width = static_cast<float>(swapChainExtent.width);
    viewport.height = static_cast<float>(swapChainExtent.height);
    viewport.minDepth = 0.0f;
    viewport.maxDepth = 1.0f;
    vkCmdSetViewport(commandBuffer,0,1,&viewport);
    VkRect2D scissor{};
    scissor.offset = {0, 0};
    scissor.extent = swapChainExtent;
    vkCmdSetScissor(commandBuffer,0,1,&scissor);

    vkCmdDraw(commandBuffer,3,1,0,0);
    vkCmdEndRenderPass(commandBuffer);
    //记录是否成功
    if (vkEndCommandBuffer(commandBuffer)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to record command buffer!");
    }
}

void PR_BASE::APPEnter::drawFrame()
{
    vkWaitForFences(device,1,&fence,VK_TRUE,UINT64_MAX);
    vkResetFences(device,1,&fence);
    uint32_t imageIndex ;
    vkAcquireNextImageKHR(device,swapChain,UINT64_MAX,imageAcquiredSemaphore,VK_NULL_HANDLE,&imageIndex);
    vkResetCommandBuffer(commandBuffer,0);
    recordCommandBuffer(commandBuffer,imageIndex);
    //录制好将命令缓冲区提交，等待image接受
    VkSubmitInfo submitInfo{};
    submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
    VkSemaphore waitSemaphores[] = {imageAcquiredSemaphore};
    VkPipelineStageFlags waitStages[] = {VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT};
    submitInfo.waitSemaphoreCount = 1;
    submitInfo.pWaitSemaphores = waitSemaphores;
    submitInfo.pWaitDstStageMask = waitStages;

    submitInfo.commandBufferCount = 1;
    submitInfo.pCommandBuffers = &commandBuffer;

    VkSemaphore signalSemaphores[] = {renderFinishedSemaphore};
    submitInfo.signalSemaphoreCount = 1;
    submitInfo.pSignalSemaphores = signalSemaphores;

    if (vkQueueSubmit(graphicsQueue,1,&submitInfo,fence)!=VK_SUCCESS)
    {
        throw std::runtime_error("failed to submit draw command buffer!");
    }

    //绘制在交换链上
    VkPresentInfoKHR presentInfo{};
    presentInfo.sType = VK_STRUCTURE_TYPE_PRESENT_INFO_KHR;
    presentInfo.waitSemaphoreCount = 1;
    presentInfo.pWaitSemaphores = &signalSemaphores[0];
    
    VkSwapchainKHR swapChains[] = {swapChain};
    presentInfo.swapchainCount = 1;
    presentInfo.pSwapchains = swapChains;
    presentInfo.pImageIndices = &imageIndex;
    presentInfo.pResults = nullptr;

    vkQueuePresentKHR(presentQueue,&presentInfo);
}


//获取物理显卡中支持的交换链参数列表
PR_BASE::SwapChainSupportDetails PR_BASE::APPEnter::querySwapChainSupport(const VkPhysicalDevice& device)
{
    SwapChainSupportDetails details;
    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device,surface,&details.capabilities);
    uint32_t formatCount=0;
    vkGetPhysicalDeviceSurfaceFormatsKHR(device,surface,&formatCount,NULL);
    if (formatCount !=0)
    {
        details.formats.resize(formatCount);
        vkGetPhysicalDeviceSurfaceFormatsKHR(device,surface,&formatCount,details.formats.data());
    }

    uint32_t presentModeCount=0;
    vkGetPhysicalDeviceSurfacePresentModesKHR(device,surface,&presentModeCount,NULL);
    if (presentModeCount !=0)
    {
        details.presentModes.resize(presentModeCount);
        vkGetPhysicalDeviceSurfacePresentModesKHR(device,surface,&presentModeCount,details.presentModes.data());
    }
    
    return details;
}

VkSurfaceFormatKHR PR_BASE::APPEnter::chooseSwapSurfaceFormat(const std::vector<VkSurfaceFormatKHR>& availableFormats)
{
    //当交互链通过后选择相应的设置Surface format (color depth)，Presentation mode (conditions for "swapping" images to the screen)，Swap extent (resolution of images in swap chain)

    //寻找srgb的format并返回
    for (const auto& availableFormat : availableFormats)
    {
        if (availableFormat.format == VK_FORMAT_B8G8R8A8_SRGB && availableFormat.colorSpace ==VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
        {
            return availableFormat;
        }
    }
    return availableFormats[0];
}

VkPresentModeKHR PR_BASE::APPEnter::chooseSwapPresentMode(const std::vector<VkPresentModeKHR>& availablePresentModes)
{
    for (const auto& availablePresentMode : availablePresentModes)
    {
        if (availablePresentMode == VK_PRESENT_MODE_MAILBOX_KHR)
            return availablePresentMode;
    }
    return VK_PRESENT_MODE_FIFO_KHR;
}

VkExtent2D PR_BASE::APPEnter::chooseSwapExtent(const VkSurfaceCapabilitiesKHR& capabilities)
{
    //设置交换链像素大小
    //max定义和windows重复
    if (capabilities.currentExtent.width != (std::numeric_limits<uint32_t>::max)())
    {
        return capabilities.currentExtent;
    }else{
        
        int width, height;
        glfwGetFramebufferSize(window->getWindow(),&width,&height);
        VkExtent2D actualExtent = {
            static_cast<uint32_t>(width),
            static_cast<uint32_t>(height)
        };
        //确保大小在显卡支持的范围中
        actualExtent.width = std::clamp(actualExtent.width,capabilities.minImageExtent.width,capabilities.maxImageExtent.width);
        actualExtent.height = std::clamp(actualExtent.height,capabilities.minImageExtent.height,capabilities.maxImageExtent.height);
        return actualExtent;
    }
}

PR_BASE::APPEnter::~APPEnter()
{
    delete window;
}
