using System.Runtime.InteropServices;

using Gossamer.External.Vulkan;

using static Gossamer.External.Vulkan.Api;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.BackEnd;

abstract class SafeHandle : IDisposable
{
    public bool IsInvalid => handle == IntPtr.Zero;

    protected nint handle;

    public nint DangerousGetHandle() => handle;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseHandle();
        }
    }

    protected abstract void ReleaseHandle();

    ~SafeHandle()
    {
        Dispose(false);
    }
}

sealed class SafeNativeStringArray : SafeHandle
{
    readonly SafeNativeString[] strings;

    public int Capacity { get; }

    public int Count { get; private set; }

    public nint this[int index]
    {
        get
        {
            if (index < 0 || index >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Marshal.ReadIntPtr(handle, index * nint.Size);
        }
        set
        {
            if (index < 0 || index >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Marshal.WriteIntPtr(handle, index * nint.Size, value);
        }
    }

    public SafeNativeStringArray(int capacity)
    {
        strings = new SafeNativeString[capacity];
        handle = Marshal.AllocHGlobal(capacity * nint.Size);
        Capacity = capacity;
    }

    protected override void ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
    }

    public void Add(string str)
    {
        if (Count >= Capacity)
        {
            throw new InvalidOperationException("Array is full.");
        }

        strings[Count] = new SafeNativeString(str);
        this[Count] = strings[Count].DangerousGetHandle();
        Count++;
    }
}

sealed class SafeNativeString : SafeHandle
{
    public SafeNativeString(string str)
    {
        handle = Marshal.StringToHGlobalAnsi(str);
    }

    protected override void ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
    }
}

unsafe class BackEndGfx : IDisposable
{
    bool isDisposed;

    PFN_vkDebugUtilsMessengerCallbackEXT? VulkanDebugMessengerCallback;
    PFN_vkSetDebugUtilsObjectNameEXT? VulkanDebugSetObjectName;

    VkDebugUtilsMessengerEXT debugUtilsMessenger;

    VkInstance instance;

    public void Create()
    {
        InitializeVulkanInstance(enableValidation: true, enableDebugging: true);
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
            var vkDestroyDebugUtilsMessengerEXT = Marshal.GetDelegateForFunctionPointer<PFN_vkDestroyDebugUtilsMessengerEXT>(vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT"));
            vkDestroyDebugUtilsMessengerEXT(instance, debugUtilsMessenger, default);
            debugUtilsMessenger = default;
        }
    }

    uint VulkanDebugMessageCallback(VkDebugUtilsMessageSeverityEXT severity, VkDebugUtilsMessageTypeEXT type, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, nint pUserData)
    {
        //Log.Level level = a switch
        //{
        //    VK_DEBUG_UTILS_MESSAGE_SEVERITY.VERBOSE => Log.Level.Debug,
        //    VK_DEBUG_UTILS_MESSAGE_SEVERITY.INFO => Log.Level.Information,
        //    VK_DEBUG_UTILS_MESSAGE_SEVERITY.WARNING => Log.Level.Warning,
        //    VK_DEBUG_UTILS_MESSAGE_SEVERITY.ERROR => Log.Level.Error,
        //    _ => Log.Level.Debug
        //};
        //
        //Application.Current.Log.Append(level, Utf8StringMarshaller.ConvertToManaged((byte*)c->pMessage) ?? "", DateTime.Now, "Vulkan", "Validation");
        return 0;
    }

    void InitializeVulkanInstance(bool enableValidation, bool enableDebugging)
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const string VK_KHR_win32_surface = "VK_KHR_win32_surface";
                ThrowIf(!availableInstanceExtensions.Contains(VK_KHR_win32_surface), $"Vulkan: Required feature {VK_KHR_win32_surface} not available.");

                enabledExtensionNames.Add(VK_KHR_win32_surface);
            }

            if (enableValidation)
            {
                const string VK_LAYER_KHRONOS_validation = "VK_LAYER_KHRONOS_validation";
                if (availableInstanceLayers.Contains(VK_LAYER_KHRONOS_validation))
                {
                    enabledLayerNames.Add(VK_LAYER_KHRONOS_validation);
                }
            }

            if (enableDebugging)
            {
                const string VK_EXT_debug_utils = "VK_EXT_debug_utils";
                if (availableInstanceExtensions.Contains(VK_EXT_debug_utils))
                {
                    enabledExtensionNames.Add(VK_EXT_debug_utils);
                }
            }
        }

        using SafeNativeString applicationName = new("Gossamer");
        using SafeNativeString engineName = new("Gossamer");

        VkApplicationInfo applicationInfo = new(default)
        {
            ApplicationName = applicationName.DangerousGetHandle(),
            ApplicationVersion = MAKE_API_VERSION(0, 0, 1, 0),

            EngineName = engineName.DangerousGetHandle(),
            EngineVersion = MAKE_API_VERSION(0, 0, 1, 0),
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

        if (enableDebugging)
        {
            const string STR_vkCreateDebugUtilsMessengerEXT = "vkCreateDebugUtilsMessengerEXT";
            const string STR_vkDestroyDebugUtilsMessengerEXT = "vkDestroyDebugUtilsMessengerEXT";
            const string STR_vkSetDebugUtilsObjectNameEXT = "vkSetDebugUtilsObjectNameEXT";

            nint debugCreateUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkCreateDebugUtilsMessengerEXT);
            ThrowIf(debugCreateUtilsMessengerPfn == nint.Zero, "Vulkan: Failed to get debug utils create messenger function pointer.");
            nint debugDestroyUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkDestroyDebugUtilsMessengerEXT);
            ThrowIf(debugDestroyUtilsMessengerPfn == nint.Zero, "Vulkan: Failed to get debug utils destroy messenger function pointer.");
            nint debugUtilsObjectNamePfn = vkGetInstanceProcAddr(instance, STR_vkSetDebugUtilsObjectNameEXT);
            ThrowIf(debugUtilsObjectNamePfn == nint.Zero, "Vulkan: Failed to get debug utils object name function pointer.");

            VulkanDebugMessengerCallback = VulkanDebugMessageCallback;
            VulkanDebugSetObjectName = Marshal.GetDelegateForFunctionPointer<PFN_vkSetDebugUtilsObjectNameEXT>(debugUtilsObjectNamePfn);

            VkDebugUtilsMessengerCreateInfoEXT debugUtilsMessengerCreateInfo = new(default)
            {
                MessageSeverity = VkDebugUtilsMessageSeverityEXT.ERROR | VkDebugUtilsMessageSeverityEXT.WARNING,
                MessageType = VkDebugUtilsMessageTypeEXT.GENERAL | VkDebugUtilsMessageTypeEXT.VALIDATION,
                UserCallback = Marshal.GetFunctionPointerForDelegate(VulkanDebugMessengerCallback),
            };

            var vkCreateDebugUtilsMessengerEXT = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateDebugUtilsMessengerEXT>(debugCreateUtilsMessengerPfn);

            VkDebugUtilsMessengerEXT debugUtilsMessenger = default;
            ThrowIfFailed(vkCreateDebugUtilsMessengerEXT(instance, &debugUtilsMessengerCreateInfo, default, &debugUtilsMessenger), "Vulkan: Failed to create debug utils messenger.");
            this.debugUtilsMessenger = debugUtilsMessenger;
        }
    }
}
