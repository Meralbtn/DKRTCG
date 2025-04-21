#pragma once
#include <vulkan/vulkan.h>
#include <PRWindow.h>
#include <Pipeline.h>
#include<vector>
#include<optional>
const std::vector<const char*> validationLayers = {
    "VK_LAYER_KHRONOS_validation"
};
#ifdef NDEBUG
const bool enableValidationLayers = false;
#else
const bool enableValidationLayers = true;
#endif

namespace PR_BASE
{
    struct QueueFamilyIndices
    {
        std::optional<uint32_t> graphicsFamily;
        //获取支持表示的队列
        std::optional<uint32_t> presentFamily;
        bool isComplete();
    };
    
    struct SwapChainSupportDetails
    {
        VkSurfaceCapabilitiesKHR capabilities;
        //颜色模式
        std::vector<VkSurfaceFormatKHR> formats;
        //表示模式
        std::vector<VkPresentModeKHR> presentModes;
    };
    
    class APPEnter
    {
    public:
        APPEnter();
        void run();
        ~APPEnter();
        //APPEnter(const APPEnter&) = delete;
        //APPEnter& operator=(const APPEnter&) = delete;
        VkExtent2D getSwapChainExtent();
        VkFormat getVkFormat();
        VkDevice getDevice();
    private:
        void createInstance();

        VkSemaphore imageAcquiredSemaphore;
        VkSemaphore renderFinishedSemaphore;
        VkFence fence;
        
        PRWindow* window;
        VkInstance instance;
        VkPhysicalDevice physicalDevice = VK_NULL_HANDLE;
        VkDevice device;
        
        //隐式删除
        Pipeline pipeline;
        VkQueue graphicsQueue;
        
        VkQueue presentQueue;
        VkSurfaceKHR surface;
        uint32_t width=600;
        uint32_t height=800;
        std::string appName="Vulkan";
        VkSwapchainKHR swapChain;
        VkExtent2D swapChainExtent;
        VkFormat swapChainImageFormat;
        std::vector<VkImageView> swapImageViews;
        std::vector<VkImage> swapChainImages;

        std::vector<VkFramebuffer> swapChainFramebuffers;

        VkCommandPool commandPool;

        VkCommandBuffer commandBuffer;
        const std::vector<const char*> deviceExtensions = {VK_KHR_SWAPCHAIN_EXTENSION_NAME};
        void initVulkan();
        void setWindow();
        void update();
        void mainLoop();
        void destroy();
        bool checkValidationLayerSupport();
        void pickPhysicsDevice();
        bool isDeviceSuitable(const VkPhysicalDevice& device);
        QueueFamilyIndices findQueueFamilies(VkPhysicalDevice device);
        void createSurfaceKHR();
        void createLogicalDevice();
        bool checkDeviceExtensionSupport(const VkPhysicalDevice& device);
        void createSwapChain();
        
        void createImageViews();

        void createFramebuffers();
        //创建命令池，所有的绘制命令在command buffer中执行而不是函数
        void createCommandPool();

        void createCommandBuffer();
        void createSyncObjects();
        void recordCommandBuffer(VkCommandBuffer commandBuffer, uint32_t imageIndex);
        void drawFrame();
        SwapChainSupportDetails querySwapChainSupport(const VkPhysicalDevice& device);
        

        VkSurfaceFormatKHR chooseSwapSurfaceFormat(const std::vector<VkSurfaceFormatKHR>& availableFormats);
        VkPresentModeKHR chooseSwapPresentMode(const std::vector<VkPresentModeKHR>& availablePresentModes);
        VkExtent2D chooseSwapExtent(const VkSurfaceCapabilitiesKHR& capabilities);
    };
}
