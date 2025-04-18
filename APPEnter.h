#pragma once
#include <vulkan/vulkan.h>
#include <PRWindow.h>
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
        APPEnter(const APPEnter&) = delete;
        APPEnter& operator=(const APPEnter&) = delete;
        const VkExtent2D getSwapChainExtent()const;
        VkFormat getVkFormat()const;
        VkDevice getDevice()const;
    private:
        void createInstance();
        PRWindow* window;
        VkInstance instance;
        VkPhysicalDevice physicalDevice = VK_NULL_HANDLE;
        VkDevice device;
        
        //隐式删除
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

        void createPipeline();
        SwapChainSupportDetails querySwapChainSupport(const VkPhysicalDevice& device);
        

        VkSurfaceFormatKHR chooseSwapSurfaceFormat(const std::vector<VkSurfaceFormatKHR>& availableFormats);
        VkPresentModeKHR chooseSwapPresentMode(const std::vector<VkPresentModeKHR>& availablePresentModes);
        VkExtent2D chooseSwapExtent(const VkSurfaceCapabilitiesKHR& capabilities);
    };
}
