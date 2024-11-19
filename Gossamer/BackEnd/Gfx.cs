using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security;

using Gossamer.External.Vulkan;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

public enum Format
{

}

public enum GfxPhysicalDeviceType
{
    Discrete,
    Integrated,
    Virtual,
    Cpu,
    Other
}

public record class GfxPresentation(Color ClearColor);
public record class GfxSwapChainPresentation(Color ClearColor, nint Handle, bool EnableVerticalSync) : GfxPresentation(ClearColor);
public record class GfxDirectXPresentation(Color ClearColor, nint Handle, Format Format, uint Width, uint Height) : GfxPresentation(ClearColor);

public record class GfxApiParameters(
    Gossamer.AppInfo AppInfo,
    bool EnableValidation,
    bool EnableDebugging,
    bool EnableSwapchain
);

public record class GfxParameters(
    GfxPhysicalDevice PhysicalDevice
)
{
    /// <summary>
    /// Selects the optimal device from the given array of devices. The optimal device is a discrete GPU if available, otherwise an integrated GPU.
    /// </summary>
    /// <param name="devices"></param>
    /// <returns></returns>
    public static GfxPhysicalDevice SelectOptimalDevice(GfxPhysicalDevice[] devices)
    {
        ThrowInvalidDataIf(devices.Length == 0, "No devices available.");

        GfxPhysicalDevice? selectedDevice = null;
        foreach (GfxPhysicalDevice device in devices)
        {
            if (device.Type == GfxPhysicalDeviceType.Discrete)
            {
                selectedDevice = device;
                break;
            }
            else if (device.Type == GfxPhysicalDeviceType.Integrated && selectedDevice == null)
            {
                selectedDevice = device;
            }
        }

        return selectedDevice ?? devices[0];
    }
}

public record class GfxCapabilities(
    bool CanValidate,
    bool CanDebug,
    bool CanSwap
);

public record class GfxPhysicalDevice(
    GfxPhysicalDeviceType Type,
    Guid Id,
    string Name,
    Version DriverVersion,
    Version ApiVersion
);

unsafe class Gfx : IDisposable
{
    readonly Logger logger = Gossamer.Instance.Log.GetLogger(nameof(Gfx), true, true);

    bool isDisposed;

    PFN_vkDebugUtilsMessengerCallbackEXT? VulkanDebugMessengerCallback;
    PFN_vkSetDebugUtilsObjectNameEXT? VulkanDebugSetObjectName;

    VkDebugUtilsMessengerEXT debugUtilsMessenger;

    VkInstance instance;
    VkDevice device;

    readonly GfxApiParameters apiParameters;
    GfxParameters? parameters;

    GfxCapabilities capabilities = new(
        CanValidate: false,
        CanDebug: false,
        CanSwap: false
    );

    public Gfx(GfxApiParameters apiParameters)
    {
        this.apiParameters = apiParameters;

        InitializeVulkanInstance();
    }

    public void Create(GfxParameters parameters)
    {
        this.parameters = parameters;
        InitializeVulkanDevice();
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

        uint availableApiVersionRaw = 0;
        ThrowIfFailed(vkEnumerateInstanceVersion(&availableApiVersionRaw), "Vulkan: Failed to get instance version.");

        Version availableApiVersion = ParseVersion(availableApiVersionRaw);

        // Check if the available version is compatible with the required version
        {
            Version requiredApiVersion = new(1, 3, 0);
            ThrowNotSupportedIf(availableApiVersion < requiredApiVersion, $"Vulkan: Required API version {requiredApiVersion} not available.");
        }

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
            ThrowNotSupportedIf(!availableInstanceExtensions.Contains(VK_KHR_surface), $"Vulkan: Required feature {VK_KHR_surface} not available.");

            enabledExtensionNames.Add(VK_KHR_surface);

            if (apiParameters.EnableSwapchain)
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

            if (apiParameters.EnableValidation)
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

            if (apiParameters.EnableDebugging)
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

        using SafeNativeString applicationName = new(apiParameters.AppInfo.Name);
        using SafeNativeString engineName = new(engineInfo.Name);

        VkApplicationInfo applicationInfo = new(default)
        {
            ApiVersion = availableApiVersionRaw,

            ApplicationName = applicationName.DangerousGetHandle(),
            ApplicationVersion = MakeApiVersion(0, apiParameters.AppInfo.Version.Major, apiParameters.AppInfo.Version.Minor, apiParameters.AppInfo.Version.Build),

            EngineName = engineName.DangerousGetHandle(),
            EngineVersion = MakeApiVersion(0, engineInfo.Version.Major, engineInfo.Version.Minor, engineInfo.Version.Build),
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

        if (apiParameters.EnableDebugging && capabilities.CanDebug)
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

    void InitializeVulkanDevice()
    {
        AssertNotNull(parameters);

        VkPhysicalDevice physicalDevice = GetVulkanPhysicalDevice(parameters.PhysicalDevice);
        ThrowInvalidOperationIf(!physicalDevice.HasValue, $"Vulkan: Physical device {parameters.PhysicalDevice} not found.");

        VkPhysicalDeviceProperties physicalDeviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &physicalDeviceProperties);

        HashSet<string> availableDeviceExtensions = [];

        // Get available device extensions
        {
            uint deviceExtensionCount = 0;
            ThrowIfFailed(vkEnumerateDeviceExtensionProperties(physicalDevice, default, &deviceExtensionCount, default), "Vulkan: Failed to get device extension count.");

            VkExtensionProperties* extensionProperties = stackalloc VkExtensionProperties[(int)deviceExtensionCount];
            ThrowIfFailed(vkEnumerateDeviceExtensionProperties(physicalDevice, default, &deviceExtensionCount, extensionProperties), "Vulkan: Failed to get device extensions.");

            for (int i = 0; i < deviceExtensionCount; i++)
            {
                availableDeviceExtensions.Add(new string((sbyte*)extensionProperties[i].ExtensionName));
            }
        }

        SafeNativeStringArray enabledDeviceExtensionNames = new(capacity: 16);

        {
            const string VK_KHR_swapchain = "VK_KHR_swapchain";
            const string VK_KHR_dynamic_rendering = "VK_KHR_swapchain";
            const string VK_KHR_external_memory_win32 = "VK_KHR_swapchain";
            const string VK_EXT_memory_budget = "VK_KHR_swapchain";
            const string VK_EXT_subgroup_size_control = "VK_KHR_swapchain";
            const string VK_EXT_robustness2 = "VK_EXT_robustness2";

            void AddExtension(string extension)
            {
                if (availableDeviceExtensions.Contains(extension))
                {
                    enabledDeviceExtensionNames.Add(extension);
                }
                else
                {
                    logger.Warning($"Vulkan: Extension {extension} is not available.");
                }
            }

            AddExtension(VK_KHR_swapchain);
            AddExtension(VK_KHR_dynamic_rendering);
            AddExtension(VK_EXT_memory_budget);
            AddExtension(VK_EXT_subgroup_size_control);
            AddExtension(VK_EXT_robustness2);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddExtension(VK_KHR_external_memory_win32);
            }
        }

        VkPhysicalDeviceRobustness2FeaturesEXT probeRobustness2Features = new(default);
        VkPhysicalDeviceShaderSubgroupExtendedTypesFeatures probeSubgroupExtendedTypesFeatures = new(next: Next(&probeRobustness2Features));
        VkPhysicalDevice16BitStorageFeatures probeFloat16StorageFeatures = new(next: Next(&probeSubgroupExtendedTypesFeatures));
        VkPhysicalDeviceShaderFloat16Int8Features probeFloat16ShaderFeatures = new(next: Next(&probeFloat16StorageFeatures));
        VkPhysicalDeviceDynamicRenderingFeatures probeDynamicRenderingFeatures = new(next: Next(&probeFloat16ShaderFeatures));
        VkPhysicalDeviceFeatures2 probePhysicalDeviceFeatures2 = new(next: Next(&probeDynamicRenderingFeatures));
        vkGetPhysicalDeviceFeatures2(physicalDevice, &probePhysicalDeviceFeatures2);

        probeRobustness2Features.RobustBufferAccess2 = 0;
        probeRobustness2Features.RobustImageAccess2 = 0;
        bool enableFp16 = probeFloat16ShaderFeatures.ShaderFloat16 > 0 && probeFloat16StorageFeatures.StorageBuffer16BitAccess > 0;

        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.SamplerAnisotropy == 0, "Vulkan: Required feature anisotropic filtering is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.GeometryShader == 0, "Vulkan: Required feature geometry shader is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.DrawIndirectFirstInstance == 0, "Vulkan: Required feature draw indirect first instance is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.FragmentStoresAndAtomics == 0, "Vulkan: Required feature fragment stores and atomics is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.VertexPipelineStoresAndAtomics == 0, "Vulkan: Required feature vertex pipeline stores and atomics is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.ShaderStorageImageWriteWithoutFormat == 0, "Vulkan: Required feature shader storage image write without format is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.ShaderImageGatherExtended == 0, "Vulkan: Required feature shader image gather extended is not supported.");
        ThrowNotSupportedIf(probePhysicalDeviceFeatures2.Features.IndependentBlend == 0, "Vulkan: Required feature independent blend is not supported.");
        ThrowNotSupportedIf(probeDynamicRenderingFeatures.DynamicRendering == 0, "Vulkan: Required feature dynamic rendering is not supported.");

        probePhysicalDeviceFeatures2.Features = new()
        {
            SamplerAnisotropy = 1,
            GeometryShader = 1,
            DrawIndirectFirstInstance = 1,
            FragmentStoresAndAtomics = 1,
            VertexPipelineStoresAndAtomics = 1,
            ShaderStorageImageWriteWithoutFormat = 1,
            ShaderImageGatherExtended = 1,
            IndependentBlend = 1,
            ShaderInt16 = enableFp16 ? 1u : 0u
        };

        uint generalQueueFamilyIndex = uint.MaxValue;
        uint transferQueueFamilyIndex = uint.MaxValue;

        // Find suitable queue families
        {
            uint physicalDeviceQueueFamilyPropertyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &physicalDeviceQueueFamilyPropertyCount);

            VkQueueFamilyProperties* physicalDeviceQueueFamilyProperties = stackalloc VkQueueFamilyProperties[(int)physicalDeviceQueueFamilyPropertyCount];
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &physicalDeviceQueueFamilyPropertyCount, physicalDeviceQueueFamilyProperties);

            for (uint i = 0; i < physicalDeviceQueueFamilyPropertyCount; i++)
            {
                VkQueueFlags queueFlags = physicalDeviceQueueFamilyProperties[i].QueueFlags;

                if (queueFlags.HasFlag(VkQueueFlags.GRAPHICS_BIT) && queueFlags.HasFlag(VkQueueFlags.COMPUTE_BIT))
                {
                    generalQueueFamilyIndex = i;
                }
                else if (queueFlags.HasFlag(VkQueueFlags.TRANSFER_BIT) && !queueFlags.HasFlag(VkQueueFlags.GRAPHICS_BIT) && !queueFlags.HasFlag(VkQueueFlags.COMPUTE_BIT))
                {
                    transferQueueFamilyIndex = i;
                }
            }

            ThrowIf(generalQueueFamilyIndex == uint.MaxValue, "Vulkan: Failed to find a queue family.");
            if (transferQueueFamilyIndex == uint.MaxValue)
            {
                transferQueueFamilyIndex = generalQueueFamilyIndex;
            }
        }

        bool gotDedicatedTransferQueue = generalQueueFamilyIndex != transferQueueFamilyIndex;

        float queuePriority = 0.0f;
        VkDeviceQueueCreateInfo generalQueueCreateInfo = new(default)
        {
            QueueCount = 1,
            QueuePriorities = &queuePriority,
            QueueFamilyIndex = generalQueueFamilyIndex
        };

        VkDeviceQueueCreateInfo transferQueueCreateInfo = new(default)
        {
            QueueCount = 1,
            QueuePriorities = &queuePriority,
            QueueFamilyIndex = transferQueueFamilyIndex
        };

        VkDeviceQueueCreateInfo* deviceQueueCreateInfos = stackalloc VkDeviceQueueCreateInfo[2]
        {
            generalQueueCreateInfo,
            transferQueueCreateInfo
        };

        VkDeviceCreateInfo deviceCreateInfo = new(default)
        {
            EnabledFeatures = default,
            EnabledExtensionCount = (uint)enabledDeviceExtensionNames.Count,
            EnabledExtensionNames = enabledDeviceExtensionNames.DangerousGetHandle(),
            QueueCreateInfoCount = gotDedicatedTransferQueue ? 2u : 1u,
            QueueCreateInfos = deviceQueueCreateInfos,
            Next = (nint)(&probePhysicalDeviceFeatures2)
        };

        VkDevice device = default;
        ThrowIfFailed(vkCreateDevice(physicalDevice, &deviceCreateInfo, default, &device), "Vulkan: Failed to create device.");
        this.device = device;
    }

    VkPhysicalDevice GetVulkanPhysicalDevice(GfxPhysicalDevice gfxPhysicalDevice)
    {
        uint physicalDeviceCount = 0;
        ThrowIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default), "Vulkan: Failed to get physical device count.");

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices), "Vulkan: Failed to get physical devices.");

        GfxPhysicalDevice[] devices = new GfxPhysicalDevice[(int)physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            VkPhysicalDeviceProperties properties;
            vkGetPhysicalDeviceProperties(physicalDevices[i], &properties);

            if (gfxPhysicalDevice.Id == new Guid(new ReadOnlySpan<byte>(properties.PipelineCacheUuid, 16)))
            {
                return physicalDevices[i];
            }
        }

        return default;
    }

    /// <summary>
    /// Enumerates the available physical devices.
    /// </summary>
    /// <returns></returns>
    public GfxPhysicalDevice[] EnumeratePhysicalDevices()
    {
        uint physicalDeviceCount = 0;
        ThrowIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default), "Vulkan: Failed to get physical device count.");

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices), "Vulkan: Failed to get physical devices.");

        GfxPhysicalDevice[] devices = new GfxPhysicalDevice[(int)physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            VkPhysicalDeviceProperties properties;
            vkGetPhysicalDeviceProperties(physicalDevices[i], &properties);

            GfxPhysicalDeviceType type = properties.DeviceType switch
            {
                VkPhysicalDeviceType.INTEGRATED_GPU => GfxPhysicalDeviceType.Integrated,
                VkPhysicalDeviceType.DISCRETE_GPU => GfxPhysicalDeviceType.Discrete,
                VkPhysicalDeviceType.VIRTUAL_GPU => GfxPhysicalDeviceType.Virtual,
                VkPhysicalDeviceType.CPU => GfxPhysicalDeviceType.Cpu,
                _ => GfxPhysicalDeviceType.Other
            };

            devices[i] = new GfxPhysicalDevice(
                Type: type,
                Id: new Guid(new ReadOnlySpan<byte>(properties.PipelineCacheUuid, 16)),
                Name: new string((sbyte*)properties.DeviceName),
                DriverVersion: ParseVersion(properties.DriverVersion),
                ApiVersion: ParseVersion(properties.ApiVersion)
            );
        }

        return devices;
    }
}
