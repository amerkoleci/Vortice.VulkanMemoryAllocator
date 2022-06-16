// Copyright © Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using NUnit.Framework;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static Vortice.Vulkan.Vma;
using System.Numerics;

namespace Vortice.VulkanMemoryAllocator.Test;

[TestFixture(TestOf = typeof(Vma))]
public unsafe class Tests
{
    private static readonly string AssetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
    private VkInstance _instance;
    private VkPhysicalDevice _physicalDevice;
    private VkDevice _device;

    [SetUp]
    public void Setup()
    {
        vkInitialize().CheckResult();

        var instanceCreateInfo = new VkInstanceCreateInfo
        {
            sType = VkStructureType.InstanceCreateInfo,
        };
        vkCreateInstance(&instanceCreateInfo, null, out _instance).CheckResult();
        vkLoadInstanceOnly(_instance);

        // Find physical device, setup queue's and create device.
        int physicalDevicesCount = 0;
        vkEnumeratePhysicalDevices(_instance, &physicalDevicesCount, null).CheckResult();

        if (physicalDevicesCount == 0)
        {
            throw new Exception("Vulkan: Failed to find GPUs with Vulkan support");
        }

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[physicalDevicesCount];
        vkEnumeratePhysicalDevices(_instance, &physicalDevicesCount, physicalDevices).CheckResult();
        _physicalDevice = physicalDevices[0];

        VkDeviceCreateInfo deviceCreateInfo = new()
        {
            sType = VkStructureType.DeviceCreateInfo,
        };

        vkCreateDevice(_physicalDevice, &deviceCreateInfo, null, out _device).CheckResult();
        vkLoadDevice(_device);
    }

    [TearDown]
    public void TearDown()
    {
        vkDestroyDevice(_device);
        vkDestroyInstance(_instance);
    }

    [TestCase]
    public void TestCreateAllocator()
    {
        VmaAllocatorCreateInfo allocatorInfo = new()
        {
            PhysicalDevice = _physicalDevice,
            Device = _device,
            Instance = _instance,
            //VulkanApiVersion = VkVersion.Version_1_1
        };
        vmaCreateAllocator(&allocatorInfo, out VmaAllocator allocator).CheckResult();

        vkDeviceWaitIdle(_device);
        vmaDestroyAllocator(allocator);
    }

    [TestCase]
    public void TestVertexBuffer()
    {
        VmaAllocatorCreateInfo allocatorInfo = new()
        {
            PhysicalDevice = _physicalDevice,
            Device = _device,
            Instance = _instance,
            //VulkanApiVersion = VkVersion.Version_1_1
        };
        vmaCreateAllocator(&allocatorInfo, out VmaAllocator allocator).CheckResult();

        ReadOnlySpan<VertexPositionColor> sourceData = new VertexPositionColor[]
        {
            new VertexPositionColor(new Vector3(0f, 0.5f, 0.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f))
        };
        uint vertexBufferSize = (uint)(sourceData.Length * VertexPositionColor.SizeInBytes);

        VkBufferCreateInfo vertexBufferInfo = new()
        {
            sType = VkStructureType.BufferCreateInfo,
            size = vertexBufferSize,
            // Buffer is used as the copy source
            usage = VkBufferUsageFlags.TransferSrc
        };

        // Create a host-visible buffer to copy the vertex data to (staging buffer)
        VmaAllocationCreateInfo memoryInfo = new()
        {
            flags = VmaAllocationCreateFlags.HostAccessSequentialWrite | VmaAllocationCreateFlags.Mapped,
            usage = VmaMemoryUsage.Auto
        };

        VmaAllocationInfo allocationInfo;
        vmaCreateBuffer(allocator, &vertexBufferInfo,
            &memoryInfo,
            out VkBuffer stagingBuffer,
            out VmaAllocation stagingBufferAllocation,
            &allocationInfo).CheckResult();

        // Map and copy
        void* pMappedData = allocationInfo.pMappedData;
        Span<VertexPositionColor> destinationData = new(pMappedData, sourceData.Length);
        sourceData.CopyTo(destinationData);

        vkDeviceWaitIdle(_device);
        vmaDestroyBuffer(allocator, stagingBuffer, stagingBufferAllocation);
        vmaDestroyAllocator(allocator);
    }
}
