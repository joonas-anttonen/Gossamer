#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Vulkan.Vma;

/// <summary>
/// Flags for created #VmaAllocator.
/// </summary>
[Flags]
enum VmaAllocatorCreateFlags : uint
{
    /// <summary> Allocator and all objects created from it will not be synchronized internally, so you must guarantee they are used from only one thread at a time or synchronized externally by you.
    /// Using this flag may increase performance because internal mutexes are not used.
    /// </summary>
    EXTERNALLY_SYNCHRONIZED_BIT = 0x00000001,
    /// <summary>  Enables usage of VK_KHR_dedicated_allocation extension.
    /// 
    /// The flag works only if VmaAllocatorCreateInfo::vulkanApiVersion `== VK_API_VERSION_1_0`.
    /// When it is `VK_API_VERSION_1_1`, the flag is ignored because the extension has been promoted to Vulkan 1.1.
    /// 
    /// Using this extension will automatically allocate dedicated blocks of memory for
    /// some buffers and images instead of suballocating place for them out of bigger
    /// memory blocks (as if you explicitly used #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT
    /// flag) when it is recommended by the driver. It may improve performance on some
    /// GPUs.
    /// 
    /// You may set this flag only if you found out that following device extensions are
    /// supported, you enabled them while creating Vulkan device passed as
    /// VmaAllocatorCreateInfo::device, and you want them to be used internally by this
    /// library:
    /// 
    /// - VK_KHR_get_memory_requirements2 (device extension)
    /// - VK_KHR_dedicated_allocation (device extension)
    /// 
    /// When this flag is set, you can experience following warnings reported by Vulkan
    /// validation layer. You can ignore them.
    /// 
    /// > vkBindBufferMemory(): Binding memory to buffer 0x2d but vkGetBufferMemoryRequirements() has not been called on that buffer.
    /// </summary>
    KHR_DEDICATED_ALLOCATION_BIT = 0x00000002,
    /// <summary> 
    /// Enables usage of VK_KHR_bind_memory2 extension.
    /// 
    /// The flag works only if VmaAllocatorCreateInfo::vulkanApiVersion `== VK_API_VERSION_1_0`.
    /// When it is `VK_API_VERSION_1_1`, the flag is ignored because the extension has been promoted to Vulkan 1.1.
    /// 
    /// You may set this flag only if you found out that this device extension is supported,
    /// you enabled it while creating Vulkan device passed as VmaAllocatorCreateInfo::device,
    /// and you want it to be used internally by this library.
    /// 
    /// The extension provides functions `vkBindBufferMemory2KHR` and `vkBindImageMemory2KHR`,
    /// which allow to pass a chain of `pNext` structures while binding.
    /// This flag is required if you use `pNext` parameter in vmaBindBufferMemory2() or vmaBindImageMemory2().
    /// </summary>
    KHR_BIND_MEMORY2_BIT = 0x00000004,
    /// <summary> 
    /// Enables usage of VK_EXT_memory_budget extension.
    /// 
    /// You may set this flag only if you found out that this device extension is supported,
    /// you enabled it while creating Vulkan device passed as VmaAllocatorCreateInfo::device,
    /// and you want it to be used internally by this library, along with another instance extension
    /// VK_KHR_get_physical_device_properties2, which is required by it (or Vulkan 1.1, where this extension is promoted).
    /// 
    /// The extension provides query for current memory usage and budget, which will probably
    /// be more accurate than an estimation used by the library otherwise.
    /// </summary>
    EXT_MEMORY_BUDGET_BIT = 0x00000008,
    /// <summary> 
    /// Enables usage of VK_AMD_device_coherent_memory extension.
    /// 
    /// You may set this flag only if you:
    /// 
    /// - found out that this device extension is supported and enabled it while creating Vulkan device passed as VmaAllocatorCreateInfo::device,
    /// - checked that `VkPhysicalDeviceCoherentMemoryFeaturesAMD::deviceCoherentMemory` is true and set it while creating the Vulkan device,
    /// - want it to be used internally by this library.
    /// 
    /// The extension and accompanying device feature provide access to memory types with
    /// `VK_MEMORY_PROPERTY_DEVICE_COHERENT_BIT_AMD` and `VK_MEMORY_PROPERTY_DEVICE_UNCACHED_BIT_AMD` flags.
    /// They are useful mostly for writing breadcrumb markers - a common method for debugging GPU crash/hang/TDR.
    /// 
    /// When the extension is not enabled, such memory types are still enumerated, but their usage is illegal.
    /// To protect from this error, if you don't create the allocator with this flag, it will refuse to allocate any memory or create a custom pool in such memory type,
    /// returning `VK_ERROR_FEATURE_NOT_PRESENT`.
    /// </summary>
    AMD_DEVICE_COHERENT_MEMORY_BIT = 0x00000010,
    /// <summary> 
    /// Enables usage of "buffer device address" feature, which allows you to use function
    /// `vkGetBufferDeviceAddress*` to get raw GPU pointer to a buffer and pass it for usage inside a shader.
    /// 
    /// You may set this flag only if you:
    /// 
    /// 1. (For Vulkan version < 1.2) Found as available and enabled device extension
    /// VK_KHR_buffer_device_address.
    /// This extension is promoted to core Vulkan 1.2.
    /// 2. Found as available and enabled device feature `VkPhysicalDeviceBufferDeviceAddressFeatures::bufferDeviceAddress`.
    /// 
    /// When this flag is set, you can create buffers with `VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT` using VMA.
    /// The library automatically adds `VK_MEMORY_ALLOCATE_DEVICE_ADDRESS_BIT` to
    /// allocated memory blocks wherever it might be needed.
    /// 
    /// For more information, see documentation chapter \ref enabling_buffer_device_address.
    /// </summary>
    BUFFER_DEVICE_ADDRESS_BIT = 0x00000020,
    /// <summary> 
    /// Enables usage of VK_EXT_memory_priority extension in the library.
    /// 
    /// You may set this flag only if you found available and enabled this device extension,
    /// along with `VkPhysicalDeviceMemoryPriorityFeaturesEXT::memoryPriority == VK_TRUE`,
    /// while creating Vulkan device passed as VmaAllocatorCreateInfo::device.
    /// 
    /// When this flag is used, VmaAllocationCreateInfo::priority and VmaPoolCreateInfo::priority
    /// are used to set priorities of allocated Vulkan memory. Without it, these variables are ignored.
    /// 
    /// A priority must be a floating-point value between 0 and 1, indicating the priority of the allocation relative to other memory allocations.
    /// Larger values are higher priority. The granularity of the priorities is implementation-dependent.
    /// It is automatically passed to every call to `vkAllocateMemory` done by the library using structure `VkMemoryPriorityAllocateInfoEXT`.
    /// The value to be used for default priority is 0.5.
    /// For more details, see the documentation of the VK_EXT_memory_priority extension.
    /// </summary>
    EXT_MEMORY_PRIORITY_BIT = 0x00000040,
    /// <summary> 
    /// Enables usage of VK_KHR_maintenance4 extension in the library.
    /// 
    /// You may set this flag only if you found available and enabled this device extension,
    /// while creating Vulkan device passed as VmaAllocatorCreateInfo::device.
    /// </summary>
    KHR_MAINTENANCE4_BIT = 0x00000080,
    /// <summary> 
    /// Enables usage of VK_KHR_maintenance5 extension in the library.
    /// 
    /// You should set this flag if you found available and enabled this device extension,
    /// while creating Vulkan device passed as VmaAllocatorCreateInfo::device.
    /// </summary>
    KHR_MAINTENANCE5_BIT = 0x00000100,

    /// <summary> 
    /// Enables usage of VK_KHR_external_memory_win32 extension in the library.
    /// 
    /// You should set this flag if you found available and enabled this device extension,
    /// while creating Vulkan device passed as VmaAllocatorCreateInfo::device.
    /// For more information, see \ref vk_khr_external_memory_win32.
    /// </summary>
    VMA_ALLOCATOR_CREATE_KHR_EXTERNAL_MEMORY_WIN32_BIT = 0x00000200,
}

/// <summary>
/// Intended usage of the allocated memory.
/// </summary>
enum VmaMemoryUsage : uint
{
    /// <summary>
    /// No intended memory usage specified.
    /// Use other members of VmaAllocationCreateInfo to specify your requirements.
    /// </summary>
    UNKNOWN = 0,
    /// <summary>
    /// Lazily allocated GPU memory having `VK_MEMORY_PROPERTY_LAZILY_ALLOCATED_BIT`.
    /// Exists mostly on mobile platforms. Using it on desktop PC or other GPUs with no such memory type present will fail the allocation.
    /// 
    /// Usage: Memory for transient attachment images (color attachments, depth attachments etc.), created with `VK_IMAGE_USAGE_TRANSIENT_ATTACHMENT_BIT`.
    /// 
    /// Allocations with this usage are always created as dedicated - it implies #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT.
    /// </summary>
    GPU_LAZILY_ALLOCATED = 6,
    /// <summary>
    /// Selects best memory type automatically.
    /// This flag is recommended for most common use cases.
    /// 
    /// When using this flag, if you want to map the allocation (using vmaMapMemory() or #VMA_ALLOCATION_CREATE_MAPPED_BIT),
    /// you must pass one of the flags: #VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT or #VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
    /// in VmaAllocationCreateInfo::flags.
    /// 
    /// It can be used only with functions that let the library know `VkBufferCreateInfo` or `VkImageCreateInfo`, e.g.
    /// vmaCreateBuffer(), vmaCreateImage(), vmaFindMemoryTypeIndexForBufferInfo(), vmaFindMemoryTypeIndexForImageInfo()
    /// and not with generic memory allocation functions.
    /// </summary>
    AUTO = 7,
    /// <summary>
    /// Selects best memory type automatically with preference for GPU (device) memory.
    /// 
    /// When using this flag, if you want to map the allocation (using vmaMapMemory() or #VMA_ALLOCATION_CREATE_MAPPED_BIT),
    /// you must pass one of the flags: #VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT or #VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
    /// in VmaAllocationCreateInfo::flags.
    /// 
    /// It can be used only with functions that let the library know `VkBufferCreateInfo` or `VkImageCreateInfo`, e.g.
    /// vmaCreateBuffer(), vmaCreateImage(), vmaFindMemoryTypeIndexForBufferInfo(), vmaFindMemoryTypeIndexForImageInfo()
    /// and not with generic memory allocation functions.
    /// </summary>
    AUTO_PREFER_DEVICE = 8,
    /// <summary>
    /// Selects best memory type automatically with preference for CPU (host) memory.
    /// 
    /// When using this flag, if you want to map the allocation (using vmaMapMemory() or #VMA_ALLOCATION_CREATE_MAPPED_BIT),
    /// you must pass one of the flags: #VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT or #VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
    /// in VmaAllocationCreateInfo::flags.
    /// 
    /// It can be used only with functions that let the library know `VkBufferCreateInfo` or `VkImageCreateInfo`, e.g.
    /// vmaCreateBuffer(), vmaCreateImage(), vmaFindMemoryTypeIndexForBufferInfo(), vmaFindMemoryTypeIndexForImageInfo()
    /// and not with generic memory allocation functions.
    /// </summary>
    AUTO_PREFER_HOST = 9,
}

/// <summary>
/// Flags to be passed as VmaAllocationCreateInfo::flags.
/// </summary>
[Flags]
enum VmaAllocationCreateFlags : uint
{
    /// <summary> Set this flag if the allocation should have its own memory block.
    /// 
    /// Use it for special, big resources, like fullscreen images used as attachments.
    /// 
    /// If you use this flag while creating a buffer or an image, `VkMemoryDedicatedAllocateInfo`
    /// structure is applied if possible.
    /// </summary>
    VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT = 0x00000001,

    /// <summary> Set this flag to only try to allocate from existing `VkDeviceMemory` blocks and never create new such block.
    /// 
    /// If new allocation cannot be placed in any of the existing blocks, allocation
    /// fails with `VK_ERROR_OUT_OF_DEVICE_MEMORY` error.
    /// 
    /// You should not use #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT and
    /// #VMA_ALLOCATION_CREATE_NEVER_ALLOCATE_BIT at the same time. It makes no sense.
    /// </summary>
    VMA_ALLOCATION_CREATE_NEVER_ALLOCATE_BIT = 0x00000002,
    /// <summary> Set this flag to use a memory that will be persistently mapped and retrieve pointer to it.
    /// 
    /// Pointer to mapped memory will be returned through VmaAllocationInfo::pMappedData.
    /// 
    /// It is valid to use this flag for allocation made from memory type that is not
    /// `HOST_VISIBLE`. This flag is then ignored and memory is not mapped. This is
    /// useful if you need an allocation that is efficient to use on GPU
    /// (`DEVICE_LOCAL`) and still want to map it directly if possible on platforms that
    /// support it (e.g. Intel GPU).
    /// </summary>
    MAPPED = 0x00000004,
    /// <summary> \deprecated Preserved for backward compatibility. Consider using vmaSetAllocationName() instead.
    /// 
    /// Set this flag to treat VmaAllocationCreateInfo::pUserData as pointer to a
    /// null-terminated string. Instead of copying pointer value, a local copy of the
    /// string is made and stored in allocation's `pName`. The string is automatically
    /// freed together with the allocation. It is also used in vmaBuildStatsString().
    /// </summary>
    VMA_ALLOCATION_CREATE_USER_DATA_COPY_STRING_BIT = 0x00000020,
    /// <summary> Allocation will be created from upper stack in a double stack pool.
    /// 
    /// This flag is only allowed for custom pools created with #VMA_POOL_CREATE_LINEAR_ALGORITHM_BIT flag.
    /// </summary>
    VMA_ALLOCATION_CREATE_UPPER_ADDRESS_BIT = 0x00000040,
    /// <summary> Create both buffer/image and allocation, but don't bind them together.
    /// It is useful when you want to bind yourself to do some more advanced binding, e.g. using some extensions.
    /// The flag is meaningful only with functions that bind by default: vmaCreateBuffer(), vmaCreateImage().
    /// Otherwise it is ignored.
    /// 
    /// If you want to make sure the new buffer/image is not tied to the new memory allocation
    /// through `VkMemoryDedicatedAllocateInfoKHR` structure in case the allocation ends up in its own memory block,
    /// use also flag #VMA_ALLOCATION_CREATE_CAN_ALIAS_BIT.
    /// </summary>
    VMA_ALLOCATION_CREATE_DONT_BIND_BIT = 0x00000080,
    /// <summary> Create allocation only if additional device memory required for it, if any, won't exceed
    /// memory budget. Otherwise return `VK_ERROR_OUT_OF_DEVICE_MEMORY`.
    /// </summary>
    VMA_ALLOCATION_CREATE_WITHIN_BUDGET_BIT = 0x00000100,
    /// <summary> Set this flag if the allocated memory will have aliasing resources.
    /// 
    /// Usage of this flag prevents supplying `VkMemoryDedicatedAllocateInfoKHR` when #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT is specified.
    /// Otherwise created dedicated memory will not be suitable for aliasing resources, resulting in Vulkan Validation Layer errors.
    /// </summary>
    VMA_ALLOCATION_CREATE_CAN_ALIAS_BIT = 0x00000200,
    /// <summary>
    /// Requests possibility to map the allocation (using vmaMapMemory() or #VMA_ALLOCATION_CREATE_MAPPED_BIT).
    /// 
    /// - If you use #VMA_MEMORY_USAGE_AUTO or other `VMA_MEMORY_USAGE_AUTO*` value,
    ///   you must use this flag to be able to map the allocation. Otherwise, mapping is incorrect.
    /// - If you use other value of #VmaMemoryUsage, this flag is ignored and mapping is always possible in memory types that are `HOST_VISIBLE`.
    ///   This includes allocations created in \ref custom_memory_pools.
    /// 
    /// Declares that mapped memory will only be written sequentially, e.g. using `memcpy()` or a loop writing number-by-number,
    /// never read or accessed randomly, so a memory type can be selected that is uncached and write-combined.
    /// 
    /// \warning Violating this declaration may work correctly, but will likely be very slow.
    /// Watch out for implicit reads introduced by doing e.g. `pMappedData[i] += x;`
    /// Better prepare your data in a local variable and `memcpy()` it to the mapped pointer all at once.
    /// </summary>
    HOST_ACCESS_SEQUENTIAL_WRITE = 0x00000400,
    /// <summary>
    /// Requests possibility to map the allocation (using vmaMapMemory() or #VMA_ALLOCATION_CREATE_MAPPED_BIT).
    /// 
    /// - If you use #VMA_MEMORY_USAGE_AUTO or other `VMA_MEMORY_USAGE_AUTO*` value,
    ///   you must use this flag to be able to map the allocation. Otherwise, mapping is incorrect.
    /// - If you use other value of #VmaMemoryUsage, this flag is ignored and mapping is always possible in memory types that are `HOST_VISIBLE`.
    ///   This includes allocations created in \ref custom_memory_pools.
    /// 
    /// Declares that mapped memory can be read, written, and accessed in random order,
    /// so a `HOST_CACHED` memory type is preferred.
    /// </summary>
    HOST_ACCESS_RANDOM = 0x00000800,
    /// <summary>
    /// Together with #VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT or #VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT,
    /// it says that despite request for host access, a not-`HOST_VISIBLE` memory type can be selected
    /// if it may improve performance.
    /// 
    /// By using this flag, you declare that you will check if the allocation ended up in a `HOST_VISIBLE` memory type
    /// (e.g. using vmaGetAllocationMemoryProperties()) and if not, you will create some "staging" buffer and
    /// issue an explicit transfer to write/read your data.
    /// To prepare for this possibility, don't forget to add appropriate flags like
    /// `VK_BUFFER_USAGE_TRANSFER_DST_BIT`, `VK_BUFFER_USAGE_TRANSFER_SRC_BIT` to the parameters of created buffer or image.
    /// </summary>
    VMA_ALLOCATION_CREATE_HOST_ACCESS_ALLOW_TRANSFER_INSTEAD_BIT = 0x00001000,
    /// <summary> Allocation strategy that chooses smallest possible free range for the allocation
    /// to minimize memory usage and fragmentation, possibly at the expense of allocation time.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT = 0x00010000,
    /// <summary> Allocation strategy that chooses first suitable free range for the allocation -
    /// not necessarily in terms of the smallest offset but the one that is easiest and fastest to find
    /// to minimize allocation time, possibly at the expense of allocation quality.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT = 0x00020000,
    /// <summary> Allocation strategy that chooses always the lowest offset in available space.
    /// This is not the most efficient strategy but achieves highly packed data.
    /// Used internally by defragmentation, not recommended in typical usage.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_MIN_OFFSET_BIT = 0x00040000,
    /// <summary> Alias to #VMA_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_BEST_FIT_BIT = VMA_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT,
    /// <summary> Alias to #VMA_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_FIRST_FIT_BIT = VMA_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT,
    /// <summary> A bit mask to extract only `STRATEGY` bits from entire set of flags.
    /// </summary>
    VMA_ALLOCATION_CREATE_STRATEGY_MASK =
        VMA_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT |
        VMA_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT |
        VMA_ALLOCATION_CREATE_STRATEGY_MIN_OFFSET_BIT,
}

/// <summary>
// Flags to be passed as VmaPoolCreateInfo::flags.
/// </summary>
enum VmaPoolCreateFlagBits
{
    /// <summary> Use this flag if you always allocate only buffers and linear images or only optimal images out of this pool and so Buffer-Image Granularity can be ignored.
    /// 
    /// This is an optional optimization flag.
    /// 
    /// If you always allocate using vmaCreateBuffer(), vmaCreateImage(),
    /// vmaAllocateMemoryForBuffer(), then you don't need to use it because allocator
    /// knows exact type of your allocations so it can handle Buffer-Image Granularity
    /// in the optimal way.
    /// 
    /// If you also allocate using vmaAllocateMemoryForImage() or vmaAllocateMemory(),
    /// exact type of such allocations is not known, so allocator must be conservative
    /// in handling Buffer-Image Granularity, which can lead to suboptimal allocation
    /// (wasted memory). In that case, if you can make sure you always allocate only
    /// buffers and linear images or only optimal images out of this pool, use this flag
    /// to make allocator disregard Buffer-Image Granularity and so make allocations
    /// faster and more optimal.
    /// </summary>
    VMA_POOL_CREATE_IGNORE_BUFFER_IMAGE_GRANULARITY_BIT = 0x00000002,

    /// <summary> Enables alternative, linear allocation algorithm in this pool.
    /// 
    /// Specify this flag to enable linear allocation algorithm, which always creates
    /// new allocations after last one and doesn't reuse space from allocations freed in
    /// between. It trades memory consumption for simplified algorithm and data
    /// structure, which has better performance and uses less memory for metadata.
    /// 
    /// By using this flag, you can achieve behavior of free-at-once, stack,
    /// ring buffer, and double stack.
    /// For details, see documentation chapter \ref linear_algorithm.
    /// </summary>
    VMA_POOL_CREATE_LINEAR_ALGORITHM_BIT = 0x00000004,

    /** Bit mask to extract only `ALGORITHM` bits from entire set of flags.
    */
    VMA_POOL_CREATE_ALGORITHM_MASK =
        VMA_POOL_CREATE_LINEAR_ALGORITHM_BIT,

    VMA_POOL_CREATE_FLAG_BITS_MAX_ENUM = 0x7FFFFFFF
}

/// <summary>
/// Flags to be passed as VmaDefragmentationInfo::flags.
/// </summary>
enum VmaDefragmentationFlagBits
{
    /// <summary> Use simple but fast algorithm for defragmentation.
    /// May not achieve best results but will require least time to compute and least allocations to copy.
    /// </summary>
    VMA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST_BIT = 0x1,
    /// <summary> Default defragmentation algorithm, applied also when no `ALGORITHM` flag is specified.
    /// Offers a balance between defragmentation quality and the amount of allocations and bytes that need to be moved.
    /// </summary>
    VMA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED_BIT = 0x2,
    /// <summary> Perform full defragmentation of memory.
    /// Can result in notably more time to compute and allocations to copy, but will achieve best memory packing.
    /// </summary>
    VMA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL_BIT = 0x4,
    /// <summary> Use the most roboust algorithm at the cost of time to compute and number of copies to make.
    /// Only available when bufferImageGranularity is greater than 1, since it aims to reduce
    /// alignment issues between different types of resources.
    /// Otherwise falls back to same behavior as #VMA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL_BIT.
    /// </summary>
    VMA_DEFRAGMENTATION_FLAG_ALGORITHM_EXTENSIVE_BIT = 0x8,

    /// A bit mask to extract only `ALGORITHM` bits from entire set of flags.
    VMA_DEFRAGMENTATION_FLAG_ALGORITHM_MASK =
        VMA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST_BIT |
        VMA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED_BIT |
        VMA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL_BIT |
        VMA_DEFRAGMENTATION_FLAG_ALGORITHM_EXTENSIVE_BIT,

    VMA_DEFRAGMENTATION_FLAG_BITS_MAX_ENUM = 0x7FFFFFFF
}

/// <summary>
/// Operation performed on single defragmentation move. See structure #VmaDefragmentationMove.
/// </summary>
enum VmaDefragmentationMoveOperation
{
    /// Buffer/image has been recreated at `dstTmpAllocation`, data has been copied, old buffer/image has been destroyed. `srcAllocation` should be changed to point to the new place. This is the default value set by vmaBeginDefragmentationPass().
    VMA_DEFRAGMENTATION_MOVE_OPERATION_COPY = 0,
    /// Set this value if you cannot move the allocation. New place reserved at `dstTmpAllocation` will be freed. `srcAllocation` will remain unchanged.
    VMA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE = 1,
    /// Set this value if you decide to abandon the allocation and you destroyed the buffer/image. New place reserved at `dstTmpAllocation` will be freed, along with `srcAllocation`, which will be destroyed.
    VMA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY = 2,
}

/// <summary>
/// Flags to be passed as VmaVirtualBlockCreateInfo::flags.
/// </summary>
enum VmaVirtualBlockCreateFlagBits
{
    /// <summary> Enables alternative, linear allocation algorithm in this virtual block.
    /// 
    /// Specify this flag to enable linear allocation algorithm, which always creates
    /// new allocations after last one and doesn't reuse space from allocations freed in
    /// between. It trades memory consumption for simplified algorithm and data
    /// structure, which has better performance and uses less memory for metadata.
    /// 
    /// By using this flag, you can achieve behavior of free-at-once, stack,
    /// ring buffer, and double stack.
    /// For details, see documentation chapter \ref linear_algorithm.
    /// </summary>
    VMA_VIRTUAL_BLOCK_CREATE_LINEAR_ALGORITHM_BIT = 0x00000001,

    /// <summary> Bit mask to extract only `ALGORITHM` bits from entire set of flags.
    /// </summary>
    VMA_VIRTUAL_BLOCK_CREATE_ALGORITHM_MASK =
        VMA_VIRTUAL_BLOCK_CREATE_LINEAR_ALGORITHM_BIT,

    VMA_VIRTUAL_BLOCK_CREATE_FLAG_BITS_MAX_ENUM = 0x7FFFFFFF
}

/// <summary>
/// Flags to be passed as VmaVirtualAllocationCreateInfo::flags.
/// </summary>
[Flags]
enum VmaVirtualAllocationCreateFlagBits : uint
{
    /// <summary> Allocation will be created from upper stack in a double stack pool.
    /// 
    /// This flag is only allowed for virtual blocks created with #VMA_VIRTUAL_BLOCK_CREATE_LINEAR_ALGORITHM_BIT flag.
    /// </summary>
    VMA_VIRTUAL_ALLOCATION_CREATE_UPPER_ADDRESS_BIT = VmaAllocationCreateFlags.VMA_ALLOCATION_CREATE_UPPER_ADDRESS_BIT,
    /// <summary> Allocation strategy that tries to minimize memory usage.
    /// </summary>
    VMA_VIRTUAL_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT = VmaAllocationCreateFlags.VMA_ALLOCATION_CREATE_STRATEGY_MIN_MEMORY_BIT,
    /// <summary> Allocation strategy that tries to minimize allocation time.
    /// </summary>
    VMA_VIRTUAL_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT = VmaAllocationCreateFlags.VMA_ALLOCATION_CREATE_STRATEGY_MIN_TIME_BIT,
    /** Allocation strategy that chooses always the lowest offset in available space.
    This is not the most efficient strategy but achieves highly packed data.
    */
    VMA_VIRTUAL_ALLOCATION_CREATE_STRATEGY_MIN_OFFSET_BIT = VmaAllocationCreateFlags.VMA_ALLOCATION_CREATE_STRATEGY_MIN_OFFSET_BIT,
    /// <summary> A bit mask to extract only `STRATEGY` bits from entire set of flags.
    /// 
    /// These strategy flags are binary compatible with equivalent flags in #VmaAllocationCreateFlagBits.
    /// </summary>
    VMA_VIRTUAL_ALLOCATION_CREATE_STRATEGY_MASK = VmaAllocationCreateFlags.VMA_ALLOCATION_CREATE_STRATEGY_MASK,
}

/// <summary>
/// Represents main object of this library initialized.
/// <para/>
/// Fill structure #VmaAllocatorCreateInfo and call function vmaCreateAllocator() to create it.
/// Call function vmaDestroyAllocator() to destroy it.
/// <para/>
/// It is recommended to create just one object of this type per `VkDevice` object,
/// right after Vulkan is initialized and keep it alive until before Vulkan device is destroyed.
/// </summary>
readonly struct VmaAllocator { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// Represents custom memory pool
/// <para/>
/// Fill structure VmaPoolCreateInfo and call function vmaCreatePool() to create it.
/// Call function vmaDestroyPool() to destroy it.
/// <para/>
/// For more information see[Custom memory pools](@ref choosing_memory_type_custom_memory_pools).
/// </summary>
readonly struct VmaPool { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// Represents single memory allocation.
/// <para/>
/// It may be either dedicated block of `VkDeviceMemory` or a specific region of a bigger block of this type
/// plus unique offset.
/// <para/>
/// There are multiple ways to create such object.
/// You need to fill structure VmaAllocationCreateInfo.
/// For more information see [Choosing memory type] (@ref choosing_memory_type).
/// <para/>
/// Although the library provides convenience functions that create Vulkan buffer or image,
/// allocate memory for it and bind them together,
/// binding of the allocation to a buffer or an image is out of scope of the allocation itself.
/// Allocation object can exist without buffer/image bound,
/// binding can be done manually by the user, and destruction of it can be done
/// independently of destruction of the allocation.
/// <para/>
/// The object also remembers its size and some other information.
/// To retrieve this information, use function vmaGetAllocationInfo() and inspect
/// returned structure VmaAllocationInfo.
/// </summary>
readonly struct VmaAllocation { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// An opaque object that represents started defragmentation process.
/// <para/>
/// Fill structure #VmaDefragmentationInfo and call function vmaBeginDefragmentation() to create it.
/// Call function vmaEndDefragmentation() to destroy it.
/// </summary>
readonly struct VmaDefragmentationContext { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// Represents single memory allocation done inside VmaVirtualBlock.
/// <para/>
/// Use it as a unique identifier to virtual allocation within the single block.
/// <para/>
/// Use value `VK_NULL_HANDLE` to represent a null/invalid allocation.
/// </summary>
readonly struct VmaVirtualAllocation { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// Handle to a virtual block object that allows to use core allocation algorithm without allocating any real GPU memory.
/// <para/>
/// Fill in #VmaVirtualBlockCreateInfo structure and use vmaCreateVirtualBlock() to create it. Use vmaDestroyVirtualBlock() to destroy it.
/// For more information, see documentation chapter \ref virtual_allocator.
/// <para/>
/// This object is not thread-safe - should not be used from multiple threads simultaneously, must be synchronized externally.
/// </summary>
readonly struct VmaVirtualBlock { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

/// <summary>
/// Callback function called after successful vkAllocateMemory.
/// </summary>
/// <param name="allocator"></param>
/// <param name="memoryType"></param>
/// <param name="memory"></param>
/// <param name="size"></param>
/// <param name="pUserData"></param>
unsafe delegate void PFN_vmaAllocateDeviceMemoryFunction(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData);

/// <summary>
/// Callback function called before vkFreeMemory.
/// </summary>
/// <param name="allocator"></param>
/// <param name="memoryType"></param>
/// <param name="memory"></param>
/// <param name="size"></param>
/// <param name="pUserData"></param>
unsafe delegate void PFN_vmaFreeDeviceMemoryFunction(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData);

/// <summary>
/// Set of callbacks that the library will call for `vkAllocateMemory` and `vkFreeMemory`.
/// 
/// Provided for informative purpose, e.g. to gather statistics about number of
/// allocations or total amount of memory allocated in Vulkan.
/// 
/// Used in VmaAllocatorCreateInfo::pDeviceMemoryCallbacks.
/// </summary>
unsafe struct VmaDeviceMemoryCallbacks
{
    public nint pfnAllocate;
    public nint pfnFree;
    public nint pUserData;
}

/// <summary>
/// Pointers to some Vulkan functions - a subset used by the library.
/// 
/// Used in VmaAllocatorCreateInfo::pVulkanFunctions.
/// </summary>
unsafe struct VmaVulkanFunctions { }

/// <summary>
/// Description of a Allocator to be created.
/// </summary>
unsafe struct VmaAllocatorCreateInfo
{
    /// <summary>
    /// Flags for created allocator. Use #VmaAllocatorCreateFlagBits enum.
    /// </summary>
    public VmaAllocatorCreateFlags Flags;
    /// <summary>
    /// Vulkan physical device. 
    /// <br/>
    /// It must be valid throughout whole lifetime of created allocator.
    /// </summary>
    public VkPhysicalDevice PhysicalDevice;
    /// <summary>
    /// Vulkan device.
    /// <br/>
    /// It must be valid throughout whole lifetime of created allocator.
    /// </summary>
    public VkDevice Device;
    /// <summary>
    /// Preferred size of a single `VkDeviceMemory` block to be allocated from large heaps > 1 GiB. Optional.
    /// Set to 0 to use default, which is currently 256 MiB.
    /// </summary>
    public ulong PreferredLargeHeapBlockSize;
    /// <summary>
    /// Custom CPU memory allocation callbacks. Optional. Optional, can be null. When specified, will also be used for all CPU-side memory allocations.
    /// </summary>
    public nint pAllocationCallbacks;
    /// <summary>
    /// Informative callbacks for `vkAllocateMemory`, `vkFreeMemory`. Optional. Optional, can be null.
    /// </summary>
    public VmaDeviceMemoryCallbacks* DeviceMemoryCallbacks;
    /// <summary>
    /// 
    /// </summary>
    public nint pHeapSizeLimit;
    /// <summary>
    /// Pointers to Vulkan functions. Can be null.
    /// </summary>
    public VmaVulkanFunctions* pVulkanFunctions;
    /// <summary>
    /// Handle to Vulkan instance object.
    /// <br/>
    /// Starting from version 3.0.0 this member is no longer optional, it must be set!
    /// </summary>
    public VkInstance Instance;
    /// <summary>
    /// Optional. Vulkan version that the application uses.
    /// 
    /// It must be a value in the format as created by macro `VK_MAKE_VERSION` or a constant like: `VK_API_VERSION_1_1`, `VK_API_VERSION_1_0`.
    /// 
    /// The patch version number specified is ignored.Only the major and minor versions are considered.
    /// 
    /// Only versions 1.0, 1.1, 1.2, 1.3 are supported by the current implementation.
    /// 
    /// Leaving it initialized to zero is equivalent to `VK_API_VERSION_1_0`.
    /// 
    /// It must match the Vulkan version used by the application and supported on the selected physical device,
    /// so it must be no higher than `VkApplicationInfo::apiVersion` passed to `vkCreateInstance`
    /// 
    /// and no higher than `VkPhysicalDeviceProperties::apiVersion` found on the physical device used.
    /// </summary>
    public uint VulkanApiVersion;
    /// <summary>
    /// 
    /// </summary>
    public nint pTypeExternalMemoryHandleTypes;
}

/// <summary>
/// Information about existing #VmaAllocator object.
/// </summary>
struct VmaAllocatorInfo
{
    /// <summary> 
    /// Handle to Vulkan instance object.
    /// <br/>
    /// This is the same value as has been passed through VmaAllocatorCreateInfo::instance.
    /// </summary>
    public VkInstance Instance;
    /// <summary> 
    /// Handle to Vulkan physical device object.
    /// <br/>
    /// This is the same value as has been passed through VmaAllocatorCreateInfo::physicalDevice.
    /// </summary>
    public VkPhysicalDevice PhysicalDevice;
    /// <summary> 
    /// Handle to Vulkan device object.
    /// <br/>
    /// This is the same value as has been passed through VmaAllocatorCreateInfo::device.
    /// </summary>
    public VkDevice Device;
}

/// /** \brief Calculated statistics of memory usage e.g. in a specific memory type, heap, custom pool, or total.
/// 
/// These are fast to calculate.
/// See functions: vmaGetHeapBudgets(), vmaGetPoolStatistics().
/// */
struct VmaStatistics
{
    /// /** \brief Number of `VkDeviceMemory` objects - Vulkan memory blocks allocated.
    /// */
    public uint blockCount;
    /// /** \brief Number of #VmaAllocation objects allocated.
    /// 
    /// Dedicated allocations have their own blocks, so each one adds 1 to `allocationCount` as well as `blockCount`.
    /// */
    public uint allocationCount;
    /// /** \brief Number of bytes allocated in `VkDeviceMemory` blocks.
    /// 
    /// \note To avoid confusion, please be aware that what Vulkan calls an "allocation" - a whole `VkDeviceMemory` object
    /// (e.g. as in `VkPhysicalDeviceLimits::maxMemoryAllocationCount`) is called a "block" in VMA, while VMA calls
    /// "allocation" a #VmaAllocation object that represents a memory region sub-allocated from such block, usually for a single buffer or image.
    /// */
    public ulong blockBytes;
    /// /** \brief Total number of bytes occupied by all #VmaAllocation objects.
    /// 
    /// Always less or equal than `blockBytes`.
    /// Difference `(blockBytes - allocationBytes)` is the amount of memory allocated from Vulkan
    /// but unused by any #VmaAllocation.
    /// */
    public ulong allocationBytes;
}

/// /** \brief More detailed statistics than #VmaStatistics.
/// 
/// These are slower to calculate. Use for debugging purposes.
/// See functions: vmaCalculateStatistics(), vmaCalculatePoolStatistics().
/// 
/// Previous version of the statistics API provided averages, but they have been removed
/// because they can be easily calculated as:
/// 
/// \code
/// VkDeviceSize allocationSizeAvg = detailedStats.statistics.allocationBytes / detailedStats.statistics.allocationCount;
/// VkDeviceSize unusedBytes = detailedStats.statistics.blockBytes - detailedStats.statistics.allocationBytes;
/// VkDeviceSize unusedRangeSizeAvg = unusedBytes / detailedStats.unusedRangeCount;
/// \endcode
/// */
struct VmaDetailedStatistics
{
    /// Basic statistics.
    public VmaStatistics statistics;
    /// Number of free ranges of memory between allocations.
    public uint unusedRangeCount;
    /// Smallest allocation size. `VK_WHOLE_SIZE` if there are 0 allocations.
    public ulong allocationSizeMin;
    /// Largest allocation size. 0 if there are 0 allocations.
    public ulong allocationSizeMax;
    /// Smallest empty range size. `VK_WHOLE_SIZE` if there are 0 empty ranges.
    public ulong unusedRangeSizeMin;
    /// Largest empty range size. 0 if there are 0 empty ranges.
    public ulong unusedRangeSizeMax;
}

/// /** \brief Statistics of current memory usage and available budget for a specific memory heap.
/// 
/// These are fast to calculate.
/// See function vmaGetHeapBudgets().
/// */
struct VmaBudget
{
    /// /** \brief Statistics fetched from the library.
    /// */
    public VmaStatistics statistics;
    /// /** \brief Estimated current memory usage of the program, in bytes.
    /// 
    /// Fetched from system using VK_EXT_memory_budget extension if enabled.
    /// 
    /// It might be different than `statistics.blockBytes` (usually higher) due to additional implicit objects
    /// also occupying the memory, like swapchain, pipelines, descriptor heaps, command buffers, or
    /// `VkDeviceMemory` blocks allocated outside of this library, if any.
    /// */
    public ulong usage;
    /// /** \brief Estimated amount of memory available to the program, in bytes.
    /// 
    /// Fetched from system using VK_EXT_memory_budget extension if enabled.
    /// 
    /// It might be different (most probably smaller) than `VkMemoryHeap::size[heapIndex]` due to factors
    /// external to the program, decided by the operating system.
    /// Difference `budget - usage` is the amount of additional memory that can probably
    /// be allocated without problems. Exceeding the budget may result in various problems.
    /// */
    public ulong budget;
}

/// /** \brief Parameters of new #VmaAllocation.
/// 
/// To be used with functions like vmaCreateBuffer(), vmaCreateImage(), and many others.
/// */
struct VmaAllocationCreateInfo
{
    /// Use #VmaAllocationCreateFlagBits enum.
    public VmaAllocationCreateFlags Flags;
    /// /** \brief Intended usage of memory.
    /// 
    /// You can leave #VMA_MEMORY_USAGE_UNKNOWN if you specify memory requirements in other way. \n
    /// If `pool` is not null, this member is ignored.
    /// */
    public VmaMemoryUsage Usage;
    /// /** \brief Flags that must be set in a Memory Type chosen for an allocation.
    /// 
    /// Leave 0 if you specify memory requirements in other way. \n
    /// If `pool` is not null, this member is ignored.*/
    public VkMemoryProperty RequiredFlags;
    /// /** \brief Flags that preferably should be set in a memory type chosen for an allocation.
    /// 
    /// Set to 0 if no additional flags are preferred. \n
    /// If `pool` is not null, this member is ignored. */
    public VkMemoryProperty PreferredFlags;
    /// /** \brief Bitmask containing one bit set for every memory type acceptable for this allocation.
    /// 
    /// Value 0 is equivalent to `UINT32_MAX` - it means any memory type is accepted if
    /// it meets other requirements specified by this structure, with no further
    /// restrictions on memory type index. \n
    /// If `pool` is not null, this member is ignored.
    /// */
    public uint MemoryTypeBits;
    /// /** \brief Pool that this allocation should be created in.
    /// 
    /// Leave `VK_NULL_HANDLE` to allocate from default pool. If not null, members:
    /// `usage`, `requiredFlags`, `preferredFlags`, `memoryTypeBits` are ignored.
    /// */
    public VmaPool Pool;
    /// /** \brief Custom general-purpose pointer that will be stored in #VmaAllocation, can be read as VmaAllocationInfo::pUserData and changed using vmaSetAllocationUserData().
    /// 
    /// If #VMA_ALLOCATION_CREATE_USER_DATA_COPY_STRING_BIT is used, it must be either
    /// null or pointer to a null-terminated string. The string will be then copied to
    /// internal buffer, so it doesn't need to be valid after allocation call.
    /// */
    public nint UserData;
    /// /** \brief A floating-point value between 0 and 1, indicating the priority of the allocation relative to other memory allocations.
    /// 
    /// It is used only when #VMA_ALLOCATOR_CREATE_EXT_MEMORY_PRIORITY_BIT flag was used during creation of the #VmaAllocator object
    /// and this allocation ends up as dedicated or is explicitly forced as dedicated using #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT.
    /// Otherwise, it has the priority of a memory block where it is placed and this variable is ignored.
    /// */
    public float Priority;
}

/// <summary>
/// Parameters of #VmaAllocation objects, that can be retrieved using function vmaGetAllocationInfo().
/// There is also an extended version of this structure that carries additional parameters: #VmaAllocationInfo2.
/// </summary>
struct VmaAllocationInfo
{
    /// /** \brief Memory type index that this allocation was allocated from.
    /// 
    /// It never changes.
    /// */
    public uint MemoryType;
    /// /** \brief Handle to Vulkan memory object.
    /// 
    /// Same memory object can be shared by multiple allocations.
    /// 
    /// It can change after the allocation is moved during \ref defragmentation.
    /// */
    public VkDeviceMemory DeviceMemory;
    /// /** \brief Offset in `VkDeviceMemory` object to the beginning of this allocation, in bytes. `(deviceMemory, offset)` pair is unique to this allocation.
    /// 
    /// You usually don't need to use this offset. If you create a buffer or an image together with the allocation using e.g. function
    /// vmaCreateBuffer(), vmaCreateImage(), functions that operate on these resources refer to the beginning of the buffer or image,
    /// not entire device memory block. Functions like vmaMapMemory(), vmaBindBufferMemory() also refer to the beginning of the allocation
    /// and apply this offset automatically.
    /// 
    /// It can change after the allocation is moved during \ref defragmentation.
    /// */
    public ulong Offset;
    /// <summary>
    /// Size of this allocation, in bytes. It never changes.
    /// 
    /// Allocation size returned in this variable may be greater than the size
    /// requested for the resource e.g. as `VkBufferCreateInfo::size`. Whole size of the
    /// allocation is accessible for operations on memory e.g. using a pointer after
    /// mapping with vmaMapMemory(), but operations on the resource e.g. using
    /// `vkCmdCopyBuffer` must be limited to the size of the resource.
    /// </summary>
    public ulong Size;
    /// /** \brief Pointer to the beginning of this allocation as mapped data.
    /// 
    /// If the allocation hasn't been mapped using vmaMapMemory() and hasn't been
    /// created with #VMA_ALLOCATION_CREATE_MAPPED_BIT flag, this value is null.
    /// 
    /// It can change after call to vmaMapMemory(), vmaUnmapMemory().
    /// It can also change after the allocation is moved during \ref defragmentation.
    /// */
    public nint MappedData;
    /// /** \brief Custom general-purpose pointer that was passed as VmaAllocationCreateInfo::pUserData or set using vmaSetAllocationUserData().
    /// 
    /// It can change after call to vmaSetAllocationUserData() for this allocation.
    /// */
    public nint UserData;
    /// /** \brief Custom allocation name that was set with vmaSetAllocationName().
    /// 
    /// It can change after call to vmaSetAllocationName() for this allocation.
    /// 
    /// Another way to set custom name is to pass it in VmaAllocationCreateInfo::pUserData with
    /// additional flag #VMA_ALLOCATION_CREATE_USER_DATA_COPY_STRING_BIT set [DEPRECATED].
    /// */
    public nint Name;
}

[SuppressUnmanagedCodeSecurity]
static unsafe class Api
{
    const string VmaLibrary = "Gossamer.Vma.dll";
    const CallingConvention CallConvention = CallingConvention.Cdecl;

    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaAllocation t) => t.Value != default;
    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaAllocator t) => t.Value != default;
    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaPool t) => t.Value != default;
    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaDefragmentationContext t) => t.Value != default;
    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaVirtualAllocation t) => t.Value != default;
    /// <summary>
    /// Checks if the given opaque pointer object has a value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> True if the opaque pointer object has a value, otherwise false. </returns>
    public static bool HasValue(VmaVirtualBlock t) => t.Value != default;

    /// <summary>
    /// Creates VmaAllocator object.
    /// </summary>
    /// <param name="pCreateInfo"></param>
    /// <param name="pAllocator"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaCreateAllocator(VmaAllocatorCreateInfo* pCreateInfo, VmaAllocator* pAllocator);

    /// <summary>
    /// Destroys allocator object.
    /// </summary>
    /// <param name="pAllocator"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaDestroyAllocator(VmaAllocator pAllocator);

    /// <summary>
    /// Retrieves information about current memory usage and budget for all memory heaps.
    /// This function is called "get" not "calculate" because it is very fast, suitable to be called
    /// every frame or every allocation. For more detailed statistics use vmaCalculateStatistics().
    /// </summary>
    /// <param name="pAllocator"></param>
    /// <param name="pBudgets">Must point to array with number of elements at least equal to number of memory heaps in physical device used.</param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaGetHeapBudgets(VmaAllocator pAllocator, VmaBudget* pBudgets);

    /// <summary>
    /// Given Memory Type Index, returns Property Flags of this memory type.
    /// </summary>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern void vmaGetMemoryTypeProperties(
        VmaAllocator allocator,
        uint memoryTypeIndex,
        VkMemoryProperty* pFlags);

    /// <summary>
    /// Creates a new `VkBuffer`, allocates and binds memory for it.
    /// <para/>
    /// This function automatically:<br/>
    /// 
    /// -# Creates buffer. <br/>
    /// -# Allocates appropriate memory for it.<br/>
    /// -# Binds the buffer with the memory.<br/>
    /// 
    /// If any of these operations fail, buffer and allocation are not created,
    /// returned value is negative error code, `*pBuffer` and `*pAllocation` are null.
    /// <para/>
    /// If the function succeeded, you must destroy both buffer and allocation when you
    /// no longer need them using either convenience function vmaDestroyBuffer() or
    /// separately, using `vkDestroyBuffer()` and vmaFreeMemory().
    /// <para/>
    /// If #VMA_ALLOCATOR_CREATE_KHR_DEDICATED_ALLOCATION_BIT flag was used,
    /// VK_KHR_dedicated_allocation extension is used internally to query driver whether
    /// it requires or prefers the new buffer to have dedicated allocation.If yes,
    /// and if dedicated allocation is possible
    /// (#VMA_ALLOCATION_CREATE_NEVER_ALLOCATE_BIT is not used), it creates dedicated
    /// allocation for this buffer, just like when using
    /// #VMA_ALLOCATION_CREATE_DEDICATED_MEMORY_BIT.
    /// <para/>
    /// This function creates a new `VkBuffer`. Sub-allocation of parts of one large buffer,
    /// although recommended as a good practice, is out of scope of this library and could be implemented
    /// by the user as a higher-level logic on top of VMA.
    /// </summary>
    /// <param name="pAllocator"></param>
    /// <param name="pBufferCreateInfo"></param>
    /// <param name="pAllocationCreateInfo"></param>
    /// <param name="pBuffer"></param>
    /// <param name="pAllocation"></param>
    /// <param name="pAllocationInfo"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaCreateBuffer(
        VmaAllocator pAllocator,
        VkBufferCreateInfo* pBufferCreateInfo,
        VmaAllocationCreateInfo* pAllocationCreateInfo,
        VkBuffer* pBuffer,
        VmaAllocation* pAllocation,
        VmaAllocationInfo* pAllocationInfo);

    /// <summary>
    /// Similar to vmaCreateBuffer() but for an image.
    /// </summary>
    /// <param name="pAllocator"></param>
    /// <param name="pImageCreateInfo"></param>
    /// <param name="pAllocationCreateInfo"></param>
    /// <param name="pImage"></param>
    /// <param name="pAllocation"></param>
    /// <param name="pAllocationInfo"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaCreateImage(
        VmaAllocator pAllocator,
        VkImageCreateInfo* pImageCreateInfo,
        VmaAllocationCreateInfo* pAllocationCreateInfo,
        VkImage* pImage,
        VmaAllocation* pAllocation,
        VmaAllocationInfo* pAllocationInfo);

    /// <summary>
    /// Destroys Vulkan buffer and frees allocated memory.
    /// <para/>
    /// This is just a convenience function equivalent to: <br/>
    /// vkDestroyBuffer(device, buffer, allocationCallbacks); <br/>
    /// vmaFreeMemory(allocator, allocation);
    /// <para/>
    /// It is safe to pass null as buffer and/or allocation.
    /// </summary>
    /// <param name="allocator"></param>
    /// <param name="buffer"></param>
    /// <param name="allocation"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaDestroyBuffer(
        VmaAllocator allocator,
        VkBuffer buffer,
        VmaAllocation allocation);

    /// <summary>
    /// Destroys Vulkan buffer and image allocated memory.
    /// <para/>
    /// This is just a convenience function equivalent to: <br/>
    /// vkDestroyImage(device, image, allocationCallbacks); <br/>
    /// vmaFreeMemory(allocator, allocation); 
    /// <para/>
    /// It is safe to pass null as buffer and/or allocation.
    /// </summary>
    /// <param name="allocator"></param>
    /// <param name="buffer"></param>
    /// <param name="allocation"></param>
    /// <returns></returns>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaDestroyImage(
        VmaAllocator allocator,
        VkImage image,
        VmaAllocation allocation);

    /// <summary>
    /// Maps memory represented by given allocation and returns pointer to it.
    /// Maps memory represented by given allocation to make it accessible to CPU code.
    /// When succeeded, `*ppData` contains pointer to first byte of this memory.
    /// 
    /// \warning
    /// If the allocation is part of a bigger `VkDeviceMemory` block, returned pointer is
    /// correctly offsetted to the beginning of region assigned to this particular allocation.
    /// Unlike the result of `vkMapMemory`, it points to the allocation, not to the beginning of the whole block.
    /// You should not add VmaAllocationInfo::offset to it!
    /// 
    /// Mapping is internally reference-counted and synchronized, so despite raw Vulkan
    /// function `vkMapMemory()` cannot be used to map same block of `VkDeviceMemory`
    /// multiple times simultaneously, it is safe to call this function on allocations
    /// assigned to the same memory block. Actual Vulkan memory will be mapped on first
    /// mapping and unmapped on last unmapping.
    /// 
    /// If the function succeeded, you must call vmaUnmapMemory() to unmap the
    /// allocation when mapping is no longer needed or before freeing the allocation, at
    /// the latest.
    /// 
    /// It also safe to call this function multiple times on the same allocation. You
    /// must call vmaUnmapMemory() same number of times as you called vmaMapMemory().
    /// 
    /// It is also safe to call this function on allocation created with
    /// #VMA_ALLOCATION_CREATE_MAPPED_BIT flag. Its memory stays mapped all the time.
    /// You must still call vmaUnmapMemory() same number of times as you called
    /// vmaMapMemory(). You must not call vmaUnmapMemory() additional time to free the
    /// "0-th" mapping made automatically due to #VMA_ALLOCATION_CREATE_MAPPED_BIT flag.
    /// 
    /// This function fails when used on allocation made in memory type that is not
    /// `HOST_VISIBLE`.
    /// 
    /// This function doesn't automatically flush or invalidate caches.
    /// If the allocation is made from a memory types that is not `HOST_COHERENT`,
    /// you also need to use vmaInvalidateAllocation() / vmaFlushAllocation(), as required by Vulkan specification.
    /// </summary>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaMapMemory(
        VmaAllocator allocator,
        VmaAllocation allocation,
        void** ppData);

    /// <summary>
    /// Unmaps memory represented by given allocation, mapped previously using vmaMapMemory().
    /// For details, see description of vmaMapMemory().
    /// This function doesn't automatically flush or invalidate caches.
    /// If the allocation is made from a memory types that is not `HOST_COHERENT`,
    /// you also need to use vmaInvalidateAllocation() / vmaFlushAllocation(), as required by Vulkan specification.
    /// </summary>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern void vmaUnmapMemory(
        VmaAllocator allocator,
        VmaAllocation allocation);

    /// <summary>
    /// Flushes memory of given allocation.
    /// Calls `vkFlushMappedMemoryRanges()` for memory associated with given range of given allocation.
    /// It needs to be called after writing to a mapped memory for memory types that are not `HOST_COHERENT`.
    /// Unmap operation doesn't do that automatically.
    /// - `offset` must be relative to the beginning of allocation.
    /// - `size` can be `VK_WHOLE_SIZE`. It means all memory from `offset` the the end of given allocation.
    /// - `offset` and `size` don't have to be aligned.
    /// They are internally rounded down/up to multiply of `nonCoherentAtomSize`.
    /// - If `size` is 0, this call is ignored.
    /// - If memory type that the `allocation` belongs to is not `HOST_VISIBLE` or it is `HOST_COHERENT`,
    /// this call is ignored.
    /// Warning! `offset` and `size` are relative to the contents of given `allocation`.
    /// If you mean whole allocation, you can pass 0 and `VK_WHOLE_SIZE`, respectively.
    /// Do not pass allocation's offset as `offset`!!!
    /// This function returns the `VkResult` from `vkFlushMappedMemoryRanges` if it is
    /// called, otherwise `VK_SUCCESS`.
    /// </summary>
    [DllImport(VmaLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vmaFlushAllocation(
        VmaAllocator allocator,
        VmaAllocation allocation,
        ulong offset,
        ulong size);
}