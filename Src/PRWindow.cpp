#include <PRWindow.h>

namespace PR_BASE
{
    PRWindow::PRWindow(const uint32_t& width,const uint32_t& height,const std::string& title):width(width),height(height)
    ,windowTitle(title)
    {
    
    }

    PRWindow::~PRWindow()
    {
        glfwDestroyWindow(window);
    }

    void PRWindow::initWindow()
    {
        //初始化窗口
        glfwInit();
        //禁止opengl
        glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
        glfwWindowHint(GLFW_RESIZABLE, GLFW_FALSE);

        window=glfwCreateWindow(width,height,windowTitle.c_str(),NULL,NULL);
    }

    GLFWwindow* PRWindow::getWindow()
    {
        return window;
    }
    void PRWindow::destroywindow()
    {
        glfwDestroyWindow(window);
    }
}
