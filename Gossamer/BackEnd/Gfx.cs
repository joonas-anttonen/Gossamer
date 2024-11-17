using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Gossamer.External.Vulkan;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

public record class GfxPresentation(Color ClearColor);
public record class GfxSwapChainPresentation(Color ClearColor, nint Handle, bool EnableVerticalSync) : GfxPresentation(ClearColor);
public record class GfxDirectXPresentation(Color ClearColor, nint Handle, Format Format, uint Width, uint Height) : GfxPresentation(ClearColor);

public record class GfxParameters(
    Gossamer.AppInfo AppInfo,
    bool EnableValidation,
    bool EnableDebugging,
    bool EnableSwapchain
);

public record class GfxCapabilities(
    bool CanValidate,
    bool CanDebug,
    bool CanSwap
);

unsafe class Gfx : IDisposable
{
    readonly Logger logger = Gossamer.Instance.Log.GetLogger(nameof(Gfx), true, true);

    bool isDisposed;

    PFN_vkDebugUtilsMessengerCallbackEXT? VulkanDebugMessengerCallback;
    PFN_vkSetDebugUtilsObjectNameEXT? VulkanDebugSetObjectName;

    VkDebugUtilsMessengerEXT debugUtilsMessenger;

    VkInstance instance;

    GfxParameters parameters = new(
        Gossamer.AppInfo.FromCallingAssembly(),
        EnableValidation: true,
        EnableDebugging: true,
        EnableSwapchain: true
    );

    GfxCapabilities capabilities = new(
        CanValidate: false,
        CanDebug: false,
        CanSwap: false
    );

    public void Create(GfxParameters parameters)
    {
        this.parameters = parameters;

        InitializeVulkanInstance();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        isDisposed = true;

        if (debugUtilsMessenger.HasValue)
        {
            var vkDestroyDebugUtilsMessengerEXT = (delegate*<VkInstance, VkDebugUtilsMessengerEXT, nint, void>)vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT");

            vkDestroyDebugUtilsMessengerEXT(instance, debugUtilsMessenger, default);
            debugUtilsMessenger = default;
        }
    }

    uint VulkanDebugMessageCallback(VkDebugUtilsMessageSeverityEXT severity, VkDebugUtilsMessageTypeEXT type, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, nint pUserData)
    {
        GossamerLog.Level level = severity switch
        {
            VkDebugUtilsMessageSeverityEXT.VERBOSE => GossamerLog.Level.Debug,
            VkDebugUtilsMessageSeverityEXT.INFO => GossamerLog.Level.Information,
            VkDebugUtilsMessageSeverityEXT.WARNING => GossamerLog.Level.Warning,
            VkDebugUtilsMessageSeverityEXT.ERROR => GossamerLog.Level.Error,
            _ => GossamerLog.Level.Debug
        };

        Gossamer.Instance.Log.Append(level, Utf8StringMarshaller.ConvertToManaged((byte*)pCallbackData->pMessage) ?? "", DateTime.Now, "Vulkan", "Validation");
        return 0;
    }

    void InitializeVulkanInstance()
    {
        HashSet<string> availableInstanceLayers = [];
        HashSet<string> availableInstanceExtensions = [];

        uint availableApiVersion = 0;
        ThrowIfFailed(vkEnumerateInstanceVersion(&availableApiVersion), "Vulkan: Failed to get instance version.");

        Version apiVersion = ParseVersion(availableApiVersion);

        // Get available instance layers
        {
            uint instanceLayerCount = 0;
            ThrowIfFailed(vkEnumerateInstanceLayerProperties(&instanceLayerCount, default), "Vulkan: Failed to get instance layer count.");

            VkLayerProperties* layerProperties = stackalloc VkLayerProperties[(int)instanceLayerCount];
            ThrowIfFailed(vkEnumerateInstanceLayerProperties(&instanceLayerCount, layerProperties), "Vulkan: Failed to get instance layers.");

            for (int i = 0; i < instanceLayerCount; i++)
            {
                availableInstanceLayers.Add(new string((sbyte*)layerProperties[i].LayerName));
            }
        }

        // Get available instance extensions
        {
            uint instanceExtensionCount = 0;
            ThrowIfFailed(vkEnumerateInstanceExtensionProperties(default, &instanceExtensionCount), "Vulkan: Failed to get instance extension count.");

            VkExtensionProperties* extensionProperties = stackalloc VkExtensionProperties[(int)instanceExtensionCount];
            ThrowIfFailed(vkEnumerateInstanceExtensionProperties(default, &instanceExtensionCount, extensionProperties), "Vulkan: Failed to get instance extensions.");

            for (int i = 0; i < instanceExtensionCount; i++)
            {
                availableInstanceExtensions.Add(new string((sbyte*)extensionProperties[i].ExtensionName));
            }
        }

        SafeNativeStringArray enabledLayerNames = new(capacity: 8);
        SafeNativeStringArray enabledExtensionNames = new(capacity: 8);

        // Enable required instance layers and extensions
        {
            // VK_KHR_surface should always be available but let's check anyway
            const string VK_KHR_surface = "VK_KHR_surface";
            ThrowIf(!availableInstanceExtensions.Contains(VK_KHR_surface), $"Vulkan: Required feature {VK_KHR_surface} not available.");

            enabledExtensionNames.Add(VK_KHR_surface);

            if (parameters.EnableSwapchain)
            {
                // FIXME: Add support for other platforms

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const string VK_KHR_win32_surface = "VK_KHR_win32_surface";

                    if (availableInstanceExtensions.Contains(VK_KHR_win32_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_win32_surface);
                        capabilities = capabilities with { CanSwap = true };
                    }
                    else
                    {
                        logger.Warning($"Vulkan: Swapchain is enabled but {VK_KHR_win32_surface} is not available.");
                    }
                }
                else
                {
                    logger.Warning("Vulkan: Swapchain is enabled but platform is not supported for it.");
                }
            }

            if (parameters.EnableValidation)
            {
                const string VK_LAYER_KHRONOS_validation = "VK_LAYER_KHRONOS_validation";
                if (availableInstanceLayers.Contains(VK_LAYER_KHRONOS_validation))
                {
                    enabledLayerNames.Add(VK_LAYER_KHRONOS_validation);
                    capabilities = capabilities with { CanValidate = true };
                }
                else
                {
                    logger.Warning($"Vulkan: Validation is enabled but {VK_LAYER_KHRONOS_validation} is not available.");
                }
            }

            if (parameters.EnableDebugging)
            {
                const string VK_EXT_debug_utils = "VK_EXT_debug_utils";
                if (availableInstanceExtensions.Contains(VK_EXT_debug_utils))
                {
                    enabledExtensionNames.Add(VK_EXT_debug_utils);
                    capabilities = capabilities with { CanDebug = true };
                }
                else
                {
                    logger.Warning($"Vulkan: Debugging is enabled but {VK_EXT_debug_utils} is not available.");
                }
            }
        }

        Gossamer.AppInfo engineInfo = Gossamer.AppInfo.FromCallingAssembly();

        using SafeNativeString applicationName = new(parameters.AppInfo.Name);
        using SafeNativeString engineName = new(engineInfo.Name);

        VkApplicationInfo applicationInfo = new(default)
        {
            ApiVersion = MAKE_API_VERSION(0, 1, 2, 0),

            ApplicationName = applicationName.DangerousGetHandle(),
            ApplicationVersion = MAKE_API_VERSION(0, parameters.AppInfo.Version.Major, parameters.AppInfo.Version.Minor, parameters.AppInfo.Version.Build),

            EngineName = engineName.DangerousGetHandle(),
            EngineVersion = MAKE_API_VERSION(0, engineInfo.Version.Major, engineInfo.Version.Minor, engineInfo.Version.Build),
        };

        VkInstanceCreateInfo instanceCreateInfo = new(default)
        {
            ApplicationInfo = &applicationInfo,

            EnabledExtensionCount = (uint)enabledExtensionNames.Count,
            EnabledExtensionNames = enabledExtensionNames.DangerousGetHandle(),

            EnabledLayerCount = (uint)enabledLayerNames.Count,
            EnabledLayerNames = enabledLayerNames.DangerousGetHandle(),
        };

        VkInstance instance = default;
        ThrowIfFailed(vkCreateInstance(&instanceCreateInfo, default, &instance), "Vulkan: Failed to create instance.");
        this.instance = instance;

        if (parameters.EnableDebugging && capabilities.CanDebug)
        {
            const string STR_vkCreateDebugUtilsMessengerEXT = "vkCreateDebugUtilsMessengerEXT";
            const string STR_vkDestroyDebugUtilsMessengerEXT = "vkDestroyDebugUtilsMessengerEXT";
            const string STR_vkSetDebugUtilsObjectNameEXT = "vkSetDebugUtilsObjectNameEXT";

            nint debugCreateUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkCreateDebugUtilsMessengerEXT);
            if (debugCreateUtilsMessengerPfn == nint.Zero)
            {
                logger.Warning($"Vulkan: Debugging is enabled but {STR_vkCreateDebugUtilsMessengerEXT} is not available.");
            }
            nint debugDestroyUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkDestroyDebugUtilsMessengerEXT);
            if (debugDestroyUtilsMessengerPfn == nint.Zero)
            {
                logger.Warning($"Vulkan: Debugging is enabled but {STR_vkDestroyDebugUtilsMessengerEXT} is not available.");
            }
            nint debugUtilsObjectNamePfn = vkGetInstanceProcAddr(instance, STR_vkSetDebugUtilsObjectNameEXT);
            if (debugUtilsObjectNamePfn == nint.Zero)
            {
                logger.Warning($"Vulkan: Debugging is enabled but {STR_vkSetDebugUtilsObjectNameEXT} is not available.");
            }

            bool debugFunctionsOk = debugCreateUtilsMessengerPfn != nint.Zero && debugDestroyUtilsMessengerPfn != nint.Zero && debugUtilsObjectNamePfn != nint.Zero;
            if (debugFunctionsOk)
            {
                VulkanDebugMessengerCallback = VulkanDebugMessageCallback;
                VulkanDebugSetObjectName = Marshal.GetDelegateForFunctionPointer<PFN_vkSetDebugUtilsObjectNameEXT>(debugUtilsObjectNamePfn);

                VkDebugUtilsMessengerCreateInfoEXT debugUtilsMessengerCreateInfo = new(default)
                {
                    MessageSeverity = VkDebugUtilsMessageSeverityEXT.ERROR | VkDebugUtilsMessageSeverityEXT.WARNING,
                    MessageType = VkDebugUtilsMessageTypeEXT.GENERAL | VkDebugUtilsMessageTypeEXT.VALIDATION,
                    UserCallback = Marshal.GetFunctionPointerForDelegate(VulkanDebugMessengerCallback),
                };

                var vkCreateDebugUtilsMessengerEXT = (delegate*<VkInstance, VkDebugUtilsMessengerCreateInfoEXT*, nint, VkDebugUtilsMessengerEXT*, VkResult>)debugCreateUtilsMessengerPfn;

                VkDebugUtilsMessengerEXT debugUtilsMessenger = default;
                ThrowIfFailed(vkCreateDebugUtilsMessengerEXT(instance, &debugUtilsMessengerCreateInfo, default, &debugUtilsMessenger), "Vulkan: Failed to create debug utils messenger.");
                this.debugUtilsMessenger = debugUtilsMessenger;
            }
            else
            {
                capabilities = capabilities with { CanDebug = false };
            }
        }
    }
}
