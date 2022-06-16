// Copyright © Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using NUnit.Framework;
using Vortice.Vulkan;

namespace Vortice.SpirvCross.Test;

[TestFixture(TestOf=typeof(Vma))]
public class Tests
{
    private static readonly string AssetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");

}
