#pragma once
#include <string>
#include <GLFW/glfw3.h>

namespace PR_BASE
{
    class PRWindow
    {
    
    public:
        PRWindow(const uint32_t& width,const uint32_t& height,const std::string&  title);
        ~PRWindow();
        void initWindow();
        void destroywindow();
        PRWindow(const PRWindow&) = delete;
        PRWindow& operator=(const PRWindow&) = delete;
        GLFWwindow* getWindow();
    private:
        uint32_t width, height;
        GLFWwindow* window;
        std::string windowTitle;
    };


    
}
